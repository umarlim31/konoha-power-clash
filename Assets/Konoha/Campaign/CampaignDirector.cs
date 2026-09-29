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
        private NetworkVariable<int> biroSteps = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
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
        public int BiroSteps => biroSteps.Value;
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
            biroSteps.Value = objectives.BiroSteps;
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
            DespawnAll(gardaEnemies);
            DespawnAll(counterEnemies);
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
