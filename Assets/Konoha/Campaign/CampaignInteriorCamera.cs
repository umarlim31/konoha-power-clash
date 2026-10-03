using Konoha.Character;
using UnityEngine;

namespace Konoha.Campaign
{
    // 0.6.1 (owner): walking into a pondok, pendopo, warung or the polsek used to hide the
    // roof; now the roof stays and the camera comes inside instead: close, low and under
    // the roof, like standing in the room. Leaving restores the camera the player had.
    // A "roof" is any occluder candidate (CampaignOccluders) that covers the hero from
    // 1.2 to 9 m above him and is room-sized (not a pole, not the whole plaza). Bounds are
    // cached once: roofs do not move. Works in KARIER and MODE PRESIDEN.
    // 0.6.3: the underside comes from CampaignRoofUnderside (the model pendopo was never
    // detected before), and while inside the orbit is held under the roof: pitch, zoom and
    // look-at height are clamped every frame before the camera moves, so the roof never
    // has to be hidden. Runs before MobileCombatCamera (execution order).
    [DefaultExecutionOrder(-100)]
    public sealed class CampaignInteriorCamera : MonoBehaviour
    {
        public MobileCombatCamera follow;
        public CampaignOccluders occluders;
        public float distance = 2.8f;
        public float pitch = 6f;
        public float focusHeight = 1.3f;
        public float minRoofHeight = 1.2f;
        public float maxRoofHeight = 9f;
        public float minSize = 1.8f;
        public float maxArea = 170f;
        public float leaveDelay = 0.4f;
        // Room kept between the camera and the underside of the roof.
        public float headroom = 0.35f;

        // 0.6.4: the rooms are measured by the generator (editor) and saved with the scene, so
        // nothing depends on renderer bounds at runtime (a statically batched model roof was
        // never found on the tablet in 0.6.1-0.6.3). A roof renderer may be null (code roofs
        // that are never hidden anyway).
        public Bounds[] roofs = new Bounds[0];
        public float[] undersides = new float[0];
        public Renderer[] roofRenderers = new Renderer[0];
        private bool inside;
        private float outsideSince = -1f;
        private float nextCheck;
        private float lowestUnderside;
        private float savedPitch, savedDistance, savedFocus, savedMinDistance, savedMinPitch, savedMaxDistance;
        private float appliedMinDistance, appliedMinPitch, appliedMaxDistance, appliedFocus;

        public static bool Inside { get; private set; }
        public int RoomCount => roofs != null ? roofs.Length : 0;

        // Generator: measure every room-sized roof among these renderers (editor time).
        public void Bake(System.Collections.Generic.IEnumerable<Renderer> renderers)
        {
            var list = new System.Collections.Generic.List<Bounds>();
            var under = new System.Collections.Generic.List<float>();
            var owners = new System.Collections.Generic.List<Renderer>();
            var seen = new System.Collections.Generic.HashSet<Renderer>();
            foreach (Renderer candidate in renderers)
            {
                if (candidate == null || !seen.Add(candidate) || !LooksLikeRoof(candidate.name) ||
                    candidate.GetComponent<TextMesh>() != null)
                    continue;
                Bounds b = candidate.bounds;
                if (b.size.x < minSize || b.size.z < minSize || b.size.x * b.size.z > maxArea)
                    continue;
                list.Add(b);
                under.Add(CampaignRoofUnderside.Of(candidate));
                owners.Add(candidate);
            }
            roofs = list.ToArray();
            undersides = under.ToArray();
            roofRenderers = owners.ToArray();
        }

