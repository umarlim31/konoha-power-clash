using System.Collections.Generic;
using Konoha.Campaign;
using UnityEngine;

namespace Konoha.Editor
{
    // 0.6.0 KARIER Level 1 "Warga Biasa": job places, the crowd that gathers at a fight,
    // the police patrol motor and the job props carried by the hero (ojol motor with a
    // passenger, cement sack). Everything is decor without colliders, parented under
    // "Kota Hidup" like the other moving pieces; KarierCrowd / KarierController animate it.
    internal sealed partial class CampaignCapitalArt
    {
        // Ground points used by KarierController (all on tested walkable ground: blusukan
        // points, walker paths, the gang spawn points and the route checkpoints).
        internal static readonly string[] KarierPlaceNames =
        {
            "POS RONDA", "TUKANG SAYUR", "PANGKALAN OJOL", "WARKOP", "TUKANG BAKSO",
            "PLAZA", "TAMAN BARAT", "TAMAN TIMUR", "MARKAS KOALISI", "KANTOR KELURAHAN"
        };
        internal static readonly Vector3 KarierKuliPickup = new Vector3(-10.5f, 0f, -34f);
        internal static readonly Vector3 KarierKuliDrop = new Vector3(10.5f, 0f, -34f);
        internal static readonly Vector3 KarierWarkop = new Vector3(-8.3f, 0f, -41.5f);
        internal static readonly Vector3 KarierPosRt = new Vector3(-8.6f, 0f, -26f);
        internal static readonly Vector3 KarierPremanCenter = new Vector3(1f, 0f, -29.5f);
        internal static readonly Vector3[] KarierPremanPoints = { new Vector3(2f, 0.1f, -30f), new Vector3(0f, 0.1f, -28.5f) };
        internal static readonly Vector3 KarierPremanLook = new Vector3(8.6f, 0f, -26f);

        internal static Vector3[] KarierPlacePoints(Vector3 plaza) => new[]
        {
            new Vector3(-8.6f, 0f, -26f), new Vector3(8.6f, 0f, -26f), new Vector3(13f, 0f, -21f),
            KarierWarkop, new Vector3(8.3f, 0f, -41.5f), new Vector3(plaza.x, 0f, plaza.z),
            new Vector3(-20f, 0f, 1.5f), new Vector3(20f, 0f, 1.5f), new Vector3(-14f, 0f, -5f), new Vector3(14f, 0f, -5f)
        };

        internal sealed class KarierScene
        {
            public KarierCrowd crowd;
            public Transform ojolMotor;
            public GameObject ojolPassenger;
            public Transform sack;
        }

        internal KarierScene BuildKarier()
        {
            var scene = new KarierScene();
            var props = new GameObject("Karier properti").transform;
            props.SetParent(kotaRoot, false);
            Material sackCloth = Surface("KarierSakSemen", new Color(.80f, .74f, .62f), .1f);

            // KULI: tumpukan semen (pickup) and the gorong-gorong project (drop). Props stand
            // outside the 2.6 m zone so the hero can always reach its centre.
            var pile = new GameObject("Karier tumpukan semen").transform;
            pile.SetParent(props, false);
            pile.position = KarierKuliPickup + new Vector3(-2.2f, 0f, 0f);
            for (int i = 0; i < 9; i++)
                KotaPart(pile, "Karier sak semen", PrimitiveType.Cube,
                    new Vector3((i % 3 - 1) * .5f, .12f + (i / 3) * .23f, (i % 2) * .08f), new Vector3(.46f, .22f, .32f), sackCloth, i == 0);
            Pikap(props, KarierKuliPickup + new Vector3(-3.4f, 0f, -3.2f), 15f);
            KarierText(pile, "TUMPUKAN SEMEN", new Vector3(0f, 1.25f, -.3f), .16f, new Color(.95f, .92f, .82f));

            var proyek = new GameObject("Karier proyek gorong-gorong").transform;
            proyek.SetParent(props, false);
            proyek.position = KarierKuliDrop + new Vector3(2.4f, 0f, 0f);
            KotaPart(proyek, "Karier galian", PrimitiveType.Cube, new Vector3(0f, .02f, 0f), new Vector3(1.8f, .03f, 2.4f), dark);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                KotaPart(proyek, "Karier kerucut", PrimitiveType.Cylinder,
                    new Vector3(Mathf.Sin(angle) * 1.6f, .25f, Mathf.Cos(angle) * 1.9f), new Vector3(.22f, .25f, .22f), terpalOrange);
            }
            for (int i = 0; i < 3; i++)
                KotaPart(proyek, "Karier sak semen", PrimitiveType.Cube, new Vector3(-1.4f + i * .5f, .12f, -1.9f), new Vector3(.46f, .22f, .32f), sackCloth);
            KotaPart(proyek, "Karier papan proyek tiang", PrimitiveType.Cube, new Vector3(1.2f, .8f, -2.3f), new Vector3(.08f, 1.6f, .08f), timber);
            KotaPart(proyek, "Karier papan proyek", PrimitiveType.Cube, new Vector3(1.2f, 1.6f, -2.3f), new Vector3(1.6f, .7f, .05f), flagWhite);
            KarierText(proyek, "PROYEK GORONG-GORONG\nDANA ASPIRASI Rp 2 M\n(dikerjakan 2 orang)", new Vector3(1.2f, 1.6f, -2.34f), .1f, new Color(.15f, .15f, .15f));

