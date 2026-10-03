using UnityEngine;

namespace Konoha.Campaign
{
    // 0.6.0 KARIER: a fight is the whole kampung's business (owner request).
    //  - Gather: one warga shouts "WOI! ADA YANG BERANTEM!", the others come running from
    //    the surrounding streets, stand in a ring and record it with their phones (0.6.1:
    //    phone held up in both hands with a glowing screen, comments pop up above them).
    //  - Melerai: a bapak with a peci steps between the hero and the preman, arms spread.
    //  - Police: a patrol motor drives in from the jalan raya with its siren, the officer gets
    //    off and walks to the hero. 0.6.1 arrest: the officer cuffs the hero, the motor takes
    //    him to the POLSEK (KarierController drives the hero), then rides away.
    // 0.6.1: nobody walks or drives through walls any more. Every step is checked against
    // the scene colliders (sphere casts, characters ignored); people come only from places
    // with a clear way to the fight, step around obstacles, and stop when there is no way.
    // Purely local presentation like CampaignCityLife: no colliders, no NetworkObjects.
    public sealed class KarierCrowd : MonoBehaviour
    {
        // [0] the one who shouts, [1] the one who melerai, the rest watch and record.
        public CampaignCityLife.Walker[] people = new CampaignCityLife.Walker[0];
        public TextMesh shoutBubble, meleraiBubble, policeBubble;
        // 0.6.1: comments of the recording warga ("VIRALIN!").
        public TextMesh[] commentBubbles = new TextMesh[0];
        public Transform policeMotor;
        public Renderer[] sirenRed = new Renderer[0];
        public Renderer[] sirenBlue = new Renderer[0];
        public GameObject policeRider;
        public CampaignCityLife.Walker policeOfficer;
        // Patrol motors come from the jalan raya, south of the boulevard.
        public Vector3 policeGarage = new Vector3(0f, 0f, -62f);
        public Vector2 boundaryCenter = new Vector2(0f, 4f);
        public Vector2 boundaryRadii = new Vector2(34f, 58f);
        public float runSpeed = 4.2f;
        public float walkSpeed = 1.6f;
        public float policeSpeed = 10f;
        public float comeFrom = 15f;

        private enum State { Hidden, Coming, Watching, Leaving, Melerai }
        private enum Police { Hidden, Arriving, Here, Ride, RideDone, Leaving }

        private static readonly string[] Comments =
        {
            "REKAM! REKAM!", "VIRALIN!", "WADUH...", "UPLOAD KE GRUP!", "AYO BANG!",
            "KASIHAN...", "PANGGIL PAK RT!", "LIVE IG DULU", "SINYALNYA JELEK!", "NO VIRAL NO JUSTICE"
        };
        private static readonly float[] SteerAngles = { 0f, 35f, -35f, 70f, -70f, 110f, -110f };
        private static readonly RaycastHit[] hits = new RaycastHit[16];
        private static readonly Collider[] overlaps = new Collider[16];

        private State[] states = new State[0];
        private Vector3[] targets = new Vector3[0];
        private float[] phases = new float[0];
        private float[] stuck = new float[0];
        private Vector3 center;
        private Vector3 meleraiFacing;
        private float shoutUntil, meleraiUntil, policeTalkUntil;
        private Police police = Police.Hidden;
        private Vector3[] policePath = new Vector3[0];
        private Vector3[] arrivalPath = new Vector3[0];
        private int policeLeg;
        private Vector3 policeLookAt;
        private float nextSiren;
        private float rideSpeed;
        private bool officerWalking;
        private Vector3 officerTarget;
        private float officerPhase;
        private float nextComment;
        private float[] commentUntil = new float[0];
        private int[] commentOwner = new int[0];
        private System.Random random = new System.Random(611);
        private Camera cachedCamera;

        public bool Gathered { get; private set; }
        public bool PoliceArrived => police == Police.Here;
        public bool PoliceBusy => police != Police.Hidden;
        public bool RideDone => police == Police.RideDone;
        public bool OfficerArrived { get; private set; }
        // Where a passenger sits on the patrol motor (behind the officer) and how it faces.
        public Vector3 PassengerSeat => policeMotor != null ? policeMotor.position - policeMotor.forward * 0.72f : Vector3.zero;
        public Quaternion MotorRotation => policeMotor != null ? policeMotor.rotation : Quaternion.identity;
        public Vector3 MotorPosition => policeMotor != null ? policeMotor.position : Vector3.zero;

