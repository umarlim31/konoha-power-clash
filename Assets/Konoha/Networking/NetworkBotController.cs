using Konoha.Character;
using Unity.Netcode;
using UnityEngine;

namespace Konoha.Networking
{
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

        public CharacterMotor motor;

        private CharacterController controller;
        private NetworkPlayerCombat combat;
        private NetworkHeroKit heroKit;
        private float nextThinkTime;
        private float nextAttackTime;
        private float nextDodgeTime;
        private Vector3 desiredDirection;
        private NetworkPlayerCombat currentTarget;

        public int Team => botTeam.Value;
        public int Slot => botSlot.Value;
        public PrototypeHero Hero => (PrototypeHero)botHero.Value;
        public string HeroName => NetworkHeroKit.GetHeroName(Hero);

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

        private void Update()
        {
            if (!IsServer || !IsSpawned)
                return;

            NetworkMatchManager match = NetworkMatchManager.Instance;
            if (match == null || !match.AllowsGameplay || combat == null || combat.IsKnockedOut)
            {
                desiredDirection = Vector3.zero;
                return;
            }

            if (match.IsRuler(NetworkObject))
            {
                currentTarget = null;
                desiredDirection = Vector3.zero;
                PinToChair(match);
                return;
            }

            TrySeatObjective(match);

            if (match.IsRuler(NetworkObject))
            {
                currentTarget = null;
                desiredDirection = Vector3.zero;
                PinToChair(match);
                return;
            }

            if (Time.unscaledTime >= nextThinkTime)
            {
                nextThinkTime = Time.unscaledTime + 0.20f;
                Think(match);
            }

            Move(match);
            TryAttack();
            TryDefensiveDodge();
        }

        private void Think(NetworkMatchManager match)
        {
            currentTarget = FindBestEnemy(match);

            if (currentTarget != null)
            {
                Vector3 toEnemy = currentTarget.transform.position - transform.position;
                toEnemy.y = 0f;

                float preferredRange = heroKit != null ? heroKit.GetBasicRange() * 0.78f : 2.1f;
                if (toEnemy.sqrMagnitude > preferredRange * preferredRange)
                {
                    desiredDirection = toEnemy.normalized;
                    return;
                }

                desiredDirection = Vector3.zero;
                return;
            }

            Vector3 anchor = GetObjectiveAnchor(match);
            Vector3 toAnchor = anchor - transform.position;
            toAnchor.y = 0f;
            desiredDirection = toAnchor.sqrMagnitude > 0.30f ? toAnchor.normalized : Vector3.zero;
        }

        private Vector3 GetObjectiveAnchor(NetworkMatchManager match)
        {
            float side = Team == NetworkTeamUtility.CyanTeam ? -1f : 1f;
            float lane = (Slot - 1.5f) * 1.15f;

            if (match.ChairOwnerTeam == Team && match.HasRuler)
            {
                float angle = Slot * Mathf.PI * 0.5f + (Team == NetworkTeamUtility.CyanTeam ? 0f : 0.7f);
                return match.ChairPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 3.0f;
            }

            if (match.ChairOwnerTeam == Team)
                return match.ChairPosition + new Vector3(side * 2.3f, 0f, lane);

            return match.ChairPosition + new Vector3(side * 0.7f, 0f, lane * 0.35f);
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

                bool nearObjective =
                    (candidate.transform.position - match.ChairPosition).sqrMagnitude <=
                    (NetworkMatchManager.CaptureRadius + 2f) * (NetworkMatchManager.CaptureRadius + 2f);

                float score = distanceSqr * (nearObjective ? 0.55f : 1f);

                if (match.IsRuler(candidate.NetworkObject))
                    score *= 0.30f;

                if (score < bestScore && distanceSqr <= 64f)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private void TrySeatObjective(NetworkMatchManager match)
        {
            if (match == null ||
                match.HasRuler ||
                match.ChairOwnerTeam != Team ||
                combat == null ||
                combat.IsKnockedOut)
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

            nextAttackTime = Time.unscaledTime + cooldown * Random.Range(0.95f, 1.18f);
            currentTarget.ServerReceiveDamage(damage, NetworkMatchManager.NoClient);
            heroKit?.ServerGainPengaruh(Mathf.Clamp(damage / 3, 3, 10));

            NetworkHeroKit targetKit = currentTarget.GetComponent<NetworkHeroKit>();
            heroKit?.ServerOnBasicHit(targetKit, damage);

            Debug.Log(
                "[KONOHA BOT] Basic | " + HeroName +
                " -> " + currentTarget.name +
                " | dmg=" + damage);
        }

        private void TryDefensiveDodge()
        {
            if (controller == null ||
                combat == null ||
                combat.Wibawa > 35 ||
                currentTarget == null ||
                Time.unscaledTime < nextDodgeTime)
                return;

            Vector3 threat = currentTarget.transform.position - transform.position;
            threat.y = 0f;

            if (threat.sqrMagnitude > 16f)
                return;

            nextDodgeTime = Time.unscaledTime + Random.Range(3.6f, 5.0f);

            Vector3 side = Vector3.Cross(Vector3.up, threat.normalized);
            if (((Slot + Mathf.FloorToInt(Time.unscaledTime)) & 1) == 0)
                side = -side;

            controller.Move(side * 2.0f);
        }
    }
}
