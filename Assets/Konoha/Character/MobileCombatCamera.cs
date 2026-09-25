using Konoha.Networking;
using UnityEngine;

namespace Konoha.Character
{
    public sealed class MobileCombatCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 13.8f, -10.8f);
        public Vector3 crowdedOffset = new Vector3(0f, 16.4f, -13.4f);
        [Min(0.01f)] public float followTime = 0.17f;
        [Min(0.01f)] public float zoomTime = 0.24f;
        // Enabled only for the solo scene. Keep the arena in frame when a player
        // explores along the boundary; the network match keeps its original camera.
        public bool limitFocusToArena;
        public Vector2 focusXLimits = new Vector2(-6.5f, 6.5f);
        public Vector2 focusZLimits = new Vector2(-6f, 5f);

        private Vector3 smoothVelocity;
        private Vector3 currentOffset;
        private Vector3 offsetVelocity;
        private float crowdFactor;
        private float nextCrowdCheck;
        private bool initialized;

        private void LateUpdate()
        {
            if (target == null)
                return;

            if (Time.unscaledTime >= nextCrowdCheck)
            {
                nextCrowdCheck = Time.unscaledTime + 0.28f;
                crowdFactor = EvaluateCrowding();
            }

            Vector3 desiredOffset = Vector3.Lerp(offset, crowdedOffset, crowdFactor);
            currentOffset = initialized
                ? Vector3.SmoothDamp(currentOffset, desiredOffset, ref offsetVelocity, zoomTime)
                : desiredOffset;

            Vector3 focus = GetFocusPoint();
            if (limitFocusToArena)
            {
                focus.x = Mathf.Clamp(focus.x, focusXLimits.x, focusXLimits.y);
                focus.z = Mathf.Clamp(focus.z, focusZLimits.x, focusZLimits.y);
            }
            Vector3 destination = focus + currentOffset;

            transform.position = initialized
                ? Vector3.SmoothDamp(transform.position, destination, ref smoothVelocity, followTime)
                : destination;

            initialized = true;

            Vector3 lookDirection = focus - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }

        private Vector3 GetFocusPoint()
        {
            Vector3 focus = target.position;
            NetworkMatchManager match = NetworkMatchManager.Instance;

            if (match == null || !match.AllowsGameplay)
                return focus;

            Vector3 chair = match.ChairPosition;
            Vector3 delta = chair - target.position;
            delta.y = 0f;

            if (delta.sqrMagnitude <= 110f)
                focus = Vector3.Lerp(target.position, chair, 0.22f);

            return focus;
        }

        private float EvaluateCrowding()
        {
            if (limitFocusToArena)
                return 0f;
            NetworkPlayerCombat[] actors =
                Object.FindObjectsByType<NetworkPlayerCombat>(FindObjectsSortMode.None);

            int nearby = 0;
            Vector3 origin = target.position;

            foreach (NetworkPlayerCombat actor in actors)
            {
                if (actor == null || !actor.IsSpawned || actor.IsKnockedOut)
                    continue;

                Vector3 delta = actor.transform.position - origin;
                delta.y = 0f;

                if (delta.sqrMagnitude <= 64f)
                    nearby++;
            }

            if (nearby <= 2)
                return 0f;

            return Mathf.InverseLerp(2f, 6f, nearby);
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused)
                initialized = false;
        }
    }
}
