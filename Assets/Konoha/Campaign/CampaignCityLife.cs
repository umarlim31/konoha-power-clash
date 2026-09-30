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
            [NonSerialized] public float phase, dodge, panicUntil;
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
        public float fleeDistance = 6f;
        public float hornMinSeconds = 7f, hornMaxSeconds = 16f;
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
            DriveTraffic(dt);
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

        private void DriveTraffic(float dt)
        {
            float perimeter = RingLength(ringCorners);
            for (int i = 0; i < vehicles.Length; i++)
            {
                Vehicle vehicle = vehicles[i];
                if (vehicle == null || vehicle.body == null)
                    continue;

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

            if (Time.time >= nextHorn)
            {
                nextHorn = Time.time + NextHornDelay();
                Vehicle loud = NearestVehicle();
                if (loud != null && CampaignAudio.Instance != null)
                    CampaignAudio.Instance.PlayAt(CampaignSound.Klakson, loud.body.position, 0.45f, loud.motor ? 1.35f : 1f);
            }
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
            IReadOnlyList<CampaignEnemy> enemies = CampaignEnemy.Active;
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i] != null)
                    threats.Add(enemies[i].transform.position);
        }

        private void MoveWalkers(float dt, Vector3? hero)
        {
            float now = Time.time;
            for (int i = 0; i < walkers.Length; i++)
            {
                Walker walker = walkers[i];
                if (walker == null || walker.root == null)
                    continue;
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

            // A fight nearby: turn away from it and hurry.
            if (NearestThreat(basePoint, out Vector3 threat) < fleeDistance && now >= walker.panicUntil)
            {
                walker.direction = Vector3.Dot(along, basePoint - threat) >= 0f ? 1f : -1f;
                walker.panicUntil = now + 2.5f;
            }
            float pace = now < walker.panicUntil ? 2.3f : 1f;

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
                    wantedDodge = Vector3.Dot(offset, side) >= 0f ? 1.6f : -1.6f;
            }
            walker.dodge = Mathf.MoveTowards(walker.dodge, wantedDodge, 3f * dt);

            walker.phase += dt * walker.speed * pace * 5.2f;
            float bob = Mathf.Abs(Mathf.Sin(walker.phase)) * .035f;
            Vector3 point = Vector3.Lerp(walker.from, walker.to, walker.progress) + side * walker.dodge;
            walker.root.position = new Vector3(point.x, walker.from.y + bob, point.z);
            Vector3 facing = along * walker.direction;
            walker.root.rotation = Quaternion.Slerp(walker.root.rotation, Quaternion.LookRotation(facing, Vector3.up), 8f * dt);

            float swing = Mathf.Sin(walker.phase) * (pace > 1f ? 40f : 28f);
            SetSwing(walker.legLeft, swing);
            SetSwing(walker.legRight, -swing);
            SetSwing(walker.armLeft, -swing * .8f);
            SetSwing(walker.armRight, swing * .8f);
        }

        // Vendors and onlookers: stay put, gesture, and turn to watch the hero pass by.
        private void Gesture(Walker walker, float dt, Vector3? hero)
        {
            walker.phase += dt;
            if (hero.HasValue)
            {
                Vector3 look = hero.Value - walker.root.position;
                look.y = 0f;
                if (look.sqrMagnitude < 64f && look.sqrMagnitude > .01f)
                    walker.root.rotation = Quaternion.Slerp(walker.root.rotation, Quaternion.LookRotation(look), 2f * dt);
            }
            SetSwing(walker.armRight, -25f + Mathf.Sin(walker.phase * 1.4f) * 18f);
            SetSwing(walker.armLeft, Mathf.Sin(walker.phase * .7f) * 6f);
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
            foreach (Walker walker in walkers)
                if (walker != null && walker.root != null)
                    Show(walker.root.gameObject, (walker.root.position - eye).sqrMagnitude < limit);
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
