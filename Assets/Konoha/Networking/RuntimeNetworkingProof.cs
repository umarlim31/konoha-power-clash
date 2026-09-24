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
        public RectTransform networkPanel;
        public Text networkLegend;

        public GameObject playerPrefab;
        public GameObject botPrefab;
        public GameObject matchPrefab;
        public GameObject offlineHero;
        public GameObject offlineDriver;

        private readonly HashSet<ulong> serverSpawnedPlayers = new HashSet<ulong>();
        private const string LastHostAddressKey = "Konoha.LastHostAddress";

        private NetworkManager manager;
        private UnityTransport transport;
        private bool initialized;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (!Application.isPlaying)
            {
                SetStatus("NETWORK READY | 0.0.6A.2");
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

            if (!IsValidNetworkPrefab(playerPrefab) ||
                !IsValidNetworkPrefab(botPrefab) ||
                !IsValidNetworkPrefab(matchPrefab))
            {
                Debug.LogError("[KONOHA NET] Player, bot, or match prefab is missing/invalid.");
                SetStatus("NETWORK ERROR | PREFAB");
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
                manager.AddNetworkPrefab(playerPrefab);
                manager.AddNetworkPrefab(botPrefab);
                manager.AddNetworkPrefab(matchPrefab);
            }
            catch (Exception exception)
            {
                Debug.LogError("[KONOHA NET] Prefab registration failed: " + exception);
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
            SetStatus("NETWORK READY | 0.0.6A.2");
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
            SetNetworkCompact(true);

            EnsureMatchManager();
            EnsurePlayerObject(manager.LocalClientId);

            SetStatus("HOST STARTED | WAIT FOR PLAYERS | START MATCH");
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
            SetNetworkCompact(true);
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
            SetNetworkCompact(false);
            SetStatus("NETWORK READY | 0.0.6A.2");
        }

        private void OnClientConnected(ulong clientId)
        {
            if (manager != null && manager.IsServer)
            {
                EnsureMatchManager();
                EnsurePlayerObject(clientId);
            }

            string role = manager != null && manager.IsHost ? "HOST" : "CLIENT";
            SetStatus("CONNECTED | " + role + " | MATCH READY");
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (manager != null && manager.IsServer)
                serverSpawnedPlayers.Remove(clientId);

            SetStatus("DISCONNECTED | CLIENT " + clientId);
        }

        private void EnsureMatchManager()
        {
            if (manager == null || !manager.IsServer || matchPrefab == null)
                return;

            if (NetworkMatchManager.Instance != null && NetworkMatchManager.Instance.IsSpawned)
                return;

            GameObject instance = null;

            try
            {
                instance = Instantiate(matchPrefab, Vector3.zero, Quaternion.identity);
                NetworkObject networkObject = instance.GetComponent<NetworkObject>();
                if (networkObject == null)
                    throw new InvalidOperationException("Match prefab has no NetworkObject.");

                networkObject.Spawn(true);
                Debug.Log("[KONOHA MATCH] Match state NetworkObject spawned.");
            }
            catch (Exception exception)
            {
                if (instance != null)
                    Destroy(instance);

                Debug.LogError("[KONOHA MATCH] Match manager spawn failed: " + exception);
                SetStatus("MATCH STATE SPAWN FAILED");
            }
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
                Vector3 spawnPosition = NetworkTeamUtility.GetSpawnPosition(clientId);
                Quaternion spawnRotation = NetworkTeamUtility.GetSpawnRotation(clientId);
                instance = Instantiate(playerPrefab, spawnPosition, spawnRotation);

                NetworkObject networkObject = instance.GetComponent<NetworkObject>();
                if (networkObject == null)
                    throw new InvalidOperationException("Spawned player prefab has no NetworkObject.");

                networkObject.SpawnAsPlayerObject(clientId, true);

                Debug.Log(
                    "[KONOHA SPAWN] SpawnAsPlayerObject | client=" + clientId +
                    " | team=" + NetworkTeamUtility.GetTeamName(NetworkTeamUtility.GetTeam(clientId)) +
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

        private void SetNetworkCompact(bool compact)
        {
            if (networkPanel != null)
                networkPanel.sizeDelta = compact
                    ? new Vector2(330f, 62f)
                    : new Vector2(450f, 172f);

            if (addressInput != null)
                addressInput.gameObject.SetActive(!compact);

            if (hostButton != null)
                hostButton.gameObject.SetActive(!compact);

            if (clientButton != null)
                clientButton.gameObject.SetActive(!compact);

            if (shutdownButton != null)
            {
                shutdownButton.gameObject.SetActive(true);
                RectTransform stopRect = shutdownButton.GetComponent<RectTransform>();
                if (stopRect != null)
                {
                    stopRect.sizeDelta = compact ? new Vector2(88f, 36f) : new Vector2(126f, 48f);
                    stopRect.anchoredPosition = compact ? new Vector2(112f, 12f) : new Vector2(135f, 10f);
                }
            }

            if (status != null)
            {
                RectTransform statusRect = status.GetComponent<RectTransform>();
                if (statusRect != null)
                {
                    statusRect.sizeDelta = compact ? new Vector2(220f, 34f) : new Vector2(420f, 32f);
                    statusRect.anchoredPosition = compact ? new Vector2(-46f, -8f) : new Vector2(0f, -10f);
                }

                status.fontSize = compact ? 13 : 17;
            }

            if (networkLegend != null)
                networkLegend.gameObject.SetActive(!compact);
        }

        private void SetStatus(string value)
        {
            if (status != null)
                status.text = value;

            Debug.Log("[KONOHA NET] " + value);
        }

        private static bool IsValidNetworkPrefab(GameObject prefab)
        {
            return prefab != null && prefab.GetComponent<NetworkObject>() != null;
        }
    }
}
