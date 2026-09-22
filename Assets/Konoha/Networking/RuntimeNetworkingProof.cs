using System;
using System.Collections.Generic;
using Konoha.Character;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Networking
{
    public sealed class RuntimeNetworkingProof : MonoBehaviour
    {
        public Text status;
        public Button hostButton;
        public Button clientButton;
        public Button shutdownButton;
        public InputField addressInput;

        public GameObject playerPrefab;
        public GameObject offlineHero;
        public GameObject offlineDriver;

        private readonly HashSet<ulong> serverSpawnedPlayers = new HashSet<ulong>();
        private const string LastHostAddressKey = "Konoha.LastHostAddress";

        private NetworkManager manager;
        private UnityTransport transport;
        private bool initialized;

        private void Start()
        {
            // Start runs after all Awake calls on the NetworkManager/Transport have completed.
            // This makes runtime prefab registration deterministic on Android.
            Initialize();
        }

        public void Initialize()
        {
            if (!Application.isPlaying)
            {
                SetStatus("NETWORK READY | 0.0.3B");
                return;
            }

            if (initialized)
                return;

            manager = GetComponent<NetworkManager>();
            transport = GetComponent<UnityTransport>();

            if (manager == null || transport == null)
            {
                Debug.LogError("[KONOHA NET] Missing NetworkManager or UnityTransport.");
                SetStatus("NETWORK ERROR | MISSING COMPONENT");
                return;
            }

            if (playerPrefab == null || playerPrefab.GetComponent<NetworkObject>() == null)
            {
                Debug.LogError("[KONOHA NET] Network player prefab is missing or invalid.");
                SetStatus("NETWORK ERROR | PLAYER PREFAB");
                return;
            }

            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.TickRate = 60;

            if (addressInput != null)
            {
                string savedAddress = PlayerPrefs.GetString(LastHostAddressKey, addressInput.text);
                if (!string.IsNullOrWhiteSpace(savedAddress))
                    addressInput.text = savedAddress;
            }

            try
            {
                // Every peer registers the exact same prefab before Host/Client startup.
                manager.AddNetworkPrefab(playerPrefab);
            }
            catch (Exception exception)
            {
                Debug.LogError("[KONOHA NET] Player prefab registration failed: " + exception);
                SetStatus("NETWORK ERROR | PREFAB REGISTER");
                return;
            }

            if (hostButton != null) hostButton.onClick.AddListener(StartHost);
            if (clientButton != null) clientButton.onClick.AddListener(StartClient);
            if (shutdownButton != null) shutdownButton.onClick.AddListener(Shutdown);

            manager.OnClientConnectedCallback += OnClientConnected;
            manager.OnClientDisconnectCallback += OnClientDisconnected;

            initialized = true;
            SetNetworkButtons(false);
            SetStatus("NETWORK READY | COMBAT LOOP")
        }

        private void OnDestroy()
        {
            if (!initialized)
                return;

            if (hostButton != null) hostButton.onClick.RemoveListener(StartHost);
            if (clientButton != null) clientButton.onClick.RemoveListener(StartClient);
            if (shutdownButton != null) shutdownButton.onClick.RemoveListener(Shutdown);

            if (manager != null)
            {
                manager.OnClientConnectedCallback -= OnClientConnected;
                manager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        private void StartHost()
        {
            if (!CanStartNetwork("HOST"))
                return;

            transport.SetConnectionData("0.0.0.0", 7777, "0.0.0.0");
            bool started = manager.StartHost();

            if (!started)
            {
                SetStatus("HOST START FAILED");
                return;
            }

            DisableOfflinePrototype();
            SetNetworkButtons(true);

            // OnClientConnected normally creates P0 during StartHost. This explicit call
            // is intentionally idempotent and guarantees the host PlayerObject exists.
            EnsurePlayerObject(manager.LocalClientId);

            SetStatus("HOST STARTED | P" + manager.LocalClientId + " OWNER | 7777");
        }

        private void StartClient()
        {
            if (!CanStartNetwork("CLIENT"))
                return;

            string address = addressInput == null || string.IsNullOrWhiteSpace(addressInput.text)
                ? "127.0.0.1"
                : addressInput.text.Trim();

            PlayerPrefs.SetString(LastHostAddressKey, address);
            PlayerPrefs.Save();

            transport.SetConnectionData(address, 7777);
            bool started = manager.StartClient();

            if (!started)
            {
                SetStatus("CLIENT START FAILED");
                return;
            }

            DisableOfflinePrototype();
            SetNetworkButtons(true);
            SetStatus("CLIENT STARTING | " + address + ":7777");
        }

        private bool CanStartNetwork(string mode)
        {
            if (!initialized || manager == null || transport == null)
            {
                SetStatus(mode + " ERROR | NETWORK NOT INITIALIZED");
                return false;
            }

            if (manager.IsListening)
            {
                SetStatus("NETWORK ACTIVE | PRESS STOP FIRST");
                return false;
            }

            return true;
        }

        private void Shutdown()
        {
            if (manager != null && manager.IsListening)
                manager.Shutdown();

            serverSpawnedPlayers.Clear();
            RestoreOfflinePrototype();
            SetNetworkButtons(false);
            SetStatus("NETWORK READY | SMOOTH MOVE + COMBAT");
        }

        private void OnClientConnected(ulong clientId)
        {
            if (manager != null && manager.IsServer)
                EnsurePlayerObject(clientId);

            string role = manager != null && manager.IsHost ? "HOST" : "CLIENT";
            SetStatus("CONNECTED | CLIENT " + clientId + " | " + role + " | COMBAT READY");
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (manager != null && manager.IsServer)
                serverSpawnedPlayers.Remove(clientId);

            SetStatus("DISCONNECTED | CLIENT " + clientId);
        }

        private void EnsurePlayerObject(ulong clientId)
        {
            if (manager == null || !manager.IsServer || playerPrefab == null)
                return;

            if (!serverSpawnedPlayers.Add(clientId))
                return;

            GameObject instance = null;

            try
            {
                Vector3 spawnPosition = GetSpawnPosition(clientId);
                instance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

                NetworkObject networkObject = instance.GetComponent<NetworkObject>();
                if (networkObject == null)
                    throw new InvalidOperationException("Spawned player prefab has no NetworkObject.");

                networkObject.SpawnAsPlayerObject(clientId, true);

                Debug.Log(
                    "[KONOHA SPAWN] SpawnAsPlayerObject | client=" + clientId +
                    " | position=" + spawnPosition);
            }
            catch (Exception exception)
            {
                serverSpawnedPlayers.Remove(clientId);

                if (instance != null)
                    Destroy(instance);

                Debug.LogError("[KONOHA SPAWN] Player spawn failed for client " + clientId + ": " + exception);
                SetStatus("SPAWN FAILED | CLIENT " + clientId);
            }
        }

        private static Vector3 GetSpawnPosition(ulong clientId)
        {
            if (clientId == 0)
                return new Vector3(-3f, 0.1f, -3f);

            if (clientId == 1)
                return new Vector3(3f, 0.1f, -3f);

            int slot = (int)(clientId % 6);
            float x = -6f + slot * 2.4f;
            float z = clientId % 2 == 0 ? 1f : 4f;
            return new Vector3(x, 0.1f, z);
        }

        private void DisableOfflinePrototype()
        {
            if (offlineDriver != null)
                offlineDriver.SetActive(false);

            if (offlineHero != null)
                offlineHero.SetActive(false);
        }

        private void RestoreOfflinePrototype()
        {
            if (offlineHero != null)
                offlineHero.SetActive(true);

            if (offlineDriver != null)
                offlineDriver.SetActive(true);

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                MobileCombatCamera follow = mainCamera.GetComponent<MobileCombatCamera>();
                if (follow != null && offlineHero != null)
                    follow.target = offlineHero.transform;
            }
        }

        private void SetNetworkButtons(bool networkActive)
        {
            if (hostButton != null) hostButton.interactable = !networkActive;
            if (clientButton != null) clientButton.interactable = !networkActive;
            if (shutdownButton != null) shutdownButton.interactable = networkActive;
        }

        private void SetStatus(string value)
        {
            if (status != null)
                status.text = value;

            Debug.Log("[KONOHA NET] " + value);
        }
    }
}
