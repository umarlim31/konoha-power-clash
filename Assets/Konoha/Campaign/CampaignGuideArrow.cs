using Unity.Netcode;
using UnityEngine;

namespace Konoha.Campaign
{
    // 0.6.0: replaces the tall gold light pillar of 0.5.0 (owner: "bikin visual kurang").
    // A small flat chevron lies on the ground next to the hero and points to the current
    // target; a low dashed ring with a short label marks the target itself. Both hide when
    // there is no target. Used by KARIER and by MODE PRESIDEN (Jalur Takhta).
    public sealed class CampaignGuideArrow : MonoBehaviour
    {
        public Transform arrow;
        public Transform ring;
        public TextMesh ringLabel;
        public float distanceFromHero = 1.7f;

        private static CampaignGuideArrow instance;
        private bool hasTarget;
        private Vector3 target;
        private float arrival;
        private string label = string.Empty;
        private string shownLabel;
        private Camera cachedCamera;

        public static bool HasTarget => instance != null && instance.hasTarget;

        private void Awake() => instance = this;

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        // Point to this ground position; the chevron hides inside arrivalRadius.
        public static void Show(Vector3 position, float arrivalRadius, string text)
        {
            if (instance == null)
                return;
            instance.hasTarget = true;
            instance.target = position;
            instance.arrival = Mathf.Max(0.5f, arrivalRadius);
            instance.label = text ?? string.Empty;
        }

        public static void Hide()
        {
            if (instance != null)
                instance.hasTarget = false;
        }

        private void LateUpdate()
        {
            Transform hero = LocalHero();
            bool active = hasTarget && hero != null;
            SetActive(ring, active);
            SetActive(ringLabel, active && label.Length > 0);
            if (!active)
            {
                SetActive(arrow, false);
                return;
            }

            float now = Time.time;
            if (ring != null)
            {
                ring.position = new Vector3(target.x, target.y + 0.06f, target.z);
                ring.rotation = Quaternion.Euler(0f, now * 40f, 0f);
                float pulse = 1f + 0.06f * Mathf.Sin(now * 4f);
                ring.localScale = new Vector3(pulse, 1f, pulse);
            }
            if (ringLabel != null)
            {
                ringLabel.transform.position = new Vector3(target.x, target.y + 2.3f, target.z);
                if (shownLabel != label)
                {
                    shownLabel = label;
                    ringLabel.text = label;
                }
                if (cachedCamera == null)
                    cachedCamera = Camera.main;
                if (cachedCamera != null)
                {
                    Vector3 look = ringLabel.transform.position - cachedCamera.transform.position;
                    if (look.sqrMagnitude > 0.01f)
                        ringLabel.transform.rotation = Quaternion.LookRotation(look);
                }
            }

            Vector3 delta = target - hero.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            bool point = distance > arrival;
            SetActive(arrow, point);
            if (!point || arrow == null)
                return;
            Vector3 direction = delta / distance;
            arrow.position = hero.position + direction * distanceFromHero + Vector3.up * 0.12f;
            arrow.rotation = Quaternion.LookRotation(direction, Vector3.up);
            float beat = 1f + 0.12f * Mathf.Sin(now * 6f);
            arrow.localScale = new Vector3(beat, 1f, beat);
        }

        private static void SetActive(Component target, bool value)
        {
            if (target != null && target.gameObject.activeSelf != value)
                target.gameObject.SetActive(value);
        }

        private static Transform LocalHero()
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null || manager.LocalClient == null || manager.LocalClient.PlayerObject == null)
                return null;
            return manager.LocalClient.PlayerObject.transform;
        }
    }
}
