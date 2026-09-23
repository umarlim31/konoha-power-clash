using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Networking
{
    public sealed class NetworkWibawaBar : NetworkBehaviour
    {
        public Canvas worldCanvas;
        public RectTransform healthFill;
        public RectTransform shieldFill;
        public Image healthImage;
        public Image shieldImage;
        public Image teamStrip;
        public Text valueText;

        public float visibleDistance = 14f;

        private NetworkPlayerCombat combat;
        private Camera cachedCamera;
        private float nextRefresh;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            combat = GetComponent<NetworkPlayerCombat>();
            cachedCamera = Camera.main;
            Refresh(true);
        }

        private void LateUpdate()
        {
            if (!IsSpawned || worldCanvas == null)
                return;

            if (cachedCamera == null)
                cachedCamera = Camera.main;

            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 0.08f;
                Refresh(false);
            }

            if (cachedCamera == null || !worldCanvas.gameObject.activeSelf)
                return;

            Vector3 direction = worldCanvas.transform.position - cachedCamera.transform.position;
            if (direction.sqrMagnitude > 0.001f)
                worldCanvas.transform.rotation = Quaternion.LookRotation(direction);
        }

        private void Refresh(bool force)
        {
            combat ??= GetComponent<NetworkPlayerCombat>();
            if (combat == null || worldCanvas == null)
                return;

            bool localHuman = GetComponent<NetworkBotController>() == null && IsOwner;
            NetworkMatchManager match = NetworkMatchManager.Instance;
            bool ruler = match != null && match.IsRuler(NetworkObject);
            float distance = GetViewerDistance();

            bool visible = localHuman ||
                           combat.IsKnockedOut ||
                           ruler ||
                           distance <= visibleDistance;

            if (force || worldCanvas.gameObject.activeSelf != visible)
                worldCanvas.gameObject.SetActive(visible);

            if (!visible)
                return;

            float healthRatio = Mathf.Clamp01(combat.Wibawa / (float)NetworkPlayerCombat.MaxWibawa);
            float shieldRatio = Mathf.Clamp01(combat.Shield / 50f);

            if (healthFill != null)
                healthFill.anchorMax = new Vector2(healthRatio, 1f);

            if (shieldFill != null)
            {
                shieldFill.anchorMax = new Vector2(shieldRatio, 1f);
                shieldFill.gameObject.SetActive(combat.Shield > 0 && !combat.IsKnockedOut);
            }

            if (healthImage != null)
            {
                healthImage.color = combat.IsKnockedOut
                    ? new Color(0.35f, 0.08f, 0.08f, 0.95f)
                    : healthRatio > 0.50f
                        ? new Color(0.28f, 0.88f, 0.42f, 0.98f)
                        : healthRatio > 0.20f
                            ? new Color(1.00f, 0.72f, 0.18f, 0.98f)
                            : new Color(0.95f, 0.20f, 0.16f, 0.98f);
            }

            if (shieldImage != null)
                shieldImage.color = new Color(0.25f, 0.72f, 1.00f, 0.90f);

            if (teamStrip != null)
                teamStrip.color = NetworkTeamUtility.GetTeamColor(NetworkObject != null
                    ? NetworkTeamUtility.GetTeam(NetworkObject)
                    : -1);

            if (valueText != null)
            {
                if (combat.IsKnockedOut)
                {
                    valueText.text = "RUNTUH";
                }
                else
                {
                    valueText.text = combat.Shield > 0
                        ? combat.Wibawa + "  +" + combat.Shield
                        : combat.Wibawa.ToString();
                }
            }

            float scale = localHuman
                ? 1f
                : Mathf.Lerp(0.95f, 0.76f, Mathf.InverseLerp(5f, visibleDistance, distance));

            worldCanvas.transform.localScale = Vector3.one * (0.008f * scale);
        }

        private float GetViewerDistance()
        {
            if (NetworkManager != null &&
                NetworkManager.LocalClient != null &&
                NetworkManager.LocalClient.PlayerObject != null)
            {
                Vector3 a = NetworkManager.LocalClient.PlayerObject.transform.position;
                Vector3 b = transform.position;
                a.y = 0f;
                b.y = 0f;
                return Vector3.Distance(a, b);
            }

            if (cachedCamera == null)
                cachedCamera = Camera.main;

            return cachedCamera != null
                ? Vector3.Distance(cachedCamera.transform.position, transform.position)
                : 0f;
        }
    }
}