        private void Awake()
        {
            states = new State[people.Length];
            targets = new Vector3[people.Length];
            phases = new float[people.Length];
            stuck = new float[people.Length];
            for (int i = 0; i < people.Length; i++)
            {
                phases[i] = i * 1.3f;
                Show(people[i], false);
            }
            commentUntil = new float[commentBubbles.Length];
            commentOwner = new int[commentBubbles.Length];
            foreach (TextMesh bubble in commentBubbles)
                SetBubble(bubble, false);
            Show(policeOfficer, false);
            if (policeMotor != null) policeMotor.gameObject.SetActive(false);
            SetBubble(shoutBubble, false);
            SetBubble(meleraiBubble, false);
            SetBubble(policeBubble, false);
        }

        // --- Beats --------------------------------------------------------------------------

        public void Gather(Vector3 fight)
        {
            center = new Vector3(fight.x, 0f, fight.z);
            Gathered = true;
            int count = people.Length;
            for (int i = 0; i < count; i++)
            {
                CampaignCityLife.Walker person = people[i];
                if (person == null || person.root == null || states[i] == State.Melerai)
                    continue;
                float baseAngle = i * 360f / Mathf.Max(1, count) + 17f;
                float ring = i == 0 ? 5.2f : 5.8f + (i % 3) * 0.6f;
                if (states[i] == State.Watching || states[i] == State.Coming)
                {
                    // Already here: just take a place in the new ring if it is reachable.
                    Vector3 place = PlaceAround(baseAngle, ring);
                    if (Free(place) && !Blocked(person.root.position, place, .3f))
                        targets[i] = place;
                    continue;
                }
                if (!PickApproach(baseAngle, ring, i == 0, out Vector3 from, out Vector3 slot))
                    continue; // No clear way from any street: this warga stays home.
                if (states[i] == State.Hidden)
                    person.root.position = from;
                Show(person, true);
                targets[i] = slot;
                states[i] = State.Coming;
                stuck[i] = 0f;
            }
            shoutUntil = Time.time + 3f;
            nextComment = Time.time + 2f;
        }

        // A start point out of sight and a place in the ring, joined by a clear straight way.
        private bool PickApproach(float baseAngle, float ring, bool close, out Vector3 from, out Vector3 slot)
        {
            float[] distances = close ? new[] { 9f, 7f } : new[] { comeFrom + 2f, comeFrom - 3f, 8f };
            float[] offsets = { 0f, 25f, -25f, 50f, -50f };
            foreach (float offset in offsets)
            {
                slot = PlaceAround(baseAngle + offset, ring);
                if (!Free(slot))
                    continue;
                foreach (float distance in distances)
                {
                    from = PlaceAround(baseAngle + offset, distance);
                    if (Free(from) && !Blocked(from, slot, .3f))
                        return true;
                }
            }
            from = slot = Vector3.zero;
            return false;
        }

        private Vector3 PlaceAround(float degrees, float radius)
        {
            float a = degrees * Mathf.Deg2Rad;
            return Clamp(center + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radius);
        }

        public void Melerai(Vector3 hero, Vector3 preman, float seconds)
        {
            if (people.Length < 2 || people[1] == null || people[1].root == null)
                return;
            Vector3 middle = (hero + preman) * 0.5f;
            middle.y = 0f;
            Vector3 across = preman - hero;
            across.y = 0f;
            meleraiFacing = across.sqrMagnitude > 0.01f ? new Vector3(-across.z, 0f, across.x).normalized : Vector3.forward;
            if (states[1] == State.Hidden)
            {
                Vector3 start = Clamp(middle - meleraiFacing * 6f);
                if (!Free(start) || Blocked(start, middle, .3f))
                    start = Clamp(middle + meleraiFacing * 6f);
                if (!Free(start) || Blocked(start, middle, .3f))
                    return; // Nobody can get there without walking through a wall.
                people[1].root.position = start;
                Show(people[1], true);
            }
            targets[1] = middle;
            states[1] = State.Melerai;
            stuck[1] = 0f;
            meleraiUntil = Time.time + Mathf.Max(1f, seconds) + 0.8f;
        }

