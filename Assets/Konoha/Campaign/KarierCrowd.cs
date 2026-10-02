using UnityEngine;

namespace Konoha.Campaign
{
    // 0.6.0 KARIER: a fight is the whole kampung's business (owner request).
    //  - Gather: one warga shouts "WOI! ADA YANG BERANTEM!", the others come running from
    //    the surrounding streets, stand in a ring and record it with their phones.
    //  - Melerai: a bapak with a peci steps between the hero and the preman, arms spread.
    //  - Police: a patrol motor drives in from the jalan raya with its siren, the officer gets
    //    off; later he rides away again.
    // Purely local presentation like CampaignCityLife: no colliders, no NetworkObjects.
    // KarierController decides when each beat happens (KarierLife rules).
    public sealed class KarierCrowd : MonoBehaviour
    {
        // [0] the one who shouts, [1] the one who melerai, the rest watch and record.
        public CampaignCityLife.Walker[] people = new CampaignCityLife.Walker[0];
        public TextMesh shoutBubble, meleraiBubble, policeBubble;
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
        public float policeSpeed = 10f;
        public float comeFrom = 15f;

        private enum State { Hidden, Coming, Watching, Leaving, Melerai }
        private enum Police { Hidden, Arriving, Here, Leaving }

        private State[] states = new State[0];
        private Vector3[] targets = new Vector3[0];
        private float[] phases = new float[0];
        private Vector3 center;
        private Vector3 meleraiFacing;
        private float shoutUntil, meleraiUntil, policeTalkUntil;
        private Police police = Police.Hidden;
        private Vector3[] policePath = new Vector3[0];
        private int policeLeg;
        private Vector3 policeLookAt;
        private float nextSiren;
        private Camera cachedCamera;

        public bool Gathered { get; private set; }
        public bool PoliceArrived => police == Police.Here;
        public bool PoliceBusy => police != Police.Hidden;

