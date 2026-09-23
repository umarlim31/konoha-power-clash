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

        private NetworkVariable<int> shield = new NetworkVariable<int>(
            0,
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
        private NetworkHeroKit heroKit;

        public int Wibawa => health.Value;
        public int Shield => shield.Value;
        public bool IsKnockedOut => knockedOut.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            identity = GetComponent<NetworkPlayerIdentity>();
            movement = GetComponent<NetworkPlayerMovement>();
            heroKit = GetComponent<NetworkHeroKit>();
            cachedCamera = Camera.main;

            health.OnValueChanged += OnHealthChanged;
            shield.OnValueChanged += OnShieldChanged;
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
            shield.OnValueChanged -= OnShieldChanged;
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
            heroKit ??= GetComponent<NetworkHeroKit>();

            if (!IsOwner ||
                !IsSpawned ||
                knockedOut.Value ||
                match == null ||
                !match.AllowsGameplay ||
                match.IsRuler(OwnerClientId) ||
                (heroKit != null && heroKit.IsStunned))
                return;

            float cooldown = heroKit != null ? heroKit.GetBasicCooldown() : attackCooldown;

            if (Time.unscaledTime < localNextAttackTime)
                return;

            localNextAttackTime = Time.unscaledTime + cooldown;
            BasicAttackServerRpc();
        }

        [ServerRpc]
        private void BasicAttackServerRpc()
        {
            NetworkMatchManager match = NetworkMatchManager.Instance;
            heroKit ??= GetComponent<NetworkHeroKit>();

            if (knockedOut.Value ||
                match == null ||
                !match.AllowsGameplay ||
                match.IsRuler(OwnerClientId) ||
                (heroKit != null && heroKit.IsStunned) ||
                NetworkManager == null ||
                NetworkManager.SpawnManager == null)
                return;

            float cooldown = heroKit != null ? heroKit.GetBasicCooldown() : attackCooldown;
            double now = Time.realtimeSinceStartupAsDouble;

            if (now < serverNextAttackTime)
                return;

            serverNextAttackTime = now + cooldown;

            int attackerTeam = NetworkTeamUtility.GetTeam(OwnerClientId);
            float range = heroKit != null ? heroKit.GetBasicRange() : attackRange;
            int damage = heroKit != null ? heroKit.GetBasicDamage() : BasicAttackDamage;

            NetworkPlayerCombat bestTarget = null;
            float bestDistanceSqr = range * range;

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

            bestTarget.ServerReceiveDamage(damage, OwnerClientId);

            NetworkHeroKit targetKit = bestTarget.GetComponent<NetworkHeroKit>();
            heroKit?.ServerOnBasicHit(targetKit, damage);

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
            shield.Value = 0;
            knockedOut.Value = false;
            heroKit ??= GetComponent<NetworkHeroKit>();
            heroKit?.ServerResetForMatch();
            respawnTicket.Value += 1;
        }

        public void ServerGrantShield(int amount)
        {
            if (!IsServer || amount <= 0 || knockedOut.Value)
                return;

            shield.Value = Mathf.Clamp(shield.Value + amount, 0, 50);
        }

        public void ServerReceiveDamage(int damage, ulong sourceClientId)
        {
            if (!IsServer || knockedOut.Value || damage <= 0)
                return;

            heroKit ??= GetComponent<NetworkHeroKit>();

            NetworkHeroKit attackerKit = FindHeroKitByOwner(sourceClientId);
            float outgoingMultiplier = attackerKit != null ? attackerKit.GetOutgoingDamageMultiplier() : 1f;
            float incomingMultiplier = heroKit != null ? heroKit.GetIncomingDamageMultiplier() : 1f;
            int adjustedDamage = Mathf.Max(
                1,
                Mathf.RoundToInt(damage * outgoingMultiplier * incomingMultiplier));

            int absorbed = Mathf.Min(shield.Value, adjustedDamage);
            if (absorbed > 0)
            {
                shield.Value -= absorbed;
                adjustedDamage -= absorbed;
            }

            if (adjustedDamage > 0)
                health.Value = Mathf.Clamp(health.Value - adjustedDamage, 0, MaxWibawa);

            attackerKit?.ServerGainPengaruh(Mathf.Clamp(damage / 2, 4, 15));

            if (health.Value == 0 && !serverRespawnRunning)
            {
                NetworkMatchManager.Instance?.HandlePlayerKnockedOut(OwnerClientId);
                respawnRoutine = StartCoroutine(ServerKnockoutAndRespawn());
            }
        }

        private NetworkHeroKit FindHeroKitByOwner(ulong clientId)
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
                return null;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject == null ||
                    !networkObject.IsPlayerObject ||
                    networkObject.OwnerClientId != clientId)
                    continue;

                return networkObject.GetComponent<NetworkHeroKit>();
            }

            return null;
        }

        private IEnumerator ServerKnockoutAndRespawn()
        {
            serverRespawnRunning = true;
            knockedOut.Value = true;
            heroKit ??= GetComponent<NetworkHeroKit>();
            heroKit?.ServerOnKnockedOut();

            Debug.Log("[KONOHA COMBAT] WIBAWA RUNTUH | player=" + OwnerClientId);

            yield return new WaitForSecondsRealtime(respawnDelay);

            respawnTicket.Value += 1;
            health.Value = MaxWibawa;
            shield.Value = 0;
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

        private void OnShieldChanged(int previous, int current)
        {
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

            heroKit ??= GetComponent<NetworkHeroKit>();
            NetworkMatchManager match = NetworkMatchManager.Instance;

            bool gameplayLocked =
                knockedOut.Value ||
                match == null ||
                !match.AllowsGameplay ||
                match.IsRuler(OwnerClientId) ||
                (heroKit != null && heroKit.IsStunned);

            if (gameplayLocked)
            {
                attackButton.interactable = false;

                if (attackButtonLabel != null)
                {
                    if (knockedOut.Value)
                        attackButtonLabel.text = "BASIC\nRUNTUH";
                    else if (match != null && match.IsRuler(OwnerClientId))
                        attackButtonLabel.text = "BASIC\nPENGUASA";
                    else if (heroKit != null && heroKit.IsStunned)
                        attackButtonLabel.text = "BASIC\nSTUN";
                    else
                        attackButtonLabel.text = "BASIC";
                }

                return;
            }

            float remaining = Mathf.Max(0f, localNextAttackTime - Time.unscaledTime);
            attackButton.interactable = remaining <= 0f;

            if (attackButtonLabel != null)
                attackButtonLabel.text = remaining > 0f
                    ? "BASIC\n" + remaining.ToString("0.0")
                    : "BASIC";
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
            string shieldText = shield.Value > 0 ? " +" + shield.Value + " SHIELD" : "";
            healthLabel.text = "WIBAWA " + current + "/" + MaxWibawa + shieldText;
            healthLabel.color = current > 50
                ? new Color(0.55f, 1f, 0.55f)
                : current > 20
                    ? new Color(1f, 0.82f, 0.25f)
                    : new Color(1f, 0.35f, 0.30f);
        }
    }
}
