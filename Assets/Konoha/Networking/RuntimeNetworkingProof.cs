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

        private NetworkManager manager;
        private UnityTransport transport;
        private bool initialized;

        private void Awake()
        {
            // The spike scene is generated in the Editor, but Button.onClick listeners
            // added there are not persisted as runtime listeners. Always wire the proof
            // again when the player actually starts on Android.
            Initialize();
        }

        public void Initialize()
        {
            if (initialized) return;

            manager = GetComponent<NetworkManager>();
            transport = GetComponent<UnityTransport>();

            if (manager == null || transport == null)
            {
                Debug.LogError("[KONOHA NET] Missing NetworkManager or UnityTransport.");
                SetStatus("NETWORK ERROR | MISSING COMPONENT");
                return;
            }

            // Explicitly wire NGO to UTP. This keeps the generated scene deterministic
            // instead of relying on inspector/default transport discovery.
            manager.NetworkConfig.NetworkTransport = transport;

            if (hostButton != null) hostButton.onClick.AddListener(StartHost);
            if (clientButton != null) clientButton.onClick.AddListener(StartClient);
            if (shutdownButton != null) shutdownButton.onClick.AddListener(Shutdown);

            manager.OnClientConnectedCallback += OnClientConnected;
            manager.OnClientDisconnectCallback += OnClientDisconnected;

            initialized = true;
            SetStatus("NETWORK READY | NGO + UTP");
        }

        private void OnDestroy()
        {
            if (!initialized) return;

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
            if (manager == null || transport == null)
            {
                SetStatus("HOST ERROR | NETWORK NOT INITIALIZED");
                return;
            }

            if (manager.IsListening) return;

            transport.SetConnectionData("0.0.0.0", 7777, "0.0.0.0");
            bool started = manager.StartHost();
            SetStatus(started ? "HOST STARTED | PORT 7777" : "HOST START FAILED");
        }

        private void StartClient()
        {
            if (manager == null || transport == null)
            {
                SetStatus("CLIENT ERROR | NETWORK NOT INITIALIZED");
                return;
            }

            if (manager.IsListening) return;

            string address = addressInput == null || string.IsNullOrWhiteSpace(addressInput.text)
                ? "127.0.0.1"
                : addressInput.text.Trim();

            transport.SetConnectionData(address, 7777);
            bool started = manager.StartClient();
            SetStatus(started ? "CLIENT STARTING | " + address + ":7777" : "CLIENT START FAILED");
        }

        private void Shutdown()
        {
            if (manager != null && manager.IsListening) manager.Shutdown();
            SetStatus("NETWORK READY | NGO + UTP");
        }

        private void OnClientConnected(ulong clientId)
        {
            SetStatus("CONNECTED | CLIENT " + clientId + " | " + (manager != null && manager.IsHost ? "HOST" : "CLIENT"));
        }

        private void OnClientDisconnected(ulong clientId)
        {
            SetStatus("DISCONNECTED | CLIENT " + clientId);
        }

        private void SetStatus(string value)
        {
            if (status != null) status.text = value;
            Debug.Log("[KONOHA NET] " + value);
        }
    }
}
