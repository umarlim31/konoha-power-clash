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

        public float nameplateVisibleDistance = 15f;
        public float nameplateScaleNear = 1f;
        public float nameplateScaleFar = 0.78f;

        private Camera cachedCamera;
        private Color baseBodyColor;
        private Color baseFacingColor;
        private bool knockedOut;
        private Coroutine damageFlashRoutine;
        private float nextReadabilityRefresh;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            int team = NetworkTeamUtility.GetTeam(NetworkObject);
            Color teamColor = NetworkTeamUtility.GetTeamColor(team);
            baseBodyColor = Color.Lerp(teamColor, new Color(0.08f, 0.10f, 0.13f), 0.32f);
            baseFacingColor = Color.Lerp(teamColor, Color.white, 0.28f);

            ApplyCurrentVisual();

            NetworkBotController bot = GetComponent<NetworkBotController>();

            if (localOwnerMarker != null)
                localOwnerMarker.SetActive(IsOwner && bot == null);

            cachedCamera = Camera.main;
            RefreshOwnershipLabel();
            RefreshReadability(true);

            gameObject.name = bot != null
                ? "NetworkBot_" + NetworkTeamUtility.GetTeamName(team) + "_" + bot.Slot
                : "NetworkPlayer_" + OwnerClientId + (IsOwner ? "_LOCAL" : "_REMOTE");

            Debug.Log(
                "[KONOHA SPAWN] Actor ready | owner=" + OwnerClientId +
                " | localClient=" + NetworkManager.LocalClientId +
                " | isOwner=" + IsOwner +
                " | bot=" + (bot != null));
        }

        private void Update()
        {
            if (!IsSpawned || Time.unscaledTime < nextReadabilityRefresh)
                return;

            nextReadabilityRefresh = Time.unscaledTime + 0.20f;
            RefreshOwnershipLabel();
            RefreshReadability(false);
        }

        public void RefreshOwnershipLabel()
        {
            if (ownershipLabel == null || !IsSpawned)
                return;

            NetworkBotController bot = GetComponent<NetworkBotController>();
            NetworkHeroKit kit = GetComponent<NetworkHeroKit>();
            NetworkMatchManager match = NetworkMatchManager.Instance;
            int team = NetworkTeamUtility.GetTeam(NetworkObject);

            string heroName = kit != null
                ? NetworkHeroKit.GetHeroName(kit.Hero)
                : bot != null ? bot.HeroName : "HERO";

            string identity = bot != null
                ? heroName + " • BOT"
                : IsOwner
                    ? heroName + " • YOU"
                    : heroName + " • P" + OwnerClientId;

            bool ruler = match != null && match.IsRuler(NetworkObject);
            ownershipLabel.text = ruler
                ? identity + "\nPENGUASA"
                : identity;

            if (knockedOut)
            {
                ownershipLabel.color = new Color(1f, 0.32f, 0.28f);
            }
            else if (bot == null && IsOwner)
            {
                ownershipLabel.color = new Color(1.00f, 0.92f, 0.48f);
            }
            else
            {
                ownershipLabel.color = Color.Lerp(
                    NetworkTeamUtility.GetTeamColor(team),
                    Color.white,
                    0.28f);
            }
        }

        private void LateUpdate()
        {
            if (!IsSpawned || ownershipLabel == null || !ownershipLabel.gameObject.activeSelf)
                return;

            if (cachedCamera == null)
                cachedCamera = Camera.main;

            if (cachedCamera == null)
                return;

            Vector3 direction = ownershipLabel.transform.position - cachedCamera.transform.position;
            if (direction.sqrMagnitude > 0.001f)
                ownershipLabel.transform.rotation = Quaternion.LookRotation(direction);
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
            RefreshOwnershipLabel();
            RefreshReadability(true);
        }

        private void RefreshReadability(bool force)
        {
            if (ownershipLabel == null)
                return;

            if (cachedCamera == null)
                cachedCamera = Camera.main;

            NetworkBotController bot = GetComponent<NetworkBotController>();
            bool localHuman = bot == null && IsOwner;
            NetworkMatchManager match = NetworkMatchManager.Instance;
            bool ruler = match != null && match.IsRuler(NetworkObject);

            float distance = GetViewerDistance();

            bool visible = localHuman ||
                           knockedOut ||
                           ruler ||
                           cachedCamera == null ||
                           distance <= nameplateVisibleDistance;

            if (force || ownershipLabel.gameObject.activeSelf != visible)
                ownershipLabel.gameObject.SetActive(visible);

            if (!visible)
                return;

            float t = Mathf.InverseLerp(7f, nameplateVisibleDistance, distance);
            float scale = Mathf.Lerp(nameplateScaleNear, nameplateScaleFar, t);
            ownershipLabel.transform.localScale = Vector3.one * scale;
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
