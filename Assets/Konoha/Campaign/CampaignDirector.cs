using System.Collections.Generic;
using Konoha.Networking;
using Unity.Netcode;
using UnityEngine;

namespace Konoha.Campaign
{
    // Host-authoritative owner of a Jalur Takhta run (Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md §14).
    // The server runs CampaignObjectiveDirector, places the real enemies and answers the
    // shared hero combat rules (ICombatRules). Every peer reads the replicated summary
    // below for the HUD and for respawn positions, so co-op clients can join later
    // without a second source of truth. Solo is simply a host with one player.
    public sealed class CampaignDirector : NetworkBehaviour, ICombatRules
    {
        public static CampaignDirector Instance { get; private set; }

        public GameObject enemyPrefab;

        private NetworkVariable<int> phase = new NetworkVariable<int>(
            (int)CampaignPhase.GerbangRakyat, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> sealMask = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> power = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> checkpoint = new NetworkVariable<int>(
            (int)CampaignCheckpoint.GerbangRakyat, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> runtuhCount = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // 0.0.9.2.1: the hero is chosen before the run and locked until ULANG.
        private NetworkVariable<bool> heroLocked = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // RESTU RAKYAT blessing earned at the Gerbang Rakyat.
        private NetworkVariable<bool> restu = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> majelisStarted = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> majelisBlock = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> majelisLeaderDown = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> majelisRemaining = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> majelisTotal = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> seniorsAlive = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // Biro Prosedur loket hall (0.0.9.3).
        private NetworkVariable<bool> biroStarted = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> loketProgress0 = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> loketProgress1 = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> loketProgress2 = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> loketStampedMask = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> loketContestedMask = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> biroDoorOpen = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> biroLeaderDown = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> gateCleared = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> gateRemaining = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> gardaRemaining = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> counterRemaining = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // 0.1.0 Garda Takhta, Fase Memerintah and result screen.
        private NetworkVariable<int> gardaTotal = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> panglimaFraction = new NetworkVariable<float>(
            1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> gardaLockdown = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> reignStarted = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> reignRuntuh = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> counterWaves = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<ulong> seatedObjectId = new NetworkVariable<ulong>(
            ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<double> runStartedAt = new NetworkVariable<double>(
            0d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<double> runEndedAt = new NetworkVariable<double>(
            -1d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // 0.4.0 Musim Pemilu: political resources and the LAWAN / RANGKUL offer.
        private NetworkVariable<int> modal = new NetworkVariable<int>(
            CampaignTuning.Politik.ModalStart, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> jatah = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> restuScore = new NetworkVariable<int>(
            CampaignTuning.Politik.RestuStart, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> offerSector = new NetworkVariable<int>(
            -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // Majelis path in bits 0–1, Biro path in bits 2–3 (SectorPath values).
        private NetworkVariable<int> pathBits = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> tookLoan = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // 0.5.0 BLUSUKAN progress.
        private NetworkVariable<int> blusukanMask = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> blusukanCurrent = new NetworkVariable<int>(
            -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> blusukanProgress = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly HashSet<ulong> countedDefeats = new HashSet<ulong>();
        private bool majelisBought;
        private bool biroBought;

        private readonly List<CampaignEnemy> gateEnemies = new List<CampaignEnemy>();
        private readonly List<CampaignEnemy> gardaEnemies = new List<CampaignEnemy>();
        private readonly List<CampaignEnemy> majelisEnemies = new List<CampaignEnemy>();
        private readonly MajelisEncounter majelis = new MajelisEncounter();
        private readonly List<CampaignEnemy> biroEnemies = new List<CampaignEnemy>();
        private readonly BiroEncounter biro = new BiroEncounter();
        private CampaignEnemy biroLeader;
        private bool biroSpawned;
        private bool biroPending;
        private bool biroContestedAnnounced;
        private int nextArsipPoint;
        private readonly List<CampaignEnemy> counterEnemies = new List<CampaignEnemy>();

        private CampaignStage stage;
        private CampaignObjectiveDirector objectives;
        private bool gateSpawned;
        private bool gardaSpawned;
        private readonly GardaEncounter garda = new GardaEncounter();
        private CampaignEnemy panglima;
        private int counterFaction;
        // Out-of-combat Wibawa recovery per hero (NetworkObjectId).
        private readonly Dictionary<ulong, int> lastWibawa = new Dictionary<ulong, int>();
        private readonly Dictionary<ulong, float> lastHurtAt = new Dictionary<ulong, float>();
        private readonly Dictionary<ulong, float> recoveryCarry = new Dictionary<ulong, float>();
        private readonly List<NetworkObject> heroScratch = new List<NetworkObject>();
        private bool majelisSpawned;
        private bool majelisPending;
        // Enemies are spawned from Update, never from inside another object's OnNetworkSpawn.
        private bool gatePending;

        // 0.6.0 KARIER Level 1 (solo host only): no Jalur Takhta route; preman encounters are
        // requested by KarierController. Set by CampaignSession before Spawn().
        private bool karierMode;
        private readonly List<CampaignEnemy> karierEnemies = new List<CampaignEnemy>();
        private float karierCalmUntil;
        private bool karierClearPending;

        // Latest player-facing message on this peer (set by NotifyClientRpc).
        public string LastMessage { get; private set; } = string.Empty;
        public float LastMessageUntil { get; private set; }

        public CampaignPhase Phase => (CampaignPhase)phase.Value;
        public int Power => power.Value;
        public int TargetPower => CampaignTuning.Memerintah.TargetPower;
        public int RequiredSeals => CampaignTuning.PreviewSlice.RequiredSeals;
        public int RuntuhCount => runtuhCount.Value;
        public bool HeroLocked => heroLocked.Value;
        public bool RestuActive => restu.Value;

        // Majelis Daun sidang (0.0.9.2).
        public bool MajelisStarted => majelisStarted.Value;
        public bool MajelisBlock => majelisBlock.Value;
        public bool MajelisLeaderDown => majelisLeaderDown.Value;
        public int MajelisRemaining => majelisRemaining.Value;
        public int MajelisTotal => majelisTotal.Value;
        public int SeniorsAlive => seniorsAlive.Value;
        // Biro Prosedur (0.0.9.3).
        public bool BiroStarted => biroStarted.Value;
        public bool BiroDoorOpen => biroDoorOpen.Value;
        public bool BiroLeaderDown => biroLeaderDown.Value;
        public int LoketCount => CampaignTuning.Biro.LoketCount;
        public bool LoketStamped(int loket) => (loketStampedMask.Value & (1 << loket)) != 0;
        public bool LoketContested(int loket) => (loketContestedMask.Value & (1 << loket)) != 0;
        public float LoketProgress(int loket) =>
            loket == 0 ? loketProgress0.Value : loket == 1 ? loketProgress1.Value : loket == 2 ? loketProgress2.Value : 0f;

        public int LoketStampedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < LoketCount; i++)
                    if (LoketStamped(i)) count++;
                return count;
            }
        }
        public bool GateCleared => gateCleared.Value;
        public int GateRemaining => gateRemaining.Value;
        public int GardaRemaining => gardaRemaining.Value;
        public int CounterRemaining => counterRemaining.Value;
        public int GardaTotal => gardaTotal.Value;
        public float PanglimaFraction => panglimaFraction.Value;
        public bool GardaLockdown => gardaLockdown.Value;
        public bool ReignStarted => reignStarted.Value;
        public int ReignRuntuh => reignRuntuh.Value;
        public int CounterWaves => counterWaves.Value;
        public ulong SeatedObjectId => seatedObjectId.Value;
        // Seconds since MULAI; frozen at the victory moment (result screen).
        public double RunSeconds
        {
            get
            {
                double now = NetworkManager != null ? NetworkManager.ServerTime.Time : 0d;
                double end = runEndedAt.Value >= 0d ? runEndedAt.Value : now;
                return HeroLocked ? System.Math.Max(0d, end - runStartedAt.Value) : 0d;
            }
        }
        public double RunStartedAt => runStartedAt.Value;
        public CampaignCheckpoint Checkpoint => (CampaignCheckpoint)checkpoint.Value;

        public int SealCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < CampaignTuning.Seals.SectorCount; i++)
                    if ((sealMask.Value & (1 << i)) != 0) count++;
                return count;
            }
        }

        public bool HasSeal(CampaignSector sector) => (sealMask.Value & (1 << (int)sector)) != 0;

        // 0.4.0 Musim Pemilu (read by the HUD, the LAWAN / RANGKUL panel and the Koran).
        public int BlusukanCount => stage != null ? stage.blusukanPoints.Length : 0;
        public bool BlusukanGreeted(int index) => (blusukanMask.Value & (1 << index)) != 0;
        public int BlusukanCurrent => blusukanCurrent.Value;
        public float BlusukanProgress => blusukanProgress.Value;
        public int SuaraCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < BlusukanCount; i++)
                    if (BlusukanGreeted(i)) count++;
                return count;
            }
        }
        public bool BlusukanDone => SuaraCount >= BlusukanCount;
        public int Modal => modal.Value;
        public int Jatah => jatah.Value;
        public int Restu => restuScore.Value;
        public bool TookLoan => tookLoan.Value;
        public int OfferSector => offerSector.Value;
        public SectorPath Path(CampaignSector sector) =>
            sector == CampaignSector.MajelisDaun ? (SectorPath)(pathBits.Value & 3)
            : sector == CampaignSector.BiroProsedur ? (SectorPath)((pathBits.Value >> 2) & 3)
            : SectorPath.Belum;
        public int RangkulCount =>
            (Path(CampaignSector.MajelisDaun) == SectorPath.Dirangkul ? 1 : 0) +
            (Path(CampaignSector.BiroProsedur) == SectorPath.Dirangkul ? 1 : 0);
        public CampaignEnding Ending => CampaignRunState.EndingFor(RangkulCount, Jatah);
        public bool KarierMode => karierMode;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Instance = this;
            CombatRules.Register(this);
            stage = FindFirstObjectByType<CampaignStage>();

            if (!IsServer)
                return;

            if (stage == null)
            {
                Debug.LogError("[KONOHA CAMPAIGN] CampaignStage missing from the scene.");
                return;
            }

            objectives = new CampaignObjectiveDirector(stage.Layout);
            objectives.Notified += OnObjectiveMessage;
            objectives.MajelisRequested += RequestMajelis;
            objectives.BiroRequested += RequestBiro;
            objectives.GardaRequested += SpawnGarda;
            objectives.CounterattackRequested += SpawnCounterattack;
            objectives.Won += OnWon;
            objectives.Kudeta += OnKudeta;
            gatePending = !karierMode;
            SyncState();
            Debug.Log("[KONOHA CAMPAIGN] Director ready (host authority).");
        }