        public void Disperse()
        {
            Gathered = false;
            for (int i = 0; i < people.Length; i++)
            {
                if (states[i] == State.Hidden || people[i] == null || people[i].root == null)
                    continue;
                Vector3 away = people[i].root.position - center;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = Vector3.back;
                targets[i] = Clamp(center + away.normalized * (comeFrom + 3f));
                states[i] = State.Leaving;
                stuck[i] = 0f;
            }
            shoutUntil = meleraiUntil = 0f;
            for (int i = 0; i < commentBubbles.Length; i++)
            {
                commentUntil[i] = 0f;
                SetBubble(commentBubbles[i], false);
            }
        }

        public void DispatchPolice(Vector3 target)
        {
            if (policeMotor == null || police == Police.Arriving || police == Police.Here)
                return;
            target.y = 0f;
            policeLookAt = target;
            // Up the boulevard from the jalan raya, then across towards the hero; each leg ends
            // early if something solid is in the way (the officer walks the rest).
            var turn = new Vector3(0f, 0f, Mathf.Clamp(target.z - 4f, policeGarage.z, 60f));
            turn = Reach(policeGarage, turn, .5f);
            Vector3 approach = turn - target;
            approach.y = 0f;
            Vector3 stop = approach.sqrMagnitude > 0.01f ? target + approach.normalized * 3.2f : target + Vector3.back * 3.2f;
            stop = Reach(turn, Clamp(stop), .5f);
            arrivalPath = new[] { policeGarage, turn, stop };
            policePath = arrivalPath;
            policeLeg = 1;
            rideSpeed = policeSpeed;
            policeMotor.position = policeGarage;
            policeMotor.rotation = Quaternion.LookRotation(Flat(turn - policeGarage, Vector3.forward));
            policeMotor.gameObject.SetActive(true);
            if (policeRider != null) policeRider.SetActive(true);
            Show(policeOfficer, false);
            officerWalking = false;
            OfficerArrived = false;
            police = Police.Arriving;
            nextSiren = 0f;
        }

        // The officer walks over to this point (arrest) and stops next to it.
        public void OfficerWalkTo(Vector3 point)
        {
            if (police != Police.Here || policeOfficer == null || policeOfficer.root == null)
                return;
            officerTarget = new Vector3(point.x, 0f, point.z);
            officerWalking = true;
        }

        // The arrested hero is taken along this path on the patrol motor (officer in front).
        public void BeginRide(Vector3[] path, float speed)
        {
            if (policeMotor == null || path == null || path.Length == 0)
                return;
            var full = new Vector3[path.Length + 1];
            full[0] = policeMotor.position;
            for (int i = 0; i < path.Length; i++)
                full[i + 1] = new Vector3(path[i].x, 0f, path[i].z);
            policePath = full;
            policeLeg = 1;
            rideSpeed = speed;
            officerWalking = false;
            Show(policeOfficer, false);
            if (policeRider != null) policeRider.SetActive(true);
            SetBubble(policeBubble, false);
            police = Police.Ride;
        }

        public void PoliceLeave()
        {
            if (policeMotor == null || police == Police.Hidden || police == Police.Leaving)
                return;
            if (police == Police.RideDone)
                policePath = new[] { policeMotor.position, new Vector3(0f, 0f, policeMotor.position.z), policeGarage };
            else if (police == Police.Here || policeLeg >= policePath.Length)
                policePath = new[] { policeMotor.position, arrivalPath.Length > 1 ? arrivalPath[1] : policeGarage, policeGarage };
            else
                policePath = new[] { policeMotor.position, policeGarage };
            policeLeg = 1;
            rideSpeed = policeSpeed;
            officerWalking = false;
            Show(policeOfficer, false);
            if (policeRider != null) policeRider.SetActive(true);
            SetBubble(policeBubble, false);
            police = Police.Leaving;
        }

        // --- Animation ----------------------------------------------------------------------

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;
            float now = Time.time;
            for (int i = 0; i < people.Length; i++)
                Animate(i, dt, now);
            if (states.Length > 1 && states[1] == State.Melerai && now >= meleraiUntil)
            {
                // Back to the ring (or home when the crowd already left).
                states[1] = Gathered ? State.Coming : State.Leaving;
                Vector3 back = people[1].root.position - center;
                back.y = 0f;
                if (back.sqrMagnitude < 0.01f) back = Vector3.left;
                targets[1] = Clamp(center + back.normalized * (Gathered ? 6f : comeFrom + 3f));
                stuck[1] = 0f;
            }
            DrivePolice(dt, now);
            WalkOfficer(dt, now);
            UpdateComments(now);
            Bubbles(now);
        }

