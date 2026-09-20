using UnityEngine;

namespace Konoha.Character
{
    public sealed class MobileCombatCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 13f, -10f);
        [Min(0.01f)] public float followTime = 0.16f;
        private Vector3 smoothVelocity;
        private bool initialized;
        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 destination = target.position + offset;
            transform.position = initialized
                ? Vector3.SmoothDamp(transform.position, destination, ref smoothVelocity, followTime)
                : destination;
            initialized = true;
            // Fixed azimuth keeps screen-up aligned to world-forward movement.
            transform.rotation = Quaternion.LookRotation(-offset, Vector3.up);
        }
        private void OnApplicationPause(bool paused) { if (!paused) initialized = false; }
    }
}