        public override void OnNetworkDespawn()
        {
            if (objectives != null)
            {
                objectives.Notified -= OnObjectiveMessage;
                objectives.MajelisRequested -= RequestMajelis;
                objectives.BiroRequested -= RequestBiro;
                objectives.GardaRequested -= SpawnGarda;
                objectives.CounterattackRequested -= SpawnCounterattack;
                objectives.Won -= OnWon;
                objectives.Kudeta -= OnKudeta;
            }

            if (Instance == this)
                Instance = null;

            CombatRules.Unregister(this);
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsServer || !IsSpawned || objectives == null)
                return;

            if (karierMode)
            {
                UpdateKarier();
                return;
            }

            if (gatePending)
            {
                gatePending = false;
                SpawnGate();
            }

            if (majelisPending)
            {
                majelisPending = false;
                SpawnMajelis();
            }

            if (biroPending)
            {
                biroPending = false;
                SpawnBiro();
            }

            // Hero selection screen: the world waits (AllowsGameplay is false) until MULAI.
            if (!heroLocked.Value)
            {
                SyncState();
                return;
            }

            float deltaTime = Mathf.Min(Time.deltaTime, CampaignTuning.PreviewSlice.MaxFrameSeconds);
            NetworkObject player = PrimaryPlayer();
            if (player != null)
            {
                NetworkPlayerCombat combat = player.GetComponent<NetworkPlayerCombat>();
                if (combat == null || !combat.IsKnockedOut)
                    objectives.Tick(player.transform.position, deltaTime);
            }

            if (gateSpawned && !objectives.GateCleared && CountAlive(gateEnemies) == 0)
            {
                objectives.MarkGateCleared();
                GrantRestu();
            }

            if (majelisSpawned && !majelisBought)
                UpdateMajelis();

            if (biroSpawned && !biroBought)
                UpdateBiro(player, deltaTime);

            CountDefeats();

            if (gardaSpawned)
                UpdateGarda(player);

            UpdateRecovery(deltaTime);

            SyncState();
        }

        // Solo: the host's hero drives the objective checks. Co-op will need a per-player
        // rule (e.g. any player inside the ring); kept in one place for that change.
        private NetworkObject PrimaryPlayer()
        {
            if (NetworkManager == null)
                return null;

            if (NetworkManager.LocalClient != null && NetworkManager.LocalClient.PlayerObject != null)
                return NetworkManager.LocalClient.PlayerObject;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
                if (networkObject != null && networkObject.IsPlayerObject)
                    return networkObject;

            return null;
        }

