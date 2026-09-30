using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Konoha.Editor
{
    // 0.2.0/0.2.1 "Suasana Nusantara": everyday Indonesian street life around the fictional
    // Konoha capital. Everything is primitive/procedural decor, batching-static, sharing a
    // handful of opaque materials, and WITHOUT colliders, so the tested walkable route and
    // spawn points are untouched. Tall pieces are named "NusantaraTall ..." so the camera
    // occluder hides them when they cover the hero; floor pieces are "NusantaraGround ...".
    //
    // IP/satire: all texts are fictional; no party symbols (no bull head, no banyan tree,
    // no party colours as a set), no real logos or faces. 0.2.1 removed the 17-an
    // decoration (umbul-umbul, bendera segitiga) on the owner's request.
    internal sealed partial class CampaignCapitalArt
    {
        private Material asphalt, kerb, kerbRoad, concrete, steel, cable, porcelain, bamboo, timber,
            terpal, terpalOrange, flagRed, flagWhite, flagYellow, flagGreen, flagBlue,
            bark, canopy, canopyLight, haze, sawah, chrome, rubber, cartWhite, cartGlass;
        private Mesh mountain;

        private void BuildNusantara()
        {
            CreateNusantaraPalette();
            JalanRaya();
            BoulevardKerbs();
            TiangListrik();
            Baliho();
            WarungDanGerobak();
            Trembesi(new Vector3(-24f, 0f, -44f), 0f);
            Trembesi(new Vector3(24f, 0f, -44f), 40f);
            // 0.2.2: moved from x ±38 to ±35, off the new side roads (x ±41).
            Trembesi(new Vector3(-35f, 0f, 6f), 80f);
            Trembesi(new Vector3(35f, 0f, 14f), 120f);
            SawahDanGunung();
            // 0.2.2 Kota Hidup (CampaignCapitalArt.KotaHidup.cs).
            BuildKotaHidup();
            // 0.2.5 Nusantara Megah (CampaignCapitalArt.Megah.cs).
            BuildNusantaraMegah();
        }

        private void CreateNusantaraPalette()
        {
            asphalt = Surface("NusantaraAsphalt", Color.white, .12f);
            asphalt.SetTexture("_BaseMap", ColorTexture("Asphalt", (x, y) =>
            {
                float n = Mathf.PerlinNoise(x * .3f, y * .3f) * .06f + Mathf.PerlinNoise(x * .05f, y * .05f) * .04f;
                // Dashed white centre line along u (half of every tile), faded paint.
                bool dash = Mathf.Abs(y - 128) < 5 && x < 128;
                return dash ? new Color(.86f, .85f, .80f) : new Color(.18f + n, .18f + n, .19f + n);
            }));
            asphalt.SetTextureScale("_BaseMap", new Vector2(44f, 1f));

            // One yellow + one black stone per texture tile, about 0.5 m each.
            var stripes = ColorTexture("KerbStripes", (x, y) =>
                x < 128 ? new Color(.93f, .76f, .10f) : new Color(.08f, .08f, .08f));
            kerb = Surface("NusantaraKerbStripes", Color.white, .2f);
            kerb.SetTexture("_BaseMap", stripes);
            kerb.SetTextureScale("_BaseMap", new Vector2(43f, 1f));
            kerbRoad = Surface("NusantaraKerbRoad", Color.white, .2f);
            kerbRoad.SetTexture("_BaseMap", stripes);
            kerbRoad.SetTextureScale("_BaseMap", new Vector2(220f, 1f));

            concrete = Surface("NusantaraConcrete", new Color(.62f, .62f, .60f), .1f);
            concrete.SetTexture("_BaseMap", Texture("Grain"));
            steel = Surface("NusantaraSteel", new Color(.36f, .37f, .39f), .45f, .6f);
            cable = Surface("NusantaraCable", new Color(.05f, .05f, .05f), .3f);
            porcelain = Surface("NusantaraPorcelain", new Color(.90f, .90f, .86f), .7f);
            bamboo = Surface("NusantaraBamboo", new Color(.70f, .58f, .31f), .25f);
            timber = Surface("NusantaraTimber", new Color(.44f, .29f, .16f), .15f);
            terpal = Surface("NusantaraTerpalBlue", new Color(.13f, .33f, .68f), .35f);
            terpalOrange = Surface("NusantaraTerpalOrange", new Color(.91f, .45f, .10f), .35f);
            flagRed = Surface("NusantaraFlagRed", new Color(.80f, .10f, .10f), .15f);
            flagWhite = Surface("NusantaraFlagWhite", new Color(.94f, .93f, .90f), .15f);
            flagYellow = Surface("NusantaraFlagYellow", new Color(.96f, .78f, .12f), .15f);
            flagGreen = Surface("NusantaraFlagGreen", new Color(.10f, .55f, .26f), .15f);
            flagBlue = Surface("NusantaraFlagBlue", new Color(.12f, .35f, .74f), .15f);
            bark = Surface("NusantaraBark", new Color(.30f, .24f, .18f), .1f);
            bark.SetTexture("_BaseMap", Texture("Grain"));
            canopy = Surface("NusantaraCanopy", new Color(.15f, .34f, .12f), .12f);
            canopyLight = Surface("NusantaraCanopySunlit", new Color(.26f, .46f, .14f), .12f);
            haze = Surface("NusantaraGunungHaze", new Color(.34f, .43f, .52f), 0f);
            chrome = Surface("NusantaraChrome", new Color(.75f, .76f, .78f), .8f, .9f);
            rubber = Surface("NusantaraRubber", new Color(.06f, .06f, .06f), .2f);
            cartWhite = Surface("NusantaraCartWhite", new Color(.92f, .92f, .88f), .4f);
            cartGlass = Surface("NusantaraCartGlass", new Color(.62f, .78f, .84f), .85f);

            sawah = Surface("NusantaraSawah", Color.white, .25f);
            sawah.SetTexture("_BaseMap", ColorTexture("Sawah", (x, y) =>
            {
                // Rows of young rice with a glint of flooded soil; pematang every 128 px.
                bool pematang = y % 128 < 5 || x % 256 < 4;
                if (pematang) return new Color(.42f, .36f, .22f);
                bool row = x % 8 < 5;
                float n = Mathf.PerlinNoise(x * .05f, y * .05f) * .08f;
                return row ? new Color(.30f + n, .56f + n, .14f) : new Color(.26f, .40f + n, .30f);
            }));
            sawah.SetTextureScale("_BaseMap", new Vector2(10f, 10f));

            foreach (var mat in new[] { asphalt, kerb, kerbRoad, concrete, bark, sawah })
                EditorUtility.SetDirty(mat);
            // Photo textures (Art/Textures/<Slot>) or detailed procedural surfaces.
            ApplyRealNusantaraSurfaces(landscapeGrass);

            mountain = CampaignCapitalMeshes.Lathe("NusantaraGunung", new[] {
                new Vector2(1f, 0f), new Vector2(.72f, .18f), new Vector2(.42f, .55f),
                new Vector2(.16f, .95f), new Vector2(.10f, 1f), new Vector2(0f, .97f) }, 28);
        }

        // --- Street --------------------------------------------------------------------------

        // Far (south) pavement of the jalan raya: poles, ruko and pedestrians (0.2.2).
        private const float FarTrotoarZ = -65.6f;
        // Poles stand at the kerb edge of that pavement, clear of the ruko awnings.
        private const float FarPoleZ = -64.95f;

        // A busy road just south of the spawn, beyond the invisible oval boundary.
        private void JalanRaya()
        {
            Block("NusantaraGround jalan raya", new Vector3(0f, .012f, -60f), new Vector3(220f, .02f, 9f), asphalt);
            Block("NusantaraGround kerb jalan raya", new Vector3(0f, .07f, -55.35f), new Vector3(220f, .14f, .3f), kerbRoad);
            // Trotoar between the road and the gate plaza.
            Block("NusantaraGround trotoar", new Vector3(0f, .02f, -54.2f), new Vector3(220f, .03f, 2f), concrete);
            Block("NusantaraGround kerb seberang", new Vector3(0f, .07f, -64.65f), new Vector3(220f, .14f, .3f), kerbRoad);
            Block("NusantaraGround trotoar seberang", new Vector3(0f, .02f, FarTrotoarZ), new Vector3(220f, .03f, 2f), concrete);
        }

        // Black-yellow kerb stones along the boulevard: the most Indonesian street detail.
        private void BoulevardKerbs()
        {
            foreach (int side in new[] { -1, 1 })
            {
                var stone = Block("NusantaraGround kerb boulevard", new Vector3(side * 3.12f, .07f, -30.5f),
                    new Vector3(43f, .14f, .24f), kerb);
                stone.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
        }

        // Concrete utility poles with overhead cables. They run along both sides of the
        // boulevard (x ±12.5). 0.2.2: the road line moved from the near trotoar (z -54.4) to
        // the FAR side of the jalan raya (z -64.95). At -54.4 the wires hung right under the
        // spawn camera and crossed the whole screen (owner screenshot 0.2.1); the far side
        // stays behind the camera. The span over the boulevard entrance is gone for the same reason.
        private void TiangListrik()
        {
            const float top = 7.6f, sideX = 12.5f;
            float[] boulevard = { -50f, -37f, -27f, -14f };
            foreach (int side in new[] { -1, 1 })
            {
                Vector3 previous = Vector3.zero;
                for (int i = 0; i < boulevard.Length; i++)
                {
                    var p = new Vector3(side * sideX, 0f, boulevard[i]);
                    Pole(p, side, i == 1);
                    if (i > 0)
                        Span(previous, p);
                    previous = p;
                }
            }

            // Along the far side of the road, x -98 .. 98 (kerb edge of the far pavement).
            Vector3 last = new Vector3(-98f, 0f, FarPoleZ);
            Pole(last, 1, false);
            for (int k = 1; k <= 14; k++)
            {
                var p = new Vector3(-98f + k * 14f, 0f, FarPoleZ);
                Pole(p, 1, k % 5 == 2);
                Span(last, p);
                last = p;
            }

            void Pole(Vector3 at, int facing, bool transformer)
            {
                Cylinder("NusantaraTall tiang listrik", at + Vector3.up * top * .5f, new Vector3(.26f, top * .5f, .26f), concrete);
                Block("NusantaraTall palang tiang", at + Vector3.up * (top - .3f), new Vector3(1.7f, .1f, .12f), steel);
                for (int n = -1; n <= 1; n++)
                    Cylinder("Nusantara isolator", at + new Vector3(n * .7f, top - .15f, 0f), new Vector3(.08f, .1f, .08f), porcelain);
                if (transformer)
                {
                    Block("NusantaraTall trafo", at + new Vector3(facing * .45f, 5.3f, 0f), new Vector3(.6f, .9f, .55f), steel);
                    Block("Nusantara papan bahaya", at + new Vector3(facing * .14f, 2.6f, -.14f), new Vector3(.3f, .4f, .02f), flagYellow);
                }
            }

            void Span(Vector3 from, Vector3 to)
            {
                // Crossarm runs along x; spans along x keep the three wires stacked, spans
                // along z spread them across the crossarm.
                bool alongX = Mathf.Abs(to.x - from.x) > Mathf.Abs(to.z - from.z);
                for (int n = -1; n <= 1; n++)
                {
                    Vector3 offset = alongX ? new Vector3(0f, top - .08f - (n + 1) * .12f, 0f)
                        : new Vector3(n * .7f, top - .08f, 0f);
                    Wire(from + offset, to + offset, .45f + .12f * (n + 1), alongX ? 6 : 5);
                }
            }
        }

        // Fictional satirical billboards. Readable from the spawn camera.
        private void Baliho()
        {
            BalihoBoard(new Vector3(-16.5f, 0f, -47f), 16f, flagWhite, flagRed,
                "SELAMAT DATANG\nDI IBU KOTA KONOHA", Color.black);
            BalihoBoard(new Vector3(16.5f, 0f, -47f), -16f, flagYellow, flagBlue,
                "JALAN RUSAK?\nSABAR, MASIH DIANGGARKAN", Color.black);
            BalihoBoard(new Vector3(-15.5f, 0f, -22f), 20f, flagBlue, flagYellow,
                "KONOHA MAJU\nRAKYAT ANTRI", Color.white);
            BalihoBoard(new Vector3(15.5f, 0f, -22f), -20f, flagGreen, flagWhite,
                "DILARANG KAMPANYE DI SINI*\n*kecuali yang sedang berkuasa", Color.white);
        }

        private void BalihoBoard(Vector3 p, float yaw, Material background, Material stripe, string text, Color ink)
        {
            var group = Group("NusantaraTall baliho", p, yaw);
            foreach (int side in new[] { -1, 1 })
                LocalPart(group, "NusantaraTall baliho tiang", PrimitiveType.Cylinder,
                    new Vector3(side * 2.3f, 2.4f, .1f), new Vector3(.16f, 2.4f, .16f), steel);
            LocalPart(group, "NusantaraTall baliho papan", PrimitiveType.Cube,
                new Vector3(0f, 5.3f, 0f), new Vector3(6.2f, 2.9f, .14f), background);
            LocalPart(group, "NusantaraTall baliho list", PrimitiveType.Cube,
                new Vector3(0f, 6.83f, -.01f), new Vector3(6.3f, .22f, .16f), stripe);
            LocalPart(group, "NusantaraTall baliho list", PrimitiveType.Cube,
                new Vector3(0f, 3.77f, -.01f), new Vector3(6.3f, .22f, .16f), stripe);
            Label(group, text, new Vector3(0f, 5.3f, -.09f), .42f, ink);
        }

        // --- Street vendors -------------------------------------------------------------

        private void WarungDanGerobak()
        {
            // Warung kopi facing the boulevard, west of the gate.
            var warung = Group("NusantaraTall warung kopi", new Vector3(-12.5f, 0f, -40.5f), -90f);
            LocalPart(warung, "Nusantara lantai warung", PrimitiveType.Cube, new Vector3(0f, .05f, 0f), new Vector3(4.2f, .1f, 2.8f), timber);
            LocalPart(warung, "Nusantara meja warung", PrimitiveType.Cube, new Vector3(0f, .55f, -.95f), new Vector3(3.8f, .9f, .5f), timber);
            LocalPart(warung, "Nusantara papan meja", PrimitiveType.Cube, new Vector3(0f, 1.03f, -.95f), new Vector3(4f, .06f, .7f), flagWhite);
            foreach (int x in new[] { -1, 1 })
                foreach (int z in new[] { -1, 1 })
                    LocalPart(warung, "NusantaraTall tiang warung", PrimitiveType.Cylinder,
                        new Vector3(x * 1.95f, 1.3f, z * 1.25f), new Vector3(.1f, 1.3f, .1f), bamboo);
            var roofPart = LocalPart(warung, "NusantaraTall terpal warung", PrimitiveType.Cube,
                new Vector3(0f, 2.65f, 0f), new Vector3(4.6f, .06f, 3.3f), terpal);
            roofPart.transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
            LocalPart(warung, "NusantaraTall terpal pinggir", PrimitiveType.Cube,
                new Vector3(0f, 2.45f, -1.7f), new Vector3(4.6f, .35f, .04f), terpalOrange);
            // Rows of sachet coffee hanging from the roof edge.
            Material[] sachet = { flagRed, flagYellow, flagGreen, flagBlue, terpalOrange };
            for (int i = 0; i < 7; i++)
                LocalPart(warung, "Nusantara renteng sachet", PrimitiveType.Cube,
                    new Vector3(-1.6f + i * .53f, 1.85f, -1.45f), new Vector3(.14f, .8f, .02f), sachet[i % sachet.Length]);
            foreach (float x in new[] { -1.2f, -.7f })
                LocalPart(warung, "Nusantara termos", PrimitiveType.Cylinder, new Vector3(x, 1.25f, -.95f), new Vector3(.18f, .18f, .18f), flagRed);
            LocalPart(warung, "Nusantara toples kerupuk", PrimitiveType.Cylinder, new Vector3(1f, 1.3f, -.95f), new Vector3(.3f, .22f, .3f), cartGlass);
            // Bench in front (bangku panjang).
            LocalPart(warung, "Nusantara bangku panjang", PrimitiveType.Cube, new Vector3(0f, .45f, -1.9f), new Vector3(3.2f, .07f, .38f), timber);
            foreach (int x in new[] { -1, 1 })
                LocalPart(warung, "Nusantara kaki bangku", PrimitiveType.Cube, new Vector3(x * 1.4f, .22f, -1.9f), new Vector3(.08f, .44f, .32f), timber);
            Label(warung, "WARKOP RAKYAT", new Vector3(0f, 3.05f, -1.75f), .32f, Color.white);

            // Parked motor bebek in front of the warung.
            Motor(new Vector3(-9.6f, 0f, -43.2f), 70f, flagRed);
            Motor(new Vector3(-9.8f, 0f, -38.4f), 110f, flagBlue);
            Motor(new Vector3(-10.2f, 0f, -36.6f), 100f, cable);

            // Gerobak bakso with its umbrella, east of the gate.
            var cart = Group("NusantaraTall gerobak bakso", new Vector3(11.8f, 0f, -41f), 90f);
            LocalPart(cart, "Nusantara badan gerobak", PrimitiveType.Cube, new Vector3(0f, .95f, 0f), new Vector3(1.5f, .9f, .75f), cartWhite);
            LocalPart(cart, "Nusantara kaca gerobak", PrimitiveType.Cube, new Vector3(0f, 1.65f, 0f), new Vector3(1.4f, .5f, .65f), cartGlass);
            LocalPart(cart, "Nusantara atap gerobak", PrimitiveType.Cube, new Vector3(0f, 1.95f, 0f), new Vector3(1.6f, .06f, .85f), flagBlue);
            foreach (int z in new[] { -1, 1 })
            {
                var wheel = LocalPart(cart, "Nusantara roda gerobak", PrimitiveType.Cylinder,
                    new Vector3(.2f, .35f, z * .45f), new Vector3(.62f, .04f, .62f), rubber);
                wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                LocalPart(cart, "Nusantara gagang gerobak", PrimitiveType.Cube,
                    new Vector3(-1.05f, .95f, z * .3f), new Vector3(.8f, .05f, .05f), timber);
            }
            LocalPart(cart, "Nusantara kaki gerobak", PrimitiveType.Cube, new Vector3(.65f, .25f, 0f), new Vector3(.06f, .5f, .5f), timber);
            LocalPart(cart, "NusantaraTall tiang payung", PrimitiveType.Cylinder, new Vector3(.4f, 2.4f, 0f), new Vector3(.05f, .5f, .05f), steel);
            LocalPart(cart, "NusantaraTall payung", PrimitiveType.Cylinder, new Vector3(.4f, 2.9f, 0f), new Vector3(2.2f, .04f, 2.2f), terpalOrange);
            Label(cart, "BAKSO MANTAP", new Vector3(0f, 1.05f, -.39f), .22f, new Color(.75f, .1f, .1f));
        }

        private void Motor(Vector3 p, float yaw, Material paint)
        {
            var motor = Group("Nusantara motor bebek", p, yaw);
            LocalPart(motor, "Nusantara bodi motor", PrimitiveType.Cube, new Vector3(0f, .55f, 0f), new Vector3(.34f, .34f, 1.15f), paint);
            LocalPart(motor, "Nusantara tameng motor", PrimitiveType.Cube, new Vector3(0f, .75f, .52f), new Vector3(.36f, .6f, .12f), paint);
            LocalPart(motor, "Nusantara jok motor", PrimitiveType.Cube, new Vector3(0f, .8f, -.2f), new Vector3(.3f, .09f, .6f), rubber);
            LocalPart(motor, "Nusantara setang motor", PrimitiveType.Cube, new Vector3(0f, 1.08f, .52f), new Vector3(.62f, .04f, .05f), chrome);
            LocalPart(motor, "Nusantara lampu motor", PrimitiveType.Cube, new Vector3(0f, 1f, .6f), new Vector3(.16f, .1f, .04f), porcelain);
            foreach (float z in new[] { -.48f, .52f })
            {
                var wheel = LocalPart(motor, "Nusantara roda motor", PrimitiveType.Cylinder,
                    new Vector3(0f, .28f, z), new Vector3(.5f, .05f, .5f), rubber);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            LocalPart(motor, "Nusantara standar", PrimitiveType.Cube, new Vector3(.2f, .15f, 0f), new Vector3(.03f, .3f, .03f), chrome)
                .transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
        }

        // Rain tree (trembesi): the common wide-umbrella shade tree of Indonesian roads.
        // Deliberately not a beringin, which is also a real party symbol.
        private void Trembesi(Vector3 p, float twist)
        {
            Cylinder("NusantaraTall batang trembesi", p + Vector3.up * 1.7f, new Vector3(.75f, 1.7f, .75f), bark);
            for (int i = 0; i < 3; i++)
            {
                float angle = twist + i * 120f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Stick("NusantaraTall dahan trembesi", p + Vector3.up * 3.1f, p + Vector3.up * 5.2f + direction * 2.6f, .32f, bark);
            }
            Vector3[] crowns =
            {
                new Vector3(0f, 6.3f, 0f), new Vector3(2.6f, 5.8f, .8f), new Vector3(-2.3f, 5.9f, 1.4f),
                new Vector3(.6f, 5.7f, -2.6f), new Vector3(-1.2f, 6.1f, -1.6f)
            };
            for (int i = 0; i < crowns.Length; i++)
                Ellipsoid("NusantaraTall tajuk trembesi", p + Quaternion.Euler(0f, twist, 0f) * crowns[i],
                    new Vector3(5.2f, 1.8f, 5.2f) * (i == 0 ? 1.15f : .9f), i % 2 == 0 ? canopy : canopyLight);
        }

        // Rice fields on both sides of the capital and volcanoes on the hazy horizon.
        private void SawahDanGunung()
        {
            // 0.2.2: pushed out beyond the side roads and kampung houses; the north field sits
            // between the north road (z 70) and the bay (z 110).
            Block("NusantaraGround sawah", new Vector3(-92f, .006f, -20f), new Vector3(70f, .02f, 90f), sawah);
            Block("NusantaraGround sawah", new Vector3(92f, .006f, -20f), new Vector3(70f, .02f, 90f), sawah);
            Block("NusantaraGround sawah", new Vector3(-75f, .006f, 94f), new Vector3(50f, .02f, 28f), sawah);
            // 0.2.5: pushed back and narrowed (the lathe radius is the scale) so they rise
            // behind the bay instead of burying the bridge, islands and coastal town.
            MeshObject("Nusantara gunung", mountain, new Vector3(-90f, -.5f, 235f), new Vector3(60f, 50f, 60f), haze);
            MeshObject("Nusantara gunung", mountain, new Vector3(30f, -.5f, 250f), new Vector3(70f, 62f, 70f), haze);
            MeshObject("Nusantara gunung", mountain, new Vector3(130f, -.5f, 215f), new Vector3(50f, 40f, 50f), haze);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer.gameObject.name == "Nusantara gunung")
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        // --- Helpers ---------------------------------------------------------------------

        private GameObject Cylinder(string name, Vector3 p, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Configure(go, name, p, scale, mat);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        // A thin cylinder between two points.
        private GameObject Stick(string name, Vector3 a, Vector3 b, float thickness, Material mat)
        {
            Vector3 delta = b - a;
            var go = Cylinder(name, (a + b) * .5f, new Vector3(thickness, delta.magnitude * .5f, thickness), mat);
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            return go;
        }

        // A sagging cable between two points (parabolic sag), as box segments.
        private void Wire(Vector3 a, Vector3 b, float sag, int segments)
        {
            Vector3 previous = a;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 next = Vector3.Lerp(a, b, t) + Vector3.down * (sag * 4f * t * (1f - t));
                Vector3 delta = next - previous;
                var go = Block("Nusantara kabel", (previous + next) * .5f, new Vector3(.035f, .035f, delta.magnitude + .02f), cable);
                go.transform.rotation = Quaternion.LookRotation(delta.normalized);
                previous = next;
            }
        }

        private Transform Group(string name, Vector3 p, float yaw)
        {
            var group = new GameObject(name).transform;
            group.SetParent(root);
            group.position = p;
            group.rotation = Quaternion.Euler(0f, yaw, 0f);
            return group;
        }

        private GameObject LocalPart(Transform parent, string name, PrimitiveType type, Vector3 localPosition,
            Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        // Flat text facing -z of the parent (readable from the south / spawn side).
        private TextMesh Label(Transform parent, string text, Vector3 localPosition, float scale, Color color)
        {
            var go = new GameObject("NusantaraTall teks " + text.Split('\n')[0]);
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

        // Full-colour procedural texture (256 px, repeat), stored with the other capital assets.
        private static Texture2D ColorTexture(string name, Func<int, int, Color> pixel)
        {
            string path = CampaignCapitalMeshes.Folder + "/" + name + ".asset";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            const int size = 256;
            if (tex == null) { tex = new Texture2D(size, size, TextureFormat.RGBA32, true); AssetDatabase.CreateAsset(tex, path); }
            tex.name = name; tex.wrapMode = TextureWrapMode.Repeat; tex.filterMode = FilterMode.Trilinear; tex.anisoLevel = 4;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = pixel(x, y);
            tex.SetPixels(pixels); tex.Apply(true, false); EditorUtility.SetDirty(tex);
            return tex;
        }
    }
}
