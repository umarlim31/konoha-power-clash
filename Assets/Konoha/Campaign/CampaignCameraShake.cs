using UnityEngine;

namespace Konoha.Campaign
{
    // 0.2.3: short camera shake for impacts (Jalur Takhta only). It runs after the orbit
    // camera (execution order) and only adds a small ROTATION on top of the camera's own
    // absolute look rotation, which the camera recomputes every frame. The camera position
    // is never touched, so orbit, zoom, KAMERA AWAL and the occluders are unaffected.
    [DefaultExecutionOrder(10000)]
    public sealed class CampaignCameraShake : MonoBehaviour
    {
        public static CampaignCameraShake Instance { get; private set; }

        // Degrees of shake at strength 1.
        public float maxDegrees = 2.2f;

        private float strength;
        private float until;
        private float duration = .2f;
        private bool applied;
        private Quaternion lastBase, lastResult;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // Strength 0..1; a stronger shake replaces a weaker running one.
        public static void Shake(float amount, float seconds)
        {
            CampaignCameraShake shake = Instance;
            if (shake == null || !shake.isActiveAndEnabled)
                return;
            float remaining = shake.CurrentStrength();
            if (amount < remaining)
                return;
            shake.strength = Mathf.Clamp01(amount);
            shake.duration = Mathf.Max(.05f, seconds);
            shake.until = Time.unscaledTime + shake.duration;
        }

        private float CurrentStrength()
        {
            float left = until - Time.unscaledTime;
            return left <= 0f ? 0f : strength * (left / duration);
        }

        private void LateUpdate()
        {
            // If no camera script rewrote the rotation this frame (hero select, paused), undo
            // our previous offset first so the shake can never accumulate.
            if (applied && transform.rotation == lastResult)
                transform.rotation = lastBase;
            applied = false;

            float k = CurrentStrength();
            if (k <= 0f)
                return;
            float t = Time.unscaledTime * 38f;
            float degrees = maxDegrees * k;
            lastBase = transform.rotation;
            transform.rotation = lastBase * Quaternion.Euler(
                (Mathf.PerlinNoise(t, .3f) - .5f) * 2f * degrees,
                (Mathf.PerlinNoise(.7f, t) - .5f) * 2f * degrees,
                (Mathf.PerlinNoise(t, t * .5f) - .5f) * degrees);
            lastResult = transform.rotation;
            applied = true;
        }
    }
}
