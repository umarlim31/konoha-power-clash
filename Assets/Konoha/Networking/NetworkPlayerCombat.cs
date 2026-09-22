using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Networking
{
    public sealed class NetworkPlayerCombat : NetworkBehaviour
    {
        public const int MaxWibawa = 100;
        public const int BasicAttackDamage = 20;

        public float attackRange = 2.7f;
        public float attackCooldown = 0.65f;
        public float respawnDelay = 6f;
        public TextMesh healthLabel;

        private NetworkVariable<int> health = new NetworkVariable<int>(
            MaxWibawa,
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
        private Coroutine respawnRoutine;
        private Camera cachedCamera;
        private NetworkPlayerIdentity identity;
        private NetworkPlayerMovement movement;

        public int Wibawa => health.Value;
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

            if (respawnRoutine != null)
                StopCoroutine(respawnRoutine);

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
            NetworkMatchManager match = NetworkMatchManager.Instance;

            if (!IsOwner ||
                !IsSpawned ||
                knockedOut.Value ||
                match == null ||
                !match.AllowsGameplay ||
                match.IsRuler(OwnerClientId))
                return;

            if (Time.unscaledTime < localNextAttackTime)
                return;

            localNextAttackTime = Time.unscaledTime + attackCooldown;
            BasicAttackServerRpc();
        }

        [ServerRpc]
        private void BasicAttackServerRpc()
        {
            NetworkMatchManager match = NetworkMatchManager.Instance;

            if (knockedOut.Value ||
                match == null ||
                !match.AllowsGameplay ||
                match.IsRuler(OwnerClientId) ||
                NetworkManager == null ||
                NetworkManager.SpawnManager == null)
                return;

            double now = Time.realtimeSinceStartupAsDouble;
            if (now < serverNextAttackTime)
                return;

            serverNextAttackTime = now + attackCooldown;

            int attackerTeam = NetworkTeamUtility.GetTeam(OwnerClientId);
            NetworkPlayerCombat bestTarget = null;
            float bestDistanceSqr = attackRange * attackRange;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject == null ||
                    networkObject == NetworkObject ||
                    !networkObject.IsPlayerObject)
                    continue;

                if (NetworkTeamUtility.GetTeam(networkObject.OwnerClientId) == attackerTeam)
                    continue;

                NetworkPlayerCombat candidate = networkObject.GetComponent<NetworkPlayerCombat>();
                if (candidate == null ||
                    !candidate.IsSpawned ||
                    candidate.knockedOut.Value ||
                    candidate.health.Value <= 0)
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
                " | wibawa=" + bestTarget.health.Value);
        }

        public void ServerResetForMatch()
        {
            if (!IsServer)
                return;

            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
                respawnRoutine = null;
            }

            serverRespawnRunning = false;
            health.Value = MaxWibawa;
            knockedOut.Value = false;
            respawnTicket.Value += 1;
        }

        private void ApplyServerDamage(int damage)
        {
            if (!IsServer || knockedOut.Value)
                return;

            int nextHealth = Mathf.Clamp(health.Value - Mathf.Max(0, damage), 0, MaxWibawa);
            health.Value = nextHealth;

            if (nextHealth == 0 && !serverRespawnRunning)
            {
                NetworkMatchManager.Instance?.HandlePlayerKnockedOut(OwnerClientId);
                respawnRoutine = StartCoroutine(ServerKnockoutAndRespawn());
            }
        }

        private IEnumerator ServerKnockoutAndRespawn()
        {
            serverRespawnRunning = true;
            knockedOut.Value = true;

            Debug.Log("[KONOHA COMBAT] WIBAWA RUNTUH | player=" + OwnerClientId);

            yield return new WaitForSecondsRealtime(respawnDelay);

            respawnTicket.Value += 1;
            health.Value = MaxWibawa;
            knockedOut.Value = false;
            serverRespawnRunning = false;
            respawnRoutine = null;

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

            transform.position = NetworkTeamUtility.GetSpawnPosition(OwnerClientId);
            transform.rotation = Quaternion.identity;

            if (controllerWasEnabled)
                controller.enabled = true;

            movement?.ResetLocalInput();

            Debug.Log(
                "[KONOHA COMBAT] Owner respawn teleport | player=" + OwnerClientId +
                " | position=" + transform.position);
        }

        private void UpdateAttackButtonVisual()
        {
            if (!IsOwner || attackButton == null)
                return;

            NetworkMatchManager match = NetworkMatchManager.Instance;
            bool gameplayLocked =
                knockedOut.Value ||
                match == null ||
                !match.AllowsGameplay ||
                match.IsRuler(OwnerClientId);

            if (gameplayLocked)
            {
                attackButton.interactable = false;

                if (attackButtonLabel != null)
                {
                    if (knockedOut.Value)
                        attackButtonLabel.text = "ATTACK\nRUNTUH";
                    else if (match != null && match.IsRuler(OwnerClientId))
                        attackButtonLabel.text = "ATTACK\nPENGUASA";
                    else
                        attackButtonLabel.text = "ATTACK";
                }

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
                healthLabel.text = "WIBAWA RUNTUH\nRESPAWN...";
                healthLabel.color = new Color(1f, 0.30f, 0.27f);
                return;
            }

            int current = health.Value;
            healthLabel.text = "WIBAWA " + current + "/" + MaxWibawa;
            healthLabel.color = current > 50
                ? new Color(0.55f, 1f, 0.55f)
                : current > 20
                    ? new Color(1f, 0.82f, 0.25f)
                    : new Color(1f, 0.35f, 0.30f);
        }
    }
}
