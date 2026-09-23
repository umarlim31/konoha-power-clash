using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Networking
{
    public enum PrototypeHero
    {
        Mega = 0,
        Prabowo = 1,
        Abah = 2,
        Jokowi = 3
    }

    public sealed class NetworkHeroKit : NetworkBehaviour
    {
        public const int MaxPengaruh = 100;

        private NetworkVariable<int> heroId = new NetworkVariable<int>(
            (int)PrototypeHero.Mega,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<int> pengaruh = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<int> rage = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<double> silencedUntil = new NetworkVariable<double>(
            0d,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<double> stunnedUntil = new NetworkVariable<double>(
            0d,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<double> garudaUntil = new NetworkVariable<double>(
            0d,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<Vector3> roadCenter = new NetworkVariable<Vector3>(
            Vector3.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<double> roadUntil = new NetworkVariable<double>(
            0d,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<Vector3> narasiCenter = new NetworkVariable<Vector3>(
            Vector3.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<double> narasiUntil = new NetworkVariable<double>(
            0d,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private Button heroButton;
        private Button s1Button;
        private Button s2Button;
        private Button ultimateButton;
        private Text heroButtonLabel;
        private Text s1Label;
        private Text s2Label;
        private Text ultimateLabel;
        private Text heroStatusText;

        private float localS1Ready;
        private float localS2Ready;
        private float localUltimateReady;
        private double serverS1Ready;
        private double serverS2Ready;
        private double nextNarasiDamageTick;
        private int megaBasicCombo;

        public PrototypeHero Hero => (PrototypeHero)heroId.Value;
        public int Pengaruh => pengaruh.Value;
        public int Rage => rage.Value;

        private double ServerClock =>
            NetworkManager != null ? NetworkManager.ServerTime.Time : Time.realtimeSinceStartupAsDouble;

        private ulong DamageSourceClientId =>
            GetComponent<NetworkBotController>() != null
                ? NetworkMatchManager.NoClient
                : OwnerClientId;

        private ulong DamageSourceActorNetworkObjectId =>
            IsSpawned ? NetworkObjectId : NetworkMatchManager.NoClient;

        public bool IsSilenced => IsSpawned && ServerClock < silencedUntil.Value;
        public bool IsStunned => IsSpawned && ServerClock < stunnedUntil.Value;
        public bool IsGarudaActive => IsSpawned && ServerClock < garudaUntil.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            heroId.OnValueChanged += OnHeroChanged;
            pengaruh.OnValueChanged += OnResourceChanged;
            rage.OnValueChanged += OnResourceChanged;

            NetworkBotController bot = GetComponent<NetworkBotController>();

            if (IsServer)
                heroId.Value = bot != null ? (int)bot.Hero : (int)(OwnerClientId % 4UL);

            if (IsOwner && bot == null)
                BindHud();

            RefreshIdentityLabel();
        }

        public override void OnNetworkDespawn()
        {
            heroId.OnValueChanged -= OnHeroChanged;
            pengaruh.OnValueChanged -= OnResourceChanged;
            rage.OnValueChanged -= OnResourceChanged;

            UnbindHud();
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (IsServer)
                ServerTickPassive();

            if (!IsOwner || GetComponent<NetworkBotController>() != null)
                return;

            TickLocalPassive();

            if (heroButton == null || s1Button == null || s2Button == null || ultimateButton == null)
                BindHud();

            RefreshHud();
        }

        public int GetBasicDamage()
        {
            switch (Hero)
            {
                case PrototypeHero.Prabowo: return 24;
                case PrototypeHero.Abah: return 17;
                case PrototypeHero.Jokowi: return 15;
                default: return 18;
            }
        }

        public float GetBasicRange()
        {
            switch (Hero)
            {
                case PrototypeHero.Abah: return 6.8f;
                case PrototypeHero.Prabowo: return 2.8f;
                default: return 2.7f;
            }
        }

        public float GetBasicCooldown()
        {
            switch (Hero)
            {
                case PrototypeHero.Prabowo: return IsGarudaActive ? 0.62f : 0.88f;
                case PrototypeHero.Abah: return 0.72f;
                case PrototypeHero.Jokowi: return 0.68f;
                default: return 0.64f;
            }
        }

        public float GetOutgoingDamageMultiplier()
        {
            return Hero == PrototypeHero.Prabowo && IsGarudaActive ? 1.30f : 1f;
        }

        public float GetIncomingDamageMultiplier()
        {
            return Hero == PrototypeHero.Prabowo && IsGarudaActive ? 0.72f : 1f;
        }

        public float GetMovementSpeedMultiplier()
        {
            if (!IsSpawned)
                return 1f;

            double now = ServerClock;
            float multiplier = Hero == PrototypeHero.Prabowo && now < garudaUntil.Value ? 1.25f : 1f;
            int team = NetworkTeamUtility.GetTeam(NetworkObject);

            NetworkHeroKit[] kits = FindObjectsByType<NetworkHeroKit>(FindObjectsSortMode.None);
            foreach (NetworkHeroKit kit in kits)
            {
                if (kit == null || !kit.IsSpawned)
                    continue;

                if (kit.Hero == PrototypeHero.Jokowi &&
                    now < kit.roadUntil.Value &&
                    NetworkTeamUtility.GetTeam(kit.NetworkObject) == team &&
                    HorizontalDistanceSqr(transform.position, kit.roadCenter.Value) <= 25f)
                {
                    multiplier = Mathf.Max(multiplier, 1.35f);
                }

                if (kit.Hero == PrototypeHero.Abah &&
                    now < kit.narasiUntil.Value &&
                    HorizontalDistanceSqr(transform.position, kit.narasiCenter.Value) <= 25f)
                {
                    bool sameTeam = NetworkTeamUtility.GetTeam(kit.NetworkObject) == team;
                    multiplier *= sameTeam ? 1.20f : 0.75f;
                }
            }

            return Mathf.Clamp(multiplier, 0.5f, 1.65f);
        }

        public void ServerGainPengaruh(int amount)
        {
            if (!IsServer || amount <= 0)
                return;

            if (Hero == PrototypeHero.Abah)
            {
                int nearbyHeroes = CountNearbyHeroes(6f, false);
                float audienceMultiplier = 1f + Mathf.Min(3, nearbyHeroes) * 0.20f;
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * audienceMultiplier));
            }

            pengaruh.Value = Mathf.Clamp(pengaruh.Value + amount, 0, MaxPengaruh);
        }

        public void ServerResetForMatch()
        {
            if (!IsServer)
                return;

            pengaruh.Value = 0;
            rage.Value = 0;
            silencedUntil.Value = 0d;
            stunnedUntil.Value = 0d;
            garudaUntil.Value = 0d;
            roadUntil.Value = 0d;
            narasiUntil.Value = 0d;
            serverS1Ready = 0d;
            serverS2Ready = 0d;
            megaBasicCombo = 0;
        }

        public void ServerOnKnockedOut()
        {
            if (!IsServer)
                return;

            garudaUntil.Value = 0d;
            roadUntil.Value = 0d;
            narasiUntil.Value = 0d;
            stunnedUntil.Value = 0d;
            silencedUntil.Value = 0d;
        }

        private void ServerTickPassive()
        {
            double now = ServerClock;

            if (Hero == PrototypeHero.Abah &&
                now < narasiUntil.Value &&
                now >= nextNarasiDamageTick)
            {
                nextNarasiDamageTick = now + 1d;

                foreach (NetworkHeroKit enemy in ServerEnemies())
                {
                    if (HorizontalDistanceSqr(enemy.transform.position, narasiCenter.Value) > 25f)
                        continue;

                    NetworkPlayerCombat enemyCombat = enemy.GetComponent<NetworkPlayerCombat>();
                    enemyCombat?.ServerReceiveDamage(4, DamageSourceClientId, DamageSourceActorNetworkObjectId);
                }
            }

            if (Hero == PrototypeHero.Jokowi &&
                now < roadUntil.Value &&
                serverS1Ready > now &&
                HasFriendlyTeammateUsingRoad())
            {
                serverS1Ready = Math.Max(now, serverS1Ready - Time.deltaTime * 0.75d);
            }
        }

        private void TickLocalPassive()
        {
            if (Hero != PrototypeHero.Jokowi ||
                ServerClock >= roadUntil.Value ||
                localS1Ready <= Time.unscaledTime)
                return;

            if (HasFriendlyTeammateUsingRoad())
                localS1Ready = Mathf.Max(Time.unscaledTime, localS1Ready - Time.deltaTime * 0.75f);
        }

        private bool HasFriendlyTeammateUsingRoad()
        {
            int team = NetworkTeamUtility.GetTeam(NetworkObject);
            NetworkHeroKit[] kits = FindObjectsByType<NetworkHeroKit>(FindObjectsSortMode.None);

            foreach (NetworkHeroKit kit in kits)
            {
                if (kit == null || kit == this || !kit.IsSpawned)
                    continue;

                if (NetworkTeamUtility.GetTeam(kit.NetworkObject) != team)
                    continue;

                if (HorizontalDistanceSqr(kit.transform.position, roadCenter.Value) <= 25f)
                    return true;
            }

            return false;
        }

        private int CountNearbyHeroes(float radius, bool teammatesOnly)
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
                return 0;

            int count = 0;
            int team = NetworkTeamUtility.GetTeam(NetworkObject);
            float radiusSqr = radius * radius;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (!NetworkTeamUtility.IsCombatActor(networkObject) ||
                    networkObject == NetworkObject)
                    continue;

                if (teammatesOnly && NetworkTeamUtility.GetTeam(networkObject) != team)
                    continue;

                if (HorizontalDistanceSqr(networkObject.transform.position, transform.position) <= radiusSqr)
                    count++;
            }

            return count;
        }

        public void ServerOnBasicHit(NetworkHeroKit target, int damage)
        {
            if (!IsServer)
                return;

            if (Hero == PrototypeHero.Abah && target != null)
            {
                Vector3 direction = target.transform.position - transform.position;
                PlayAbilityFxClientRpc(
                    (int)Hero,
                    0,
                    target.transform.position,
                    direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward);
            }

            if (Hero == PrototypeHero.Mega)
            {
                rage.Value = Mathf.Clamp(rage.Value + damage * 2, 0, 100);
                megaBasicCombo = (megaBasicCombo + 1) % 3;

                if (megaBasicCombo == 0 && target != null)
                    target.ServerApplyKnockback(transform.forward * 1.5f);
            }
        }

        public void ServerApplySilence(float seconds)
        {
            if (!IsServer)
                return;

            silencedUntil.Value = Math.Max(silencedUntil.Value, ServerClock + seconds);
        }

        public void ServerApplyStun(float seconds)
        {
            if (!IsServer)
                return;

            stunnedUntil.Value = Math.Max(stunnedUntil.Value, ServerClock + seconds);
        }

        public void ServerApplyKnockback(Vector3 displacement)
        {
            if (!IsServer)
                return;

            float resistance = GetServerKnockbackResistance();
            ApplyDisplacementClientRpc(displacement * Mathf.Clamp01(1f - resistance), false);
        }

        private float GetServerKnockbackResistance()
        {
            float resistance = IsGarudaActive ? 0.30f : 0f;
            int team = NetworkTeamUtility.GetTeam(NetworkObject);

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (!NetworkTeamUtility.IsCombatActor(networkObject))
                    continue;

                if (NetworkTeamUtility.GetTeam(networkObject) != team)
                    continue;

                NetworkHeroKit allyKit = networkObject.GetComponent<NetworkHeroKit>();
                if (allyKit == null || allyKit == this || allyKit.Hero != PrototypeHero.Prabowo)
                    continue;

                if (HorizontalDistanceSqr(transform.position, allyKit.transform.position) <= 25f)
                    resistance = Mathf.Max(resistance, 0.18f);
            }

            return Mathf.Clamp(resistance, 0f, 0.60f);
        }

        public void TryCycleHero()
        {
            if (!IsOwner || !IsSpawned)
                return;

            NetworkMatchManager match = NetworkMatchManager.Instance;
            if (match == null ||
                (match.State != GreyboxMatchState.Waiting && match.State != GreyboxMatchState.Result))
                return;

            SelectHeroServerRpc(((int)Hero + 1) % 4);
        }

        public void TryS1()
        {
            if (!CanUseHeroAbility() || Time.unscaledTime < localS1Ready)
                return;

            float cooldown = GetS1Cooldown();
            localS1Ready = Time.unscaledTime + cooldown;
            UseS1ServerRpc(transform.forward);
        }

        public void TryS2()
        {
            if (!CanUseHeroAbility() || Time.unscaledTime < localS2Ready)
                return;

            float cooldown = GetS2Cooldown();
            localS2Ready = Time.unscaledTime + cooldown;
            UseS2ServerRpc(transform.forward);
        }

        public void TryUltimate()
        {
            if (!CanUseHeroAbility() || pengaruh.Value < MaxPengaruh || Time.unscaledTime < localUltimateReady)
                return;

            localUltimateReady = Time.unscaledTime + 0.5f;
            UseUltimateServerRpc(transform.forward);
        }

        private bool CanUseHeroAbility()
        {
            NetworkMatchManager match = NetworkMatchManager.Instance;
            NetworkPlayerCombat combat = GetComponent<NetworkPlayerCombat>();

            return IsOwner &&
                   IsSpawned &&
                   combat != null &&
                   !combat.IsKnockedOut &&
                   match != null &&
                   match.AllowsGameplay &&
                   !match.IsRuler(OwnerClientId) &&
                   !IsSilenced &&
                   !IsStunned;
        }

        [ServerRpc]
        private void SelectHeroServerRpc(int requestedHero)
        {
            NetworkMatchManager match = NetworkMatchManager.Instance;
            if (match == null ||
                (match.State != GreyboxMatchState.Waiting && match.State != GreyboxMatchState.Result))
                return;

            heroId.Value = Mathf.Clamp(requestedHero, 0, 3);
            pengaruh.Value = 0;
            rage.Value = 0;
            megaBasicCombo = 0;
        }

        [ServerRpc]
        private void UseS1ServerRpc(Vector3 direction)
        {
            ServerTryExecuteS1(direction);
        }

        [ServerRpc]
        private void UseS2ServerRpc(Vector3 direction)
        {
            ServerTryExecuteS2(direction);
        }

        [ServerRpc]
        private void UseUltimateServerRpc(Vector3 direction)
        {
            ServerTryExecuteUltimate(direction);
        }

        public bool ServerBotTryS1(Vector3 direction)
        {
            return GetComponent<NetworkBotController>() != null && ServerTryExecuteS1(direction);
        }

        public bool ServerBotTryS2(Vector3 direction)
        {
            return GetComponent<NetworkBotController>() != null && ServerTryExecuteS2(direction);
        }

        public bool ServerBotTryUltimate(Vector3 direction)
        {
            return GetComponent<NetworkBotController>() != null && ServerTryExecuteUltimate(direction);
        }

        private bool ServerTryExecuteS1(Vector3 direction)
        {
            if (!ServerCanCast() || ServerClock < serverS1Ready)
                return false;

            direction = FlattenDirection(direction);
            serverS1Ready = ServerClock + GetS1Cooldown();

            switch (Hero)
            {
                case PrototypeHero.Mega:
                    ServerMegaCharge(direction);
                    break;
                case PrototypeHero.Prabowo:
                    ServerCommanderLeap(direction);
                    break;
                case PrototypeHero.Abah:
                    ServerNarasi();
                    break;
                case PrototypeHero.Jokowi:
                    ServerInfrastruktur();
                    break;
            }

            return true;
        }

        private bool ServerTryExecuteS2(Vector3 direction)
        {
            if (!ServerCanCast() || ServerClock < serverS2Ready)
                return false;

            direction = FlattenDirection(direction);
            serverS2Ready = ServerClock + GetS2Cooldown();

            switch (Hero)
            {
                case PrototypeHero.Mega:
                    ServerKader();
                    break;
                case PrototypeHero.Prabowo:
                    ServerBaris(direction);
                    break;
                case PrototypeHero.Abah:
                    ApplyDisplacementClientRpc(direction * 5.2f, true);
                    PlayAbilityFxClientRpc((int)Hero, 2, transform.position, direction);
                    break;
                case PrototypeHero.Jokowi:
                    ApplyDisplacementClientRpc(direction * 5.8f, true);
                    PlayAbilityFxClientRpc((int)Hero, 2, transform.position, direction);
                    break;
            }

            return true;
        }

        private bool ServerTryExecuteUltimate(Vector3 direction)
        {
            if (!ServerCanCast() || pengaruh.Value < MaxPengaruh)
                return false;

            pengaruh.Value = 0;
            direction = FlattenDirection(direction);

            switch (Hero)
            {
                case PrototypeHero.Mega:
                    ServerMoncongPutih(direction);
                    break;
                case PrototypeHero.Prabowo:
                    garudaUntil.Value = ServerClock + 8d;
                    PlayAbilityFxClientRpc((int)Hero, 3, transform.position, direction);
                    break;
                case PrototypeHero.Abah:
                    ServerPidatoKebangsaan();
                    break;
                case PrototypeHero.Jokowi:
                    ServerProyekNasional();
                    break;
            }

            return true;
        }

        private bool ServerCanCast()
        {
            NetworkMatchManager match = NetworkMatchManager.Instance;
            NetworkPlayerCombat combat = GetComponent<NetworkPlayerCombat>();
            NetworkBotController bot = GetComponent<NetworkBotController>();

            bool isRuler = match != null &&
                           (bot != null
                               ? match.IsRuler(NetworkObject)
                               : match.IsRuler(OwnerClientId));

            return IsServer &&
                   combat != null &&
                   !combat.IsKnockedOut &&
                   match != null &&
                   match.AllowsGameplay &&
                   !isRuler &&
                   ServerClock >= silencedUntil.Value &&
                   ServerClock >= stunnedUntil.Value;
        }

        private void ServerMegaCharge(Vector3 direction)
        {
            Vector3 start = transform.position;
            Vector3 end = start + direction * 6.2f;
            float forceMultiplier = rage.Value >= 100 ? 1.25f : 1f;

            if (rage.Value >= 100)
                rage.Value = 0;

            foreach (NetworkHeroKit enemy in ServerEnemies())
            {
                if (DistancePointToSegmentXZ(enemy.transform.position, start, end) > 1.45f)
                    continue;

                NetworkPlayerCombat combat = enemy.GetComponent<NetworkPlayerCombat>();
                if (combat != null)
                    combat.ServerReceiveDamage(20, DamageSourceClientId, DamageSourceActorNetworkObjectId);

                enemy.ServerApplyKnockback(direction * 3.0f * forceMultiplier);
                enemy.ServerApplyStun(0.65f);
            }

            ApplyDisplacementClientRpc(direction * 6.2f, false);
            PlayAbilityFxClientRpc((int)Hero, 1, start, direction);
        }

        private void ServerKader()
        {
            Vector3 center = transform.position + transform.forward * 2.4f;
            PlayAbilityFxClientRpc((int)Hero, 2, center, transform.forward);
            ServerGainPengaruh(4);
        }

        private void ServerMoncongPutih(Vector3 direction)
        {
            Vector3 start = transform.position;
            Vector3 end = start + direction * 12f;

            foreach (NetworkHeroKit enemy in ServerEnemies())
            {
                if (DistancePointToSegmentXZ(enemy.transform.position, start, end) > 2.2f)
                    continue;

                NetworkPlayerCombat combat = enemy.GetComponent<NetworkPlayerCombat>();
                if (combat != null)
                    combat.ServerReceiveDamage(30, DamageSourceClientId, DamageSourceActorNetworkObjectId);

                enemy.ServerApplyKnockback(direction * 5.2f);
            }

            PlayAbilityFxClientRpc((int)Hero, 3, start, direction);
        }

        private void ServerCommanderLeap(Vector3 direction)
        {
            Vector3 landing = transform.position + direction * 4.2f;
            ApplyDisplacementClientRpc(direction * 4.2f, true);

            foreach (NetworkHeroKit enemy in ServerEnemies())
            {
                Vector3 projected = enemy.transform.position;
                if (HorizontalDistanceSqr(projected, landing) > 10.24f)
                    continue;

                NetworkPlayerCombat combat = enemy.GetComponent<NetworkPlayerCombat>();
                if (combat != null)
                    combat.ServerReceiveDamage(22, DamageSourceClientId, DamageSourceActorNetworkObjectId);

                Vector3 away = FlattenDirection(projected - landing);
                enemy.ServerApplyKnockback(away * 2.2f);
            }

            PlayAbilityFxClientRpc((int)Hero, 1, landing, direction);
        }

        private void ServerBaris(Vector3 direction)
        {
            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (!NetworkTeamUtility.IsCombatActor(networkObject) || networkObject == NetworkObject)
                    continue;

                Vector3 delta = networkObject.transform.position - transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude > 25f)
                    continue;

                Vector3 toward = delta.sqrMagnitude > 0.001f ? delta.normalized : direction;
                float facingDot = Vector3.Dot(direction, toward);
                bool sameTeam = NetworkTeamUtility.GetTeam(networkObject) ==
                                NetworkTeamUtility.GetTeam(NetworkObject);

                NetworkPlayerCombat combat = networkObject.GetComponent<NetworkPlayerCombat>();
                NetworkHeroKit kit = networkObject.GetComponent<NetworkHeroKit>();

                if (sameTeam)
                {
                    if (facingDot <= 0.15f)
                        combat?.ServerGrantShield(20);
                }
                else if (facingDot >= 0.25f)
                {
                    combat?.ServerReceiveDamage(10, DamageSourceClientId, DamageSourceActorNetworkObjectId);
                    kit?.ServerApplyKnockback(toward * 3.0f);
                }
            }

            PlayAbilityFxClientRpc((int)Hero, 2, transform.position, direction);
        }

        private void ServerNarasi()
        {
            narasiCenter.Value = transform.position;
            narasiUntil.Value = ServerClock + 6d;
            nextNarasiDamageTick = ServerClock + 1d;
            ServerGainPengaruh(5);
            PlayAbilityFxClientRpc((int)Hero, 1, narasiCenter.Value, Vector3.forward);
        }

        private void ServerPidatoKebangsaan()
        {
            foreach (NetworkHeroKit enemy in ServerEnemies())
            {
                if (HorizontalDistanceSqr(enemy.transform.position, transform.position) > 49f)
                    continue;

                enemy.ServerApplySilence(3f);
                NetworkPlayerCombat combat = enemy.GetComponent<NetworkPlayerCombat>();
                combat?.ServerReceiveDamage(18, DamageSourceClientId, DamageSourceActorNetworkObjectId);
            }

            PlayAbilityFxClientRpc((int)Hero, 3, transform.position, Vector3.forward);
        }

        private void ServerInfrastruktur()
        {
            roadCenter.Value = transform.position + transform.forward * 2.5f;
            roadUntil.Value = ServerClock + 8d;
            ServerGainPengaruh(4);
            PlayAbilityFxClientRpc((int)Hero, 1, roadCenter.Value, transform.forward);
        }

        private void ServerProyekNasional()
        {
            PlayAbilityFxClientRpc((int)Hero, 3, Vector3.zero, Vector3.forward);
        }

        private NetworkHeroKit[] ServerEnemies()
        {
            if (!IsServer || NetworkManager == null || NetworkManager.SpawnManager == null)
                return Array.Empty<NetworkHeroKit>();

            int team = NetworkTeamUtility.GetTeam(NetworkObject);
            var result = new System.Collections.Generic.List<NetworkHeroKit>();

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (!NetworkTeamUtility.IsCombatActor(networkObject) || networkObject == NetworkObject)
                    continue;

                if (NetworkTeamUtility.GetTeam(networkObject) == team)
                    continue;

                NetworkHeroKit kit = networkObject.GetComponent<NetworkHeroKit>();
                NetworkPlayerCombat combat = networkObject.GetComponent<NetworkPlayerCombat>();

                if (kit != null && combat != null && !combat.IsKnockedOut)
                    result.Add(kit);
            }

            return result.ToArray();
        }

        [ClientRpc]
        private void ApplyDisplacementClientRpc(Vector3 displacement, bool phaseThrough)
        {
            if (!IsOwner)
                return;

            CharacterController controller = GetComponent<CharacterController>();
            if (controller == null)
                return;

            if (phaseThrough)
            {
                bool wasEnabled = controller.enabled;
                controller.enabled = false;
                transform.position += displacement;
                controller.enabled = wasEnabled;
            }
            else
            {
                controller.Move(displacement);
            }
        }

        [ClientRpc]
        private void PlayAbilityFxClientRpc(int hero, int slot, Vector3 position, Vector3 direction)
        {
            if (!isActiveAndEnabled)
                return;

            StartCoroutine(AbilityFxRoutine((PrototypeHero)hero, slot, position, direction));
        }

        private IEnumerator AbilityFxRoutine(PrototypeHero hero, int slot, Vector3 position, Vector3 direction)
        {
            if (hero == PrototypeHero.Abah && slot == 1)
            {
                GameObject zone = CreateBox(
                    "NarasiZone",
                    position + Vector3.up * 0.035f,
                    new Vector3(10f, 0.07f, 10f),
                    new Color(0.20f, 0.65f, 0.95f),
                    false);
                yield return new WaitForSecondsRealtime(6f);
                if (zone != null) Destroy(zone);
                yield break;
            }

            if (hero == PrototypeHero.Jokowi && slot == 1)
            {
                GameObject road = CreateBox(
                    "InfrastrukturRoad",
                    position + Vector3.up * 0.035f,
                    new Vector3(4f, 0.07f, 10f),
                    new Color(0.35f, 0.85f, 0.35f),
                    false);
                road.transform.rotation = Quaternion.LookRotation(
                    direction.sqrMagnitude > 0.01f ? direction : Vector3.forward);
                yield return new WaitForSecondsRealtime(8f);
                if (road != null) Destroy(road);
                yield break;
            }

            if (hero == PrototypeHero.Mega && slot == 2)
            {
                yield return SpawnBlocker(position, 6f);
                yield break;
            }

            if (hero == PrototypeHero.Jokowi && slot == 3)
            {
                GameObject[] structures =
                {
                    CreateBox("BuildNode_A", new Vector3(-8f, 0.7f, 4f), new Vector3(4f, 1.4f, 0.8f), new Color(0.72f,0.72f,0.78f), true),
                    CreateBox("BuildNode_B", new Vector3(8f, 0.7f, -4f), new Vector3(4f, 1.4f, 0.8f), new Color(0.72f,0.72f,0.78f), true),
                    CreateBox("BuildNode_C", new Vector3(0f, 0.35f, 8f), new Vector3(5f, 0.7f, 2f), new Color(0.55f,0.62f,0.70f), true)
                };

                yield return new WaitForSecondsRealtime(12f);

                foreach (GameObject structure in structures)
                    if (structure != null) Destroy(structure);

                yield break;
            }

            Color color = HeroFxColor(hero);
            Vector3 size = slot == 3 ? new Vector3(4.5f, 0.08f, 8f) : new Vector3(3.5f, 0.06f, 3.5f);
            GameObject fx = CreateBox("HeroAbilityFX", position + Vector3.up * 0.04f, size, color, false);
            fx.transform.rotation = Quaternion.LookRotation(direction.sqrMagnitude > 0.01f ? direction : Vector3.forward);
            yield return new WaitForSecondsRealtime(slot == 3 ? 1.0f : 0.45f);
            if (fx != null) Destroy(fx);
        }

        private IEnumerator SpawnBlocker(Vector3 center, float duration)
        {
            Vector3 right = transform.right;
            GameObject a = CreateBox("Kader_A", center + right * 1.2f + Vector3.up, new Vector3(1.1f, 2f, 0.8f), new Color(0.75f,0.18f,0.20f), true);
            GameObject b = CreateBox("Kader_B", center - right * 1.2f + Vector3.up, new Vector3(1.1f, 2f, 0.8f), new Color(0.75f,0.18f,0.20f), true);
            yield return new WaitForSecondsRealtime(duration);
            if (a != null) Destroy(a);
            if (b != null) Destroy(b);
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Color color, bool collider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = scale;

            Renderer renderer = box.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                renderer.material = material;
            }

            if (!collider)
            {
                Collider c = box.GetComponent<Collider>();
                if (c != null) Destroy(c);
            }

            return box;
        }

        private void BindHud()
        {
            if (!IsOwner)
                return;

            heroButton = FindButton("HeroButton");
            s1Button = FindButton("S1Button");
            s2Button = FindButton("S2Button");
            ultimateButton = FindButton("UltimateButton");

            heroButtonLabel = heroButton != null ? heroButton.GetComponentInChildren<Text>() : null;
            s1Label = s1Button != null ? s1Button.GetComponentInChildren<Text>() : null;
            s2Label = s2Button != null ? s2Button.GetComponentInChildren<Text>() : null;
            ultimateLabel = ultimateButton != null ? ultimateButton.GetComponentInChildren<Text>() : null;

            GameObject statusObject = GameObject.Find("HeroStatusText");
            heroStatusText = statusObject != null ? statusObject.GetComponent<Text>() : null;

            if (heroButton != null)
            {
                heroButton.onClick.RemoveListener(TryCycleHero);
                heroButton.onClick.AddListener(TryCycleHero);
            }

            if (s1Button != null)
            {
                s1Button.onClick.RemoveListener(TryS1);
                s1Button.onClick.AddListener(TryS1);
            }

            if (s2Button != null)
            {
                s2Button.onClick.RemoveListener(TryS2);
                s2Button.onClick.AddListener(TryS2);
            }

            if (ultimateButton != null)
            {
                ultimateButton.onClick.RemoveListener(TryUltimate);
                ultimateButton.onClick.AddListener(TryUltimate);
            }

            RefreshHud();
        }

        private void UnbindHud()
        {
            if (heroButton != null) heroButton.onClick.RemoveListener(TryCycleHero);
            if (s1Button != null) s1Button.onClick.RemoveListener(TryS1);
            if (s2Button != null) s2Button.onClick.RemoveListener(TryS2);
            if (ultimateButton != null) ultimateButton.onClick.RemoveListener(TryUltimate);
        }

        private void RefreshHud()
        {
            NetworkMatchManager match = NetworkMatchManager.Instance;
            bool heroSelectable = match != null &&
                                  (match.State == GreyboxMatchState.Waiting || match.State == GreyboxMatchState.Result);

            if (heroButton != null)
                heroButton.interactable = heroSelectable;

            if (heroButtonLabel != null)
                heroButtonLabel.text = "HERO\n" + GetHeroName(Hero);

            bool abilityAllowed = CanUseHeroAbility();
            RefreshAbilityButton(s1Button, s1Label, GetS1Name(), localS1Ready, abilityAllowed);
            RefreshAbilityButton(s2Button, s2Label, GetS2Name(), localS2Ready, abilityAllowed);

            if (ultimateButton != null)
                ultimateButton.interactable = abilityAllowed && pengaruh.Value >= MaxPengaruh;

            if (ultimateLabel != null)
                ultimateLabel.text = pengaruh.Value >= MaxPengaruh
                    ? "ULT\n" + GetUltimateName()
                    : "ULT " + pengaruh.Value + "%";

            if (heroStatusText != null)
            {
                string status = IsStunned ? "STUN" : IsSilenced ? "SILENCE" : "READY";
                string special = Hero == PrototypeHero.Mega ? " | RAGE " + rage.Value + "%" :
                                 Hero == PrototypeHero.Prabowo && IsGarudaActive ? " | GARUDA" : "";
                heroStatusText.text = GetHeroName(Hero) + " | PENGARUH " + pengaruh.Value + "/100 | " + status + special;
            }
        }

        private static void RefreshAbilityButton(Button button, Text label, string name, float readyAt, bool allowed)
        {
            if (button == null)
                return;

            float remaining = Mathf.Max(0f, readyAt - Time.unscaledTime);
            button.interactable = allowed && remaining <= 0f;

            if (label != null)
                label.text = remaining > 0f ? name + "\n" + remaining.ToString("0.0") : name;
        }

        private void OnHeroChanged(int previous, int current)
        {
            localS1Ready = 0f;
            localS2Ready = 0f;
            RefreshIdentityLabel();
            RefreshHud();
        }

        private void OnResourceChanged(int previous, int current)
        {
            RefreshHud();
        }

        private void RefreshIdentityLabel()
        {
            NetworkPlayerIdentity identity = GetComponent<NetworkPlayerIdentity>();
            if (identity != null)
                identity.RefreshOwnershipLabel();
        }

        private float GetS1Cooldown()
        {
            switch (Hero)
            {
                case PrototypeHero.Mega: return 10f;
                case PrototypeHero.Prabowo: return 9f;
                case PrototypeHero.Abah: return 11f;
                case PrototypeHero.Jokowi: return 10f;
                default: return 10f;
            }
        }

        private float GetS2Cooldown()
        {
            switch (Hero)
            {
                case PrototypeHero.Mega: return 14f;
                case PrototypeHero.Prabowo: return 12f;
                case PrototypeHero.Abah: return 8f;
                case PrototypeHero.Jokowi: return 10f;
                default: return 10f;
            }
        }

        public static string GetHeroName(PrototypeHero hero)
        {
            switch (hero)
            {
                case PrototypeHero.Mega: return "MEGA";
                case PrototypeHero.Prabowo: return "PRABOWO";
                case PrototypeHero.Abah: return "ABAH";
                case PrototypeHero.Jokowi: return "JOKOWI";
                default: return hero.ToString().ToUpperInvariant();
            }
        }

        private string GetS1Name()
        {
            switch (Hero)
            {
                case PrototypeHero.Mega: return "BANTENG\nCHARGE";
                case PrototypeHero.Prabowo: return "CMD\nLEAP";
                case PrototypeHero.Abah: return "NARASI";
                case PrototypeHero.Jokowi: return "INFRA\nSTRUKTUR";
                default: return "S1";
            }
        }

        private string GetS2Name()
        {
            switch (Hero)
            {
                case PrototypeHero.Mega: return "KADER!";
                case PrototypeHero.Prabowo: return "BARIS!";
                case PrototypeHero.Abah: return "ELECTRIC\nDASH";
                case PrototypeHero.Jokowi: return "BLUSUKAN";
                default: return "S2";
            }
        }

        private string GetUltimateName()
        {
            switch (Hero)
            {
                case PrototypeHero.Mega: return "MONCONG";
                case PrototypeHero.Prabowo: return "GARUDA";
                case PrototypeHero.Abah: return "PIDATO";
                case PrototypeHero.Jokowi: return "PROYEK";
                default: return "ULT";
            }
        }

        private static Button FindButton(string name)
        {
            GameObject obj = GameObject.Find(name);
            return obj != null ? obj.GetComponent<Button>() : null;
        }

        private static Vector3 FlattenDirection(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
        }

        private static float HorizontalDistanceSqr(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z;
        }

        private static float DistancePointToSegmentXZ(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector2 p = new Vector2(point.x, point.z);
            Vector2 p0 = new Vector2(a.x, a.z);
            Vector2 p1 = new Vector2(b.x, b.z);
            Vector2 ab = p1 - p0;
            float denominator = ab.sqrMagnitude;

            if (denominator <= 0.0001f)
                return Vector2.Distance(p, p0);

            float t = Mathf.Clamp01(Vector2.Dot(p - p0, ab) / denominator);
            return Vector2.Distance(p, p0 + ab * t);
        }

        private static Color HeroFxColor(PrototypeHero hero)
        {
            switch (hero)
            {
                case PrototypeHero.Mega: return new Color(0.80f, 0.15f, 0.20f);
                case PrototypeHero.Prabowo: return new Color(0.90f, 0.68f, 0.18f);
                case PrototypeHero.Abah: return new Color(0.20f, 0.65f, 0.95f);
                case PrototypeHero.Jokowi: return new Color(0.35f, 0.85f, 0.35f);
                default: return Color.white;
            }
        }
    }
}