            // The crowd of a fight: [0] shouts, [1] melerai (peci), the rest record it.
            var crowd = new GameObject("Karier kerumunan").AddComponent<KarierCrowd>();
            crowd.transform.SetParent(kotaRoot, false);
            int[] seeds = { 3, 2, 1, 4, 6, 8, 11, 13, 16, 9 };
            var people = new List<CampaignCityLife.Walker>();
            for (int i = 0; i < seeds.Length; i++)
            {
                CampaignCityLife.Walker person = Person(new Vector3(0f, 0f, -58f), 0f, seeds[i], true);
                person.root.name = i == 0 ? "Karier warga teriak" : i == 1 ? "Karier warga melerai" : "Karier warga kepo";
                person.root.SetParent(crowd.transform, true);
                people.Add(person);
            }
            crowd.people = people.ToArray();
            crowd.shoutBubble = Bubble(crowd.transform, "Karier gelembung teriak", "WOI! ADA YANG BERANTEM!", new Color(1f, .85f, .35f));
            crowd.meleraiBubble = Bubble(crowd.transform, "Karier gelembung melerai", "SUDAH, SUDAH!\nMALU SAMA TETANGGA!", new Color(.75f, 1f, .75f));
            crowd.policeBubble = Bubble(crowd.transform, "Karier gelembung polisi", "SEMUA DIAM!\nADA APA INI?!", new Color(.75f, .85f, 1f));
            crowd.boundaryCenter = BoundaryCenter;
            crowd.boundaryRadii = BoundaryRadii;
            crowd.policeGarage = new Vector3(0f, 0f, RingRoad[0].z - 2f);
            PoliceMotor(crowd);
            scene.crowd = crowd;

            // OJOL: the hero rides this green motor while the job runs (parented at runtime).
            var ojol = new GameObject("Karier motor ojol").transform;
            ojol.SetParent(kotaRoot, false);
            ojol.position = new Vector3(0f, 0f, -58f);
            MotorBody(ojol, flagGreen);
            KarierText(ojol, "OJOL KONOHA", new Vector3(0f, .7f, -.82f), .08f, Color.white).transform.localRotation = Quaternion.identity;
            var passenger = new GameObject("Karier penumpang ojol").transform;
            passenger.SetParent(ojol, false);
            KotaPart(passenger, "Karier penumpang badan", PrimitiveType.Capsule, new Vector3(0f, 1.25f, -.62f), new Vector3(.38f, .3f, .26f), shirts[1]);
            KotaPart(passenger, "Karier penumpang paha", PrimitiveType.Capsule, new Vector3(0f, .92f, -.42f), new Vector3(.32f, .14f, .4f), pants)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            KotaPart(passenger, "Karier penumpang kepala", PrimitiveType.Sphere, new Vector3(0f, 1.62f, -.64f), new Vector3(.2f, .24f, .21f), skins[1]);
            KotaPart(passenger, "Karier penumpang helm", PrimitiveType.Sphere, new Vector3(0f, 1.7f, -.66f), new Vector3(.27f, .24f, .29f), flagGreen);
            ojol.gameObject.SetActive(false);
            scene.ojolMotor = ojol;
            scene.ojolPassenger = passenger.gameObject;