        private void Start()
        {
            // Older scenes without baked rooms: measure now (fallback).
            if ((roofs == null || roofs.Length == 0) && occluders != null && occluders.candidates != null)
                Bake(occluders.candidates);
            if (undersides == null || undersides.Length != roofs.Length)
                undersides = new float[roofs.Length];
            if (roofRenderers == null || roofRenderers.Length != roofs.Length)
                roofRenderers = new Renderer[roofs.Length];
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
            // Every roof over the hero stays drawn (owner: "atapnya jangan hilang").
            occluders.kept.Clear();
            bool under = false;
            float lowest = float.MaxValue;
            for (int i = 0; i < roofs.Length; i++)
            {
                if (!Covers(i, hero.position))
                    continue;
                under = true;
                if (roofRenderers[i] != null)
                    occluders.kept[roofRenderers[i]] = undersides[i];
                lowest = Mathf.Min(lowest, undersides[i]);
            }
            if (under)
            {
                lowestUnderside = lowest;
                outsideSince = -1f;
                if (!inside) Enter();
            }
            else if (inside)
            {
                if (outsideSince < 0f) outsideSince = Time.unscaledTime;
                else if (Time.unscaledTime - outsideSince >= leaveDelay) Leave();
            }
        }

        // Before the camera moves: keep the eye under the lowest roof over the hero.
        private void LateUpdate()
        {
            if (!inside || follow == null || occluders == null || occluders.target == null)
                return;
            float heroY = occluders.target.position.y;
            float clear = lowestUnderside - heroY;
            float focus = Mathf.Clamp(clear - 0.6f, 0.9f, focusHeight);
            if (Mathf.Approximately(follow.orbitFocusHeight, appliedFocus) || follow.orbitFocusHeight > focus)
            {
                follow.orbitFocusHeight = focus;
                appliedFocus = focus;
            }
            if (follow.orbitDistance > appliedMaxDistance)
                follow.orbitDistance = appliedMaxDistance;
            float room = clear - follow.orbitFocusHeight - headroom;
            float maxPitch = Mathf.Asin(Mathf.Clamp(room / Mathf.Max(0.5f, follow.orbitDistance), 0f, 1f)) * Mathf.Rad2Deg;
            if (follow.orbitPitch > maxPitch)
                follow.orbitPitch = maxPitch;
            if (follow.minPitch > follow.orbitPitch)
                follow.minPitch = follow.orbitPitch;
        }

        // Trees, palms and gate lintels are not rooms.
        public static bool LooksLikeRoof(string name)
        {
            string lower = name.ToLowerInvariant();
            return !(lower.StartsWith("palm") || lower.Contains("tajuk") || lower.Contains("pohon") ||
                lower.Contains("lintel") || lower.Contains("gerbang") || lower.Contains("banner") || lower.StartsWith("sign") ||
                lower.Contains("gerobak") || lower.Contains("payung") || lower.Contains("becak"));
        }

        public bool UnderRoof(Vector3 p)
        {
            if (roofs == null || undersides == null || undersides.Length != roofs.Length)
                return false;
            for (int i = 0; i < roofs.Length; i++)
                if (Covers(i, p))
                    return true;
            return false;
        }

        private bool Covers(int index, Vector3 p)
        {
            Bounds b = roofs[index];
            float above = undersides[index] - p.y;
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
            savedMaxDistance = follow.maxOrbitDistance;
            appliedMinDistance = Mathf.Min(savedMinDistance, distance * 0.8f);
            appliedMinPitch = Mathf.Min(savedMinPitch, pitch * 0.5f);
            appliedMaxDistance = Mathf.Min(savedMaxDistance, distance * 1.3f);
            appliedFocus = focusHeight;
            follow.minOrbitDistance = appliedMinDistance;
            follow.maxOrbitDistance = appliedMaxDistance;
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
            // Restore the camera the player had, unless a camera preset (KAMERA JAUH/DEKAT) was
            // chosen while inside: then that preset wins and only the pitch is kept in range.
            bool ours = Mathf.Approximately(follow.minOrbitDistance, appliedMinDistance) &&
                Mathf.Approximately(follow.maxOrbitDistance, appliedMaxDistance);
            if (ours)
            {
                follow.maxOrbitDistance = savedMaxDistance;
                follow.minOrbitDistance = savedMinDistance;
                follow.minPitch = savedMinPitch;
                follow.orbitFocusHeight = savedFocus;
                follow.orbitDistance = savedDistance;
                follow.orbitPitch = savedPitch;
            }
            else
            {
                follow.orbitPitch = Mathf.Max(follow.orbitPitch, follow.minPitch);
            }
        }

        private void OnDisable()
        {
            if (inside) Leave();
        }
    }
}
