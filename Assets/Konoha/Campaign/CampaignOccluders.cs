using UnityEngine;
using UnityEngine.Rendering;

namespace Konoha.Campaign
{
    // Hides tall decor (gate roofs, lintels, walls, palm crowns, signs) while it stands
    // between the camera and the hero, then shows it again (0.0.9.4). Hidden renderers keep
    // casting shadows, so the scene does not visibly change light. The orbit camera already
    // ignores these for zoom (0.0.9.2.1); this makes sure they never cover the hero either.
    // Bounds-vs-ray checks only: no physics, no transparent materials.
    public sealed class CampaignOccluders : MonoBehaviour
    {
        public Camera viewCamera;
        public Transform target;
        public Renderer[] candidates = new Renderer[0];
        // Keep a renderer hidden briefly after it stops blocking, so it does not flicker.
        public float holdSeconds = 0.35f;
        // 0.6.2: roofs the hero stands under (CampaignInteriorCamera); never hidden.
        public readonly System.Collections.Generic.HashSet<Renderer> kept = new System.Collections.Generic.HashSet<Renderer>();

        private float[] hiddenUntil = new float[0];
        private bool[] hidden = new bool[0];
        private ShadowCastingMode[] original = new ShadowCastingMode[0];

        private void Awake()
        {
            hiddenUntil = new float[candidates.Length];
            hidden = new bool[candidates.Length];
            original = new ShadowCastingMode[candidates.Length];
            for (int i = 0; i < candidates.Length; i++)
                if (candidates[i] != null)
                    original[i] = candidates[i].shadowCastingMode;
        }

        private void LateUpdate()
        {
            if (viewCamera == null)
                viewCamera = Camera.main;
            if (viewCamera == null || target == null)
            {
                RestoreAll();
                return;
            }

            Vector3 eye = viewCamera.transform.position;
            // Two sight lines: body centre and head, so a roof edge cannot cut the hero in half.
            Vector3 body = target.position + Vector3.up * 1.0f;
            Vector3 head = target.position + Vector3.up * 2.0f;
            Ray toBody = new Ray(eye, (body - eye).normalized);
            Ray toHead = new Ray(eye, (head - eye).normalized);
            float bodyLength = Vector3.Distance(eye, body) - 0.6f;
            float headLength = Vector3.Distance(eye, head) - 0.6f;
            float now = Time.unscaledTime;

            for (int i = 0; i < candidates.Length; i++)
            {
                Renderer candidate = candidates[i];
                if (candidate == null)
                    continue;

                Bounds bounds = candidate.bounds;
                bool blocking =
                    (bounds.IntersectRay(toBody, out float bodyHit) && bodyHit < bodyLength) ||
                    (bounds.IntersectRay(toHead, out float headHit) && headHit < headLength);

                if (kept.Count > 0 && eye.y < bounds.min.y && kept.Contains(candidate))
                {
                    blocking = false;
                    hiddenUntil[i] = 0f;
                }
                if (blocking)
                    hiddenUntil[i] = now + holdSeconds;

                bool hide = now < hiddenUntil[i];
                if (hide == hidden[i])
                    continue;

                hidden[i] = hide;
                Apply(candidate, original[i], hide);
            }
        }

        private void RestoreAll()
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                if (!hidden[i] || candidates[i] == null)
                    continue;
                hidden[i] = false;
                hiddenUntil[i] = 0f;
                Apply(candidates[i], original[i], false);
            }
        }

        private void OnDisable() => RestoreAll();

        // Shadow casters stay as shadow-only; renderers without shadows are simply switched off.
        private static void Apply(Renderer renderer, ShadowCastingMode mode, bool hide)
        {
            if (mode == ShadowCastingMode.Off)
                renderer.enabled = !hide;
            else
                renderer.shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : mode;
        }
    }
}
