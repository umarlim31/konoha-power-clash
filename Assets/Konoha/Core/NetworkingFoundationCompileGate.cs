using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;

namespace Konoha.Core
{
    // 0.0.2A compile-only gate.
    // No networking behavior is started here; these references intentionally
    // force Unity to resolve NGO, the NGO Unity Transport adapter, and UTP.
    public static class NetworkingFoundationCompileGate
    {
        public static Type NetcodeType => typeof(NetworkManager);
        public static Type NetcodeTransportAdapterType => typeof(UnityTransport);
        public static Type TransportType => typeof(NetworkEndpoint);
    }
}
