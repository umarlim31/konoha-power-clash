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
        private float nextReadabilityRefresh;

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

            if (IsOwner && GetComponent<NetworkBotController>() == null)
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
            if (!IsOwner || GetComponent<NetworkBotController>() != null)
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

            if (Time.unscaledTime >= nextReadabilityRefresh)
            {
                nextReadabilityRefresh = Time.unscaledTime + 0.20f;
                RefreshHealthVisibility();
            }

            if (cachedCamera == null || !healthLabel.gameObject.activeSelf)
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
                if (!NetworkTeamUtility.IsCombatActor(networkObject) ||
                    networkObject == NetworkObject)
                    continue;

                if (NetworkTeamUtility.GetTeam(networkObject) == attackerTeam)
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

            bestTarget.ServerReceiveDamage(damage, OwnerClientId, NetworkObjectId);

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

        public void ServerReceiveDamage(
            int damage,
            ulong sourceClientId,
            ulong sourceActorNetworkObjectId = NetworkMatchManager.NoClient)
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

            DamageFeedbackClientRpc(adjustedDamage, absorbed, transform.position);
            attackerKit?.ServerGainPengaruh(Mathf.Clamp(damage / 2, 4, 15));

            if (health.Value == 0 && !serverRespawnRunning)
            {
                NetworkMatchManager.Instance?.HandleActorKnockedOut(NetworkObject);

                NetworkObject attacker = FindActorByNetworkObjectId(sourceActorNetworkObjectId);
                if (attacker == null && sourceClientId != NetworkMatchManager.NoClient)
                    attacker = FindPlayerObjectByOwner(sourceClientId);

                NetworkMatchEvents.Instance?.ServerReportKnockout(attacker, NetworkObject);
                respawnRoutine = StartCoroutine(ServerKnockoutAndRespawn());
            }
        }

        private NetworkObject FindActorByNetworkObjectId(ulong networkObjectId)
        {
            if (networkObjectId == NetworkMatchManager.NoClient ||
                NetworkManager == null ||
                NetworkManager.SpawnManager == null)
                return null;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject != null && networkObject.NetworkObjectId == networkObjectId)
                    return networkObject;
            }

            return null;
        }

        private NetworkObject FindPlayerObjectByOwner(ulong clientId)
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
                return null;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject != null &&
                    networkObject.IsPlayerObject &&
                    networkObject.OwnerClientId == clientId)
                    return networkObject;
            }

            return null;
        }

        private NetworkHeroKit FindHeroKitByOwner(ulong clientId)
        {
            if (clientId == NetworkMatchManager.NoClient ||
                NetworkManager == null ||
                NetworkManager.SpawnManager == null)
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

            transform.position = NetworkTeamUtility.GetSpawnPosition(NetworkObject);
            transform.rotation = NetworkTeamUtility.GetSpawnRotation(NetworkObject);

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

            NetworkBotController bot = GetComponent<NetworkBotController>();
            bool localHuman = bot == null && IsOwner;

            if (knockedOut.Value)
            {
                healthLabel.text = localHuman ? "WIBAWA RUNTUH\nRESPAWN..." : "RUNTUH";
                healthLabel.color = new Color(1f, 0.30f, 0.27f);
                RefreshHealthVisibility();
                return;
            }

            int current = health.Value;
            string shieldText = shield.Value > 0 ? " +" + shield.Value : "";

            if (localHuman)
                healthLabel.text = "WIBAWA " + current + "/" + MaxWibawa + shieldText;
            else if (bot != null)
                healthLabel.text = current + shieldText;
            else
                healthLabel.text = current + "/" + MaxWibawa + shieldText;

            healthLabel.color = current > 50
                ? new Color(0.55f, 1f, 0.55f)
                : current > 20
                    ? new Color(1f, 0.82f, 0.25f)
                    : new Color(1f, 0.35f, 0.30f);

            RefreshHealthVisibility();
        }

        private void RefreshHealthVisibility()
        {
            if (healthLabel == null || !IsSpawned)
                return;

            NetworkBotController bot = GetComponent<NetworkBotController>();
            bool localHuman = bot == null && IsOwner;
            NetworkMatchManager match = NetworkMatchManager.Instance;
            bool ruler = match != null && match.IsRuler(NetworkObject);

            float distance = GetDistanceFromLocalPlayer();
            bool visible = localHuman ||
                           knockedOut.Value ||
                           ruler ||
                           distance <= (bot != null ? 10.5f : 12.5f);

            if (healthLabel.gameObject.activeSelf != visible)
                healthLabel.gameObject.SetActive(visible);

            if (!visible)
                return;

            float scale = localHuman ? 1f : Mathf.Lerp(0.92f, 0.72f, Mathf.InverseLerp(4f, 12.5f, distance));
            healthLabel.transform.localScale = Vector3.one * scale;
        }

        private float GetDistanceFromLocalPlayer()
        {
            if (NetworkManager != null &&
                NetworkManager.LocalClient != null &&
                NetworkManager.LocalClient.PlayerObject != null)
            {
                Vector3 a = NetworkManager.LocalClient.PlayerObject.transform.position;
                Vector3 b = transform.position;
                a.y = 0f;
                b.y = 0f;
                return Vector3.Distance(a, b);
            }

            if (cachedCamera == null)
                cachedCamera = Camera.main;

            return cachedCamera != null
                ? Vector3.Distance(cachedCamera.transform.position, transform.position)
                : 0f;
        }

        [ClientRpc]
        private void DamageFeedbackClientRpc(int damage, int absorbed, Vector3 worldPosition)
        {
            if (damage <= 0 && absorbed <= 0)
                return;

            if (GetDistanceFromLocalPlayer() > 14f && !IsOwner)
                return;

            StartCoroutine(FloatingDamageRoutine(damage, absorbed, worldPosition));
        }

        private IEnumerator FloatingDamageRoutine(int damage, int absorbed, Vector3 worldPosition)
        {
            var root = new GameObject("DamageNumber");
            root.transform.position = worldPosition + Vector3.up * 2.55f;

            var text = root.AddComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 42;
            text.characterSize = 0.045f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.text = absorbed > 0 && damage <= 0
                ? "BLOCK " + absorbed
                : absorbed > 0
                    ? "-" + damage + "  BLOCK " + absorbed
                    : "-" + damage;
            text.color = damage > 0
                ? new Color(1f, 0.36f, 0.28f)
                : new Color(0.35f, 0.85f, 1f);

            MeshRenderer renderer = root.GetComponent<MeshRenderer>();
            if (text.font != null && renderer != null)
                renderer.sharedMaterial = text.font.material;

            float duration = 0.58f;
            float elapsed = 0f;

            while (elapsed < duration && root != null)
            {
                elapsed += Time.unscaledDeltaTime;
                root.transform.position += Vector3.up * (0.75f * Time.unscaledDeltaTime);

                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 direction = root.transform.position - cam.transform.position;
                    if (direction.sqrMagnitude > 0.001f)
                        root.transform.rotation = Quaternion.LookRotation(direction);
                }

                yield return null;
            }

            if (root != null)
                Destroy(root);
        }
    }
}
