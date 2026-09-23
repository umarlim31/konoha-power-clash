using Konoha.Character;
using Konoha.Input;
using Unity.Netcode;
using UnityEngine;

namespace Konoha.Networking
{
    public enum BotTacticalState
    {
        Capture = 0,
        Attack = 1,
        Defend = 2,
        Ruler = 3,
        Retreat = 4
    }

    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(CharacterMotor))]
    [RequireComponent(typeof(NetworkPlayerCombat))]
    public sealed class NetworkBotController : NetworkBehaviour
    {
        private NetworkVariable<int> botTeam = new NetworkVariable<int>(
            NetworkTeamUtility.CyanTeam,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<int> botSlot = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<int> botHero = new NetworkVariable<int>(
            (int)PrototypeHero.Mega,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<int> tacticalState = new NetworkVariable<int>(
            (int)BotTacticalState.Capture,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public CharacterMotor motor;

        private CharacterController controller;
        private NetworkPlayerCombat combat;
        private NetworkHeroKit heroKit;
        private float nextThinkTime;
        private float nextAttackTime;
        private float nextDodgeTime;
        private float nextAbilityDecision;
        private float nextRetreatRecheck;
        private Vector3 desiredDirection;
        private NetworkPlayerCombat currentTarget;

        public int Team => botTeam.Value;
        public int Slot => botSlot.Value;
        public PrototypeHero Hero => (PrototypeHero)botHero.Value;
        public string HeroName => NetworkHeroKit.GetHeroName(Hero);
        public BotTacticalState TacticalState => (BotTacticalState)tacticalState.Value;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            combat = GetComponent<NetworkPlayerCombat>();
            heroKit = GetComponent<NetworkHeroKit>();

            if (motor == null)
                motor = GetComponent<CharacterMotor>();
        }

        public void ConfigureBeforeSpawn(int team, int slot, PrototypeHero hero)
        {
            if (!IsServer && NetworkObject != null && NetworkObject.IsSpawned)
                return;

            botTeam.Value = Mathf.Clamp(team, NetworkTeamUtility.CyanTeam, NetworkTeamUtility.OrangeTeam);
            botSlot.Value = Mathf.Clamp(slot, 0, 3);
            botHero.Value = Mathf.Clamp((int)hero, 0, 3);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            botTeam.OnValueChanged += OnBotConfigChanged;
            botSlot.OnValueChanged += OnBotConfigChanged;
            botHero.OnValueChanged += OnBotConfigChanged;

            gameObject.name = "BOT_" + NetworkTeamUtility.GetTeamName(Team) + "_" + Slot + "_" + HeroName;

            NetworkPlayerIdentity identity = GetComponent<NetworkPlayerIdentity>();
            identity?.RefreshOwnershipLabel();

            if (IsServer)
            {
                transform.position = NetworkTeamUtility.GetTeamSpawnPosition(Team, Slot);
                transform.rotation = Quaternion.identity;
                nextThinkTime = 0f;
            }

            Debug.Log(
                "[KONOHA BOT] Spawned | team=" + NetworkTeamUtility.GetTeamName(Team) +
                " | slot=" + Slot +
                " | hero=" + HeroName);
        }

        public override void OnNetworkDespawn()
        {
            botTeam.OnValueChanged -= OnBotConfigChanged;
            botSlot.OnValueChanged -= OnBotConfigChanged;
            botHero.OnValueChanged -= OnBotConfigChanged;
            base.OnNetworkDespawn();
        }

        private void OnBotConfigChanged(int previous, int current)
        {
            gameObject.name = "BOT_" + NetworkTeamUtility.GetTeamName(Team) + "_" + Slot + "_" + HeroName;
            NetworkPlayerIdentity identity = GetComponent<NetworkPlayerIdentity>();
            identity?.RefreshOwnershipLabel();
        }

        private void Update()
        {
            if (!IsServer || !IsSpawned)
                return;

            NetworkMatchManager match = NetworkMatchManager.Instance;
            heroKit ??= GetComponent<NetworkHeroKit>();

            if (match == null ||
                !match.AllowsGameplay ||
                combat == null ||
                combat.IsKnockedOut ||
                (heroKit != null && heroKit.IsStunned))
            {
                desiredDirection = Vector3.zero;
                return;
            }

            if (match.IsRuler(NetworkObject))
            {
                tacticalState.Value = (int)BotTacticalState.Ruler;
                currentTarget = null;
                desiredDirection = Vector3.zero;
                PinToChair(match);
                return;
            }

            bool retreating = ShouldRetreat();
            if (!retreating)
                TrySeatObjective(match);

            if (match.IsRuler(NetworkObject))
            {
                tacticalState.Value = (int)BotTacticalState.Ruler;
                currentTarget = null;
                desiredDirection = Vector3.zero;
                PinToChair(match);
                return;
            }

            if (Time.unscaledTime >= nextThinkTime)
            {
                nextThinkTime = Time.unscaledTime + 0.22f;
                Think(match, retreating);
            }

            Move(match);
            TryHeroAbility(match);
            TryAttack();
            TryDefensiveDodge();
        }

        private void Think(NetworkMatchManager match, bool retreating)
        {
            if (retreating)
            {
                tacticalState.Value = (int)BotTacticalState.Retreat;
                currentTarget = FindBestEnemy(match);

                Vector3 retreatAnchor = NetworkTeamUtility.GetTeamSpawnPosition(Team, Slot);
                Vector3 toSafety = retreatAnchor - transform.position;
                toSafety.y = 0f;

                Vector3 awayFromThreat = Vector3.zero;
                if (currentTarget != null)
                {
                    awayFromThreat = transform.position - currentTarget.transform.position;
                    awayFromThreat.y = 0f;
                }

                desiredDirection = CombineDirections(
                    SafeNormalized(toSafety),
                    SafeNormalized(awayFromThreat) * 1.35f,
                    ComputeSeparation() * 1.15f);
                return;
            }

            currentTarget = FindBestEnemy(match);

            bool defending = match.ChairOwnerTeam == Team && match.HasRuler;
            tacticalState.Value = (int)(defending ? BotTacticalState.Defend :
                                        currentTarget != null ? BotTacticalState.Attack :
                                        BotTacticalState.Capture);

            if (currentTarget != null)
            {
                Vector3 toEnemy = currentTarget.transform.position - transform.position;
                toEnemy.y = 0f;
                float distance = toEnemy.magnitude;
                float preferred = GetPreferredCombatRange();
                Vector3 separation = ComputeSeparation();

                if (distance > preferred + 0.8f)
                {
                    desiredDirection = CombineDirections(
                        SafeNormalized(toEnemy),
                        separation * 0.90f,
                        GetLaneBias(match) * 0.30f);
                    return;
                }

                if (distance < Mathf.Max(1.1f, preferred - 0.9f) && IsBacklineHero())
                {
                    desiredDirection = CombineDirections(
                        -SafeNormalized(toEnemy) * 1.20f,
                        GetOrbitDirection(toEnemy) * 0.70f,
                        separation);
                    return;
                }

                desiredDirection = CombineDirections(
                    GetOrbitDirection(toEnemy) * 0.70f,
                    separation,
                    GetLaneBias(match) * 0.25f);
                return;
            }

            Vector3 anchor = GetObjectiveAnchor(match);
            Vector3 toAnchor = anchor - transform.position;
            toAnchor.y = 0f;

            desiredDirection = CombineDirections(
                toAnchor.sqrMagnitude > 0.20f ? toAnchor.normalized : Vector3.zero,
                ComputeSeparation(),
                GetLaneBias(match) * 0.25f);
        }

        private bool ShouldRetreat()
        {
            if (combat == null)
                return false;

            if (combat.Wibawa <= 22)
                return true;

            if (combat.Wibawa >= 38)
                return false;

            if (Time.unscaledTime < nextRetreatRecheck)
                return tacticalState.Value == (int)BotTacticalState.Retreat;

            nextRetreatRecheck = Time.unscaledTime + 0.45f;
            NetworkPlayerCombat threat = FindNearestEnemy();
            return threat != null &&
                   HorizontalDistanceSqr(threat.transform.position, transform.position) <= 20f;
        }

        private Vector3 GetObjectiveAnchor(NetworkMatchManager match)
        {
            float teamSign = Team == NetworkTeamUtility.CyanTeam ? -1f : 1f;

            if (match.ChairOwnerTeam == Team && match.HasRuler)
            {
                float angle = Mathf.Deg2Rad * (42f + Slot * 78f + (Team == NetworkTeamUtility.CyanTeam ? 0f : 28f));
                float radius = IsBacklineHero() ? 4.6f : 3.4f;
                return match.ChairPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            }

            if (match.ChairOwnerTeam == Team)
            {
                if (IsPreferredBotSitter())
                    return match.ChairSeatPosition;

                float angle = Mathf.Deg2Rad * (Slot * 90f + 35f);
                return match.ChairPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2.8f;
            }

            float captureAngle = Mathf.Deg2Rad * (Slot * 82f + (Team == NetworkTeamUtility.CyanTeam ? 150f : 330f));
            float captureRadius = IsBacklineHero() ? 2.7f : 1.7f;
            Vector3 capturePoint = match.ChairPosition +
                                   new Vector3(Mathf.Cos(captureAngle), 0f, Mathf.Sin(captureAngle)) * captureRadius;

            capturePoint.x += teamSign * (IsBacklineHero() ? 0.55f : 0.15f);
            return capturePoint;
        }

        private NetworkPlayerCombat FindBestEnemy(NetworkMatchManager match)
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
                return null;

            NetworkPlayerCombat best = null;
            float bestScore = float.MaxValue;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (!NetworkTeamUtility.IsCombatActor(networkObject) ||
                    networkObject == NetworkObject ||
                    NetworkTeamUtility.GetTeam(networkObject) == Team)
                    continue;

                NetworkPlayerCombat candidate = networkObject.GetComponent<NetworkPlayerCombat>();
                if (candidate == null || candidate.IsKnockedOut)
                    continue;

                Vector3 delta = candidate.transform.position - transform.position;
                delta.y = 0f;
                float distanceSqr = delta.sqrMagnitude;

                if (distanceSqr > 100f)
                    continue;

                bool nearObjective =
                    HorizontalDistanceSqr(candidate.transform.position, match.ChairPosition) <=
                    (NetworkMatchManager.CaptureRadius + 2.2f) * (NetworkMatchManager.CaptureRadius + 2.2f);

                float healthFactor = Mathf.Lerp(0.72f, 1.08f, candidate.Wibawa / 100f);
                float objectiveFactor = nearObjective ? 0.70f : 1f;
                float rulerFactor = match.IsRuler(candidate.NetworkObject) ? 0.20f : 1f;

                NetworkBotController enemyBot = candidate.GetComponent<NetworkBotController>();
                float targetSpreadFactor = enemyBot != null && enemyBot.Slot == Slot ? 0.88f : 1f;

                float score = distanceSqr *
                              healthFactor *
                              objectiveFactor *
                              rulerFactor *
                              targetSpreadFactor;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private NetworkPlayerCombat FindNearestEnemy()
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
                return null;

            NetworkPlayerCombat best = null;
            float bestDistance = float.MaxValue;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (!NetworkTeamUtility.IsCombatActor(networkObject) ||
                    networkObject == NetworkObject ||
                    NetworkTeamUtility.GetTeam(networkObject) == Team)
                    continue;

                NetworkPlayerCombat candidate = networkObject.GetComponent<NetworkPlayerCombat>();
                if (candidate == null || candidate.IsKnockedOut)
                    continue;

                float distance = HorizontalDistanceSqr(candidate.transform.position, transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        private Vector3 ComputeSeparation()
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
                return Vector3.zero;

            Vector3 force = Vector3.zero;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (!NetworkTeamUtility.IsCombatActor(networkObject) ||
                    networkObject == NetworkObject ||
                    NetworkTeamUtility.GetTeam(networkObject) != Team)
                    continue;

                Vector3 away = transform.position - networkObject.transform.position;
                away.y = 0f;
                float sqr = away.sqrMagnitude;

                if (sqr < 0.01f || sqr > 6.25f)
                    continue;

                float distance = Mathf.Sqrt(sqr);
                float strength = Mathf.InverseLerp(2.5f, 0.55f, distance);
                force += away.normalized * strength;
            }

            return Vector3.ClampMagnitude(force, 1.35f);
        }

        private Vector3 GetLaneBias(NetworkMatchManager match)
        {
            float side = Team == NetworkTeamUtility.CyanTeam ? -1f : 1f;
            float lane = (Slot - 1.5f) * 0.45f;

            Vector3 target = match.ChairPosition + new Vector3(side * 1.4f, 0f, lane * 2.0f);
            Vector3 direction = target - transform.position;
            direction.y = 0f;
            return SafeNormalized(direction);
        }

        private Vector3 GetOrbitDirection(Vector3 toEnemy)
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, SafeNormalized(toEnemy));
            bool clockwise = ((Slot + Team) & 1) == 0;
            return clockwise ? tangent : -tangent;
        }

        private float GetPreferredCombatRange()
        {
            switch (Hero)
            {
                case PrototypeHero.Abah: return 5.2f;
                case PrototypeHero.Jokowi: return 3.2f;
                case PrototypeHero.Prabowo: return 2.0f;
                default: return 2.2f;
            }
        }

        private bool IsBacklineHero()
        {
            return Hero == PrototypeHero.Abah || Hero == PrototypeHero.Jokowi;
        }

        private bool IsPreferredBotSitter()
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
                return true;

            NetworkMatchManager match = NetworkMatchManager.Instance;
            if (match == null)
                return true;

            float humanDeferRadiusSqr = 6.25f;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject == null ||
                    !networkObject.IsPlayerObject ||
                    NetworkTeamUtility.GetTeam(networkObject) != Team)
                    continue;

                NetworkPlayerCombat humanCombat = networkObject.GetComponent<NetworkPlayerCombat>();
                if (humanCombat == null || humanCombat.IsKnockedOut)
                    continue;

                if (HorizontalDistanceSqr(networkObject.transform.position, match.ChairSeatPosition) <= humanDeferRadiusSqr)
                    return false;
            }

            float myDistance = HorizontalDistanceSqr(transform.position, match.ChairSeatPosition);
            float bestDistance = myDistance;
            int bestSlot = Slot;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                NetworkBotController bot = networkObject != null
                    ? networkObject.GetComponent<NetworkBotController>()
                    : null;

                if (bot == null ||
                    bot.Team != Team ||
                    bot.combat == null ||
                    bot.combat.IsKnockedOut)
                    continue;

                float distance = HorizontalDistanceSqr(bot.transform.position, match.ChairSeatPosition);

                if (distance < bestDistance - 0.05f ||
                    (Mathf.Abs(distance - bestDistance) <= 0.05f && bot.Slot < bestSlot))
                {
                    bestDistance = distance;
                    bestSlot = bot.Slot;
                }
            }

            return Slot == bestSlot;
        }

        private void TrySeatObjective(NetworkMatchManager match)
        {
            if (match == null ||
                match.HasRuler ||
                match.ChairOwnerTeam != Team ||
                combat == null ||
                combat.IsKnockedOut ||
                !IsPreferredBotSitter())
                return;

            Vector3 delta = match.ChairSeatPosition - transform.position;
            delta.y = 0f;

            if (delta.sqrMagnitude <= NetworkMatchManager.SitRadius * NetworkMatchManager.SitRadius)
                match.ServerTrySeatBot(this);
        }

        private void PinToChair(NetworkMatchManager match)
        {
            if (controller == null || match == null)
                return;

            Vector3 delta = match.ChairSeatPosition - transform.position;
            if (delta.sqrMagnitude > 0.0004f)
                controller.Move(delta);
        }

        private void Move(NetworkMatchManager match)
        {
            if (motor == null || desiredDirection.sqrMagnitude < 0.01f)
                return;

            heroKit ??= GetComponent<NetworkHeroKit>();
            motor.speedMultiplier = heroKit != null ? heroKit.GetMovementSpeedMultiplier() : 1f;

            Vector2 input = new Vector2(desiredDirection.x, desiredDirection.z);
            motor.Step(new MoveIntent(input), Mathf.Min(Time.deltaTime, 0.05f));
        }

        private void TryHeroAbility(NetworkMatchManager match)
        {
            if (heroKit == null ||
                match == null ||
                match.IsRuler(NetworkObject) ||
                heroKit.IsStunned ||
                heroKit.IsSilenced ||
                Time.unscaledTime < nextAbilityDecision)
                return;

            Vector3 direction = currentTarget != null
                ? currentTarget.transform.position - transform.position
                : match.ChairPosition - transform.position;

            direction.y = 0f;
            direction = direction.sqrMagnitude < 0.01f ? transform.forward : direction.normalized;

            bool used = false;

            if (heroKit.Pengaruh >= NetworkHeroKit.MaxPengaruh)
            {
                used = heroKit.ServerBotTryUltimate(direction);
            }
            else
            {
                used = TryContextAbility(match, direction);
            }

            nextAbilityDecision = Time.unscaledTime + (used
                ? Random.Range(2.0f, 3.2f)
                : 0.65f);
        }

        private bool TryContextAbility(NetworkMatchManager match, Vector3 direction)
        {
            float targetDistance = currentTarget != null
                ? Mathf.Sqrt(HorizontalDistanceSqr(currentTarget.transform.position, transform.position))
                : 99f;

            switch (Hero)
            {
                case PrototypeHero.Mega:
                    if (currentTarget != null && targetDistance >= 2.8f && targetDistance <= 7.5f)
                        return heroKit.ServerBotTryS1(direction);
                    return heroKit.ServerBotTryS2(direction);

                case PrototypeHero.Prabowo:
                    if (currentTarget != null && targetDistance >= 3f && targetDistance <= 6.8f)
                        return heroKit.ServerBotTryS1(direction);
                    return heroKit.ServerBotTryS2(direction);

                case PrototypeHero.Abah:
                    if (combat != null && combat.Wibawa <= 35 && currentTarget != null && targetDistance <= 4f)
                        return heroKit.ServerBotTryS2(-direction);
                    return heroKit.ServerBotTryS1(direction);

                case PrototypeHero.Jokowi:
                    if (combat != null && combat.Wibawa <= 32 && currentTarget != null && targetDistance <= 4.5f)
                        return heroKit.ServerBotTryS2(-direction);
                    return heroKit.ServerBotTryS1(direction);

                default:
                    return false;
            }
        }

        private void TryAttack()
        {
            if (currentTarget == null || currentTarget.IsKnockedOut)
                return;

            heroKit ??= GetComponent<NetworkHeroKit>();

            float range = heroKit != null ? heroKit.GetBasicRange() : 2.7f;
            Vector3 delta = currentTarget.transform.position - transform.position;
            delta.y = 0f;

            if (delta.sqrMagnitude > range * range || Time.unscaledTime < nextAttackTime)
                return;

            Vector3 direction = delta.sqrMagnitude > 0.001f ? delta.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(direction);

            float cooldown = heroKit != null ? heroKit.GetBasicCooldown() : 0.80f;
            int damage = heroKit != null ? heroKit.GetBasicDamage() : 16;

            if (heroKit != null)
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * heroKit.GetOutgoingDamageMultiplier()));

            nextAttackTime = Time.unscaledTime + cooldown * Random.Range(0.96f, 1.16f);
            currentTarget.ServerReceiveDamage(damage, NetworkMatchManager.NoClient);
            heroKit?.ServerGainPengaruh(Mathf.Clamp(damage / 3, 3, 10));

            NetworkHeroKit targetKit = currentTarget.GetComponent<NetworkHeroKit>();
            heroKit?.ServerOnBasicHit(targetKit, damage);
        }

        private void TryDefensiveDodge()
        {
            if (controller == null ||
                combat == null ||
                combat.Wibawa > 38 ||
                currentTarget == null ||
                Time.unscaledTime < nextDodgeTime)
                return;

            Vector3 threat = currentTarget.transform.position - transform.position;
            threat.y = 0f;

            if (threat.sqrMagnitude > 20f)
                return;

            nextDodgeTime = Time.unscaledTime + Random.Range(3.8f, 5.2f);

            Vector3 side = GetOrbitDirection(threat);
            Vector3 escape = CombineDirections(
                side,
                -SafeNormalized(threat) * 0.55f,
                ComputeSeparation() * 0.45f);

            controller.Move(escape * 2.15f);
        }

        private static Vector3 CombineDirections(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 value = a + b + c;
            value.y = 0f;
            return value.sqrMagnitude > 0.001f ? value.normalized : Vector3.zero;
        }

        private static Vector3 SafeNormalized(Vector3 value)
        {
            value.y = 0f;
            return value.sqrMagnitude > 0.001f ? value.normalized : Vector3.zero;
        }

        private static float HorizontalDistanceSqr(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z;
        }
    }
}
