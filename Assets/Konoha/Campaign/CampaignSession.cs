using System;
using System.Collections.Generic;
using Konoha.Character;
using Konoha.Networking;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Konoha.Campaign
{
    // Jalur Takhta bootstrap: starts an offline local Netcode host as soon as the scene
    // opens (no room, no internet), spawns the director and the player's hero, then hands
    // the solo controls (camera-relative joystick, jump, oval boundary, fall recovery,
    // orbit camera) to that networked hero. Same prefab registration pattern as the PvP
    // RuntimeNetworkingProof, which is proven on device. Co-op clients would connect to
    // this host later; OnClientConnected already spawns their heroes.
    public sealed class CampaignSession : MonoBehaviour
    {
        public GameObject playerPrefab;
        public GameObject directorPrefab;
        public GameObject enemyPrefab;

        public CampaignStage stage;
        public CampaignTraversal traversal;
        public CampaignMonument monument;
        // Hides roofs/walls that would cover the hero (0.0.9.4).
        public CampaignOccluders occluders;
        public Transform movementCamera;
        // The generated offline character (collision probe in editor tests); hidden once
        // the networked hero exists.
        public GameObject offlineHero;

        public ushort port = 7777;
        // 0.1.0: the mode menu decides first (BeginSolo); false starts hosting immediately.
        public bool waitForMenu = true;
        public float jumpHeight = 1.25f;
        public float stepOffset = 0.25f;

        private readonly HashSet<ulong> spawnedPlayers = new HashSet<ulong>();
        private NetworkManager manager;
        private UnityTransport transport;
        private bool localHeroBound;

        public bool IsHosting => manager != null && manager.IsListening;
        public bool LocalHeroBound => localHeroBound;
        private bool prepared;
        // 0.6.0: KARIER (Level 1 Warga Biasa) instead of the Jalur Takhta route.
        private bool karier;

        private void Start()
        {
            if (!waitForMenu)
                BeginSolo();
        }

        // JALUR TAKHTA chosen in the mode menu: start the offline local host (no room, no internet).
        public void BeginSolo() => BeginSolo(false);

        // karierMode: KARIER chosen in the mode menu (same local host, different director rules).
        public void BeginSolo(bool karierMode)
        {
            if (prepared)
                return;
            prepared = true;
            karier = karierMode;

            manager = GetComponent<NetworkManager>();
            transport = GetComponent<UnityTransport>();

            if (manager == null || transport == null)
            {
                Debug.LogError("[KONOHA CAMPAIGN] Missing NetworkManager or UnityTransport.");
                return;
            }

            if (!IsNetworkPrefab(playerPrefab) || !IsNetworkPrefab(directorPrefab) || !IsNetworkPrefab(enemyPrefab))
            {
                Debug.LogError("[KONOHA CAMPAIGN] Player, director or enemy prefab is missing/invalid.");
                return;
            }

            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.TickRate = 60;

            try
            {
                manager.AddNetworkPrefab(playerPrefab);
                manager.AddNetworkPrefab(directorPrefab);
                manager.AddNetworkPrefab(enemyPrefab);
            }
            catch (Exception exception)
            {
                Debug.LogError("[KONOHA CAMPAIGN] Prefab registration failed: " + exception);
                return;
            }

            manager.OnClientConnectedCallback += OnClientConnected;

            // Loopback only: solo play needs no network and triggers no firewall prompt.
            transport.SetConnectionData("127.0.0.1", port, "127.0.0.1");
            if (!manager.StartHost())
            {
                Debug.LogError("[KONOHA CAMPAIGN] Local host failed to start.");
                return;
            }

            SpawnDirector();
            EnsurePlayer(manager.LocalClientId);
            Debug.Log("[KONOHA CAMPAIGN] Solo host started.");
        }

        private void OnDestroy()
        {
            if (manager != null)
                manager.OnClientConnectedCallback -= OnClientConnected;
        }

        private void Update()
        {
            if (prepared && !localHeroBound)
                TryBindLocalHero();
        }

        private void OnClientConnected(ulong clientId)
        {
            if (manager != null && manager.IsServer)
                EnsurePlayer(clientId);
        }

        private void SpawnDirector()
        {
            if (CampaignDirector.Instance != null && CampaignDirector.Instance.IsSpawned)
                return;

            GameObject instance = Instantiate(directorPrefab, Vector3.zero, Quaternion.identity);
            try
            {
                CampaignDirector director = instance.GetComponent<CampaignDirector>();
                if (director != null)
                    director.ConfigureKarier(karier);
                instance.GetComponent<NetworkObject>().Spawn(true);
            }
            catch (Exception exception)
            {
                Destroy(instance);
                Debug.LogError("[KONOHA CAMPAIGN] Director spawn failed: " + exception);
            }
        }

        private void EnsurePlayer(ulong clientId)
        {
            if (manager == null || !manager.IsServer || !spawnedPlayers.Add(clientId))
                return;

            Vector3 position = stage != null
                ? stage.CheckpointPosition(CampaignCheckpoint.GerbangRakyat)
                : new Vector3(0f, 0.35f, -44f);
            Quaternion rotation = stage != null && stage.chair != null
                ? CampaignStage.Facing(position, stage.chair.position)
                : Quaternion.identity;

            GameObject instance = null;
            try
            {
                instance = Instantiate(playerPrefab, position, rotation);
                instance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
            }
            catch (Exception exception)
            {
                spawnedPlayers.Remove(clientId);
                if (instance != null)
                    Destroy(instance);
                Debug.LogError("[KONOHA CAMPAIGN] Player spawn failed for client " + clientId + ": " + exception);
            }
        }

        private void TryBindLocalHero()
        {
            if (manager == null || !manager.IsListening || manager.LocalClient == null)
                return;

            NetworkObject player = manager.LocalClient.PlayerObject;
            if (player == null)
                return;

            CharacterMotor motor = player.GetComponent<CharacterMotor>();
            NetworkPlayerMovement movement = player.GetComponent<NetworkPlayerMovement>();
            if (motor == null || movement == null)
                return;

            // Solo traversal settings of 0.0.8.2, applied to this instance only; the PvP
            // prefab asset keeps allowJump = false and its original step offset.
            motor.allowJump = true;
            motor.jumpHeight = jumpHeight;
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
                controller.stepOffset = stepOffset;

            movement.externalLocomotion = true;
            movement.directionCamera = movementCamera;

            if (traversal != null)
                traversal.Bind(motor, movement);
            if (monument != null)
                monument.player = player.transform;
            if (occluders != null)
                occluders.target = player.transform;
            if (offlineHero != null)
                offlineHero.SetActive(false);

            localHeroBound = true;
            Debug.Log("[KONOHA CAMPAIGN] Solo controls bound to networked hero.");
        }

        private static bool IsNetworkPrefab(GameObject prefab)
        {
            return prefab != null && prefab.GetComponent<NetworkObject>() != null;
        }
    }
}
