using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Networking
{
    public sealed class NetworkPlayerCombat : NetworkBehaviour
    {
        public const int MaxHealth = 100;
        public const int BasicAttackDamage = 20;

        public float attackRange = 2.7f;
        public float attackCooldown = 0.65f;
        public TextMesh healthLabel;

        private NetworkVariable<int> health = new NetworkVariable<int>(
            MaxHealth,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private Button attackButton;
        private float localNextAttackTime;
        private double serverNextAttackTime;
        private Camera cachedCamera;

        public int Health => health.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            health.OnValueChanged += OnHealthChanged;
            RefreshHealthLabel(health.Value);

            cachedCamera = Camera.main;

            if (IsOwner)
                BindAttackButton();
        }

        public override void OnNetworkDespawn()
        {
            health.OnValueChanged -= OnHealthChanged;

            if (attackButton != null)
                attackButton.onClick.RemoveListener(TryBasicAttack);

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (IsOwner && attackButton == null)
                BindAttackButton();
        }

        private void LateUpdate()
        {
            if (!IsSpawned || healthLabel == null)
                return;

            if (cachedCamera == null)
                cachedCamera = Camera.main;

            if (cachedCamera == null)
                return;

            Vector3 direction = healthLabel.transform.position - cachedCamera.transform.position;
            if (direction.sqrMagnitude > 0.001f)
                healthLabel.transform.rotation = Quaternion.LookRotation(direction);
        }

        private void BindAttackButton()
        {
            GameObject buttonObject = GameObject.Find("AttackButton");
            if (buttonObject == null)
                return;

            attackButton = buttonObject.GetComponent<Button>();
            if (attackButton == null)
                return;

            attackButton.onClick.RemoveListener(TryBasicAttack);
            attackButton.onClick.AddListener(TryBasicAttack);
        }

        public void TryBasicAttack()
        {
            if (!IsOwner || !IsSpawned || Time.unscaledTime < localNextAttackTime)
                return;

            localNextAttackTime = Time.unscaledTime + attackCooldown;
            BasicAttackServerRpc();
        }

        [ServerRpc]
        private void BasicAttackServerRpc()
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
                return;

            double now = Time.realtimeSinceStartupAsDouble;
            if (now < serverNextAttackTime)
                return;

            serverNextAttackTime = now + attackCooldown;

            NetworkPlayerCombat bestTarget = null;
            float bestDistanceSqr = attackRange * attackRange;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject == null || networkObject == NetworkObject)
                    continue;

                NetworkPlayerCombat candidate = networkObject.GetComponent<NetworkPlayerCombat>();
                if (candidate == null || !candidate.IsSpawned || candidate.health.Value <= 0)
                    continue;

                Vector3 delta = candidate.transform.position - transform.position;
                delta.y = 0f;
                float distanceSqr = delta.sqrMagnitude;

                if (distanceSqr > bestDistanceSqr || distanceSqr < 0.0001f)
                    continue;

                Vector3 direction = delta.normalized;
                float facing = Vector3.Dot(transform.forward, direction);
                if (facing < -0.15f)
                    continue;

                bestTarget = candidate;
                bestDistanceSqr = distanceSqr;
            }

            if (bestTarget == null)
            {
                Debug.Log("[KONOHA COMBAT] Attack missed | attacker=" + OwnerClientId);
                return;
            }

            bestTarget.ApplyServerDamage(BasicAttackDamage);
            Debug.Log(
                "[KONOHA COMBAT] Hit | attacker=" + OwnerClientId +
                " | target=" + bestTarget.OwnerClientId +
                " | hp=" + bestTarget.health.Value);
        }

        private void ApplyServerDamage(int damage)
        {
            if (!IsServer)
                return;

            health.Value = Mathf.Clamp(health.Value - Mathf.Max(0, damage), 0, MaxHealth);
        }

        private void OnHealthChanged(int previous, int current)
        {
            RefreshHealthLabel(current);
        }

        private void RefreshHealthLabel(int current)
        {
            if (healthLabel == null)
                return;

            healthLabel.text = "HP " + current + "/" + MaxHealth;
            healthLabel.color = current > 50
                ? new Color(0.55f, 1f, 0.55f)
                : current > 20
                    ? new Color(1f, 0.82f, 0.25f)
                    : new Color(1f, 0.35f, 0.30f);
        }
    }
}
