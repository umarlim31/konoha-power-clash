using Konoha.Character;
using Konoha.Input;
using Konoha.Networking;
using Unity.Netcode;
using UnityEngine;

namespace Konoha.Campaign
{
    // Server-driven member of a fictional organisation (Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md §7).
    // Wibawa, damage, knockback, stun and the knockout state come from the shared hero
    // combat components, so every hero ability works on enemies unchanged. This class adds
    // the role stats, a simple chase/guard brain and the role/faction presentation.
    // Faction mechanics (voting block, loket, lockdown) arrive with 0.0.9.2+.
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(CharacterMotor))]
    [RequireComponent(typeof(NetworkPlayerCombat))]
    public sealed class CampaignEnemy : NetworkBehaviour, INetworkAiActor
    {
        public CharacterMotor motor;
        // Scaled per role; holds the body and ornaments (never colliders).
        public Transform visualRoot;
        public Renderer bodyRenderer;
        public Renderer accentRenderer;
        public TextMesh nameplate;

        private NetworkVariable<int> role = new NetworkVariable<int>(
            (int)UnitRole.Kroni,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<int> faction = new NetworkVariable<int>(
            (int)FactionId.GardaTakhta,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private static CampaignMonument monument;

        private NetworkPlayerCombat combat;
        private NetworkHeroKit heroKit;
        private Camera cachedCamera;
        private MaterialPropertyBlock block;
        private Vector3 anchor;
        private float nextAttackTime;
        private float despawnAt = -1f;
        private float nextPresentationRefresh;
        private int appliedRole = -1;
        private int appliedFaction = -1;
        private bool appliedDown;

        public int Team => NetworkTeamUtility.SistemTeam;
        public UnitRole Role => (UnitRole)role.Value;
        public FactionId Faction => (FactionId)faction.Value;
        public bool IsDown => combat != null && combat.IsKnockedOut;
        public bool IsElite => UnitRoleStats.For(Role).IsElite;

        private void Awake()
        {
            combat = GetComponent<NetworkPlayerCombat>();
            heroKit = GetComponent<NetworkHeroKit>();
            if (motor == null)
                motor = GetComponent<CharacterMotor>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            cachedCamera = Camera.main;
            RefreshPresentation(true);
        }

        // Called by the director right after Spawn().
        public void ServerInitialize(UnitRole unitRole, FactionId unitFaction, Vector3 guardAnchor)
        {
            if (!IsServer)
                return;

            role.Value = (int)unitRole;
            faction.Value = (int)unitFaction;
            anchor = guardAnchor;
            despawnAt = -1f;
            combat ??= GetComponent<NetworkPlayerCombat>();
            combat.ServerConfigureWibawa(UnitRoleStats.For(unitRole).Wibawa);
            nextAttackTime = Time.time + CampaignTuning.Encounters.EnemyFirstAttackDelaySeconds;
            RefreshPresentation(true);
        }

        // §6: after a hero collapses, wounded enemies recover part of their missing Wibawa.
        public void ServerRecover(float missingFraction)
        {
            if (!IsServer || combat == null || combat.IsKnockedOut)
                return;

            int missing = combat.MaxWibawaValue - combat.Wibawa;
            combat.ServerHeal(Mathf.FloorToInt(missing * Mathf.Clamp01(missingFraction)));
        }

        private void Update()
        {
            if (!IsSpawned)
                return;

            if (Time.unscaledTime >= nextPresentationRefresh)
            {
                nextPresentationRefresh = Time.unscaledTime + 0.15f;
                RefreshPresentation(false);
            }

            if (IsServer)
                ServerThink();
        }

        private void ServerThink()
        {
            if (combat == null)
                return;

            if (combat.IsKnockedOut)
            {
                // Enemies do not respawn (ServerOnActorKnockedOut returned false).
                if (despawnAt < 0f)
                    despawnAt = Time.time + CampaignTuning.Encounters.EnemyDespawnSeconds;
                else if (Time.time >= despawnAt)
                    NetworkObject.Despawn(true);
                return;
            }

            ICombatRules rules = CombatRules.Current;
            if (rules == null || !rules.AllowsGameplay || (heroKit != null && heroKit.IsStunned))
                return;

            float deltaTime = Mathf.Min(Time.deltaTime, 0.05f);
            if (deltaTime <= 0f)
                return;

            UnitRoleStats stats = UnitRoleStats.For(Role);
            NetworkPlayerCombat target = FindTarget(stats);
            Vector3 destination = anchor;
            bool move = false;

            if (target != null)
            {
                Vector3 toTarget = target.transform.position - transform.position;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude <= CampaignTuning.Encounters.EnemyAttackReach *
                    CampaignTuning.Encounters.EnemyAttackReach)
                {
                    if (toTarget.sqrMagnitude > 0.0001f)
                        transform.rotation = Quaternion.LookRotation(toTarget.normalized);
                    TryAttack(target, stats);
                }
                else if (stats.Chases)
                {
                    destination = target.transform.position;
                    move = true;
                }
            }
            else
            {
                Vector3 home = anchor - transform.position;
                home.y = 0f;
                move = home.sqrMagnitude > 0.36f;
            }

            // Always step: a zero intent still applies gravity, so idle guards settle on the floor.
            Step(move ? destination : transform.position, stats, deltaTime);
        }

        private NetworkPlayerCombat FindTarget(UnitRoleStats stats)
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
                return null;

            float aggro = CampaignTuning.Encounters.EnemyAggroRadius;
            float bestDistance = aggro * aggro;
            NetworkPlayerCombat best = null;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (!NetworkTeamUtility.IsCombatActor(networkObject) ||
                    NetworkTeamUtility.IsAiActor(networkObject) ||
                    NetworkTeamUtility.GetTeam(networkObject) == Team)
                    continue;

                NetworkPlayerCombat candidate = networkObject.GetComponent<NetworkPlayerCombat>();
                if (candidate == null || candidate.IsKnockedOut)
                    continue;

                // Guards hold their post: they ignore heroes outside the guarded radius.
                if (stats.HasGuardRadius &&
                    HorizontalDistanceSqr(candidate.transform.position, anchor) > stats.GuardRadius * stats.GuardRadius)
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

        private void TryAttack(NetworkPlayerCombat target, UnitRoleStats stats)
        {
            if (Time.time < nextAttackTime)
                return;

            nextAttackTime = Time.time + CampaignTuning.Encounters.EnemyAttackIntervalSeconds;
            // Credited to this actor, never to a client (see INetworkAiActor).
            target.ServerReceiveDamage(stats.DamagePerHit, NetworkMatchManager.NoClient, NetworkObjectId);
        }

        private void Step(Vector3 destination, UnitRoleStats stats, float deltaTime)
        {
            if (motor == null || motor.definition == null)
                return;

            Vector2 axis = Vector2.zero;
            Vector3 flat = destination - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude >= 0.0004f)
            {
                if (monument == null)
                    monument = FindFirstObjectByType<CampaignMonument>();
                Vector3 next = monument != null && monument.solid != null
                    ? CampaignMonument.GuardDestination(transform.position, destination, monument.solid.bounds)
                    : destination;

                Vector3 direction = next - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude >= 0.0004f)
                {
                    direction.Normalize();
                    axis = new Vector2(direction.x, direction.z);
                }
            }

            float heroEffects = heroKit != null ? heroKit.GetMovementSpeedMultiplier() : 1f;
            motor.speedMultiplier = stats.Speed / Mathf.Max(0.1f, motor.definition.speed) * heroEffects;
            motor.Step(new MoveIntent(axis), deltaTime);
        }

        private void LateUpdate()
        {
            if (nameplate == null || !nameplate.gameObject.activeSelf)
                return;

            if (cachedCamera == null)
                cachedCamera = Camera.main;
            if (cachedCamera == null)
                return;

            Vector3 direction = nameplate.transform.position - cachedCamera.transform.position;
            if (direction.sqrMagnitude > 0.001f)
                nameplate.transform.rotation = Quaternion.LookRotation(direction);
        }

        private void RefreshPresentation(bool force)
        {
            int currentRole = role.Value;
            int currentFaction = faction.Value;
            bool down = IsDown;

            if (!force && currentRole == appliedRole && currentFaction == appliedFaction && down == appliedDown)
                return;

            appliedRole = currentRole;
            appliedFaction = currentFaction;
            appliedDown = down;

            UnitRole unitRole = (UnitRole)currentRole;
            FactionId unitFaction = (FactionId)currentFaction;
            FactionDefinition definition = FactionDefinition.Get(unitFaction);

            if (visualRoot != null)
                visualRoot.localScale = Vector3.one * RoleScale(unitRole);

            Color primary = ToColor(definition.PrimaryColor);
            Color accent = ToColor(definition.AccentColor);
            if (down)
            {
                primary = Color.Lerp(primary, Color.black, 0.70f);
                accent = Color.Lerp(accent, Color.black, 0.70f);
            }

            block ??= new MaterialPropertyBlock();
            SetColor(bodyRenderer, primary);
            SetColor(accentRenderer, accent);

            if (nameplate != null)
            {
                string title = EncounterComposer.TitleFor(unitFaction, unitRole).ToUpperInvariant();
                nameplate.text = down ? title + "\nTUMBANG" : title + "\n" + definition.DisplayName;
                nameplate.color = down
                    ? new Color(0.62f, 0.62f, 0.62f)
                    : UnitRoleStats.For(unitRole).IsElite
                        ? new Color(1f, 0.82f, 0.42f)
                        : new Color(1f, 0.55f, 0.50f);
            }

            gameObject.name = "Enemy_" + unitFaction + "_" + unitRole;
        }

        private void SetColor(Renderer target, Color color)
        {
            if (target == null)
                return;

            target.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            target.SetPropertyBlock(block);
        }

        private static float RoleScale(UnitRole unitRole)
        {
            switch (unitRole)
            {
                case UnitRole.Kroni: return 0.88f;
                case UnitRole.Spesialis: return 0.95f;
                case UnitRole.Senior: return 1.10f;
                case UnitRole.Pemimpin: return 1.28f;
                default: return 1f;
            }
        }

        private static Color ToColor(FactionColor color) => new Color(color.R, color.G, color.B);

        private static float HorizontalDistanceSqr(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z;
        }
    }
}