        private void Animate(int i, float dt, float now)
        {
            CampaignCityLife.Walker person = people[i];
            if (person == null || person.root == null || states[i] == State.Hidden)
                return;
            Vector3 position = person.root.position;
            Vector3 to = targets[i] - position;
            to.y = 0f;
            float distance = to.magnitude;
            bool moving = (states[i] == State.Coming || states[i] == State.Leaving || states[i] == State.Melerai) && distance > 0.15f;
            if (moving)
            {
                float speed = states[i] == State.Leaving ? runSpeed * 0.6f : runSpeed;
                if (Move(person.root, targets[i], speed, dt))
                {
                    stuck[i] = 0f;
                    phases[i] += dt * 9f;
                    float swing = Mathf.Sin(phases[i]) * 42f;
                    Swing(person.legLeft, swing);
                    Swing(person.legRight, -swing);
                    Swing(person.armLeft, -swing * .8f);
                    Swing(person.armRight, swing * .8f);
                    ShowPhone(person, false);
                    return;
                }
                // No way forward: wait a moment, then give up and stay (or leave) here.
                stuck[i] += dt;
                if (stuck[i] < 2.5f)
                {
                    Swing(person.legLeft, 0f);
                    Swing(person.legRight, 0f);
                    return;
                }
                targets[i] = position;
            }

            if (states[i] == State.Leaving)
            {
                states[i] = State.Hidden;
                Show(person, false);
                return;
            }
            if (states[i] == State.Coming)
                states[i] = State.Watching;

            Swing(person.legLeft, 0f);
            Swing(person.legRight, 0f);
            phases[i] += dt;
            if (states[i] == State.Melerai)
            {
                // Arms spread, facing the hero side: "Sudah, sudah!"
                Face(person.root, meleraiFacing, dt, 6f);
                float wave = Mathf.Sin(now * 5f) * 8f;
                Set(person.armLeft, -10f, 0f, -75f - wave);
                Set(person.armRight, -10f, 0f, 75f + wave);
                ShowPhone(person, false);
                return;
            }

            Vector3 look = center - position;
            look.y = 0f;
            Face(person.root, look, dt, 4f);
            if (i == 0 && now < shoutUntil)
            {
                // Both arms waving above the head while shouting for the neighbours.
                float wave = Mathf.Sin(now * 9f) * 25f;
                Set(person.armLeft, -160f + wave, 0f, -15f);
                Set(person.armRight, -160f - wave, 0f, 15f);
                ShowPhone(person, false);
                return;
            }
            // Recording: phone up at eye height in both hands, a little sway and re-aiming.
            float sway = Mathf.Sin(phases[i] * 2.3f) * 3f;
            Set(person.armRight, -112f + sway, 0f, -14f);
            Set(person.armLeft, -108f - sway, 0f, 22f);
            ShowPhone(person, true);
        }

        // One step towards the target, around obstacles. False when every way is blocked.
        private static bool Move(Transform root, Vector3 target, float speed, float dt)
        {
            Vector3 position = root.position;
            Vector3 to = target - position;
            to.y = 0f;
            float distance = to.magnitude;
            if (distance < 0.05f)
                return true;
            Vector3 direction = to / distance;
            float step = Mathf.Min(distance, speed * dt);
            foreach (float angle in SteerAngles)
            {
                Vector3 candidate = Quaternion.Euler(0f, angle, 0f) * direction;
                if (Blocked(position, position + candidate * (step + 0.35f), .28f))
                    continue;
                root.position = position + candidate * step;
                Face(root, candidate, dt, 10f);
                return true;
            }
            return false;
        }

        private void WalkOfficer(float dt, float now)
        {
            if (police != Police.Here || policeOfficer == null || policeOfficer.root == null)
                return;
            Transform root = policeOfficer.root;
            if (officerWalking)
            {
                Vector3 to = officerTarget - root.position;
                to.y = 0f;
                if (to.magnitude > 1.2f && Move(root, officerTarget, walkSpeed * 1.4f, dt))
                {
                    officerPhase += dt * 7f;
                    float swing = Mathf.Sin(officerPhase) * 32f;
                    Swing(policeOfficer.legLeft, swing);
                    Swing(policeOfficer.legRight, -swing);
                    Swing(policeOfficer.armLeft, -swing * .7f);
                    Swing(policeOfficer.armRight, swing * .7f);
                    OfficerArrived = false;
                    return;
                }
                // Close enough (or no way further): hands forward with the cuffs.
                OfficerArrived = true;
                Face(root, to, dt, 8f);
                Swing(policeOfficer.legLeft, 0f);
                Swing(policeOfficer.legRight, 0f);
                Set(policeOfficer.armRight, -70f, 0f, -10f);
                Set(policeOfficer.armLeft, -70f, 0f, 10f);
                return;
            }
            Face(root, policeLookAt - root.position, dt, 6f);
            Swing(policeOfficer.legLeft, 0f);
            Swing(policeOfficer.legRight, 0f);
            Set(policeOfficer.armRight, -70f + Mathf.Sin(now * 3f) * 10f, 0f, 0f);
        }

