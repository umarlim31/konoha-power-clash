using Unity.Netcode.Components;

namespace Konoha.Networking
{
    /// <summary>
    /// Owner-authoritative NetworkTransform for responsive mobile player movement.
    /// The owning device moves immediately while NGO buffers/interpolates the remote copy.
    /// </summary>
    public sealed class OwnerNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