        private void SyncState()
        {
            CampaignRunState run = objectives.Run;
            phase.Value = (int)run.Phase;
            power.Value = run.Power;
            checkpoint.Value = (int)run.Checkpoint;
            runtuhCount.Value = run.RuntuhCount;
            majelisStarted.Value = objectives.MajelisEngaged;
            majelisBlock.Value = majelisSpawned && !majelisBought && majelis.BlockHolds && !majelis.Cleared;
            majelisLeaderDown.Value = majelis.LeaderFallen;
            majelisRemaining.Value = CountAlive(majelisEnemies);
            majelisTotal.Value = majelisEnemies.Count;
            seniorsAlive.Value = CountAlive(majelisEnemies, UnitRole.Senior);
            biroStarted.Value = objectives.BiroEngaged;
            loketProgress0.Value = biro.LoketCount > 0 ? biro.Progress(0) : 0f;
            loketProgress1.Value = biro.LoketCount > 1 ? biro.Progress(1) : 0f;
            loketProgress2.Value = biro.LoketCount > 2 ? biro.Progress(2) : 0f;
            int stampedMask = 0;
            for (int i = 0; i < biro.LoketCount; i++)
                if (biro.IsStamped(i)) stampedMask |= 1 << i;
            loketStampedMask.Value = stampedMask;
            biroLeaderDown.Value = biro.LeaderFallen;
            gateCleared.Value = objectives.GateCleared;
            gateRemaining.Value = CountAlive(gateEnemies);
            gardaRemaining.Value = CountAlive(gardaEnemies);
            gardaTotal.Value = gardaEnemies.Count;
            reignStarted.Value = objectives.ReignStarted;
            reignRuntuh.Value = objectives.ReignRuntuh;
            counterWaves.Value = objectives.CounterattackWaves;
            NetworkObject seated = run.Phase == CampaignPhase.Memerintah ? PrimaryPlayer() : null;
            seatedObjectId.Value = seated != null ? seated.NetworkObjectId : ulong.MaxValue;
            counterRemaining.Value = CountAlive(counterEnemies);

            blusukanMask.Value = objectives.BlusukanMask;
            blusukanCurrent.Value = objectives.BlusukanCurrent;
            blusukanProgress.Value = objectives.BlusukanProgress;
            modal.Value = run.Modal;
            jatah.Value = run.Jatah;
            restuScore.Value = run.Restu;
            tookLoan.Value = run.TookLoan;
            offerSector.Value = objectives.OfferSector;
            pathBits.Value = (int)run.GetPath(CampaignSector.MajelisDaun) | ((int)run.GetPath(CampaignSector.BiroProsedur) << 2);

            int mask = 0;
            for (int i = 0; i < CampaignTuning.Seals.SectorCount; i++)
                if (run.HasSeal((CampaignSector)i)) mask |= 1 << i;
            sealMask.Value = mask;
        }

        // --- 0.6.0 KARIER (host only) ------------------------------------------------

        // Before Spawn(): this director runs KARIER instead of the Jalur Takhta route.
        public void ConfigureKarier(bool value)
        {
            if (!IsSpawned)
                karierMode = value;
        }

        // MULAI HIDUP on the character creator: the world starts moving.
        public void ServerKarierStart()
        {
            if (!IsServer || !karierMode || heroLocked.Value)
                return;
            heroLocked.Value = true;
            runStartedAt.Value = NetworkManager.ServerTime.Time;
            runEndedAt.Value = -1d;
        }

        private void UpdateKarier()
        {
            karierEnemies.RemoveAll(enemy => enemy == null || !enemy.IsSpawned);
            if (karierClearPending)
            {
                karierClearPending = false;
                ServerKarierClear(false);
            }
            if (!heroLocked.Value)
                return;
            if (karierCalmUntil > 0f && Time.time >= karierCalmUntil)
            {
                karierCalmUntil = 0f;
                foreach (CampaignEnemy enemy in karierEnemies)
                    if (enemy != null && enemy.IsSpawned)
                        enemy.ServerSetCalmed(false);
            }
            UpdateRecovery(Mathf.Min(Time.deltaTime, CampaignTuning.PreviewSlice.MaxFrameSeconds));
        }

        // Preman memalak warga: one preman, or a preman and his boss. Returns how many stand.
        public int ServerKarierSpawnPreman(Vector3 center, Vector3[] points, Vector3 lookAt, bool withBoss)
        {
            if (!IsServer || !karierMode || points == null)
                return 0;
            int wanted = withBoss ? 2 : 1;
            int spawned = 0;
            for (int i = 0; i < points.Length && i < wanted; i++)
            {
                bool boss = withBoss && i == 1;
                CampaignEnemy enemy = SpawnEnemy(boss ? UnitRole.Guard : UnitRole.Kroni, FactionId.GardaTakhta,
                    points[i], points[i], lookAt, center, CampaignTuning.Karier.PremanLeashRadius);
                if (enemy == null)
                    continue;
                enemy.ServerSetKarier(boss ? 2 : 1);
                NetworkPlayerCombat combat = enemy.GetComponent<NetworkPlayerCombat>();
                if (combat != null)
                    combat.ServerConfigureWibawa(boss ? CampaignTuning.Karier.BosPremanWibawa : CampaignTuning.Karier.PremanWibawa);
                karierEnemies.Add(enemy);
                spawned++;
            }
            return spawned;
        }

        // A warga melerai: every standing preman holds still for a moment.
        public void ServerKarierCalm(float seconds)
        {
            if (!IsServer || !karierMode)
                return;
            karierCalmUntil = Time.time + Mathf.Max(0.1f, seconds);
            foreach (CampaignEnemy enemy in karierEnemies)
                if (enemy != null && enemy.IsSpawned && !enemy.IsOutOfFight)
                    enemy.ServerSetCalmed(true);
        }

        // kneel: the police take them (DIAMANKAN); otherwise they simply walk off.
        public void ServerKarierClear(bool kneel)
        {
            if (!IsServer || !karierMode)
                return;
            karierCalmUntil = 0f;
            foreach (CampaignEnemy enemy in karierEnemies)
            {
                if (enemy == null || !enemy.IsSpawned || enemy.IsDown)
                    continue;
                enemy.ServerSetCalmed(false);
                if (kneel)
                    enemy.ServerSurrender();
                else if (!enemy.Surrendered)
                    enemy.NetworkObject.Despawn(true);
            }
            karierEnemies.RemoveAll(enemy => enemy == null || !enemy.IsSpawned);
        }

