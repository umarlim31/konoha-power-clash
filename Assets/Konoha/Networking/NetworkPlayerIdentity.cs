using Unity.Netcode;
using UnityEngine;

namespace Konoha.Networking
{
    public sealed class NetworkPlayerIdentity : NetworkBehaviour
    {
        public Renderer bodyRenderer;
        public Renderer facingRenderer;
        public GameObject localOwnerMarker;
        public TextMesh ownershipLabel;

        private Camera cachedCamera;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            Color bodyColor = OwnerClientId switch
            {
                0 => new Color(0.15f, 0.90f, 0.80f),
                1 => new Color(1.00f, 0.55f, 0.18f),
                _ => new Color(0.65f, 0.45f, 1.00f)
            };

            ApplyColor(bodyRenderer, bodyColor);
            ApplyColor(facingRenderer, Color.Lerp(bodyColor, Color.white, 0.35f));

            if (localOwnerMarker != null)
                localOwnerMarker.SetActive(IsOwner);

            if (ownershipLabel != null)
            {
                string role = OwnerClientId == Unity.Netcode.NetworkManager.ServerClientId ? "HOST" : "CLIENT";
                ownershipLabel.text = "P" + OwnerClientId + " " + role + (IsOwner ? "\nYOU / OWNER" : "");
                ownershipLabel.color = IsOwner ? new Color(0.45f, 1f, 0.50f) : Color.white;
            }

            gameObject.name = "NetworkPlayer_" + OwnerClientId + (IsOwner ? "_LOCAL" : "_REMOTE");
            cachedCamera = Camera.main;

            Debug.Log(
                "[KONOHA SPAWN] PlayerObject ready | owner=" + OwnerClientId +
                " | localClient=" + NetworkManager.LocalClientId +
                " | isOwner=" + IsOwner);
        }

        private void LateUpdate()
        {
            if (!IsSpawned || ownershipLabel == null)
                return;

            if (cachedCamera == null)
                cachedCamera = Camera.main;

            if (cachedCamera != null)
            {
                Vector3 direction = ownershipLabel.transform.position - cachedCamera.transform.position;
                if (direction.sqrMagnitude > 0.001f)
                    ownershipLabel.transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private static void ApplyColor(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;

            Material material = renderer.material;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }
    }
}
