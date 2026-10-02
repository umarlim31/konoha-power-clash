using UnityEngine;

namespace Konoha.Campaign
{
    // 0.2.3: procedural human body for heroes and organisation members in Jalur Takhta.
    // Built from primitives by the editor generator (CampaignRigBuilder) with pivots at the
    // joints; this component poses it every frame: idle breathing, run cycle from the actor's
    // own movement, punches, flinches, skill poses, leaps/dashes and lying down (Runtuh).
    //
    // Purely presentation: reads the actor's transform and receives calls from
    // CampaignBodies / CampaignEnemy. It never moves the actor or touches combat.
    public sealed class CampaignHumanoid : MonoBehaviour
    {
        public Transform pelvis, spine, head;
        public Transform shoulderLeft, shoulderRight, elbowLeft, elbowRight;
        public Transform hipLeft, hipRight, kneeLeft, kneeRight;
        // Actor whose movement drives the run cycle (hero root / enemy root).
        public Transform motionSource;
        // Right hand holds something (Ketua's gavel): the arm stays raised.
        public GameObject heldItem;
        public float runSpeedForFullStride = 5f;
        // 0.6.0 KARIER: seated on the ojol motor (legs forward, hands on the handlebar) and
        // a cement sack held on the right shoulder. Set by KarierController.
        public bool riding;
        public bool carrying;

        public enum Travel { None, Dash, Leap }

        private Renderer[] renderers = new Renderer[0];
        private MaterialPropertyBlock block;
        private Vector3[] flashSaved = new Vector3[0];
        private bool[] flashHadColor = new bool[0];
        private Color flashColor;
        private float flashUntil = -1f;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private Vector3 lastPosition;
        private bool hasLast;
        private float speed;
        private float verticalSpeed;
        private float phase;
        private float pelvisBaseY;

        private float attackStart = -10f, attackDuration = .3f;
        private bool attackLeft;
        private float hitStart = -10f;
        private Vector3 hitFrom;
        private float skillStart = -10f;
        private int skillSlot;
        private float freezeUntil = -1f;
        private float down;
        private bool wantDown;

        private Travel expectedTravel;
        private float expectUntil = -1f;
        private Travel travel;
        private Vector3 travelFrom;
        private float travelStart, travelDuration;
        private float leapHeight;

        public Vector3 LastPosition => hasLast ? lastPosition : transform.position;
        public bool IsTravelling => travel != Travel.None;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            flashSaved = new Vector3[renderers.Length];
            flashHadColor = new bool[renderers.Length];
            block = new MaterialPropertyBlock();
            if (pelvis != null)
                pelvisBaseY = pelvis.localPosition.y;
            if (motionSource == null && transform.parent != null)
                motionSource = transform.root;
        }

        private void OnEnable() => hasLast = false;

        // --- Triggers ---------------------------------------------------------------------

        public void PlayAttack(float duration = .3f)
        {
            attackStart = Time.time;
            attackDuration = Mathf.Max(.12f, duration);
            attackLeft = !attackLeft;
        }

        public void PlayHit(Vector3 fromWorld)
        {
            hitStart = Time.time;
            hitFrom = fromWorld;
        }

        public void PlaySkill(int slot)
        {
            skillStart = Time.time;
            skillSlot = slot;
        }

        // Hit-stop: hold the current pose for a few frames on impact.
        public void Freeze(float seconds) => freezeUntil = Mathf.Max(freezeUntil, Time.time + seconds);

        public void SetDown(bool value) => wantDown = value;

        // The next big jump of the actor (within 0.4 s) is a dash or leap, not a teleport:
        // the body travels from the old position instead of snapping.
        public void ExpectTravel(Travel kind)
        {
            expectedTravel = kind;
            expectUntil = Time.time + .4f;
        }

