using System.Collections.Generic;
using Konoha.Character;
using Konoha.Input;
using Konoha.Networking;
using Unity.Netcode;
using UnityEngine;

namespace Konoha.Campaign
{
    // Telegraphed leader specials (§8): replicated so every peer shows the right warning.
    public enum EnemySpecial
    {
        None = 0,
        KetokPalu = 1,  // Majelis Ketua: damage circle in front.
        SalahLoket = 2  // Kepala Biro: circle on the hero, sends them to the farthest loket.
    }

    // Server-driven member of a fictional organisation (Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md §7).
    // Wibawa, damage, knockback, stun and the knockout state come from the shared hero
    // combat components, so every hero ability works on enemies unchanged. This class adds
    // the role stats, a simple chase/guard brain, the role/faction presentation and, since
    // 0.0.9.2, the Majelis Daun mechanics: voting-block damage reduction, surrender and the
    // Ketua's telegraphed KETOK PALU (§8.1).
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(CharacterMotor))]
    [RequireComponent(typeof(NetworkPlayerCombat))]
    public sealed class CampaignEnemy : NetworkBehaviour, INetworkAiActor, ICombatActorState
    {
        public CharacterMotor motor;
        // Scaled per role; holds the body and ornaments (never colliders).
        public Transform visualRoot;
        public Renderer bodyRenderer;
        public Renderer accentRenderer;
        public TextMesh nameplate;
        // Burgundy ring under every Majelis unit while the voting block holds.
        public GameObject blockRing;
        // Ketua's gavel (shown for the Majelis Pemimpin); its pivot rises during the telegraph.
        public GameObject gavel;
        public Transform gavelPivot;
        // Ground warning of KETOK PALU: full-size outer disc and a fill disc that grows to it.
        public Transform telegraphOuter;
        public Transform telegraphFill;
        // STEMPEL TUNDA area (§8.2) under a living Biro Pengawas.
        public GameObject stempelAura;

        private NetworkVariable<int> role = new NetworkVariable<int>(
            (int)UnitRole.Kroni,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<int> faction = new NetworkVariable<int>(
            (int)FactionId.GardaTakhta,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<bool> blockShield = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> surrendered = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // Behind a locked door (Kepala Biro before the lokets are stamped): cannot be hit.
        private NetworkVariable<bool> sealedOff = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> telegraphing = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector3> telegraphCenter = new NetworkVariable<Vector3>(
            Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> specialKind = new NetworkVariable<int>(
            (int)EnemySpecial.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> telegraphRadius = new NetworkVariable<float>(
            CampaignTuning.Majelis.KetokPaluRadius, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private static CampaignMonument monument;
        private static CampaignStage stage;
        private static readonly List<NetworkPlayerCombat> specialVictims = new List<NetworkPlayerCombat>();
        // Spawned enemies on this peer (HUD, STEMPEL TUNDA checks) without scene searches.
        private static readonly List<CampaignEnemy> active = new List<CampaignEnemy>();
        public static IReadOnlyList<CampaignEnemy> Active => active;
        private Vector3[] salahLoketTargets;
        // Earliest time the next basic hit may land on each hero (by NetworkObjectId).
        private static readonly Dictionary<ulong, float> nextHitOnTarget = new Dictionary<ulong, float>();

        private NetworkPlayerCombat combat;
        private NetworkHeroKit heroKit;
        private Camera cachedCamera;
        private MaterialPropertyBlock block;
        private Vector3 anchor;
        // Optional area leash (Majelis hall); 0 falls back to the role's guard radius at the anchor.
        private Vector3 leashCenter;
        private float leashRadius;
        private float nextAttackTime;
        private float despawnAt = -1f;
        private float nextPresentationRefresh;
        private int appliedRole = -1;
        private int appliedFaction = -1;
        private bool appliedDown;
        private bool appliedBlock;
        private bool appliedSurrender;
        private bool appliedTelegraph;
        private bool appliedSealed;

        // Server-side special attack (KETOK PALU) configuration and clock.
        private bool hasSpecial;
        private float specialRadius;
        private float specialForward;
        private float specialTelegraph;
        private int specialDamage;
        private float specialCooldown;
        private float specialTriggerRange;
        private float nextSpecialTime;
        private float telegraphEndsAt;
        // Client-side start of the visible telegraph (fill animation).
        private float localTelegraphStart = -1f;

        public int Team => NetworkTeamUtility.SistemTeam;
        public UnitRole Role => (UnitRole)role.Value;
        public FactionId Faction => (FactionId)faction.Value;
        public bool IsDown => combat != null && combat.IsKnockedOut;
        public bool Surrendered => surrendered.Value;
        // Counts as defeated for objectives: knocked out or surrendered.
        public bool IsOutOfFight => IsDown || Surrendered;
        public bool IsElite => UnitRoleStats.For(Role).IsElite;
        public bool BlockShielded => blockShield.Value;
        public bool IsTelegraphing => telegraphing.Value;
        public EnemySpecial Special => (EnemySpecial)specialKind.Value;
        // A Biro Pengawas still in the fight projects STEMPEL TUNDA.
        public bool ProjectsStempelTunda => Faction == FactionId.BiroProsedur && Role == UnitRole.Spesialis && !IsOutOfFight;

        // Any peer: is this point inside a living Pengawas's STEMPEL TUNDA radius?
        public static bool InsideStempelTunda(Vector3 position)
        {
            float radiusSqr = CampaignTuning.Biro.StempelTundaRadius * CampaignTuning.Biro.StempelTundaRadius;
            foreach (CampaignEnemy enemy in active)
                if (enemy != null && enemy.IsSpawned && enemy.ProjectsStempelTunda &&
                    HorizontalDistanceSqr(enemy.transform.position, position) <= radiusSqr)
                    return true;
            return false;
        }

        // ICombatActorState: the voting block reduces every hit; surrendered members are left alone.
        public float IncomingDamageMultiplier => MajelisEncounter.IncomingMultiplier(blockShield.Value);
        public bool IsTargetable => !surrendered.Value && !sealedOff.Value;
        public bool SealedOff => sealedOff.Value;

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
            if (!active.Contains(this))
                active.Add(this);
            cachedCamera = Camera.main;
            RefreshPresentation(true);
            RefreshTelegraph();
        }

        public override void OnNetworkDespawn()
        {
            active.Remove(this);
            base.OnNetworkDespawn();
        }


        // Called by the director right after Spawn().
        public void ServerInitialize(UnitRole unitRole, FactionId unitFaction, Vector3 guardAnchor)
        {
            ServerInitialize(unitRole, unitFaction, guardAnchor, guardAnchor, 0f);
        }

        // areaCenter/areaRadius: heroes outside this circle are ignored (members hold their hall).
        public void ServerInitialize(UnitRole unitRole, FactionId unitFaction, Vector3 home,
            Vector3 areaCenter, float areaRadius)
        {
            if (!IsServer)
                return;

            role.Value = (int)unitRole;
            faction.Value = (int)unitFaction;
            anchor = home;
            leashCenter = areaCenter;
            leashRadius = Mathf.Max(0f, areaRadius);
            despawnAt = -1f;
            blockShield.Value = false;
            surrendered.Value = false;
            sealedOff.Value = false;
            telegraphing.Value = false;
            combat ??= GetComponent<NetworkPlayerCombat>();
            combat.ServerConfigureWibawa(UnitRoleStats.For(unitRole).Wibawa);
            nextAttackTime = Time.time + CampaignTuning.Encounters.EnemyFirstAttackDelaySeconds;
            RefreshPresentation(true);
        }

        // §8.1 KETOK PALU. Only leaders own a telegraphed special (§7).
        public void ServerConfigureKetokPalu()
        {
            if (!IsServer)
                return;

            hasSpecial = true;
            specialKind.Value = (int)EnemySpecial.KetokPalu;
            specialRadius = CampaignTuning.Majelis.KetokPaluRadius;
            specialForward = CampaignTuning.Majelis.KetokPaluForwardOffset;
            specialTelegraph = CampaignTuning.Majelis.KetokPaluTelegraphSeconds;
            specialDamage = CampaignTuning.Majelis.KetokPaluDamage;
            specialCooldown = CampaignTuning.Majelis.KetokPaluCooldownSeconds;
            specialTriggerRange = CampaignTuning.Majelis.KetokPaluTriggerRange;
            telegraphRadius.Value = specialRadius;
            nextSpecialTime = Time.time + CampaignTuning.Majelis.KetokPaluFirstDelaySeconds;
        }

        // §8.2 SALAH LOKET: a circle on the hero; whoever is still inside is sent to the
        // loket farthest from them. Only after the office door opens (the director calls this).
        public void ServerConfigureSalahLoket(Vector3[] lokets)
        {
            if (!IsServer || lokets == null || lokets.Length == 0)
                return;

            hasSpecial = true;
            specialKind.Value = (int)EnemySpecial.SalahLoket;
            salahLoketTargets = (Vector3[])lokets.Clone();
            specialRadius = CampaignTuning.Biro.SalahLoketRadius;
            specialForward = 0f;
            specialTelegraph = CampaignTuning.Biro.SalahLoketTelegraphSeconds;
            specialDamage = 0;
            specialCooldown = CampaignTuning.Biro.SalahLoketCooldownSeconds;
            specialTriggerRange = CampaignTuning.Biro.SalahLoketTriggerRange;
            telegraphRadius.Value = specialRadius;
            nextSpecialTime = Time.time + CampaignTuning.Biro.SalahLoketFirstDelaySeconds;
        }

        public void ServerSetSealed(bool value)
        {
            if (IsServer && sealedOff.Value != value)
                sealedOff.Value = value;
        }

        // Changes the area this member defends (Kepala Biro leaves his office when the door opens).
        public void ServerSetLeash(Vector3 center, float radius)
        {
            if (!IsServer)
                return;
            leashCenter = center;
            leashRadius = Mathf.Max(0f, radius);
        }

        // Voting block flag set by the director every frame (cheap: only writes on change).
        public void ServerSetBlock(bool active)
        {
            if (IsServer && blockShield.Value != active)
                blockShield.Value = active;
        }

        // The Ketua fell: this member stops fighting, kneels and leaves after a moment.
        public void ServerSurrender()
        {
            if (!IsServer || surrendered.Value || IsDown)
                return;

            surrendered.Value = true;
            blockShield.Value = false;
            CancelTelegraph(false);
            despawnAt = Time.time + CampaignTuning.Majelis.SurrenderVanishSeconds;
        }

        // §6: after a hero collapses, wounded enemies recover part of their missing Wibawa.
        public void ServerRecover(float missingFraction)
        {
            if (!IsServer || combat == null || combat.IsKnockedOut || surrendered.Value)
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
                // A knockout interrupts the hammer. Enemies do not respawn
                // (ServerOnActorKnockedOut returned false).
                CancelTelegraph(false);
                if (despawnAt < 0f)
                    despawnAt = Time.time + CampaignTuning.Encounters.EnemyDespawnSeconds;
                else if (Time.time >= despawnAt)
                    NetworkObject.Despawn(true);
                return;
            }

            if (surrendered.Value)
            {
                if (Time.time >= despawnAt)
                    NetworkObject.Despawn(true);
                return;
            }

            ICombatRules rules = CombatRules.Current;
            if (rules == null || !rules.AllowsGameplay)
                return;

            if (heroKit != null && heroKit.IsStunned)
            {
                // Stunning the Ketua during the warning cancels KETOK PALU.
                CancelTelegraph(true);
                return;
            }

            float deltaTime = Mathf.Min(Time.deltaTime, 0.05f);
            if (deltaTime <= 0f)
                return;

            UnitRoleStats stats = UnitRoleStats.For(Role);

            if (telegraphing.Value)
            {
                // Planted during the warning so the circle stays honest.
                if (Time.time >= telegraphEndsAt)
                    ResolveSpecial();
                Step(transform.position, stats, deltaTime);
                return;
            }

            NetworkPlayerCombat target = FindTarget(stats);
            Vector3 destination = anchor;
            bool move = false;

            if (target != null)
            {
                Vector3 toTarget = target.transform.position - transform.position;
                toTarget.y = 0f;

                if (hasSpecial && Time.time >= nextSpecialTime &&
                    toTarget.sqrMagnitude <= specialTriggerRange * specialTriggerRange)
                {
                    BeginTelegraph(toTarget);
                    Step(transform.position, stats, deltaTime);
                    return;
                }

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

        private void BeginTelegraph(Vector3 toTarget)
        {
            Vector3 forward = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : transform.forward;
            forward.y = 0f;
            transform.rotation = Quaternion.LookRotation(forward.normalized);
            // SALAH LOKET targets the hero's spot; KETOK PALU lands in front of the Ketua.
            telegraphCenter.Value = Special == EnemySpecial.SalahLoket
                ? transform.position + toTarget
                : transform.position + forward.normalized * specialForward;
            telegraphEndsAt = Time.time + specialTelegraph;
            telegraphing.Value = true;
        }

        private void ResolveSpecial()
        {
            telegraphing.Value = false;
            nextSpecialTime = Time.time + specialCooldown;
            nextAttackTime = Time.time + CampaignTuning.Encounters.EnemyAttackIntervalSeconds;

            if (NetworkManager == null || NetworkManager.SpawnManager == null)
                return;

            Vector3 center = telegraphCenter.Value;
            float radiusSqr = specialRadius * specialRadius;
            specialVictims.Clear();
            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (!NetworkTeamUtility.IsCombatActor(networkObject) ||
                    NetworkTeamUtility.IsAiActor(networkObject) ||
                    NetworkTeamUtility.GetTeam(networkObject) == Team)
                    continue;

                NetworkPlayerCombat candidate = networkObject.GetComponent<NetworkPlayerCombat>();
                if (candidate != null && !candidate.IsKnockedOut &&
                    HorizontalDistanceSqr(candidate.transform.position, center) <= radiusSqr)
                    specialVictims.Add(candidate);
            }

            // Applied after the scan: a knockout may change the spawned object list.
            foreach (NetworkPlayerCombat victim in specialVictims)
            {
                if (victim == null)
                    continue;
                if (Special == EnemySpecial.SalahLoket)
                    SendToWrongLoket(victim);
                else if (specialDamage > 0)
                    victim.ServerReceiveDamage(specialDamage, NetworkMatchManager.NoClient, NetworkObjectId);
            }
            specialVictims.Clear();
        }

        private void SendToWrongLoket(NetworkPlayerCombat victim)
        {
            if (salahLoketTargets == null || salahLoketTargets.Length == 0)
                return;

            Vector3 from = victim.transform.position;
            Vector3 best = salahLoketTargets[0];
            float bestDistance = -1f;
            foreach (Vector3 loket in salahLoketTargets)
            {
                float distance = HorizontalDistanceSqr(loket, from);
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best = loket;
                }
            }

            NetworkHeroKit kit = victim.GetComponent<NetworkHeroKit>();
            if (kit != null)
                kit.ServerTeleport(new Vector3(best.x, Mathf.Max(best.y, 0f) + CampaignStage.DropHeight, best.z));
        }

        private void CancelTelegraph(bool restartCooldown)
        {
            if (!telegraphing.Value)
                return;

            telegraphing.Value = false;
            if (restartCooldown)
                nextSpecialTime = Time.time + specialCooldown;
        }

        private NetworkPlayerCombat FindTarget(UnitRoleStats stats)
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
                return null;

            float aggro = CampaignTuning.Encounters.EnemyAggroRadius;
            float bestDistance = aggro * aggro;
            NetworkPlayerCombat best = null;

            // Majelis members hold their hall; guards hold their post.
            bool leashed = leashRadius > 0f || stats.HasGuardRadius;
            Vector3 center = leashRadius > 0f ? leashCenter : anchor;
            float radius = leashRadius > 0f ? leashRadius : stats.GuardRadius;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (!NetworkTeamUtility.IsCombatActor(networkObject) ||
                    NetworkTeamUtility.IsAiActor(networkObject) ||
                    NetworkTeamUtility.GetTeam(networkObject) == Team)
                    continue;

                NetworkPlayerCombat candidate = networkObject.GetComponent<NetworkPlayerCombat>();
                if (candidate == null || candidate.IsKnockedOut)
                    continue;

                if (leashed && HorizontalDistanceSqr(candidate.transform.position, center) > radius * radius)
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

            // Crowd pacing: wait for a turn if another member just hit this hero.
            ulong targetId = target.NetworkObjectId;
            if (nextHitOnTarget.TryGetValue(targetId, out float slot) && Time.time < slot)
                return;
            nextHitOnTarget[targetId] = Time.time + CampaignTuning.Encounters.TargetHitSpacingSeconds;

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
                if (stage == null)
                    stage = FindFirstObjectByType<CampaignStage>();
                if (stage != null)
                    destination = stage.Steer(transform.position, destination);
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
            RefreshTelegraph();

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

        // Every peer: ground circle and raised gavel while KETOK PALU is being telegraphed.
        private void RefreshTelegraph()
        {
            bool active = IsSpawned && telegraphing.Value && !IsDown;
            if (active && localTelegraphStart < 0f)
                localTelegraphStart = Time.time;
            else if (!active)
                localTelegraphStart = -1f;

            float progress = active
                ? Mathf.Clamp01((Time.time - localTelegraphStart) /
                    Mathf.Max(0.05f, Special == EnemySpecial.SalahLoket
                        ? CampaignTuning.Biro.SalahLoketTelegraphSeconds
                        : CampaignTuning.Majelis.KetokPaluTelegraphSeconds))
                : 0f;

            if (telegraphOuter != null)
            {
                if (telegraphOuter.gameObject.activeSelf != active)
                    telegraphOuter.gameObject.SetActive(active);
                if (active)
                {
                    float diameter = telegraphRadius.Value * 2f;
                    telegraphOuter.position = telegraphCenter.Value + Vector3.up * 0.09f;
                    telegraphOuter.rotation = Quaternion.identity;
                    telegraphOuter.localScale = new Vector3(diameter, 0.006f, diameter);
                }
            }

            if (telegraphFill != null)
            {
                if (telegraphFill.gameObject.activeSelf != active)
                    telegraphFill.gameObject.SetActive(active);
                if (active)
                {
                    float diameter = telegraphRadius.Value * 2f * Mathf.Max(0.05f, progress);
                    telegraphFill.position = telegraphCenter.Value + Vector3.up * 0.11f;
                    telegraphFill.rotation = Quaternion.identity;
                    telegraphFill.localScale = new Vector3(diameter, 0.006f, diameter);
                }
            }

            if (gavelPivot != null && gavel != null && gavel.activeSelf)
                gavelPivot.localRotation = Quaternion.Euler(active ? -150f * progress : 0f, 0f, 0f);
        }

        private void RefreshPresentation(bool force)
        {
            int currentRole = role.Value;
            int currentFaction = faction.Value;
            bool down = IsDown;
            bool shielded = blockShield.Value && !down;
            bool kneeling = surrendered.Value;
            bool hammer = telegraphing.Value && !down;
            bool locked = sealedOff.Value && !down;

            if (!force && currentRole == appliedRole && currentFaction == appliedFaction && down == appliedDown &&
                shielded == appliedBlock && kneeling == appliedSurrender && hammer == appliedTelegraph &&
                locked == appliedSealed)
                return;

            appliedSealed = locked;
            appliedRole = currentRole;
            appliedFaction = currentFaction;
            appliedDown = down;
            appliedBlock = shielded;
            appliedSurrender = kneeling;
            appliedTelegraph = hammer;

            UnitRole unitRole = (UnitRole)currentRole;
            FactionId unitFaction = (FactionId)currentFaction;
            FactionDefinition definition = FactionDefinition.Get(unitFaction);

            if (visualRoot != null)
            {
                visualRoot.localScale = Vector3.one * RoleScale(unitRole);
                // Kneeling: body lowered and bowed forward.
                visualRoot.localPosition = kneeling ? new Vector3(0f, -0.45f, 0f) : Vector3.zero;
                visualRoot.localRotation = kneeling ? Quaternion.Euler(22f, 0f, 0f) : Quaternion.identity;
            }

            if (blockRing != null && blockRing.activeSelf != shielded)
                blockRing.SetActive(shielded);
            if (stempelAura != null)
            {
                bool stamping = unitRole == UnitRole.Spesialis && unitFaction == FactionId.BiroProsedur && !down && !kneeling;
                if (stempelAura.activeSelf != stamping)
                    stempelAura.SetActive(stamping);
            }
            if (gavel != null)
            {
                bool showGavel = unitRole == UnitRole.Pemimpin && unitFaction == FactionId.MajelisDaun && !kneeling;
                if (gavel.activeSelf != showGavel)
                    gavel.SetActive(showGavel);
            }

            Color primary = ToColor(definition.PrimaryColor);
            Color accent = ToColor(definition.AccentColor);
            if (down)
            {
                primary = Color.Lerp(primary, Color.black, 0.70f);
                accent = Color.Lerp(accent, Color.black, 0.70f);
            }
            else if (kneeling)
            {
                Color grey = new Color(0.55f, 0.55f, 0.55f);
                primary = Color.Lerp(primary, grey, 0.60f);
                accent = Color.Lerp(accent, grey, 0.60f);
            }

            block ??= new MaterialPropertyBlock();
            SetColor(bodyRenderer, primary);
            SetColor(accentRenderer, accent);

            if (nameplate != null)
            {
                string title = EncounterComposer.TitleFor(unitFaction, unitRole).ToUpperInvariant();
                string state = down ? "TUMBANG"
                    : kneeling ? "MENYERAH"
                    : hammer ? (Special == EnemySpecial.SalahLoket ? "SALAH LOKET!" : "KETOK PALU!")
                    : locked ? "TERKUNCI"
                    : shielded ? definition.DisplayName + "  •  BLOK"
                    : definition.DisplayName;
                nameplate.text = title + "\n" + state;
                nameplate.color = down || kneeling
                    ? new Color(0.62f, 0.62f, 0.62f)
                    : hammer
                        ? new Color(1f, 0.30f, 0.22f)
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
