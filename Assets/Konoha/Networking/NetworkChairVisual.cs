using UnityEngine;

namespace Konoha.Networking
{
    public sealed class NetworkChairVisual : MonoBehaviour
    {
        public Renderer zoneRenderer;
        public Renderer[] chairRenderers;

        private NetworkMatchManager manager;
        private Color currentColor = Color.clear;

        private void Update()
        {
            if (manager == null || !manager.IsSpawned)
                manager = NetworkMatchManager.Instance;

            Color target = GetTargetColor();

            if (Approximately(currentColor, target))
                return;

            currentColor = target;
            Apply(zoneRenderer, Color.Lerp(target, Color.black, 0.64f));

            if (chairRenderers == null)
                return;

            foreach (Renderer renderer in chairRenderers)
                Apply(renderer, Color.Lerp(target, Color.white, 0.08f));
        }

        private Color GetTargetColor()
        {
            if (manager == null)
                return new Color(0.45f, 0.50f, 0.56f);

            if (manager.State == GreyboxMatchState.SuddenPower)
                return new Color(1.00f, 0.78f, 0.18f);

            if (manager.IsContested)
                return new Color(1.00f, 0.88f, 0.24f);

            if (manager.HasRuler && manager.ChairOwnerTeam >= 0)
                return NetworkTeamUtility.GetTeamColor(manager.ChairOwnerTeam);

            if (manager.CaptureTeam >= 0)
                return NetworkTeamUtility.GetTeamColor(manager.CaptureTeam);

            if (manager.ChairOwnerTeam >= 0)
                return NetworkTeamUtility.GetTeamColor(manager.ChairOwnerTeam);

            return new Color(0.55f, 0.58f, 0.62f);
        }

        private static void Apply(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;

            Material material = renderer.material;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        private static bool Approximately(Color a, Color b)
        {
            Vector4 delta = (Vector4)a - (Vector4)b;
            return delta.sqrMagnitude < 0.0001f;
        }
    }
}
