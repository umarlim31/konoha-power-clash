using UnityEngine;

namespace Konoha.Campaign
{
    // The central landmark remains solid, but fades when it would hide the player.
    public sealed class CampaignMonument : MonoBehaviour
    {
        public Collider solid;
        public Transform player;
        public Camera viewCamera;
        public Renderer[] surfaces;
        public Material[] opaqueMaterials;
        public Material[] transparentMaterials;
        private MaterialPropertyBlock block;
        private float opacity = 1f;
        private bool transparent;

        private void LateUpdate()
        {
            if (solid == null || player == null || viewCamera == null) return;
            Vector3 direction = player.position + Vector3.up * 1.15f - viewCamera.transform.position;
            bool blocked = Time.timeScale > 0f && solid.Raycast(new Ray(viewCamera.transform.position,
                direction.normalized), out _, direction.magnitude);
            float next = Mathf.MoveTowards(opacity, blocked ? .16f : 1f, Time.unscaledDeltaTime * 4f);
            if (Mathf.Approximately(next, opacity)) return;
            opacity = next;
            bool fade = opacity < .999f;
            if (block == null) block = new MaterialPropertyBlock();
            for (int i = 0; i < surfaces.Length; i++)
            {
                if (surfaces[i] == null) continue;
                if (fade != transparent)
                    surfaces[i].sharedMaterial = fade ? transparentMaterials[i] : opaqueMaterials[i];
                if (fade)
                {
                    Color color = opaqueMaterials[i].GetColor("_BaseColor");
                    color.a = opacity; block.SetColor("_BaseColor", color);
                    surfaces[i].SetPropertyBlock(block);
                }
                else surfaces[i].SetPropertyBlock(null);
            }
            transparent = fade;
        }

        // Six-node visibility graph around the inflated monument footprint. This keeps
        // the existing lightweight guard AI from walking through the new central plinth.
        public static Vector3 GuardDestination(Vector3 from, Vector3 to, Bounds footprint)
        {
            footprint.center = new Vector3(footprint.center.x, from.y, footprint.center.z);
            footprint.size = new Vector3(footprint.size.x + 1.1f, 10f, footprint.size.z + 1.1f);
            if (footprint.Contains(to))
            {
                // A player may stand closer to the stone than the guard's clearance.
                // Project the destination to the nearest edge so the guard can still reach attack range.
                Vector3 delta = to - footprint.center;
                if (footprint.extents.x - Mathf.Abs(delta.x) < footprint.extents.z - Mathf.Abs(delta.z))
                    to.x = footprint.center.x + (delta.x < 0 ? -1 : 1) * (footprint.extents.x + .16f);
                else to.z = footprint.center.z + (delta.z < 0 ? -1 : 1) * (footprint.extents.z + .16f);
            }
            if (Clear(from, to, footprint)) return to;
            Vector3 min = footprint.min - new Vector3(.15f, 0, .15f);
            Vector3 max = footprint.max + new Vector3(.15f, 0, .15f);
            var points = new[] { from, to, new Vector3(min.x,from.y,min.z), new Vector3(max.x,from.y,min.z),
                new Vector3(min.x,from.y,max.z), new Vector3(max.x,from.y,max.z) };
            var cost = new[] { 0f, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity,
                float.PositiveInfinity, float.PositiveInfinity };
            var previous = new[] { -1,-1,-1,-1,-1,-1 };
            var used = new bool[6];
            for (int step = 0; step < 6; step++)
            {
                int current = -1;
                for (int i = 0; i < 6; i++)
                    if (!used[i] && (current < 0 || cost[i] < cost[current])) current = i;
                if (current < 0 || float.IsPositiveInfinity(cost[current]) || current == 1) break;
                used[current] = true;
                for (int i = 0; i < 6; i++)
                {
                    if (used[i] || i == current || !Clear(points[current], points[i], footprint)) continue;
                    float candidate = cost[current] + Vector3.Distance(points[current],points[i]);
                    if (candidate < cost[i]) { cost[i] = candidate; previous[i] = current; }
                }
            }
            if (previous[1] < 0) return from;
            int next = 1;
            while (previous[next] > 0) next = previous[next];
            return points[next];
        }

        private static bool Clear(Vector3 a, Vector3 b, Bounds obstacle)
        {
            Vector3 delta = b - a;
            return delta.sqrMagnitude < .0001f || !obstacle.IntersectRay(new Ray(a,delta.normalized),out float hit)
                || hit >= delta.magnitude;
        }
    }
}