        private void DrivePolice(float dt, float now)
        {
            if (policeMotor == null || police == Police.Hidden)
                return;

            bool lightsOn = police == Police.Arriving || police == Police.Here || police == Police.Ride;
            bool redPhase = Mathf.Repeat(now * 4f, 1f) < 0.5f;
            foreach (Renderer light in sirenRed)
                if (light != null) light.enabled = lightsOn && redPhase;
            foreach (Renderer light in sirenBlue)
                if (light != null) light.enabled = lightsOn && !redPhase;

            if (police == Police.Here || police == Police.RideDone)
                return;

            if (now >= nextSiren && (police == Police.Arriving || police == Police.Ride))
            {
                nextSiren = now + (police == Police.Ride ? 2.4f : 1.15f);
                if (CampaignAudio.Instance != null)
                    CampaignAudio.Instance.PlayAt(CampaignSound.Sirene, policeMotor.position, police == Police.Ride ? 0.5f : 0.9f);
            }

            if (policeLeg >= policePath.Length)
            {
                if (police == Police.Arriving)
                {
                    police = Police.Here;
                    if (policeRider != null) policeRider.SetActive(false);
                    if (policeOfficer != null && policeOfficer.root != null)
                    {
                        Vector3 side = policeMotor.right;
                        Vector3 spot = policeMotor.position + new Vector3(side.x, 0f, side.z) * 0.9f;
                        if (!Free(spot))
                            spot = policeMotor.position - new Vector3(side.x, 0f, side.z) * 0.9f;
                        policeOfficer.root.position = spot;
                        Show(policeOfficer, true);
                        // Walk up to the hero to talk (stops next to him).
                        officerTarget = policeLookAt;
                        officerWalking = true;
                    }
                    policeTalkUntil = now + 3.5f;
                }
                else if (police == Police.Ride)
                {
                    police = Police.RideDone;
                }
                else
                {
                    police = Police.Hidden;
                    policeMotor.gameObject.SetActive(false);
                }
                return;
            }

            Vector3 goal = policePath[policeLeg];
            Vector3 delta = goal - policeMotor.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            float step = rideSpeed * dt;
            if (distance <= step)
            {
                policeMotor.position = new Vector3(goal.x, 0f, goal.z);
                policeLeg++;
            }
            else
            {
                policeMotor.position += delta / distance * step;
                policeMotor.rotation = Quaternion.RotateTowards(policeMotor.rotation,
                    Quaternion.LookRotation(delta / distance), 220f * dt);
            }
        }

        private void UpdateComments(float now)
        {
            for (int i = 0; i < commentBubbles.Length; i++)
            {
                if (commentUntil[i] <= 0f)
                    continue;
                int owner = commentOwner[i];
                bool alive = now < commentUntil[i] && owner >= 0 && owner < people.Length && states[owner] == State.Watching;
                if (!alive)
                {
                    commentUntil[i] = 0f;
                    SetBubble(commentBubbles[i], false);
                    continue;
                }
                Place(commentBubbles[i], people[owner], true, 2.25f);
            }
            if (!Gathered || commentBubbles.Length == 0 || now < nextComment)
                return;
            nextComment = now + 1.6f + (float)random.NextDouble() * 1.6f;
            int pick = 2 + random.Next(Mathf.Max(1, people.Length - 2));
            if (pick >= people.Length || states[pick] != State.Watching)
                return;
            for (int i = 0; i < commentBubbles.Length; i++)
            {
                if (commentUntil[i] > 0f || commentBubbles[i] == null)
                    continue;
                commentUntil[i] = now + 2.2f;
                commentOwner[i] = pick;
                commentBubbles[i].text = Comments[random.Next(Comments.Length)];
                Place(commentBubbles[i], people[pick], true, 2.25f);
                return;
            }
        }