        private void Awake()
        {
            states = new State[people.Length];
            targets = new Vector3[people.Length];
            phases = new float[people.Length];
            for (int i = 0; i < people.Length; i++)
            {
                phases[i] = i * 1.3f;
                Show(people[i], false);
            }
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
                float angle = (i * 360f / Mathf.Max(1, count) + 17f) * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                float ring = i == 0 ? 5.2f : 5.8f + (i % 3) * 0.6f;
                targets[i] = Clamp(center + direction * ring);
                if (states[i] == State.Hidden || states[i] == State.Leaving)
                {
                    // Come running from the street behind them (a little later for each one).
                    if (states[i] == State.Hidden)
                        person.root.position = Clamp(center + direction * (comeFrom + (i % 4) * 2f));
                    Show(person, true);
                }
                states[i] = State.Coming;
            }
            shoutUntil = Time.time + 3f;
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
                people[1].root.position = Clamp(middle - meleraiFacing * 6f);
                Show(people[1], true);
            }
            targets[1] = middle;
            states[1] = State.Melerai;
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
            }
            shoutUntil = meleraiUntil = 0f;
        }

        public void DispatchPolice(Vector3 target)
        {
            if (policeMotor == null || police == Police.Arriving || police == Police.Here)
                return;
            target.y = 0f;
            policeLookAt = target;
            // Up the boulevard from the jalan raya, then across to stop beside the hero.
            var turn = new Vector3(0f, 0f, Mathf.Clamp(target.z - 4f, policeGarage.z, 60f));
            Vector3 approach = turn - target;
            approach.y = 0f;
            Vector3 stop = approach.sqrMagnitude > 0.01f ? target + approach.normalized * 3.2f : target + Vector3.back * 3.2f;
            policePath = new[] { policeGarage, turn, Clamp(stop) };
            policeLeg = 1;
            policeMotor.position = policeGarage;
            policeMotor.rotation = Quaternion.LookRotation(Flat(turn - policeGarage, Vector3.forward));
            policeMotor.gameObject.SetActive(true);
            if (policeRider != null) policeRider.SetActive(true);
            Show(policeOfficer, false);
            police = Police.Arriving;
            nextSiren = 0f;
        }

        public void PoliceLeave()
        {
            if (policeMotor == null || police == Police.Hidden || police == Police.Leaving)
                return;
            if (police == Police.Here || policeLeg >= policePath.Length)
            {
                // Back the way they came.
                policePath = new[] { policeMotor.position, policePath.Length > 1 ? policePath[1] : policeGarage, policeGarage };
            }
            else
            {
                policePath = new[] { policeMotor.position, policeGarage };
            }
            policeLeg = 1;
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
            }
            DrivePolice(dt, now);
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
                float step = Mathf.Min(distance, runSpeed * dt);
                person.root.position = new Vector3(position.x + to.x / distance * step, 0f, position.z + to.z / distance * step);
                Face(person.root, to, dt, 10f);
                phases[i] += dt * 9f;
                float swing = Mathf.Sin(phases[i]) * 42f;
                Swing(person.legLeft, swing);
                Swing(person.legRight, -swing);
                Swing(person.armLeft, -swing * .8f);
                Swing(person.armRight, swing * .8f);
                ShowPhone(person, false);
                return;
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
            // Recording with the phone (kepo).
            Set(person.armRight, -105f + Mathf.Sin(phases[i] * 3f) * 3f, 0f, 0f);
            Set(person.armLeft, Mathf.Sin(phases[i] * .7f) * 6f, 0f, 0f);
            ShowPhone(person, true);
        }

        private void DrivePolice(float dt, float now)
        {
            if (policeMotor == null || police == Police.Hidden)
                return;

            bool lightsOn = police != Police.Leaving || policeLeg < policePath.Length;
            bool redPhase = Mathf.Repeat(now * 4f, 1f) < 0.5f;
            foreach (Renderer light in sirenRed)
                if (light != null) light.enabled = lightsOn && redPhase;
            foreach (Renderer light in sirenBlue)
                if (light != null) light.enabled = lightsOn && !redPhase;

            if (police == Police.Here)
            {
                if (policeOfficer != null && policeOfficer.root != null)
                {
                    Face(policeOfficer.root, policeLookAt - policeOfficer.root.position, dt, 6f);
                    Set(policeOfficer.armRight, -70f + Mathf.Sin(now * 3f) * 10f, 0f, 0f);
                }
                return;
            }

            if (now >= nextSiren && police == Police.Arriving)
            {
                nextSiren = now + 1.15f;
                if (CampaignAudio.Instance != null)
                    CampaignAudio.Instance.PlayAt(CampaignSound.Sirene, policeMotor.position, 0.9f);
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
                        policeOfficer.root.position = policeMotor.position + new Vector3(side.x, 0f, side.z) * 0.9f;
                        Show(policeOfficer, true);
                    }
                    policeTalkUntil = now + 3.5f;
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
            float step = policeSpeed * dt;
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

        private void Bubbles(float now)
        {
            Place(shoutBubble, people.Length > 0 ? people[0] : null, now < shoutUntil && states.Length > 0 && states[0] != State.Hidden);
            Place(meleraiBubble, people.Length > 1 ? people[1] : null, states.Length > 1 && states[1] == State.Melerai);
            Place(policeBubble, policeOfficer, police == Police.Here && now < policeTalkUntil);
        }

        private void Place(TextMesh bubble, CampaignCityLife.Walker person, bool show)
        {
            bool visible = show && person != null && person.root != null && person.root.gameObject.activeSelf;
            SetBubble(bubble, visible);
            if (!visible)
                return;
            bubble.transform.position = person.root.position + Vector3.up * 2.35f;
            if (cachedCamera == null)
                cachedCamera = Camera.main;
            if (cachedCamera != null)
            {
                Vector3 look = bubble.transform.position - cachedCamera.transform.position;
                if (look.sqrMagnitude > 0.01f)
                    bubble.transform.rotation = Quaternion.LookRotation(look);
            }
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
