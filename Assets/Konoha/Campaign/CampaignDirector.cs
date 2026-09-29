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
        private bool majelisSpawned;
        private bool majelisPending;
        // Enemies are spawned from Update, never from inside another object's OnNetworkSpawn.
        private bool gatePending;

        // Latest player-facing message on this peer (set by NotifyClientRpc).
        public string LastMessage { get; private set; } = string.Empty;
        public float LastMessageUntil { get; private set; }

        public CampaignPhase Phase => (CampaignPhase)phase.Value;
        public int Power => power.Value;
        public int TargetPower => CampaignTuning.PreviewSlice.TargetPower;
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
        public int GardaTotal => 2;
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
            gatePending = true;
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

            if (majelisSpawned)
                UpdateMajelis();

            if (biroSpawned)
                UpdateBiro(player, deltaTime);

            if (gardaSpawned && CountAlive(gardaEnemies) == 0 &&
                (objectives.Run.Phase == CampaignPhase.GerbangDalam || objectives.Run.Phase == CampaignPhase.GardaTakhta))
                objectives.CompleteGarda();

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
            majelisBlock.Value = majelisSpawned && majelis.BlockHolds && !majelis.Cleared;
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
            counterRemaining.Value = CountAlive(counterEnemies);

            int mask = 0;
            for (int i = 0; i < CampaignTuning.Seals.SectorCount; i++)
                if (run.HasSeal((CampaignSector)i)) mask |= 1 << i;
            sealMask.Value = mask;
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
                OnObjectiveMessage("BLOK MAJELIS PECAH!  Serangan kini masuk penuh");

            if ((change & MajelisChange.LeaderDown) != 0)
            {
                foreach (CampaignEnemy enemy in majelisEnemies)
                    if (enemy != null && enemy.IsSpawned && enemy.Role == UnitRole.Kroni)
                        enemy.ServerSurrender();
                if ((change & MajelisChange.Cleared) == 0)
                    OnObjectiveMessage("KETUA TUMBANG!  Staf Fraksi menyerah, kalahkan sisa pejabat");
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
            OnObjectiveMessage("RESTU RAKYAT!  Damage +" +
                Mathf.RoundToInt((CampaignTuning.Restu.HeroDamageMultiplier - 1f) * 100f) + "%, damage diterima -" +
                Mathf.RoundToInt((1f - CampaignTuning.Restu.HeroDamageTakenMultiplier) * 100f) + "%, Pengaruh +" +
                CampaignTuning.Restu.PengaruhBonus + ".  Menuju PLAZA");
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
                OnObjectiveMessage("ANTRIAN!  Usir petugas dari loket " + (heroLoket + 1));
            biroContestedAnnounced = contested;

            BiroChange change = biro.TickLokets(heroLoket, contested, deltaTime);
            if ((change & BiroChange.AllStamped) != 0)
                OpenBiroOffice();
            else if ((change & BiroChange.LoketStamped) != 0)
                OnObjectiveMessage("LOKET " + (biro.LastStamped + 1) + " TERCAP  (" + biro.StampedCount + "/" +
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
            OnObjectiveMessage("3 LOKET TERCAP!  Pintu KEPALA BIRO terbuka, awas SALAH LOKET");
        }

        private void SpawnGarda()
        {
            gardaEnemies.Clear();
            Vector3 post = stage.garda.position;
            if (stage.gardaSpawnPoints.Length > 0)
                AddIfSpawned(gardaEnemies, SpawnEnemy(UnitRole.Pemimpin, FactionId.GardaTakhta,
                    stage.gardaSpawnPoints[0], post, stage.chair.position));
            if (stage.gardaSpawnPoints.Length > 1)
                AddIfSpawned(gardaEnemies, SpawnEnemy(UnitRole.Guard, FactionId.GardaTakhta,
                    stage.gardaSpawnPoints[1], post, stage.chair.position));
            gardaSpawned = true;
        }

        private void SpawnCounterattack()
        {
            counterEnemies.Clear();
            // The counterattack guards the seat, so its leash is centred on the chair.
            AddIfSpawned(counterEnemies, SpawnEnemy(UnitRole.Guard, FactionId.GardaTakhta,
                stage.counterattackSpawnPoint, stage.chair.position, stage.chair.position));
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
            DespawnAll(counterEnemies);
        }

        private void ServerRestart()
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

            // Back to the hero screen: the hero may be changed before the next run.
            heroLocked.Value = false;
            restu.Value = false;
            gatePending = true;
            SyncState();
            OnObjectiveMessage("Pilih hero untuk perjalanan baru");
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
            OnObjectiveMessage("Perjalanan dimulai! Kalahkan Kroni di GERBANG RAKYAT");
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
                ServerRestart();
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

            objectives.Interact(player.transform.position, Time.time);
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
