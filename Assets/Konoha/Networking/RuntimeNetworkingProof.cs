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

        public void Initialize()
        {
            manager = GetComponent<NetworkManager>();
            transport = GetComponent<UnityTransport>();
            hostButton.onClick.AddListener(StartHost);
            clientButton.onClick.AddListener(StartClient);
            shutdownButton.onClick.AddListener(Shutdown);
            manager.OnClientConnectedCallback += OnClientConnected;
            manager.OnClientDisconnectCallback += OnClientDisconnected;
            SetStatus("NETWORK READY | NGO + UTP");
        }

        private void OnDestroy()
        {
            if (manager == null) return;
            manager.OnClientConnectedCallback -= OnClientConnected;
            manager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        private void StartHost()
        {
            if (manager.IsListening) return;
            transport.SetConnectionData("0.0.0.0", 7777, "0.0.0.0");
            bool started = manager.StartHost();
            SetStatus(started ? "HOST STARTED | PORT 7777" : "HOST START FAILED");
        }

        private void StartClient()
        {
            if (manager.IsListening) return;
            string address = string.IsNullOrWhiteSpace(addressInput.text) ? "127.0.0.1" : addressInput.text.Trim();
            transport.SetConnectionData(address, 7777);
            bool started = manager.StartClient();
            SetStatus(started ? "CLIENT STARTING | " + address + ":7777" : "CLIENT START FAILED");
        }

        private void Shutdown()
        {
            if (manager.IsListening) manager.Shutdown();
            SetStatus("NETWORK READY | NGO + UTP");
        }

        private void OnClientConnected(ulong clientId)
        {
            SetStatus("CONNECTED | CLIENT " + clientId + " | " + (manager.IsHost ? "HOST" : "CLIENT"));
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
