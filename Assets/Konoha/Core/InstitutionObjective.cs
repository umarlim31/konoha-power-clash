using UnityEngine;

namespace Konoha.Core
{
    // Greybox-only local capture state. Network authority and contested captures come later.
    public sealed class InstitutionObjective : MonoBehaviour
    {
        public string displayName;
        public Renderer indicator;
        public float captureSeconds = 2.5f;
        public float radius = 2f;
        public bool Captured { get; private set; }
        public float Progress { get; private set; }

        public void Tick(Vector3 playerPosition, float deltaTime)
        {
            if (Captured || deltaTime <= 0f) return;
            Vector2 distance = new Vector2(playerPosition.x - transform.position.x,
                playerPosition.z - transform.position.z);
            if (distance.sqrMagnitude > radius * radius)
            {
                Progress = 0f;
                return;
            }
            Progress = Mathf.Min(captureSeconds, Progress + deltaTime);
            if (Progress >= captureSeconds)
            {
                Captured = true;
                if (indicator != null) indicator.material.SetColor("_BaseColor", new Color(0.2f, 0.87f, 0.54f));
            }
        }
    }
}
