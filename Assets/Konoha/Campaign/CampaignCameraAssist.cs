using Konoha.Character;
using UnityEngine;

namespace Konoha.Campaign
{
    // 0.6.5 KARIER (owner video): since roofs and trees no longer hide (0.6.4), a house, a cart
    // or a wall behind the hero pulled the orbit camera right into the hero's face. Now, when
    // something solid is closer than wantClear behind the hero, the camera rises (higher
    // pitch, looking down over the obstacle) until the view is clear again, then settles back
    // to the angle the player chose. Only in KARIER (CampaignOccluders.keepScenery) and never
    // inside a room (CampaignInteriorCamera holds the camera under the roof there).
    // Runs after CampaignInteriorCamera and before MobileCombatCamera.
    [DefaultExecutionOrder(-90)]
    public sealed class CampaignCameraAssist : MonoBehaviour
    {
        public MobileCombatCamera follow;
        public CampaignOccluders occluders;
        public float wantClear = 2.6f;
        public float maxPitch = 62f;
        public float riseSpeed = 80f;
        public float settleSpeed = 35f;

        private const float ThinObstacleWidth = 0.7f;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private float userPitch = -1f;
        private float lastSet = -1f;
        private float need = -1f;
        private float nextSearch;

        private void LateUpdate()
        {
            Transform hero = occluders != null ? occluders.target : null;
            if (follow == null || !follow.enabled || !follow.allowOrbit || hero == null || !occluders.keepScenery)
            {
                Release(true);
                return;
            }
            // The interior camera owns the pitch inside a room; keep our state so the camera
            // settles back to the player's angle after leaving.
            if (CampaignInteriorCamera.Inside)
                return;
            // KAMERA AWAL or a preset set an absolute pitch; a drag moves the player's angle by
            // its own change (a sideways swipe never adopts the raised pitch).
            if (lastSet < 0f || Mathf.Approximately(follow.orbitPitch, follow.resetPitch))
                userPitch = follow.orbitPitch;
            else if (!Mathf.Approximately(follow.orbitPitch, lastSet))
                userPitch = Mathf.Clamp(userPitch + (follow.orbitPitch - lastSet), follow.minPitch, 68f);

            if (Time.unscaledTime >= nextSearch || need < 0f)
            {
                nextSearch = Time.unscaledTime + 0.1f;
                Vector3 focus = hero.position + Vector3.up * follow.orbitFocusHeight;
                float want = Mathf.Max(wantClear, follow.orbitDistance * 0.35f);
                float found = userPitch;
                for (float pitch = userPitch; pitch <= maxPitch + 0.01f; pitch += 6f)
                {
                    found = Mathf.Min(pitch, maxPitch);
                    // Going lower again needs a little extra room (no wobble along a wall).
                    float margin = need >= 0f && found < need ? 0.4f : 0f;
                    if (Clearance(focus, found, hero) >= want + margin)
                        break;
                }
                need = found;
            }
            float speed = need > follow.orbitPitch ? riseSpeed : settleSpeed;
            follow.orbitPitch = Mathf.MoveTowards(follow.orbitPitch, need, speed * Time.deltaTime);
            lastSet = follow.orbitPitch;
        }

        // Free distance behind the hero at this pitch (thin poles, people and the hero ignored,
        // like MobileCombatCamera's own obstacle rule).
        private float Clearance(Vector3 focus, float pitch, Transform hero)
        {
            Vector3 direction = Quaternion.Euler(pitch, follow.orbitYaw, 0f) * Vector3.back;
            float distance = Mathf.Max(0.5f, follow.orbitDistance);
            int count = Physics.SphereCastNonAlloc(focus, .25f, direction, hits, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float clear = distance;
            for (int i = 0; i < count; i++)
            {
                Collider c = hits[i].collider;
                if (c == null || c is CharacterController || c.transform.IsChildOf(hero) || c.GetComponent<CampaignMonument>() != null)
                    continue;
                Vector3 size = c.bounds.size;
                if (size.x < ThinObstacleWidth && size.z < ThinObstacleWidth)
                    continue;
                if (hits[i].distance <= 0f)
                    continue; // Started inside it (e.g. standing in a doorway): not behind the hero.
                clear = Mathf.Min(clear, hits[i].distance);
            }
            return clear;
        }

        private void Release(bool restore)
        {
            if (restore && follow != null && lastSet >= 0f && userPitch >= 0f && Mathf.Approximately(follow.orbitPitch, lastSet))
                follow.orbitPitch = userPitch;
            lastSet = -1f;
            userPitch = -1f;
            need = -1f;
        }
    }
}
