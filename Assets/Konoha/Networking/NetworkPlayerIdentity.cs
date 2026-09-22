using System.Collections;
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
        private Color baseBodyColor;
        private Color baseFacingColor;
        private bool knockedOut;
        private Coroutine damageFlashRoutine;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            int team = NetworkTeamUtility.GetTeam(OwnerClientId);
            baseBodyColor = NetworkTeamUtility.GetTeamColor(team);
            baseFacingColor = Color.Lerp(baseBodyColor, Color.white, 0.35f);

            ApplyCurrentVisual();

            if (localOwnerMarker != null)
                localOwnerMarker.SetActive(IsOwner);

            if (ownershipLabel != null)
            {
                string role = OwnerClientId == Unity.Netcode.NetworkManager.ServerClientId ? "HOST" : "CLIENT";
                int team = NetworkTeamUtility.GetTeam(OwnerClientId);
                ownershipLabel.text = "P" + OwnerClientId + " " + role +
                                      " | " + NetworkTeamUtility.GetTeamName(team) +
                                      (IsOwner ? "\nYOU / OWNER" : "");
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

        public void PlayDamageFeedback()
        {
            if (!isActiveAndEnabled)
                return;

            if (damageFlashRoutine != null)
                StopCoroutine(damageFlashRoutine);

            damageFlashRoutine = StartCoroutine(DamageFlash());
        }

        public void SetKnockedOutVisual(bool value)
        {
            knockedOut = value;

            if (damageFlashRoutine != null)
            {
                StopCoroutine(damageFlashRoutine);
                damageFlashRoutine = null;
            }

            ApplyCurrentVisual();

            if (ownershipLabel != null)
                ownershipLabel.color = knockedOut
                    ? new Color(1f, 0.32f, 0.28f)
                    : IsOwner ? new Color(0.45f, 1f, 0.50f) : Color.white;
        }

        private IEnumerator DamageFlash()
        {
            ApplyColor(bodyRenderer, new Color(1f, 0.18f, 0.15f));
            ApplyColor(facingRenderer, Color.white);
            yield return new WaitForSecondsRealtime(0.14f);
            damageFlashRoutine = null;
            ApplyCurrentVisual();
        }

        private void ApplyCurrentVisual()
        {
            if (knockedOut)
            {
                ApplyColor(bodyRenderer, Color.Lerp(baseBodyColor, Color.black, 0.68f));
                ApplyColor(facingRenderer, Color.Lerp(baseFacingColor, Color.black, 0.72f));
                return;
            }

            ApplyColor(bodyRenderer, baseBodyColor);
            ApplyColor(facingRenderer, baseFacingColor);
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
