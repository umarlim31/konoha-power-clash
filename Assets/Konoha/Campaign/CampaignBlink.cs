using UnityEngine;

namespace Konoha.Campaign
{
    // 0.3.4: eyes of heroes, organisation members and warga blink now and then (a short
    // squash of the eye whites, pupils are their children), so faces read as alive.
    // Presentation only; nothing else reads it.
    public sealed class CampaignBlink : MonoBehaviour
    {
        public Transform[] eyes = new Transform[0];
        public float minInterval = 2.2f, maxInterval = 5.5f, closedSeconds = .12f;

        private Vector3[] open = new Vector3[0];
        private float nextBlink, reopenAt;
        private bool closed;

        private void Awake()
        {
            open = new Vector3[eyes.Length];
            for (int i = 0; i < eyes.Length; i++)
                if (eyes[i] != null) open[i] = eyes[i].localScale;
            nextBlink = Time.time + Random.Range(.3f, maxInterval);
        }

        private void Update()
        {
            float now = Time.time;
            if (!closed && now >= nextBlink)
            {
                closed = true;
                reopenAt = now + closedSeconds;
                Set(.12f);
            }
            else if (closed && now >= reopenAt)
            {
                closed = false;
                nextBlink = now + Random.Range(minInterval, maxInterval);
                Set(1f);
            }
        }

        private void OnDisable()
        {
            if (closed) Set(1f);
            closed = false;
        }

        private void Set(float openness)
        {
            for (int i = 0; i < eyes.Length; i++)
            {
                if (eyes[i] == null) continue;
                Vector3 s = open[i];
                eyes[i].localScale = new Vector3(s.x, s.y * openness, s.z);
            }
        }
    }
}
