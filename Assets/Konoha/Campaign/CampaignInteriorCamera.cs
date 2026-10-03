using Konoha.Character;
using UnityEngine;

namespace Konoha.Campaign
{
    // 0.6.1 (owner): walking into a pondok, pendopo, warung or the polsek used to hide the
    // roof; now the roof stays and the camera comes inside instead: close, low and under
    // the roof, like standing in the room. Leaving restores the camera the player had.
    // A "roof" is any occluder candidate (CampaignOccluders) that covers the hero from
    // 1.9 to 7 m above him and is room-sized (not a pole, not the whole plaza). Bounds are
    // cached once: roofs do not move. Works in KARIER and MODE PRESIDEN.
    public sealed class CampaignInteriorCamera : MonoBehaviour
    {
        public MobileCombatCamera follow;
        public CampaignOccluders occluders;
        // 0.6.2: lower and closer (the pendopo roof starts only ~1.7 m above its floor), and the
        // roof over the hero is kept visible by CampaignOccluders while inside.
        public float distance = 2.8f;
        public float pitch = 6f;
        public float focusHeight = 1.3f;
        public float minRoofHeight = 1.2f;
        public float maxRoofHeight = 9f;
        public float minSize = 1.8f;
        public float maxArea = 170f;
        public float leaveDelay = 0.4f;

        private Bounds[] roofs = new Bounds[0];
        private Renderer[] roofRenderers = new Renderer[0];
        private bool inside;
        private float outsideSince = -1f;
        private float nextCheck;
        private float savedPitch, savedDistance, savedFocus, savedMinDistance, savedMinPitch;
        private float appliedMinDistance, appliedMinPitch;

        public static bool Inside { get; private set; }

        private void Start()
        {
            if (occluders == null || occluders.candidates == null)
                return;
            var list = new System.Collections.Generic.List<Bounds>();
            var owners = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer candidate in occluders.candidates)
            {
                if (candidate == null || !candidate.gameObject.activeInHierarchy || !LooksLikeRoof(candidate.name))
                    continue;
                Bounds b = candidate.bounds;
                if (b.size.x < minSize || b.size.z < minSize || b.size.x * b.size.z > maxArea)
                    continue;
                list.Add(b);
                owners.Add(candidate);
            }
            roofs = list.ToArray();
            roofRenderers = owners.ToArray();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextCheck)
                return;
            nextCheck = Time.unscaledTime + 0.15f;
            Transform hero = occluders != null ? occluders.target : null;
            if (follow == null || !follow.enabled || hero == null)
            {
                if (occluders != null) occluders.kept.Clear();
                if (inside) Leave();
                return;
            }
            bool under = UnderRoof(hero.position);
            // Every roof over the hero stays drawn (owner: "atapnya jangan hilang").
            occluders.kept.Clear();
            if (under)
                for (int i = 0; i < roofs.Length; i++)
                    if (Covers(roofs[i], hero.position))
                        occluders.kept.Add(roofRenderers[i]);
            if (under)
            {
                outsideSince = -1f;
                if (!inside) Enter();
            }
            else if (inside)
            {
                if (outsideSince < 0f) outsideSince = Time.unscaledTime;
                else if (Time.unscaledTime - outsideSince >= leaveDelay) Leave();
            }
        }

        // Trees, palms and gate lintels are not rooms.
        private static bool LooksLikeRoof(string name)
        {
            string lower = name.ToLowerInvariant();
            return !(lower.StartsWith("palm") || lower.Contains("tajuk") || lower.Contains("pohon") ||
                lower.Contains("lintel") || lower.Contains("gerbang") || lower.Contains("banner") || lower.StartsWith("sign"));
        }

        public bool UnderRoof(Vector3 p)
        {
            foreach (Bounds b in roofs)
                if (Covers(b, p))
                    return true;
            return false;
        }

        private bool Covers(Bounds b, Vector3 p)
        {
            float above = b.min.y - p.y;
            if (above < minRoofHeight || above > maxRoofHeight)
                return false;
            return p.x > b.min.x + 0.25f && p.x < b.max.x - 0.25f && p.z > b.min.z + 0.25f && p.z < b.max.z - 0.25f;
        }

        private void Enter()
        {
            inside = true;
            Inside = true;
            savedPitch = follow.orbitPitch;
            savedDistance = follow.orbitDistance;
            savedFocus = follow.orbitFocusHeight;
            savedMinDistance = follow.minOrbitDistance;
            savedMinPitch = follow.minPitch;
            appliedMinDistance = Mathf.Min(savedMinDistance, distance * 0.8f);
            appliedMinPitch = Mathf.Min(savedMinPitch, pitch * 0.5f);
            follow.minOrbitDistance = appliedMinDistance;
            follow.minPitch = appliedMinPitch;
            follow.orbitPitch = pitch;
            follow.orbitDistance = distance;
            follow.orbitFocusHeight = focusHeight;
        }

        private void Leave()
        {
            inside = false;
            Inside = false;
            outsideSince = -1f;
            if (follow == null)
                return;
            // Restore only what is still ours: a camera preset chosen while inside wins.
            if (Mathf.Approximately(follow.minOrbitDistance, appliedMinDistance)) follow.minOrbitDistance = savedMinDistance;
            if (Mathf.Approximately(follow.minPitch, appliedMinPitch)) follow.minPitch = savedMinPitch;
            if (Mathf.Approximately(follow.orbitFocusHeight, focusHeight)) follow.orbitFocusHeight = savedFocus;
            if (Mathf.Approximately(follow.orbitDistance, distance)) follow.orbitDistance = savedDistance;
            if (Mathf.Approximately(follow.orbitPitch, pitch)) follow.orbitPitch = savedPitch;
        }

        private void OnDisable()
        {
            if (inside) Leave();
        }
    }
}