        private void Bubbles(float now)
        {
            Place(shoutBubble, people.Length > 0 ? people[0] : null, now < shoutUntil && states.Length > 0 && states[0] != State.Hidden, 2.35f);
            Place(meleraiBubble, people.Length > 1 ? people[1] : null, states.Length > 1 && states[1] == State.Melerai, 2.35f);
            Place(policeBubble, policeOfficer, police == Police.Here && now < policeTalkUntil, 2.35f);
        }

        private void Place(TextMesh bubble, CampaignCityLife.Walker person, bool show, float height)
        {
            bool visible = show && bubble != null && person != null && person.root != null && person.root.gameObject.activeSelf;
            SetBubble(bubble, visible);
            if (!visible)
                return;
            bubble.transform.position = person.root.position + Vector3.up * height;
            if (cachedCamera == null)
                cachedCamera = Camera.main;
            if (cachedCamera != null)
            {
                Vector3 look = bubble.transform.position - cachedCamera.transform.position;
                if (look.sqrMagnitude > 0.01f)
                    bubble.transform.rotation = Quaternion.LookRotation(look);
            }
        }

        // --- Collision checks (people never walk through walls) -------------------------------

        private static bool Solid(Collider collider) =>
            collider != null && !collider.isTrigger && !(collider is CharacterController);

        // Something solid between two ground points (knee to chest height)?
        public static bool Blocked(Vector3 from, Vector3 to, float radius)
        {
            Vector3 start = new Vector3(from.x, from.y + 0.75f, from.z);
            Vector3 direction = to - from;
            direction.y = 0f;
            float length = direction.magnitude;
            if (length < 0.01f)
                return false;
            int count = Physics.SphereCastNonAlloc(start, radius, direction / length, hits, length, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (Solid(hits[i].collider))
                    return true;
            return false;
        }

        // Room for a standing person here?
        public static bool Free(Vector3 point)
        {
            int count = Physics.OverlapCapsuleNonAlloc(point + Vector3.up * 0.5f, point + Vector3.up * 1.5f, 0.32f,
                overlaps, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (Solid(overlaps[i]))
                    return false;
            return true;
        }

        // The farthest point towards 'to' that can be reached in a straight line.
        public static Vector3 Reach(Vector3 from, Vector3 to, float radius)
        {
            Vector3 start = new Vector3(from.x, from.y + 0.75f, from.z);
            Vector3 direction = to - from;
            direction.y = 0f;
            float length = direction.magnitude;
            if (length < 0.01f)
                return to;
            direction /= length;
            float best = length;
            int count = Physics.SphereCastNonAlloc(start, radius, direction, hits, length, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (Solid(hits[i].collider))
                    best = Mathf.Min(best, Mathf.Max(0f, hits[i].distance - 0.8f));
            return new Vector3(from.x + direction.x * best, 0f, from.z + direction.z * best);
        }

        // --- Helpers -------------------------------------------------------------------------

        private Vector3 Clamp(Vector3 point)
        {
            Vector3 clamped = CampaignTraversal.ClampToCampus(point, boundaryCenter, boundaryRadii);
            return new Vector3(clamped.x, 0f, clamped.z);
        }

        private static Vector3 Flat(Vector3 v, Vector3 fallback)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : fallback;
        }

        private static void Face(Transform root, Vector3 direction, float dt, float rate)
        {
            direction.y = 0f;
            if (root == null || direction.sqrMagnitude < 0.0001f)
                return;
            root.rotation = Quaternion.Slerp(root.rotation, Quaternion.LookRotation(direction), rate * dt);
        }

        private static void Swing(Transform limb, float degrees) => Set(limb, degrees, 0f, 0f);

        private static void Set(Transform limb, float x, float y, float z)
        {
            if (limb != null)
                limb.localRotation = Quaternion.Euler(x, y, z);
        }

        private static void ShowPhone(CampaignCityLife.Walker person, bool show)
        {
            if (person.phone != null && person.phone.activeSelf != show)
                person.phone.SetActive(show);
        }

        private static void Show(CampaignCityLife.Walker person, bool show)
        {
            if (person != null && person.root != null && person.root.gameObject.activeSelf != show)
                person.root.gameObject.SetActive(show);
        }

        private static void SetBubble(TextMesh bubble, bool show)
        {
            if (bubble != null && bubble.gameObject.activeSelf != show)
                bubble.gameObject.SetActive(show);
        }
    }
}
