using System.Collections;
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
        public float respawnDelay = 3f;
        public TextMesh healthLabel;

        private NetworkVariable<int> health = new NetworkVariable<int>(
            MaxHealth,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<bool> knockedOut = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<int> respawnTicket = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private Button attackButton;
        private Text attackButtonLabel;
        private float localNextAttackTime;
        private double serverNextAttackTime;
        private bool serverRespawnRunning;
        private Camera cachedCamera;
        private NetworkPlayerIdentity identity;
        private NetworkPlayerMovement movement;

        public int Health => health.Value;
        public bool IsKnockedOut => knockedOut.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            identity = GetComponent<NetworkPlayerIdentity>();
            movement = GetComponent<NetworkPlayerMovement>();
            cachedCamera = Camera.main;

            health.OnValueChanged += OnHealthChanged;
            knockedOut.OnValueChanged += OnKnockedOutChanged;
            respawnTicket.OnValueChanged += OnRespawnTicketChanged;

            RefreshHealthLabel();
            identity?.SetKnockedOutVisual(knockedOut.Value);

            if (IsOwner)
                BindAttackButton();
        }

        public override void OnNetworkDespawn()
        {
            health.OnValueChanged -= OnHealthChanged;
            knockedOut.OnValueChanged -= OnKnockedOutChanged;
            respawnTicket.OnValueChanged -= OnRespawnTicketChanged;

            if (attackButton != null)
                attackButton.onClick.RemoveListener(TryBasicAttack);

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsOwner)
                return;

            if (attackButton == null)
                BindAttackButton();

            UpdateAttackButtonVisual();
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

            attackButtonLabel = attackButton.GetComponentInChildren<Text>();
            attackButton.onClick.RemoveListener(TryBasicAttack);
            attackButton.onClick.AddListener(TryBasicAttack);
        }

        public void TryBasicAttack()
        {
            if (!IsOwner || !IsSpawned || knockedOut.Value)
                return;

            if (Time.unscaledTime < localNextAttackTime)
                return;

            localNextAttackTime = Time.unscaledTime + attackCooldown;
            BasicAttackServerRpc();
        }

        [ServerRpc]
        private void BasicAttackServerRpc()
        {
            if (knockedOut.Value || NetworkManager == null || NetworkManager.SpawnManager == null)
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
                if (candidate == null || !candidate.IsSpawned || candidate.knockedOut.Value || candidate.health.Value <= 0)
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
            if (!IsServer || knockedOut.Value)
                return;

            int nextHealth = Mathf.Clamp(health.Value - Mathf.Max(0, damage), 0, MaxHealth);
            health.Value = nextHealth;

            if (nextHealth == 0 && !serverRespawnRunning)
                StartCoroutine(ServerKnockoutAndRespawn());
        }

        private IEnumerator ServerKnockoutAndRespawn()
        {
            serverRespawnRunning = true;
            knockedOut.Value = true;

            Debug.Log("[KONOHA COMBAT] KO | player=" + OwnerClientId);

            yield return new WaitForSecondsRealtime(respawnDelay);

            respawnTicket.Value += 1;
            health.Value = MaxHealth;
            knockedOut.Value = false;
            serverRespawnRunning = false;

            Debug.Log("[KONOHA COMBAT] RESPAWN | player=" + OwnerClientId);
        }

        private void OnHealthChanged(int previous, int current)
        {
            if (current < previous)
                identity?.PlayDamageFeedback();

            RefreshHealthLabel();
        }

        private void OnKnockedOutChanged(bool previous, bool current)
        {
            identity?.SetKnockedOutVisual(current);

            if (current && IsOwner)
                movement?.ResetLocalInput();

            RefreshHealthLabel();
            UpdateAttackButtonVisual();
        }

        private void OnRespawnTicketChanged(int previous, int current)
        {
            if (!IsOwner || current <= previous)
                return;

            TeleportOwnerToSpawn();
        }

        private void TeleportOwnerToSpawn()
        {
            CharacterController controller = GetComponent<CharacterController>();
            bool controllerWasEnabled = controller != null && controller.enabled;

            if (controllerWasEnabled)
                controller.enabled = false;

            transform.position = GetSpawnPosition(OwnerClientId);
            transform.rotation = Quaternion.identity;

            if (controllerWasEnabled)
                controller.enabled = true;

            movement?.ResetLocalInput();

            Debug.Log(
                "[KONOHA COMBAT] Owner respawn teleport | player=" + OwnerClientId +
                " | position=" + transform.position);
        }

        private static Vector3 GetSpawnPosition(ulong clientId)
        {
            if (clientId == 0)
                return new Vector3(-3f, 0.1f, -3f);

            if (clientId == 1)
                return new Vector3(3f, 0.1f, -3f);

            int slot = (int)(clientId % 6);
            float x = -6f + slot * 2.4f;
            float z = clientId % 2 == 0 ? 1f : 4f;
            return new Vector3(x, 0.1f, z);
        }

        private void UpdateAttackButtonVisual()
        {
            if (!IsOwner || attackButton == null)
                return;

            if (knockedOut.Value)
            {
                attackButton.interactable = false;
                if (attackButtonLabel != null)
                    attackButtonLabel.text = "ATTACK\nKO";
                return;
            }

            float remaining = Mathf.Max(0f, localNextAttackTime - Time.unscaledTime);
            attackButton.interactable = remaining <= 0f;

            if (attackButtonLabel != null)
                attackButtonLabel.text = remaining > 0f
                    ? "ATTACK\n" + remaining.ToString("0.0")
                    : "ATTACK";
        }

        private void RefreshHealthLabel()
        {
            if (healthLabel == null)
                return;

            if (knockedOut.Value)
            {
                healthLabel.text = "KO | RESPAWN...";
                healthLabel.color = new Color(1f, 0.30f, 0.27f);
                return;
            }

            int current = health.Value;
            healthLabel.text = "HP " + current + "/" + MaxHealth;
            healthLabel.color = current > 50
                ? new Color(0.55f, 1f, 0.55f)
                : current > 20
                    ? new Color(1f, 0.82f, 0.25f)
                    : new Color(1f, 0.35f, 0.30f);
        }
    }
}