        public void ServerKarierTeleportHero(Vector3 position)
        {
            if (!IsServer || !karierMode)
                return;
            NetworkObject player = PrimaryPlayer();
            NetworkHeroKit kit = player != null ? player.GetComponent<NetworkHeroKit>() : null;
            if (kit != null)
                kit.ServerTeleport(position);
        }

        // --- Encounters -------------------------------------------------------------

        private void SpawnGate()
        {
            gateEnemies.Clear();
            int count = Mathf.Min(CampaignTuning.Encounters.GateKroni, stage.gateSpawnPoints.Length);
            for (int i = 0; i < count; i++)
            {
                Vector3 point = stage.gateSpawnPoints[i];
                AddIfSpawned(gateEnemies, SpawnEnemy(UnitRole.Kroni, FactionId.GardaTakhta, point, point,
                    stage.startPoint));
            }
            gateSpawned = true;
        }

        // Majelis Daun (§8.1): placed when the plaza is reached (deferred to Update).
        private void RequestMajelis()
        {
            if (!majelisSpawned)
                majelisPending = true;
        }

        private void SpawnMajelis()
        {
            DespawnAll(majelisEnemies);
            majelis.Reset();

            Vector3 hall = stage.majelis.position;
            int point = 0;
            foreach (UnitSpawn spawn in EncounterComposer.Compose(FactionId.MajelisDaun, 1))
            {
                if (spawn.Trigger != SpawnTrigger.Start)
                    continue;
                for (int i = 0; i < spawn.Count; i++)
                {
                    if (point >= stage.majelisSpawnPoints.Length)
                    {
                        Debug.LogWarning("[KONOHA CAMPAIGN] Not enough Majelis spawn points for the roster.");
                        break;
                    }
                    Vector3 position = stage.majelisSpawnPoints[point++];
                    CampaignEnemy enemy = SpawnEnemy(spawn.Role, FactionId.MajelisDaun, position, position,
                        stage.plaza.position, hall, CampaignTuning.Majelis.LeashRadius);
                    if (enemy == null)
                        continue;
                    if (spawn.Role == UnitRole.Pemimpin)
                        enemy.ServerConfigureKetokPalu();
                    majelisEnemies.Add(enemy);
                }
            }

            majelisSpawned = true;
            UpdateMajelis();
        }

        private void UpdateMajelis()
        {
            if (majelis.Cleared)
                return;

            int seniors = CountAlive(majelisEnemies, UnitRole.Senior);
            bool leaderAlive = CountAlive(majelisEnemies, UnitRole.Pemimpin) > 0;
            int officers = seniors + CountAlive(majelisEnemies, UnitRole.Guard);
            MajelisChange change = majelis.Evaluate(seniors, leaderAlive, officers);

            if ((change & MajelisChange.BlockBroken) != 0)
                OnObjectiveMessage("SOLIDARITAS ELITE PECAH!  Serangan kini masuk penuh");

            if ((change & MajelisChange.LeaderDown) != 0)
            {
                foreach (CampaignEnemy enemy in majelisEnemies)
                    if (enemy != null && enemy.IsSpawned && enemy.Role == UnitRole.Kroni)
                        enemy.ServerSurrender();
                if ((change & MajelisChange.Cleared) == 0)
                    OnObjectiveMessage("KETUM TUMBANG!  Kader menyerah, kalahkan sisa elite");
            }

            bool block = majelis.BlockHolds && !majelis.Cleared;
            foreach (CampaignEnemy enemy in majelisEnemies)
                if (enemy != null && enemy.IsSpawned)
                    enemy.ServerSetBlock(block && !enemy.IsOutOfFight);

            if ((change & MajelisChange.Cleared) != 0 && objectives.CompleteMajelis())
                GrantPlayersPengaruh(CampaignTuning.Majelis.SealPengaruh);
        }