        public void Flash(Color color, float seconds)
        {
            // Already flashing: just extend it (a second colour would never be restored).
            if (flashUntil >= 0f)
            {
                flashUntil = Mathf.Max(flashUntil, Time.time + seconds);
                return;
            }
            if (flashUntil < 0f)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null) continue;
                    renderers[i].GetPropertyBlock(block);
                    flashHadColor[i] = block.HasColor(BaseColor);
                    Color saved = block.GetColor(BaseColor);
                    flashSaved[i] = new Vector3(saved.r, saved.g, saved.b);
                    block.SetColor(BaseColor, color);
                    renderers[i].SetPropertyBlock(block);
                }
            }
            flashColor = color;
            flashUntil = Time.time + seconds;
        }

        private void EndFlash()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].GetPropertyBlock(block);
                // Someone recoloured it during the flash (e.g. TUMBANG darkening): keep theirs.
                if (block.GetColor(BaseColor) != flashColor)
                    continue;
                if (flashHadColor[i])
                {
                    block.SetColor(BaseColor, new Color(flashSaved[i].x, flashSaved[i].y, flashSaved[i].z, 1f));
                    renderers[i].SetPropertyBlock(block);
                }
                else
                {
                    renderers[i].SetPropertyBlock(null);
                }
            }
            flashUntil = -1f;
        }

        // --- Pose ---------------------------------------------------------------------------

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            float now = Time.time;
            if (flashUntil >= 0f && now >= flashUntil)
                EndFlash();

            Transform source = motionSource != null ? motionSource : transform;
            Vector3 position = source.position;
            if (!hasLast)
            {
                lastPosition = position;
                hasLast = true;
            }

            Vector3 delta = position - lastPosition;
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            if (flat.magnitude > 1.4f)
            {
                // Big jump in one frame: a dash/leap we were told about, else a teleport.
                if (now <= expectUntil && expectedTravel != Travel.None)
                {
                    travel = expectedTravel;
                    travelFrom = lastPosition;
                    travelStart = now;
                    travelDuration = travel == Travel.Leap ? .36f : .16f;
                    leapHeight = travel == Travel.Leap ? Mathf.Clamp(flat.magnitude * .45f, 1.2f, 2.4f) : 0f;
                    expectedTravel = Travel.None;
                }
                speed = 0f;
            }
            else if (dt > 0f)
            {
                speed = Mathf.Lerp(speed, flat.magnitude / dt, 1f - Mathf.Exp(-12f * dt));
                verticalSpeed = Mathf.Lerp(verticalSpeed, delta.y / dt, 1f - Mathf.Exp(-10f * dt));
            }
            lastPosition = position;

            ApplyTravel(source, now);

            down = Mathf.MoveTowards(down, wantDown ? 1f : 0f, dt * 3.2f);
            if (now < freezeUntil)
                return;

            float run = Mathf.Clamp01(speed / Mathf.Max(.5f, runSpeedForFullStride));
            bool airborne = Mathf.Abs(verticalSpeed) > 1.6f || travel == Travel.Leap;
            phase += dt * (4f + 7f * run);
            float swing = Mathf.Sin(phase);

            // Base: idle breathing + run cycle.
            float spineX = Mathf.Sin(now * 2.1f) * 1.2f + 9f * run;
            float spineY = 0f, spineZ = 0f;
            float headX = -4f * run;
            float armL = swing * 32f * run + 4f, armR = -swing * 32f * run + 4f;
            float armLZ = 6f, armRZ = -6f;
            float elbowL = -12f - 28f * run, elbowR = -12f - 28f * run;
            float legL = -swing * 36f * run, legR = swing * 36f * run;
            float kneeL = Mathf.Max(0f, Mathf.Cos(phase)) * 50f * run + 3f;
            float kneeR = Mathf.Max(0f, -Mathf.Cos(phase)) * 50f * run + 3f;
            float bob = Mathf.Abs(swing) * .05f * run;
            float lunge = 0f;

            if (airborne)
            {
                legL = -35f; legR = -10f; kneeL = 70f; kneeR = 40f;
                armL = -30f; armR = -30f; armLZ = 35f; armRZ = -35f;
            }

            if (heldItem != null && heldItem.activeInHierarchy)
            {
                armR = -62f; elbowR = -35f; armRZ = -4f;
            }

            if (riding)
            {
                legL = legR = -78f; kneeL = kneeR = 82f;
                armL = armR = -55f; elbowL = elbowR = -25f; armLZ = 8f; armRZ = -8f;
                spineX = 6f + Mathf.Sin(now * 2.1f) * .6f; headX = 0f; bob = 0f;
            }
            else if (carrying)
            {
                armR = -150f; elbowR = -95f; armRZ = -10f;
                spineX += 4f;
            }

            // Punch: wind-up, strike, recover; arms alternate.
            float a = (now - attackStart) / attackDuration;
            if (a >= 0f && a <= 1f)
            {
                float strike = a < .3f ? -Mathf.SmoothStep(0f, 1f, a / .3f) * .45f
                    : a < .55f ? Mathf.Lerp(-.45f, 1f, Mathf.SmoothStep(0f, 1f, (a - .3f) / .25f))
                    : Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0f, 1f, (a - .55f) / .45f));
                float shoulder = -95f * Mathf.Max(0f, strike) + 45f * Mathf.Max(0f, -strike);
                float elbow = strike > 0f ? Mathf.Lerp(-80f, -6f, strike) : -85f;
                if (attackLeft) { armL = shoulder; elbowL = elbow; armLZ = 4f; }
                else { armR = shoulder; elbowR = elbow; armRZ = -4f; }
                spineY = (attackLeft ? -1f : 1f) * 26f * strike;
                spineX += 6f * Mathf.Max(0f, strike);
                lunge = .14f * Mathf.Max(0f, strike);
            }

            // Flinch away from the hit.
            float h = (now - hitStart) / .24f;
            if (h >= 0f && h <= 1f)
            {
                float k = Mathf.Sin(h * Mathf.PI);
                Vector3 local = source.InverseTransformPoint(hitFrom);
                float fromFront = local.z >= 0f ? 1f : -1f;
                spineX -= 20f * k * fromFront;
                spineZ += (local.x >= 0f ? 1f : -1f) * 7f * k;
                headX -= 18f * k * fromFront;
                armLZ += 18f * k; armRZ -= 18f * k;
            }

            // Skill poses: S1 thrust, S2 both arms forward, ultimate both arms raised.
            float s = (now - skillStart) / .55f;
            if (s >= 0f && s <= 1f)
            {
                float k = Mathf.Sin(Mathf.Min(1f, s * 1.6f) * Mathf.PI * .5f) * (1f - Mathf.SmoothStep(.7f, 1f, s));
                switch (skillSlot)
                {
                    case 1:
                        armR = Mathf.Lerp(armR, -100f, k); elbowR = Mathf.Lerp(elbowR, -5f, k);
                        armL = Mathf.Lerp(armL, 30f, k);
                        spineX += 10f * k;
                        break;
                    case 2:
                        armL = Mathf.Lerp(armL, -80f, k); armR = Mathf.Lerp(armR, -80f, k);
                        armLZ = Mathf.Lerp(armLZ, 22f, k); armRZ = Mathf.Lerp(armRZ, -22f, k);
                        elbowL = Mathf.Lerp(elbowL, -10f, k); elbowR = Mathf.Lerp(elbowR, -10f, k);
                        break;
                    default:
                        armL = Mathf.Lerp(armL, -165f, k); armR = Mathf.Lerp(armR, -165f, k);
                        armLZ = Mathf.Lerp(armLZ, 25f, k); armRZ = Mathf.Lerp(armRZ, -25f, k);
                        spineX -= 8f * k; headX -= 15f * k;
                        break;
                }
            }

            Set(spine, spineX, spineY, spineZ);
            Set(head, headX, 0f, 0f);
            Set(shoulderLeft, armL, 0f, armLZ);
            Set(shoulderRight, armR, 0f, armRZ);
            Set(elbowLeft, elbowL, 0f, 0f);
            Set(elbowRight, elbowR, 0f, 0f);
            Set(hipLeft, legL, 0f, 0f);
            Set(hipRight, legR, 0f, 0f);
            Set(kneeLeft, kneeL, 0f, 0f);
            Set(kneeRight, kneeR, 0f, 0f);
            if (pelvis != null)
                pelvis.localPosition = new Vector3(0f, pelvisBaseY + bob, lunge);

            // Runtuh: fall onto the back.
            transform.localRotation = Quaternion.Euler(-88f * Smooth(down), 0f, 0f);
            // While travelling ApplyTravel owns the position.
            if (travel == Travel.None)
                transform.localPosition = new Vector3(0f, .22f * Smooth(down), 0f);
        }

        // Dash/leap: the body slides (and arcs) from where the actor was to where it is now.
        private void ApplyTravel(Transform source, float now)
        {
            if (travel == Travel.None)
                return;
            float t = (now - travelStart) / Mathf.Max(.01f, travelDuration);
            if (t >= 1f || transform.parent == null)
            {
                travel = Travel.None;
                transform.localPosition = Vector3.zero;
                return;
            }
            float e = 1f - (1f - t) * (1f - t);
            Vector3 world = Vector3.Lerp(travelFrom, source.position, e) + Vector3.up * (leapHeight * 4f * t * (1f - t));
            transform.position = world;
        }

        private static float Smooth(float x) => x * x * (3f - 2f * x);

        private static void Set(Transform joint, float x, float y, float z)
        {
            if (joint != null)
                joint.localRotation = Quaternion.Euler(x, y, z);
        }
    }
}
