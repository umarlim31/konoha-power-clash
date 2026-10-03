using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Konoha.Campaign
{
    // 0.2.2 "Kota Hidup": cosmetic street life around the Konoha capital. Traffic drives the
    // jalan raya (left-hand traffic, as in Indonesia), warga stroll the pavements and gardens,
    // vendors and onlookers gesture at their stalls, and the plaza fountains spray.
    //
    // Everything is local presentation: no NetworkObjects, no colliders, no physics, and no
    // effect on combat or the route. One Update drives every transform (no per-NPC scripts).
    // Pieces far from the camera are switched off to keep the Android draw-call budget.
    public sealed class CampaignCityLife : MonoBehaviour
    {
        [Serializable]
        public sealed class Vehicle
        {
            public Transform body;
            public float speed = 7f;
            // Ring road: +1 runs the corners in order (counter-clockwise seen from above),
            // -1 the other way. Highway (ring = false): +1 drives east, -1 west, along x.
            public float direction = 1f;
            public bool ring = true;
            // Metres along the ring (ring) or the x position (highway).
            public float distance;
            // Distance to the LEFT of the travel direction: Indonesia drives on the left.
            // Ring vehicles use LaneRing(ringCorners, laneOffset * direction).
            public float laneOffset = 2f;
            public float highwayZ;
            public bool motor;
            [NonSerialized] public Vector3[] laneRing;
            [NonSerialized] public float nextHorn;
        }

        [Serializable]
        public sealed class Walker
        {
            public Transform root;
            public Transform legLeft, legRight, armLeft, armRight;
            // Walkers go back and forth between two points; idle warga stay at `from`.
            public Vector3 from, to;
            public float speed = 1.2f;
            [Range(0f, 1f)] public float progress;
            public float direction = 1f;
            public bool idle;
            // 0.5.0 kepo: a phone in the right hand, raised to record fights; filming warga
            // (onlookers of an accident) keep it up all the time.
            public GameObject phone;
            public bool filming;
            // 0.6.3: taken out by the generator (no free ground left after the KARIER boxes).
            public bool removed;
            [NonSerialized] public float phase, dodge, panicUntil;
            // 0.6.3: knocked down by a motor (TABRAK), short stops to check the phone, easing.
            [NonSerialized] public float downUntil, pauseUntil, nextPause, pace = 1f;
            [NonSerialized] public bool down, pausePhone;
        }

        public Vehicle[] vehicles = new Vehicle[0];
        public Walker[] walkers = new Walker[0];
        // Fountain jets (pulsing water columns) and the nozzle each droplet arcs from.
        public Transform[] jets = new Transform[0];
        public Vector3[] nozzles = new Vector3[0];
        public Transform[] droplets = new Transform[0];
        public float dropletReach = .45f, dropletHeight = .55f;

        // Closed ring road around the capital (corner points, counter-clockwise).
        public Vector3[] ringCorners = new Vector3[0];
        public float roadHalfLength = 104f;
        public float cullDistance = 60f;
        // Warga step aside when the hero comes this close, and run from fights nearby.
        public float personalSpace = 2.4f;
        // 0.6.3: KarierController widens both while the hero rides (warga jump aside early).
        [NonSerialized] public float dodgeWidth = 1.6f, dodgeRate = 3f;
        public float fleeDistance = 6f;
        // 0.5.0 kepo: warga stop and record a fight from this far, and only back off when it
        // comes closer than fleeDistance (they no longer run away from every fight).
        public float kepoDistance = 13f;
        public float hornMinSeconds = 7f, hornMaxSeconds = 16f;
        // 0.6.3 night (KarierController): two of three strollers stay home after dark.
        [NonSerialized] public bool night;
        // 0.2.7 atmosphere: songbirds around the listener and the bakso seller's bowl.
        public Vector3[] bowlSpots = new Vector3[0];
        private float nextBird, nextBowl;

        private readonly List<Vector3> threats = new List<Vector3>();
        private Vector3[] jetBaseScale = new Vector3[0];
        private float nextCull;
        private float nextHorn;
        private System.Random random;

        private void Awake()
        {
            random = new System.Random(2202);
            jetBaseScale = new Vector3[jets.Length];
            for (int i = 0; i < jets.Length; i++)
                if (jets[i] != null)
                    jetBaseScale[i] = jets[i].localScale;
            for (int i = 0; i < walkers.Length; i++)
                if (walkers[i] != null)
                    walkers[i].phase = i * 1.7f;
            nextHorn = Time.time + NextHornDelay();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            Vector3? hero = LocalHeroPosition();
            CollectThreats();
            DriveTraffic(dt, hero);
            MoveWalkers(dt, hero);
            SprayFountains();
            StreetSounds();

            if (Time.unscaledTime >= nextCull)
            {
                nextCull = Time.unscaledTime + 0.5f;
                Cull();
            }
        }

        // --- Traffic --------------------------------------------------------------------

        private void DriveTraffic(float dt, Vector3? hero)
        {
            float perimeter = RingLength(ringCorners);
            bool honked = false;
            // Cars queue behind a stopped car (stopped positions of the previous frame).
            stoppedNow.Clear();
            for (int i = 0; i < vehicles.Length; i++)
            {
                Vehicle vehicle = vehicles[i];
                if (vehicle == null || vehicle.body == null)
                    continue;

                // 0.6.5: KARIER walks onto the ring road; cars brake for someone in front of
                // them (and honk) instead of driving through.
                if (hero.HasValue)
                {
                    Vector3 toHero = hero.Value - vehicle.body.position;
                    toHero.y = 0f;
                    Vector3 ahead = vehicle.body.forward;
                    ahead.y = 0f;
                    float along = Vector3.Dot(toHero, ahead.normalized);
                    float side = Mathf.Abs(Vector3.Dot(toHero, new Vector3(ahead.z, 0f, -ahead.x).normalized));
                    if (along > -0.5f && along < (vehicle.motor ? 3.5f : 5f) && side < (vehicle.motor ? 1.1f : 1.6f))
                    {
                        if (!honked && Time.time >= vehicle.nextHorn && CampaignAudio.Instance != null)
                        {
                            vehicle.nextHorn = Time.time + 3f;
                            honked = true;
                            CampaignAudio.Instance.PlayAt(CampaignSound.Klakson, vehicle.body.position, 0.6f, vehicle.motor ? 1.35f : 1f);
                        }
                        stoppedNow.Add(vehicle.body.position);
                        continue;
                    }
                }
                if (BehindStopped(vehicle))
                {
                    stoppedNow.Add(vehicle.body.position);
                    continue;
                }

                Vector3 heading;
                Vector3 point;
                if (vehicle.ring && perimeter > 0f)
                {
                    // Each lane is its own offset ring, so cars turn the corners without jumping.
                    if (vehicle.laneRing == null || vehicle.laneRing.Length != ringCorners.Length)
                        vehicle.laneRing = LaneRing(ringCorners, vehicle.laneOffset * vehicle.direction);
                    float laneLength = RingLength(vehicle.laneRing);
                    vehicle.distance = Mathf.Repeat(vehicle.distance + vehicle.speed * vehicle.direction * dt, laneLength);
                    point = RingPoint(vehicle.laneRing, vehicle.distance, out heading);
                    heading *= vehicle.direction;
                }
                else
                {
                    vehicle.distance = WrapRoad(vehicle.distance + vehicle.speed * vehicle.direction * dt, roadHalfLength);
                    heading = new Vector3(vehicle.direction, 0f, 0f);
                    // Keep left: the lane lies to the left of the travel direction.
                    point = new Vector3(vehicle.distance, 0f, vehicle.highwayZ) +
                        new Vector3(-heading.z, 0f, heading.x) * vehicle.laneOffset;
                }

                vehicle.body.position = point;
                // Turn smoothly at the corners instead of snapping 90 degrees.
                vehicle.body.rotation = Quaternion.RotateTowards(vehicle.body.rotation,
                    Quaternion.LookRotation(heading, Vector3.up), 150f * dt);
            }

            stoppedBefore.Clear();
            stoppedBefore.AddRange(stoppedNow);

            if (Time.time >= nextHorn)
            {
                nextHorn = Time.time + NextHornDelay();
                Vehicle loud = NearestVehicle();
                if (loud != null && CampaignAudio.Instance != null)
                    CampaignAudio.Instance.PlayAt(CampaignSound.Klakson, loud.body.position, 0.45f, loud.motor ? 1.35f : 1f);
            }
        }

        private readonly List<Vector3> stoppedNow = new List<Vector3>();
        private readonly List<Vector3> stoppedBefore = new List<Vector3>();

        private bool BehindStopped(Vehicle vehicle)
        {
            if (stoppedBefore.Count == 0)
                return false;
            Vector3 ahead = vehicle.body.forward;
            ahead.y = 0f;
            ahead.Normalize();
            var right = new Vector3(ahead.z, 0f, -ahead.x);
            float gap = vehicle.motor ? 3f : 5.5f;
            foreach (Vector3 stopped in stoppedBefore)
            {
                Vector3 d = stopped - vehicle.body.position;
                d.y = 0f;
                float along = Vector3.Dot(d, ahead);
                if (along > 0.3f && along < gap && Mathf.Abs(Vector3.Dot(d, right)) < 1.4f)
                    return true;
            }
            return false;
        }

        public static float RingLength(Vector3[] corners)
        {
            if (corners == null || corners.Length < 2)
                return 0f;
            float length = 0f;
            for (int i = 0; i < corners.Length; i++)
                length += Vector3.Distance(corners[i], corners[(i + 1) % corners.Length]);
            return length;
        }

        // The ring shifted `offset` metres to the LEFT of its running direction (mitred corners).
        // A counter-clockwise ring shifted left moves inward; a negative offset moves outward,
        // which is the left-hand lane of traffic running the other way.
        public static Vector3[] LaneRing(Vector3[] corners, float offset)
        {
            if (corners == null)
                return new Vector3[0];
            int n = corners.Length;
            var result = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 previous = corners[(i + n - 1) % n], current = corners[i], next = corners[(i + 1) % n];
                Vector3 inDir = (current - previous).normalized, outDir = (next - current).normalized;
                var leftIn = new Vector3(-inDir.z, 0f, inDir.x);
                var leftOut = new Vector3(-outDir.z, 0f, outDir.x);
                float join = 1f + Vector3.Dot(leftIn, leftOut);
                result[i] = join > .05f ? current + (leftIn + leftOut) * (offset / join) : current + leftOut * offset;
            }
            return result;
        }

        // Point `distance` metres along the closed polyline, with the forward direction there.
        public static Vector3 RingPoint(Vector3[] corners, float distance, out Vector3 heading)
        {
            heading = Vector3.forward;
            if (corners == null || corners.Length < 2)
                return Vector3.zero;
            float remaining = Mathf.Repeat(distance, Mathf.Max(.001f, RingLength(corners)));
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 a = corners[i], b = corners[(i + 1) % corners.Length];
                float segment = Vector3.Distance(a, b);
                if (segment <= .0001f)
                    continue;
                heading = (b - a) / segment;
                if (remaining <= segment)
                    return a + heading * remaining;
                remaining -= segment;
            }
            return corners[0];
        }

        private void StreetSounds()
        {
            CampaignAudio audio = CampaignAudio.Instance;
            if (audio == null)
                return;
            float now = Time.time;
            if (now >= nextBird)
            {
                nextBird = now + 5f + (float)random.NextDouble() * 7f;
                audio.Play(CampaignSound.Burung, .22f, .88f + (float)random.NextDouble() * .3f);
            }
            if (bowlSpots.Length > 0 && now >= nextBowl)
            {
                nextBowl = now + 7f + (float)random.NextDouble() * 6f;
                audio.PlayAt(CampaignSound.Mangkok, bowlSpots[random.Next(bowlSpots.Length)], .55f);
            }
        }

        // Cars leaving one end of the road re-enter at the other, so traffic never ends.
        public static float WrapRoad(float x, float halfLength)
        {
            float span = Mathf.Max(1f, halfLength * 2f);
            return Mathf.Repeat(x + halfLength, span) - halfLength;
        }

        private Vehicle NearestVehicle()
        {
            Camera view = Camera.main;
            if (view == null)
                return null;
            Vehicle best = null;
            float bestDistance = float.MaxValue;
            foreach (Vehicle vehicle in vehicles)
            {
                if (vehicle == null || vehicle.body == null)
                    continue;
                float d = (vehicle.body.position - view.transform.position).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = vehicle; }
            }
            return best;
        }

        private float NextHornDelay() =>
            hornMinSeconds + (float)random.NextDouble() * Mathf.Max(0f, hornMaxSeconds - hornMinSeconds);

        // --- Warga ------------------------------------------------------------------------

        private void CollectThreats()
        {
            threats.Clear();
            // 0.5.0: only members actually fighting count (near the hero, still standing);
            // members waiting in their hall do not make warga stop and stare all day.
            Vector3? hero = LocalHeroPosition();
            IReadOnlyList<CampaignEnemy> enemies = CampaignEnemy.Active;
            for (int i = 0; i < enemies.Count; i++)
            {
                CampaignEnemy enemy = enemies[i];
                if (enemy == null || enemy.IsOutOfFight || !hero.HasValue)
                    continue;
                Vector3 d = enemy.transform.position - hero.Value;
                d.y = 0f;
                if (d.sqrMagnitude < 100f)
                    threats.Add(enemy.transform.position);
            }
        }

        private void MoveWalkers(float dt, Vector3? hero)
        {
            float now = Time.time;
            for (int i = 0; i < walkers.Length; i++)
            {
                Walker walker = walkers[i];
                if (walker == null || walker.root == null || walker.removed || !walker.root.gameObject.activeSelf)
                    continue;
                if (walker.down || now < walker.downUntil)
                {
                    LieDown(walker, now);
                    continue;
                }
                if (walker.idle)
                    Gesture(walker, dt, hero);
                else
                    Stroll(walker, dt, hero, now);
            }
        }

        private void Stroll(Walker walker, float dt, Vector3? hero, float now)
        {
            Vector3 path = walker.to - walker.from;
            float length = Mathf.Max(.5f, path.magnitude);
            Vector3 along = path / length;
            Vector3 side = new Vector3(along.z, 0f, -along.x);
            Vector3 basePoint = Vector3.Lerp(walker.from, walker.to, walker.progress);

            // A fight right next to them: turn away and hurry. A fight a bit further: stop,
            // turn towards it and record it with the phone (kepo).
            float threatDistance = NearestThreat(basePoint, out Vector3 threat);
            if (threatDistance < fleeDistance && now >= walker.panicUntil)
            {
                walker.direction = Vector3.Dot(along, basePoint - threat) >= 0f ? 1f : -1f;
                walker.panicUntil = now + 2.5f;
            }
            bool watching = now >= walker.panicUntil && threatDistance < kepoDistance;
            ShowPhone(walker, watching || walker.filming);
            if (watching)
            {
                Vector3 look = threat - walker.root.position;
                look.y = 0f;
                if (look.sqrMagnitude > .01f)
                    walker.root.rotation = Quaternion.Slerp(walker.root.rotation, Quaternion.LookRotation(look), 4f * dt);
                SetSwing(walker.legLeft, 0f);
                SetSwing(walker.legRight, 0f);
                SetSwing(walker.armLeft, Mathf.Sin(now * 2f) * 4f);
                SetSwing(walker.armRight, -105f);
                return;
            }
            float pace = now < walker.panicUntil ? 2.3f : 1f;

            // 0.6.3 (owner: "lebih natural"): now and then a warga stops for a few seconds,
            // checks the phone or looks around, then walks on; they ease in and out of walking
            // instead of starting and stopping at full speed.
            bool heroClose = hero.HasValue && (basePoint - hero.Value).sqrMagnitude < personalSpace * personalSpace * 2f;
            if (walker.nextPause <= 0f)
                walker.nextPause = now + 6f + (float)random.NextDouble() * 14f;
            if (now >= walker.nextPause && pace <= 1f && !heroClose)
            {
                walker.pauseUntil = now + 2f + (float)random.NextDouble() * 3.5f;
                walker.nextPause = walker.pauseUntil + 8f + (float)random.NextDouble() * 16f;
                walker.pausePhone = walker.phone != null && random.NextDouble() < 0.5;
            }
            bool pausing = now < walker.pauseUntil && pace <= 1f && !heroClose;
            walker.pace = Mathf.MoveTowards(walker.pace, pausing ? 0f : pace, (pausing ? 2.5f : 1.6f) * dt);
            if (walker.pace < 0.05f && pausing)
            {
                ShowPhone(walker, walker.pausePhone);
                float look = Mathf.Sin(now * 0.9f + walker.phase) * 25f;
                Vector3 ahead = along * walker.direction;
                walker.root.rotation = Quaternion.Slerp(walker.root.rotation,
                    Quaternion.LookRotation(Quaternion.Euler(0f, walker.pausePhone ? 0f : look, 0f) * ahead, Vector3.up), 3f * dt);
                SetSwing(walker.legLeft, 0f);
                SetSwing(walker.legRight, 0f);
                SetSwing(walker.armLeft, Mathf.Sin(now * 1.3f) * 3f);
                SetSwing(walker.armRight, walker.pausePhone ? -78f : Mathf.Sin(now * 1.1f) * 4f);
                return;
            }
            pace = Mathf.Max(0.05f, walker.pace);

            walker.progress += walker.direction * walker.speed * pace * dt / length;
            if (walker.progress > 1f) { walker.progress = 1f; walker.direction = -1f; }
            else if (walker.progress < 0f) { walker.progress = 0f; walker.direction = 1f; }

            // Menepi: step aside for the hero instead of walking through them.
            float wantedDodge = 0f;
            if (hero.HasValue)
            {
                Vector3 offset = basePoint - hero.Value;
                offset.y = 0f;
                if (offset.sqrMagnitude < personalSpace * personalSpace)
                    wantedDodge = Vector3.Dot(offset, side) >= 0f ? dodgeWidth : -dodgeWidth;
                // 0.6.2: never step aside into a wall; try the other side, else stay on the path.
                if (wantedDodge != 0f && KarierCrowd.Blocked(basePoint, basePoint + side * wantedDodge, .28f))
                    wantedDodge = 0f; // The other side is the hero: just keep walking on the path.
            }
            walker.dodge = Mathf.MoveTowards(walker.dodge, wantedDodge, dodgeRate * dt);

            walker.phase += dt * walker.speed * pace * 5.2f;
            float bob = Mathf.Abs(Mathf.Sin(walker.phase)) * .035f;
            Vector3 point = Vector3.Lerp(walker.from, walker.to, walker.progress) + side * walker.dodge;
            walker.root.position = new Vector3(point.x, walker.from.y + bob, point.z);
            Vector3 facing = along * walker.direction;
            walker.root.rotation = Quaternion.Slerp(walker.root.rotation, Quaternion.LookRotation(facing, Vector3.up), 8f * dt);

            float swing = Mathf.Sin(walker.phase) * (pace > 1f ? 40f : 28f * Mathf.Clamp01(pace));
            SetSwing(walker.legLeft, swing);
            SetSwing(walker.legRight, -swing);
            SetSwing(walker.armLeft, -swing * .8f);
            SetSwing(walker.armRight, swing * .8f);
        }

        // Vendors and onlookers: stay put, gesture, and turn to watch the hero pass by.
        private void Gesture(Walker walker, float dt, Vector3? hero)
        {
            walker.phase += dt;
            // 0.5.0 kepo: onlookers face a nearby fight and record it.
            float threatDistance = NearestThreat(walker.root.position, out Vector3 threat);
            bool watching = threatDistance < kepoDistance;
            ShowPhone(walker, watching || walker.filming);
            if (watching || walker.filming)
            {
                if (watching)
                {
                    Vector3 toThreat = threat - walker.root.position;
                    toThreat.y = 0f;
                    if (toThreat.sqrMagnitude > .01f)
                        walker.root.rotation = Quaternion.Slerp(walker.root.rotation, Quaternion.LookRotation(toThreat), 3f * dt);
                }
                SetSwing(walker.armRight, -105f + Mathf.Sin(walker.phase * 3f) * 3f);
                SetSwing(walker.armLeft, Mathf.Sin(walker.phase * .7f) * 6f);
                return;
            }
            if (hero.HasValue)
            {
                Vector3 look = hero.Value - walker.root.position;
                look.y = 0f;
                if (look.sqrMagnitude < 64f && look.sqrMagnitude > .01f)
                    walker.root.rotation = Quaternion.Slerp(walker.root.rotation, Quaternion.LookRotation(look), 2f * dt);
            }
            SetSwing(walker.armRight, -25f + Mathf.Sin(walker.phase * 1.4f) * 18f);
            SetSwing(walker.armLeft, Mathf.Sin(walker.phase * .7f) * 6f);
            // 0.6.3: shifting weight from one leg to the other.
            float shift = Mathf.Sin(walker.phase * .45f) * 4f;
            SetSwing(walker.legLeft, shift);
            SetSwing(walker.legRight, -shift);
        }

        // 0.6.3 TABRAK: hit by the hero's motor, the warga lies on the ground, then stands up.
        public void KnockDown(Walker walker, float seconds)
        {
            if (walker == null || walker.root == null)
                return;
            walker.downUntil = Time.time + seconds;
            walker.panicUntil = 0f;
            walker.pauseUntil = 0f;
        }

        // The nearest standing warga within radius of a point (null when none).
        public Walker StandingNear(Vector3 point, float radius)
        {
            Walker best = null;
            float bestDistance = radius * radius;
            float now = Time.time;
            foreach (Walker walker in walkers)
            {
                if (walker == null || walker.root == null || walker.removed || !walker.root.gameObject.activeInHierarchy ||
                    walker.down || now < walker.downUntil)
                    continue;
                Vector3 d = walker.root.position - point;
                d.y = 0f;
                if (d.sqrMagnitude < bestDistance)
                {
                    bestDistance = d.sqrMagnitude;
                    best = walker;
                }
            }
            return best;
        }

        private void LieDown(Walker walker, float now)
        {
            Transform root = walker.root;
            Vector3 flat = root.forward;
            flat.y = 0f;
            Quaternion upright = Quaternion.LookRotation(flat.sqrMagnitude > .001f ? flat.normalized : Vector3.forward);
            if (now < walker.downUntil)
            {
                if (!walker.down)
                {
                    walker.down = true;
                    root.position += Vector3.up * .18f;
                    root.rotation = upright * Quaternion.Euler(-88f, 0f, 0f);
                    ShowPhone(walker, false);
                }
                // Holding the leg, rocking a little: "Aduh... aduh..."
                SetSwing(walker.armLeft, -40f + Mathf.Sin(now * 4f) * 10f);
                SetSwing(walker.armRight, -40f - Mathf.Sin(now * 4f) * 10f);
                SetSwing(walker.legLeft, 20f + Mathf.Sin(now * 3f) * 8f);
                return;
            }
            walker.down = false;
            root.position -= Vector3.up * .18f;
            // Forward of a body lying on its back points up: take the yaw from its up vector.
            Vector3 facing = -root.up;
            facing.y = 0f;
            root.rotation = Quaternion.LookRotation(facing.sqrMagnitude > .001f ? facing.normalized : Vector3.forward);
            SetSwing(walker.legLeft, 0f);
        }

        private static void ShowPhone(Walker walker, bool show)
        {
            if (walker.phone != null && walker.phone.activeSelf != show)
                walker.phone.SetActive(show);
        }

        private static void SetSwing(Transform limb, float degrees)
        {
            if (limb != null)
                limb.localRotation = Quaternion.Euler(degrees, 0f, 0f);
        }

        private float NearestThreat(Vector3 point, out Vector3 threat)
        {
            threat = point;
            float best = float.MaxValue;
            for (int i = 0; i < threats.Count; i++)
            {
                Vector3 d = threats[i] - point;
                d.y = 0f;
                float m = d.magnitude;
                if (m < best) { best = m; threat = threats[i]; }
            }
            return best;
        }

        // --- Fountains --------------------------------------------------------------------

        private void SprayFountains()
        {
            float t = Time.time;
            for (int i = 0; i < jets.Length; i++)
            {
                if (jets[i] == null)
                    continue;
                Vector3 s = jetBaseScale[i];
                jets[i].localScale = new Vector3(s.x, s.y * (1f + .12f * Mathf.Sin(t * 5f + i)), s.z);
            }

            if (nozzles.Length == 0)
                return;
            int perFountain = Mathf.Max(1, droplets.Length / nozzles.Length);
            for (int i = 0; i < droplets.Length; i++)
            {
                Transform drop = droplets[i];
                if (drop == null)
                    continue;
                int fountain = Mathf.Min(nozzles.Length - 1, i / perFountain);
                int k = i % perFountain;
                drop.position = DropletPosition(nozzles[fountain], k, perFountain, t, dropletReach, dropletHeight);
            }
        }

        // Nozzles sit this high above the water, so droplets land back in the basin.
        public const float NozzleHeight = .55f;

        // Droplets arc outward from the nozzle in a ring and fall back into the basin.
        public static Vector3 DropletPosition(Vector3 nozzle, int index, int count, float time, float reach, float height)
        {
            const float period = 1.3f;
            float angle = index * Mathf.PI * 2f / Mathf.Max(1, count);
            float t = Mathf.Repeat(time / period + index * .37f, 1f);
            var outward = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            return nozzle + outward * (reach * t) + Vector3.up * (4f * height * t * (1f - t) - NozzleHeight * t);
        }

        // --- Helpers --------------------------------------------------------------------

        private void Cull()
        {
            Camera view = Camera.main;
            if (view == null)
                return;
            Vector3 eye = view.transform.position;
            float limit = cullDistance * cullDistance;
            foreach (Vehicle vehicle in vehicles)
                if (vehicle != null && vehicle.body != null)
                    Show(vehicle.body.gameObject, (vehicle.body.position - eye).sqrMagnitude < limit);
            for (int i = 0; i < walkers.Length; i++)
            {
                Walker walker = walkers[i];
                if (walker == null || walker.root == null)
                    continue;
                bool home = walker.removed || (night && !walker.idle && i % 3 != 0 && !walker.down);
                Show(walker.root.gameObject, !home && (walker.root.position - eye).sqrMagnitude < limit);
            }
        }

        private static void Show(GameObject go, bool visible)
        {
            if (go.activeSelf != visible)
                go.SetActive(visible);
        }

        private static Vector3? LocalHeroPosition()
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null || manager.LocalClient == null || manager.LocalClient.PlayerObject == null)
                return null;
            return manager.LocalClient.PlayerObject.transform.position;
        }
    }
}
