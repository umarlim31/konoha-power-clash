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
        private NetworkVariable<float> majelisHold = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
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
        private readonly List<CampaignEnemy> counterEnemies = new List<CampaignEnemy>();

        private CampaignStage stage;
        private CampaignObjectiveDirector objectives;
        private bool gateSpawned;
        private bool gardaSpawned;
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
        public float MajelisHold => majelisHold.Value;
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

            float deltaTime = Mathf.Min(Time.deltaTime, CampaignTuning.PreviewSlice.MaxFrameSeconds);
            NetworkObject player = PrimaryPlayer();
            if (player != null)
            {
                NetworkPlayerCombat combat = player.GetComponent<NetworkPlayerCombat>();
                if (combat == null || !combat.IsKnockedOut)
                    objectives.Tick(player.transform.position, deltaTime);
            }

            if (gateSpawned && !objectives.GateCleared && CountAlive(gateEnemies) == 0)
                objectives.MarkGateCleared();

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
            majelisHold.Value = objectives.MajelisHold;
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
            enemy.ServerInitialize(role, faction, anchor);
            return enemy;
        }

        private static void AddIfSpawned(List<CampaignEnemy> list, CampaignEnemy enemy)
        {
            if (enemy != null)
                list.Add(enemy);
        }

        private static int CountAlive(List<CampaignEnemy> list)
        {
            int alive = 0;
            foreach (CampaignEnemy enemy in list)
                if (enemy != null && enemy.IsSpawned && !enemy.IsDown) alive++;
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
            DespawnAll(gardaEnemies);
            DespawnAll(counterEnemies);
            gateSpawned = false;
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

            gatePending = true;
            SyncState();
            OnObjectiveMessage("Perjalanan baru dimulai");
        }

        // --- Player input --------------------------------------------------------------

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

        public bool AllowsGameplay => IsSpawned;

        // Hero changes stay open except while holding the seat.
        public bool CanSelectHero => IsSpawned && Phase != CampaignPhase.Memerintah;

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

        public float GetRespawnDelay(NetworkObject actor, float defaultDelay) =>
            CampaignTuning.Runtuh.RespawnDelaySeconds;

        public Vector3 GetRespawnPosition(NetworkObject actor)
        {
            if (stage == null)
                stage = FindFirstObjectByType<CampaignStage>();
            return stage != null
                ? stage.CheckpointPosition(Checkpoint)
                : new Vector3(0f, 0.35f, -9f);
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