            // KULI: the sack on the hero's shoulder.
            var sack = new GameObject("Karier sak di pundak").transform;
            sack.SetParent(kotaRoot, false);
            KotaPart(sack, "Karier sak semen dipikul", PrimitiveType.Cube, Vector3.zero, new Vector3(.5f, .22f, .34f), sackCloth);
            sack.gameObject.SetActive(false);
            scene.sack = sack;
            return scene;
        }

        // Plain motor bebek seen from the game camera: seat top at about 0.82 m, faces +z.
        private void MotorBody(Transform motor, Material paint)
        {
            KotaPart(motor, "Karier motor bodi", PrimitiveType.Cube, new Vector3(0f, .52f, -.05f), new Vector3(.34f, .32f, 1.2f), paint, true);
            KotaPart(motor, "Karier motor jok", PrimitiveType.Cube, new Vector3(0f, .74f, -.3f), new Vector3(.32f, .12f, .8f), rubber);
            KotaPart(motor, "Karier motor tameng", PrimitiveType.Cube, new Vector3(0f, .76f, .5f), new Vector3(.38f, .62f, .12f), paint);
            KotaPart(motor, "Karier motor setang", PrimitiveType.Cube, new Vector3(0f, 1.08f, .5f), new Vector3(.66f, .04f, .05f), chrome);
            KotaPart(motor, "Karier motor lampu", PrimitiveType.Cube, new Vector3(0f, .98f, .58f), new Vector3(.16f, .1f, .04f), lampLit);
            foreach (float z in new[] { -.55f, .58f })
                KotaPart(motor, "Karier motor roda", PrimitiveType.Cylinder, new Vector3(0f, .27f, z), new Vector3(.52f, .05f, .52f), rubber)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        // The patrol motor: white with a blue band and a red/blue light bar, an officer
        // riding it, and the same officer standing (shown once it has arrived).
        private void PoliceMotor(KarierCrowd crowd)
        {
            Material uniform = Surface("KarierSeragamPolisi", new Color(.36f, .27f, .17f), .15f);
            Material cap = Surface("KarierTopiPolisi", new Color(.20f, .16f, .11f), .3f);
            var motor = new GameObject("Karier motor polisi").transform;
            motor.SetParent(crowd.transform, false);
            motor.position = crowd.policeGarage;
            MotorBody(motor, carPaints[0]);
            KotaPart(motor, "Karier motor polisi strip", PrimitiveType.Cube, new Vector3(0f, .56f, -.05f), new Vector3(.35f, .08f, 1.21f), flagBlue);
            KotaPart(motor, "Karier motor polisi tiang lampu", PrimitiveType.Cube, new Vector3(0f, 1.2f, -.72f), new Vector3(.04f, .5f, .04f), steel);
            var red = KotaPart(motor, "Karier sirene merah", PrimitiveType.Cube, new Vector3(-.09f, 1.47f, -.72f), new Vector3(.16f, .1f, .1f),
                CampaignRigBuilder.Unlit("KarierSireneMerah", new Color(1f, .12f, .10f)));
            var blue = KotaPart(motor, "Karier sirene biru", PrimitiveType.Cube, new Vector3(.09f, 1.47f, -.72f), new Vector3(.16f, .1f, .1f),
                CampaignRigBuilder.Unlit("KarierSireneBiru", new Color(.15f, .35f, 1f)));
            KarierText(motor, "POLISI KONOHA", new Vector3(0f, .62f, -.68f), .07f, Color.white);
            crowd.sirenRed = new[] { red.GetComponent<Renderer>() };
            crowd.sirenBlue = new[] { blue.GetComponent<Renderer>() };

            var rider = new GameObject("Karier polisi berkendara").transform;
            rider.SetParent(motor, false);
            KotaPart(rider, "Karier polisi badan", PrimitiveType.Capsule, new Vector3(0f, 1.28f, -.2f), new Vector3(.4f, .32f, .26f), uniform, true);
            KotaPart(rider, "Karier polisi paha", PrimitiveType.Capsule, new Vector3(0f, .94f, .02f), new Vector3(.34f, .14f, .42f), pants)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            KotaPart(rider, "Karier polisi kepala", PrimitiveType.Sphere, new Vector3(0f, 1.66f, -.2f), new Vector3(.21f, .25f, .22f), skins[0]);
            KotaPart(rider, "Karier polisi helm", PrimitiveType.Sphere, new Vector3(0f, 1.74f, -.22f), new Vector3(.28f, .25f, .3f), carPaints[0]);
            foreach (int s in new[] { -1, 1 })
                KotaPart(rider, "Karier polisi lengan", PrimitiveType.Capsule, new Vector3(s * .24f, 1.3f, .2f), new Vector3(.1f, .28f, .1f), uniform)
                    .transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
            crowd.policeMotor = motor;
            crowd.policeRider = rider.gameObject;

            CampaignCityLife.Walker officer = Person(crowd.policeGarage, 0f, 0, true, uniform);
            officer.root.name = "Karier polisi";
            officer.root.SetParent(crowd.transform, true);
            KotaPart(officer.root, "Karier polisi topi", PrimitiveType.Cylinder, new Vector3(0f, 1.82f, 0f), new Vector3(.26f, .05f, .27f), cap);
            KotaPart(officer.root, "Karier polisi pet", PrimitiveType.Cube, new Vector3(0f, 1.8f, .13f), new Vector3(.2f, .02f, .1f), cap);
            crowd.policeOfficer = officer;
            motor.gameObject.SetActive(false);
        }

        private void Pikap(Transform parent, Vector3 at, float yaw)
        {
            var truck = new GameObject("Karier pikap material").transform;
            truck.SetParent(parent, false);
            truck.position = at;
            truck.rotation = Quaternion.Euler(0f, yaw, 0f);
            KotaPart(truck, "Karier pikap kabin", PrimitiveType.Cube, new Vector3(0f, 1.15f, 1.2f), new Vector3(1.7f, 1.5f, 1.6f), carPaints[1], true);
            KotaPart(truck, "Karier pikap kaca", PrimitiveType.Cube, new Vector3(0f, 1.5f, 1.95f), new Vector3(1.5f, .55f, .1f), windowGlass);
            KotaPart(truck, "Karier pikap bak", PrimitiveType.Cube, new Vector3(0f, .8f, -.9f), new Vector3(1.7f, .7f, 2.5f), carPaints[1]);
            KotaPart(truck, "Karier pikap muatan", PrimitiveType.Cube, new Vector3(0f, 1.27f, -.9f), new Vector3(1.4f, .3f, 2f), Surface("KarierSakSemen", new Color(.80f, .74f, .62f), .1f));
            foreach (int x in new[] { -1, 1 })
                foreach (int z in new[] { -1, 1 })
                    KotaPart(truck, "Karier pikap roda", PrimitiveType.Cylinder, new Vector3(x * .86f, .32f, z * 1.35f), new Vector3(.64f, .1f, .64f), rubber)
                        .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        // Like Label (flat text facing -z) but not named "NusantaraTall...", so it is never a
        // camera occluder candidate (some of these objects start inactive).
        private static TextMesh KarierText(Transform parent, string text, Vector3 localPosition, float scale, Color color)
        {
            var go = new GameObject("Karier teks " + text.Split('\n')[0]);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * scale;
            var label = go.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 48;
            label.characterSize = .09f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.text = text;
            label.color = color;
            if (label.font != null)
                go.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            return label;
        }

        // Speech bubble text (billboarded by KarierCrowd), hidden until needed.
        private static TextMesh Bubble(Transform parent, string name, string text, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * .3f;
            var label = go.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 48;
            label.characterSize = .22f;
            label.anchor = TextAnchor.LowerCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.text = text;
            label.color = color;
            if (label.font != null)
                go.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            go.SetActive(false);
            return label;
        }
    }
}