        // RESTU RAKYAT: full Wibawa, a Pengaruh head start and the run-long damage blessing.
        private void GrantRestu()
        {
            if (restu.Value)
                return;

            restu.Value = true;
            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject == null || !networkObject.IsPlayerObject)
                    continue;
                NetworkPlayerCombat combat = networkObject.GetComponent<NetworkPlayerCombat>();
                if (combat != null && !combat.IsKnockedOut)
                    combat.ServerHeal(combat.MaxWibawaValue);
            }
            GrantPlayersPengaruh(CampaignTuning.Restu.PengaruhBonus);
            OnObjectiveMessage("PREMAN KABUR!  Warga memberi RESTU (damage +" +
                Mathf.RoundToInt((CampaignTuning.Restu.HeroDamageMultiplier - 1f) * 100f) + "%) dan sumbangan +" +
                CampaignTuning.Politik.ModalGateBonus + " MODAL.  Sekarang BLUSUKAN: sapa warga yang bercahaya");
        }

        private void GrantPlayersPengaruh(int amount)
        {
            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject == null || !networkObject.IsPlayerObject)
                    continue;
                NetworkHeroKit kit = networkObject.GetComponent<NetworkHeroKit>();
                if (kit != null)
                    kit.ServerGainPengaruh(amount);
            }
        }

        // Biro Prosedur (§8.2): the loket hall opens when the plaza is reached (deferred to Update).
        private void RequestBiro()
        {
            if (!biroSpawned)
                biroPending = true;
        }

        private void SpawnBiro()
        {
            DespawnAll(biroEnemies);
            biro.Reset();
            biroLeader = null;
            biroDoorOpen.Value = false;
            biroContestedAnnounced = false;
            nextArsipPoint = 0;

            Vector3 hall = stage.biro.position;
            float leash = CampaignTuning.Biro.LeashRadius;
            foreach (UnitSpawn spawn in EncounterComposer.Compose(FactionId.BiroProsedur, 1))
            {
                if (spawn.Trigger == SpawnTrigger.Reinforcement)
                    continue;
                for (int i = 0; i < spawn.Count; i++)
                {
                    CampaignEnemy enemy = null;
                    switch (spawn.Role)
                    {
                        case UnitRole.Pemimpin:
                            // Waits behind the office door; untouchable until it opens.
                            enemy = SpawnEnemy(spawn.Role, FactionId.BiroProsedur, stage.biroLeaderPoint,
                                stage.biroLeaderPoint, stage.plaza.position, stage.biroOfficeCenter,
                                CampaignTuning.Biro.LockedLeaderRadius);
                            if (enemy != null)
                            {
                                enemy.ServerSetSealed(true);
                                biroLeader = enemy;
                            }
                            break;
                        case UnitRole.Spesialis:
                            enemy = SpawnEnemy(spawn.Role, FactionId.BiroProsedur, stage.biroSpecialistPoint,
                                stage.biroSpecialistPoint, stage.plaza.position, hall, leash);
                            break;
                        case UnitRole.Kroni:
                            enemy = SpawnArsip();
                            break;
                        default:
                            enemy = SpawnEnemy(spawn.Role, FactionId.BiroProsedur, stage.biroGuardPoint,
                                stage.biroGuardPoint, stage.plaza.position, hall, leash);
                            break;
                    }
                    if (enemy != null && spawn.Role != UnitRole.Kroni)
                        biroEnemies.Add(enemy);
                }
            }

            biroSpawned = true;
        }

        // Petugas Arsip alternate between the arsip points at the back of the hall.
        private CampaignEnemy SpawnArsip()
        {
            if (stage.biroArsipPoints.Length == 0)
                return null;
            Vector3 point = stage.biroArsipPoints[nextArsipPoint++ % stage.biroArsipPoints.Length];
            CampaignEnemy enemy = SpawnEnemy(UnitRole.Kroni, FactionId.BiroProsedur, point, point,
                stage.plaza.position, stage.biro.position, CampaignTuning.Biro.LeashRadius);
            if (enemy != null)
                biroEnemies.Add(enemy);
            return enemy;
        }

        private void UpdateBiro(NetworkObject player, float deltaTime)
        {
            if (biro.Cleared)
                return;

            // Which loket the hero stands in, and which lokets have a Biro member queuing.
            int heroLoket = -1;
            if (player != null)
            {
                NetworkPlayerCombat heroCombat = player.GetComponent<NetworkPlayerCombat>();
                if (heroCombat == null || !heroCombat.IsKnockedOut)
                    heroLoket = stage.LoketAt(player.transform.position);
            }

            int contestedMask = 0;
            foreach (CampaignEnemy enemy in biroEnemies)
            {
                if (enemy == null || !enemy.IsSpawned || enemy.IsOutOfFight)
                    continue;
                int loket = stage.LoketAt(enemy.transform.position);
                if (loket >= 0)
                    contestedMask |= 1 << loket;
            }
            loketContestedMask.Value = contestedMask;

            bool contested = heroLoket >= 0 && (contestedMask & (1 << heroLoket)) != 0;
            if (contested && !biroContestedAnnounced && !biro.IsStamped(heroLoket))
                OnObjectiveMessage("DISEROBOT!  Usir penyerobot antrean di " + LoketName(heroLoket));
            biroContestedAnnounced = contested;

            BiroChange change = biro.TickLokets(heroLoket, contested, deltaTime);
            if ((change & BiroChange.AllStamped) != 0)
                OpenBiroOffice();
            else if ((change & BiroChange.LoketStamped) != 0)
                OnObjectiveMessage(LoketName(biro.LastStamped) + " BERES  (" + biro.StampedCount + "/" +
                    biro.LoketCount + ")");

            int arsipAlive = CountAlive(biroEnemies, UnitRole.Kroni);
            if (biro.TickArsip(arsipAlive, EncounterComposer.ArsipMaxAlive(1), deltaTime) > 0)
                SpawnArsip();

            bool leaderAlive = biroLeader != null && biroLeader.IsSpawned && !biroLeader.IsOutOfFight;
            BiroChange result = biro.Evaluate(leaderAlive);
            if ((result & BiroChange.Cleared) != 0)
            {
                foreach (CampaignEnemy enemy in biroEnemies)
                    if (enemy != null && enemy.IsSpawned && !enemy.IsOutOfFight)
                        enemy.ServerSurrender();
                if (objectives.CompleteBiro())
                    GrantPlayersPengaruh(CampaignTuning.Biro.SealPengaruh);
            }
        }

        // All lokets stamped: the Kepala Biro's door opens and he joins the fight.
        private void OpenBiroOffice()
        {
            biroDoorOpen.Value = true;
            if (biroLeader != null && biroLeader.IsSpawned && !biroLeader.IsOutOfFight)
            {
                biroLeader.ServerSetSealed(false);
                biroLeader.ServerSetLeash(stage.biro.position, CampaignTuning.Biro.LeashRadius);
                biroLeader.ServerConfigureSalahLoket(stage.loketPoints);
            }
            OnObjectiveMessage("3 LOKET BERES!  Pintu PAK LURAH terbuka, awas \"BALIK BESOK!\"");
        }

        // §8.3 Garda Takhta: Panglima + Pengawal on the parade ground (placed when the inner gate opens).
        private void SpawnGarda()
        {
            DespawnAll(gardaEnemies);
            garda.Reset();
            panglima = null;
            gardaLockdown.Value = false;

            Vector3 post = stage.garda.position;
            int point = 0;
            foreach (UnitSpawn spawn in EncounterComposer.Compose(FactionId.GardaTakhta, 1))
            {
                if (spawn.Trigger != SpawnTrigger.Start)
                    continue;
                for (int i = 0; i < spawn.Count && point < stage.gardaSpawnPoints.Length; i++)
                {
                    Vector3 position = stage.gardaSpawnPoints[point++];
                    CampaignEnemy enemy = SpawnEnemy(spawn.Role, FactionId.GardaTakhta, position, post,
                        stage.chair.position, post, CampaignTuning.Garda.LockdownRadius);
                    if (enemy == null)
                        continue;
                    if (spawn.Role == UnitRole.Pemimpin)
                    {
                        enemy.ServerConfigureCounterPush();
                        panglima = enemy;
                        WeakenPanglima(enemy);
                    }
                    gardaEnemies.Add(enemy);
                }
            }
            gardaSpawned = true;
        }

        // 0.4.0: every embraced institution has already "conditioned" the Panglima.
        private void WeakenPanglima(CampaignEnemy enemy)
        {
            int embraced = objectives.Run.RangkulCount;
            if (embraced <= 0)
                return;
            NetworkPlayerCombat combat = enemy.GetComponent<NetworkPlayerCombat>();
            if (combat == null)
                return;
            float factor = Mathf.Max(0.4f, 1f - CampaignTuning.Politik.PanglimaWeakenPerRangkul * embraced);
            combat.ServerConfigureWibawa(Mathf.RoundToInt(UnitRoleStats.For(UnitRole.Pemimpin).Wibawa * factor));
            OnObjectiveMessage("Koalisi sudah \"mengkondisikan\" PANGLIMA:  Wibawa -" +
                Mathf.RoundToInt((1f - factor) * 100f) + "%");
        }

        // 0.4.0: +Modal for every Sistem member knocked down (surrender does not pay).
        private void CountDefeats()
        {
            CountDefeats(gateEnemies);
            CountDefeats(majelisEnemies);
            CountDefeats(biroEnemies);
            CountDefeats(gardaEnemies);
            CountDefeats(counterEnemies);
        }

        private void CountDefeats(List<CampaignEnemy> list)
        {
            foreach (CampaignEnemy enemy in list)
                if (enemy != null && enemy.IsSpawned && enemy.IsDown && countedDefeats.Add(enemy.NetworkObjectId))
                    objectives.AwardDefeatModal();
        }

        private void UpdateGarda(NetworkObject player)
        {
            if (garda.LeaderFallen)
                return;

            bool leaderAlive = panglima != null && panglima.IsSpawned && !panglima.IsOutOfFight;
            panglimaFraction.Value = leaderAlive ? panglima.WibawaFraction : 0f;
            GardaChange change = garda.Evaluate(panglimaFraction.Value, leaderAlive);

            if ((change & GardaChange.Reinforce) != 0)
            {
                Vector3 post = stage.garda.position;
                foreach (Vector3 point in stage.gardaReinforcePoints)
                    AddIfSpawned(gardaEnemies, SpawnEnemy(UnitRole.Kroni, FactionId.GardaTakhta, point, post,
                        stage.chair.position, post, CampaignTuning.Garda.LockdownRadius));
                OnObjectiveMessage("BALA BANTUAN!  Panglima memanggil PREMAN BAYARAN");
            }

            if ((change & GardaChange.LeaderDown) != 0)
            {
                gardaLockdown.Value = false;
                foreach (CampaignEnemy enemy in gardaEnemies)
                    if (enemy != null && enemy.IsSpawned && !enemy.IsOutOfFight)
                        enemy.ServerSurrender();
                objectives.CompleteGarda();
                return;
            }

            // LOCKDOWN: closes once the hero is well inside the ring during the Garda fight.
            bool heroDown = false, heroInside = false;
            if (player != null)
            {
                NetworkPlayerCombat heroCombat = player.GetComponent<NetworkPlayerCombat>();
                heroDown = heroCombat != null && heroCombat.IsKnockedOut;
                heroInside = objectives.Run.Phase == CampaignPhase.GardaTakhta &&
                    CampaignObjectiveDirector.Near(player.transform.position, stage.garda.position,
                        CampaignTuning.Garda.LockdownCloseRadius);
            }
            // Never lock the hero away from the Panglima: the ring only holds while he is inside it.
            bool panglimaInside = CampaignObjectiveDirector.Near(panglima.transform.position, stage.garda.position,
                CampaignTuning.Garda.LockdownRadius - 0.8f);
            if (garda.UpdateLockdown(heroInside && panglimaInside, heroDown || !panglimaInside))
            {
                gardaLockdown.Value = garda.LockdownClosed;
                if (garda.LockdownClosed)
                {
                    panglima.ServerDelaySpecial(CampaignTuning.Garda.CounterPushFirstDelaySeconds);
                    OnObjectiveMessage("LOCKDOWN!  Tidak ada jalan keluar sampai PANGLIMA tumbang");
                }
            }
        }

        // §9 counterattack: 2 Kroni + 1 Pengawal from the remnants of a beaten faction.
        private void SpawnCounterattack()
        {
            if (CountAlive(counterEnemies) >= CampaignTuning.Garda.MaxCounterattackAlive)
                return;

            counterEnemies.RemoveAll(enemy => enemy == null || !enemy.IsSpawned);
            FactionId faction = counterFaction++ % 2 == 0 ? FactionId.MajelisDaun : FactionId.BiroProsedur;
            Vector3[] points = stage.counterattackSpawnPoints;
            if (points.Length == 0)
                return;

            for (int i = 0; i < CampaignTuning.Memerintah.CounterattackKroni; i++)
                AddIfSpawned(counterEnemies, SpawnEnemy(UnitRole.Kroni, faction, points[i % points.Length],
                    stage.chair.position, stage.chair.position));
            // The Pengawal guards the seat, so its leash is centred on the chair.
            for (int i = 0; i < CampaignTuning.Memerintah.CounterattackGuard; i++)
                AddIfSpawned(counterEnemies, SpawnEnemy(UnitRole.Guard, faction,
                    points[(CampaignTuning.Memerintah.CounterattackKroni + i) % points.Length],
                    stage.chair.position, stage.chair.position));
        }

        private void OnKudeta()
        {
            DespawnAll(counterEnemies);
        }

        // 0.1.0: out of combat for a few seconds, a hero's Wibawa refills.
        private void UpdateRecovery(float deltaTime)
        {
            float now = Time.time;
            heroScratch.Clear();
            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
                if (networkObject != null && networkObject.IsPlayerObject)
                    heroScratch.Add(networkObject);

            foreach (NetworkObject hero in heroScratch)
            {
                NetworkPlayerCombat combat = hero.GetComponent<NetworkPlayerCombat>();
                if (combat == null)
                    continue;
                ulong id = hero.NetworkObjectId;
                int wibawa = combat.Wibawa;
                if (!lastWibawa.TryGetValue(id, out int previous) || wibawa < previous || combat.IsKnockedOut)
                {
                    lastHurtAt[id] = now;
                    recoveryCarry[id] = 0f;
                }
                lastWibawa[id] = wibawa;

                if (combat.IsKnockedOut || wibawa >= combat.MaxWibawaValue ||
                    now - lastHurtAt[id] < CampaignTuning.Recovery.DelaySeconds)
                    continue;

                float carry = recoveryCarry[id] + CampaignTuning.Recovery.WibawaPerSecond * deltaTime;
                int heal = Mathf.FloorToInt(carry);
                recoveryCarry[id] = carry - heal;
                if (heal > 0)
                {
                    combat.ServerHeal(heal);
                    lastWibawa[id] = combat.Wibawa;
                }
            }
        }

        private CampaignEnemy SpawnEnemy(UnitRole role, FactionId faction, Vector3 position, Vector3 anchor, Vector3 lookAt)
        {
            return SpawnEnemy(role, faction, position, anchor, lookAt, anchor, 0f);
        }

        private CampaignEnemy SpawnEnemy(UnitRole role, FactionId faction, Vector3 position, Vector3 anchor,
            Vector3 lookAt, Vector3 areaCenter, float areaRadius)
        {
            if (enemyPrefab == null)
            {
                Debug.LogError("[KONOHA CAMPAIGN] Enemy prefab is not assigned.");
                return null;
            }

            GameObject instance = Instantiate(enemyPrefab, position + Vector3.up * CampaignStage.DropHeight,
                CampaignStage.Facing(position, lookAt));
            CampaignEnemy enemy = instance.GetComponent<CampaignEnemy>();
            NetworkObject networkObject = instance.GetComponent<NetworkObject>();
            if (enemy == null || networkObject == null)
            {
                Destroy(instance);
                Debug.LogError("[KONOHA CAMPAIGN] Enemy prefab lacks CampaignEnemy/NetworkObject.");
                return null;
            }

            networkObject.Spawn(true);
            enemy.ServerInitialize(role, faction, anchor, areaCenter, areaRadius);
            return enemy;
        }

        private static void AddIfSpawned(List<CampaignEnemy> list, CampaignEnemy enemy)
        {
            if (enemy != null)
                list.Add(enemy);
        }

        // Standing members; knocked-out and surrendered ones no longer count.
        private static int CountAlive(List<CampaignEnemy> list)
        {
            int alive = 0;
            foreach (CampaignEnemy enemy in list)
                if (enemy != null && enemy.IsSpawned && !enemy.IsOutOfFight) alive++;
            return alive;
        }

        private static int CountAlive(List<CampaignEnemy> list, UnitRole role)
        {
            int alive = 0;
            foreach (CampaignEnemy enemy in list)
                if (enemy != null && enemy.IsSpawned && !enemy.IsOutOfFight && enemy.Role == role) alive++;
            return alive;
        }

        private void DespawnAll(List<CampaignEnemy> list)
        {
            foreach (CampaignEnemy enemy in list)
                if (enemy != null && enemy.IsSpawned) enemy.NetworkObject.Despawn(true);
            list.Clear();
        }

        private void OnWon()
        {
            // The run is over: every remaining Sistem member leaves the capital.
            DespawnAll(counterEnemies);
            DespawnAll(gardaEnemies);
            runEndedAt.Value = NetworkManager.ServerTime.Time;
        }

        // chooseHero: back to the hero screen (GANTI HERO); otherwise the same hero starts again (ULANG).
        private void ServerRestart(bool chooseHero)
        {
            DespawnAll(gateEnemies);
            DespawnAll(majelisEnemies);
            DespawnAll(biroEnemies);
            DespawnAll(gardaEnemies);
            DespawnAll(counterEnemies);
            biroSpawned = false;
            biroPending = false;
            biro.Reset();
            biroLeader = null;
            biroDoorOpen.Value = false;
            loketContestedMask.Value = 0;
            gateSpawned = false;
            majelisSpawned = false;
            majelisPending = false;
            majelis.Reset();
            gardaSpawned = false;
            garda.Reset();
            countedDefeats.Clear();
            majelisBought = false;
            biroBought = false;
            panglima = null;
            gardaLockdown.Value = false;
            counterFaction = 0;
            objectives.Restart();
            SyncState();

            // Revive every hero at the (reset) Gerbang Rakyat checkpoint.
            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject == null || !networkObject.IsPlayerObject)
                    continue;
                NetworkPlayerCombat combat = networkObject.GetComponent<NetworkPlayerCombat>();
                if (combat != null)
                    combat.ServerResetForMatch();
            }

            restu.Value = false;
            gatePending = true;
            runEndedAt.Value = -1d;
            if (chooseHero)
            {
                // Back to the hero screen: the hero may be changed before the next run.
                heroLocked.Value = false;
                OnObjectiveMessage("Pilih hero untuk perjalanan baru");
            }
            else
            {
                runStartedAt.Value = NetworkManager.ServerTime.Time;
                OnObjectiveMessage("Musim pemilu baru! Usir PREMAN BAYARAN yang memalak warga di gang");
            }
            SyncState();
        }

        // --- Player input --------------------------------------------------------------

        // MULAI on the hero screen: locks the chosen hero and starts the run.
        public void RequestStartRun()
        {
            if (IsSpawned && !heroLocked.Value)
                StartRunServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void StartRunServerRpc()
        {
            if (objectives == null || heroLocked.Value)
                return;

            heroLocked.Value = true;
            runStartedAt.Value = NetworkManager.ServerTime.Time;
            runEndedAt.Value = -1d;
            OnObjectiveMessage("Musim pemilu dimulai! Usir PREMAN BAYARAN yang memalak warga di gang");
        }

        // Result screen: ULANG (same hero) or GANTI HERO (back to the hero screen).
        public void RequestRestart(bool chooseHero)
        {
            if (IsSpawned)
                RestartServerRpc(chooseHero);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RestartServerRpc(bool chooseHero)
        {
            if (objectives != null && objectives.Run.Phase == CampaignPhase.Menang)
                ServerRestart(chooseHero);
        }

        // 0.4.0 RANGKUL button of the LAWAN / RANGKUL panel (loan: PINJAM KONSORSIUM).
        public void RequestRangkul(bool loan)
        {
            if (IsSpawned)
                RangkulServerRpc(loan);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RangkulServerRpc(bool loan)
        {
            if (objectives == null || !heroLocked.Value || objectives.OfferSector < 0)
                return;
            var sector = (CampaignSector)objectives.OfferSector;
            RangkulResult result = objectives.Rangkul(sector, loan);
            if (result == RangkulResult.TooPoor)
            {
                OnObjectiveMessage(objectives.Run.TookLoan
                    ? "MODAL KURANG.  Konsorsium sudah tidak mau minjemin lagi: hajar dulu anggotanya untuk MODAL"
                    : "MODAL KURANG.  Coba lahir di keluarga lain, atau PINJAM ke Konsorsium");
                return;
            }
            if (result != RangkulResult.Paid && result != RangkulResult.Loan)
                return;
            // The institution stands down: its members kneel and leave (no Modal for them).
            if (sector == CampaignSector.MajelisDaun)
            {
                majelisBought = true;
                StandDown(majelisEnemies);
            }
            else if (sector == CampaignSector.BiroProsedur)
            {
                biroBought = true;
                biroDoorOpen.Value = true;
                StandDown(biroEnemies);
            }
            SyncState();
        }

        // 0.5.0 Kantor Kelurahan loket names (index order of stage.loketPoints).
        public static string LoketName(int index)
        {
            switch (index)
            {
                case 0: return "FOTOKOPI KTP";
                case 1: return "CAP RT/RW";
                case 2: return "LEGALISIR";
                default: return "LOKET " + (index + 1);
            }
        }

        private void StandDown(List<CampaignEnemy> list)
        {
            foreach (CampaignEnemy enemy in list)
            {
                if (enemy == null || !enemy.IsSpawned || enemy.IsOutOfFight)
                    continue;
                enemy.ServerSetBlock(false);
                enemy.ServerSurrender();
            }
        }

        // Contextual SAHKAN / DUDUK / ULANG button.
        public void RequestInteract()
        {
            if (IsSpawned)
                InteractServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void InteractServerRpc(ServerRpcParams rpcParams = default)
        {
            if (objectives == null)
                return;

            if (objectives.Run.Phase == CampaignPhase.Menang)
            {
                ServerRestart(false);
                return;
            }

            if (!heroLocked.Value)
                return;

            NetworkObject player = FindPlayerObject(rpcParams.Receive.SenderClientId);
            if (player == null)
                return;

            NetworkPlayerCombat combat = player.GetComponent<NetworkPlayerCombat>();
            if (combat != null && combat.IsKnockedOut)
                return;

            bool wasSeated = objectives.Run.Phase == CampaignPhase.Memerintah;
            objectives.Interact(player.transform.position, Time.time);
            if (!wasSeated && (objectives.Run.Phase == CampaignPhase.Memerintah ||
                objectives.Run.Phase == CampaignPhase.Menang))
            {
                // §9 DUDUK: the hero is placed at the seat and stays there (movement locked
                // by CampaignTraversal) until BERDIRI, a push, or a Runtuh.
                NetworkHeroKit kit = player.GetComponent<NetworkHeroKit>();
                if (kit != null && stage != null && stage.chair != null)
                    kit.ServerTeleport(stage.chair.position + stage.seatOffset);
            }
            SyncState();
        }

        private NetworkObject FindPlayerObject(ulong clientId)
        {
            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
                if (networkObject != null && networkObject.IsPlayerObject && networkObject.OwnerClientId == clientId)
                    return networkObject;
            return null;
        }

        private void OnObjectiveMessage(string message)
        {
            NotifyClientRpc(message);
        }

        [ClientRpc]
        private void NotifyClientRpc(string message)
        {
            LastMessage = message;
            LastMessageUntil = Time.time + CampaignTuning.PreviewSlice.FeedbackSeconds;
        }

        // --- ICombatRules ----------------------------------------------------------------

        // Nothing moves or fights while the hero screen is open.
        public bool AllowsGameplay => IsSpawned && heroLocked.Value;

        // The hero is chosen once per run: only on the hero screen (start and after ULANG).
        public bool CanSelectHero => IsSpawned && !heroLocked.Value;

        // Campaign seating is objective state, not the PvP ruler lock: the seated hero may fight.
        public bool IsRuler(ulong clientId) => false;
        public bool IsRuler(NetworkObject actor) => false;

        // Every human is on the same side against the Sistem.
        public int GetHumanTeam(ulong clientId) => NetworkTeamUtility.CyanTeam;

        public bool ServerOnActorKnockedOut(NetworkObject actor, NetworkObject attacker)
        {
            CampaignEnemy enemy = actor != null ? actor.GetComponent<CampaignEnemy>() : null;
            if (enemy != null)
            {
                NetworkHeroKit attackerKit = attacker != null ? attacker.GetComponent<NetworkHeroKit>() : null;
                if (attackerKit != null && !NetworkTeamUtility.IsAiActor(attacker))
                    attackerKit.ServerGainPengaruh(enemy.IsElite
                        ? CampaignTuning.ResourceRules.PengaruhEliteDown
                        : CampaignTuning.ResourceRules.PengaruhKroniDown);
                return false;
            }

            if (actor != null && actor.IsPlayerObject && karierMode)
            {
                // KARIER: the preman leave with the wallet (next frame, not inside this hit).
                karierClearPending = true;
                CampaignKarier.RaiseHeroRuntuh();
                return true;
            }

            if (actor != null && actor.IsPlayerObject && objectives != null)
            {
                objectives.HandleRuntuh();
                RecoverEnemies(gateEnemies);
                RecoverEnemies(majelisEnemies);
                RecoverEnemies(biroEnemies);
                RecoverEnemies(gardaEnemies);
                RecoverEnemies(counterEnemies);
                SyncState();
            }

            return true;
        }

        private static void RecoverEnemies(List<CampaignEnemy> list)
        {
            foreach (CampaignEnemy enemy in list)
                if (enemy != null && enemy.IsSpawned)
                    enemy.ServerRecover(CampaignTuning.Runtuh.WoundedEnemyRecoveryFraction);
        }

        // RESTU RAKYAT: blessed heroes hit the Sistem harder and take less damage.
        public float GetDamageMultiplier(NetworkObject attacker, NetworkObject target)
        {
            if (!restu.Value || target == null)
                return 1f;

            bool attackerBlessed = attacker != null && attacker.IsPlayerObject &&
                target.TryGetComponent(out CampaignEnemy _);
            bool targetBlessed = target.IsPlayerObject;
            return RestuRakyat.DamageMultiplier(attackerBlessed, targetBlessed);
        }

        // STEMPEL TUNDA (§8.2): skills of a hero near a living Pengawas recharge 30% slower.
        // Evaluated on every peer from replicated positions, so the owner's HUD agrees.
        public float GetCooldownMultiplier(NetworkObject actor)
        {
            if (actor == null || !actor.IsPlayerObject)
                return 1f;
            return BiroEncounter.CooldownMultiplier(CampaignEnemy.InsideStempelTunda(actor.transform.position));
        }

        // §9: attacking from the seat reaches 1 m further.
        public float GetBasicRangeBonus(NetworkObject actor) =>
            actor != null && actor.NetworkObjectId == seatedObjectId.Value
                ? CampaignTuning.Memerintah.SeatedBasicRangeBonus
                : 0f;

        // 0.2.4: solo hero kit (HeroBalance) so all four heroes work without allies.
        public bool UsesSoloHeroKit => true;

        public float GetRespawnDelay(NetworkObject actor, float defaultDelay) =>
            CampaignTuning.Runtuh.RespawnDelaySeconds;

        public Vector3 GetRespawnPosition(NetworkObject actor)
        {
            if (stage == null)
                stage = FindFirstObjectByType<CampaignStage>();
            return stage != null
                ? stage.CheckpointPosition(Checkpoint)
                : new Vector3(0f, 0.35f, -44f);
        }

        public Quaternion GetRespawnRotation(NetworkObject actor)
        {
            if (stage == null || stage.chair == null)
                return Quaternion.identity;
            Vector3 from = stage.CheckpointPosition(Checkpoint);
            return CampaignStage.Facing(from, stage.chair.position);
        }

        public void ServerOnActorRespawned(NetworkObject actor)
        {
            if (actor == null || !actor.IsPlayerObject)
                return;

            NetworkHeroKit kit = actor.GetComponent<NetworkHeroKit>();
            if (kit != null)
                kit.ServerScalePengaruh(1f - CampaignTuning.Runtuh.PengaruhPenaltyFraction);
        }
    }
}
