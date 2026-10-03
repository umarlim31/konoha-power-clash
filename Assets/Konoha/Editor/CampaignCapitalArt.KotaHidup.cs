using System.Collections.Generic;
using Konoha.Campaign;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Konoha.Editor
{
    // 0.2.2 "Kota Hidup": the capital from the owner's concept images, built around the
    // existing (tested) route instead of replacing it.
    //  - Ring road around the capital with traffic (sedan, angkot, pikap, motor), left-hand.
    //  - Warga: walkers on pavements/gardens (step aside, flee fights), vendors and onlookers.
    //  - Fountains in the plaza canals, red-white flags and banners with a FICTIONAL Konoha
    //    crest (gold star; never the state emblem, see CLAUDE.md IP rules).
    //  - Ruko row, kampung houses with genteng roofs, and a bay with an arched bridge,
    //    lighthouses and sailboats in front of the volcanoes.
    // Moving pieces live under "Kota Hidup" (not static, animated by CampaignCityLife);
    // everything else is batching-static. Nothing here has a collider.
    internal sealed partial class CampaignCapitalArt
    {
        // Ring road centre line: jalan raya (south), x ±41 (east/west), z 70 (north, behind
        // the palace). Counter-clockwise seen from above; the invisible oval is x ±34, z -54..62.
        internal static readonly Vector3[] RingRoad =
        {
            new Vector3(-41f, 0f, -60f), new Vector3(41f, 0f, -60f),
            new Vector3(41f, 0f, 70f), new Vector3(-41f, 0f, 70f)
        };
        internal const float SideRoadX = 41f, NorthRoadZ = 70f, HouseRowX = 53f;

        internal CampaignCityLife CityLife { get; private set; }

        private Material[] carPaints, shirts, skins, wallTints, awnings;
        private Material[] hijabs;
        private Material hairGrey;
        private Material pants, hair, hijabCloth, helmet, shutter, windowGlass, bay, lighthouseWhite, sail, lampLit;
        private Transform kotaRoot;

        private void BuildKotaHidup()
        {
            CreateKotaPalette();
            RingRoads();
            RukoRow();
            KampungHouses();
            FlagsAndBanners();
            BayAndBridge();

            kotaRoot = new GameObject("Kota Hidup").transform;
            kotaRoot.SetParent(root);
            CityLife = kotaRoot.gameObject.AddComponent<CampaignCityLife>();
            CityLife.ringCorners = (Vector3[])RingRoad.Clone();
            Traffic(CityLife);
            // The gerobak bakso (CampaignCapitalArt.Nusantara.cs).
            CityLife.bowlSpots = new[] { new Vector3(11.8f, 1f, -40.2f) };
            Warga(CityLife);
            Fountains(CityLife);
        }

        private void CreateKotaPalette()
        {
            carPaints = new[]
            {
                Surface("KotaCatPutih", new Color(.90f, .90f, .88f), .7f, .3f),
                Surface("KotaCatPerak", new Color(.62f, .64f, .66f), .7f, .5f),
                Surface("KotaCatHitam", new Color(.07f, .07f, .08f), .75f, .3f),
                Surface("KotaCatMerah", new Color(.62f, .08f, .07f), .7f, .3f),
                Surface("KotaAngkotBiru", new Color(.20f, .52f, .80f), .5f, .1f),
                Surface("KotaAngkotHijau", new Color(.18f, .58f, .30f), .5f, .1f),
                Surface("KotaAngkotKuning", new Color(.93f, .76f, .16f), .5f, .1f)
            };
            shirts = new[]
            {
                Surface("KotaBajuPutih", new Color(.88f, .87f, .83f), .1f),
                Surface("KotaBajuBiru", new Color(.18f, .30f, .55f), .1f),
                Surface("KotaBajuMerah", new Color(.62f, .14f, .12f), .1f),
                Surface("KotaBajuHijau", new Color(.22f, .42f, .24f), .1f),
                Surface("KotaBajuKuning", new Color(.85f, .66f, .22f), .1f),
                Surface("KotaBajuBatik", new Color(.45f, .28f, .14f), .1f),
                Surface("KotaBajuAbu", new Color(.40f, .41f, .43f), .1f)
            };
            shirts[5].SetTexture("_BaseMap", ColorTexture("KotaBatik", (x, y) =>
            {
                // Simple kawung-like dots on brown: reads as batik from the game camera.
                int cx = x % 32 - 16, cy = y % 32 - 16;
                bool dot = cx * cx + cy * cy < 70 && (cx * cy) % 3 != 0;
                return dot ? new Color(.95f, .80f, .45f) : new Color(1f, 1f, 1f);
            }));
            skins = new[]
            {
                Surface("KotaKulitSawoMatang", new Color(.58f, .40f, .27f), .25f),
                Surface("KotaKulitKuningLangsat", new Color(.76f, .58f, .43f), .25f),
                Surface("KotaKulitGelap", new Color(.42f, .28f, .19f), .25f)
            };
            pants = Surface("KotaCelana", new Color(.14f, .15f, .19f), .1f);
            hair = Surface("KotaRambut", new Color(.05f, .04f, .04f), .35f);
            hijabCloth = Surface("KotaJilbab", new Color(.55f, .36f, .45f), .1f);
            // 0.6.2: more varied people (owner: "lebih nyata"): jilbab colours and grey hair.
            hijabs = new[]
            {
                hijabCloth,
                Surface("KotaJilbabNavy", new Color(.16f, .20f, .34f), .1f),
                Surface("KotaJilbabKrem", new Color(.84f, .76f, .62f), .1f),
                Surface("KotaJilbabHitam", new Color(.08f, .08f, .09f), .1f),
                Surface("KotaJilbabHijau", new Color(.22f, .42f, .34f), .1f)
            };
            hairGrey = Surface("KotaRambutUban", new Color(.55f, .54f, .52f), .3f);
            helmet = Surface("KotaHelm", new Color(.85f, .85f, .82f), .6f);
            shutter = Surface("KotaRolling", new Color(.55f, .57f, .58f), .45f, .4f);
            windowGlass = Surface("KotaKaca", new Color(.20f, .27f, .31f), .85f);
            lampLit = Surface("KotaLampuDepan", new Color(1f, .96f, .80f), .9f);
            bay = Surface("KotaTeluk", new Color(.13f, .40f, .55f), .9f, .1f);
            bay.SetTexture("_BaseMap", Texture("Water"));
            bay.SetTextureScale("_BaseMap", new Vector2(30f, 8f));
            lighthouseWhite = Surface("KotaMercusuar", new Color(.93f, .92f, .88f), .4f);
            sail = Surface("KotaLayar", new Color(.97f, .96f, .92f), .2f);
            wallTints = new[]
            {
                KotaVariant(ivory, "KotaTembokKrem", new Color(.86f, .80f, .66f)),
                KotaVariant(ivory, "KotaTembokPutih", new Color(.92f, .91f, .87f)),
                KotaVariant(ivory, "KotaTembokHijauMuda", new Color(.70f, .82f, .70f)),
                KotaVariant(ivory, "KotaTembokKuning", new Color(.92f, .82f, .52f)),
                KotaVariant(ivory, "KotaTembokBiruMuda", new Color(.68f, .79f, .86f))
            };
            awnings = new[] { terpal, terpalOrange, flagGreen, flagRed };
            EditorUtility.SetDirty(bay);
            EditorUtility.SetDirty(shirts[5]);
        }

        // A copy of a (possibly photo-textured) surface with another tint.
        private Material KotaVariant(Material source, string name, Color tint)
        {
            string path = CampaignCapitalMeshes.Folder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(source); AssetDatabase.CreateAsset(mat, path); }
            mat.CopyPropertiesFromMaterial(source);
            mat.name = name;
            mat.SetColor("_BaseColor", tint);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // --- Roads ---------------------------------------------------------------------

        private void RingRoads()
        {
            // The jalan raya (z -60) already exists; add both side roads and the north road.
            var sideAsphalt = KotaVariant(asphalt, "KotaAspalSamping", asphalt.GetColor("_BaseColor"));
            sideAsphalt.SetTextureScale("_BaseMap", new Vector2(28f, 1f));
            var northAsphalt = KotaVariant(asphalt, "KotaAspalUtara", asphalt.GetColor("_BaseColor"));
            northAsphalt.SetTextureScale("_BaseMap", new Vector2(18f, 1f));
            float sideLength = NorthRoadZ - RingRoad[0].z + 9f;
            float sideCentre = (NorthRoadZ + RingRoad[0].z) * .5f;
            foreach (int side in new[] { -1, 1 })
            {
                var road = Block("NusantaraGround jalan samping", new Vector3(side * SideRoadX, .04f, sideCentre),
                    new Vector3(sideLength, .02f, 9f), sideAsphalt);
                road.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                var walk = Block("NusantaraGround trotoar samping", new Vector3(side * (SideRoadX + 5.5f), .05f, sideCentre),
                    new Vector3(sideLength, .03f, 2f), concrete);
                walk.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
            Block("NusantaraGround jalan utara", new Vector3(0f, .065f, NorthRoadZ), new Vector3(SideRoadX * 2f + 9f, .02f, 9f), northAsphalt);
            Block("NusantaraGround trotoar utara", new Vector3(0f, .085f, NorthRoadZ + 5.5f), new Vector3(SideRoadX * 2f + 11f, .03f, 2f), concrete);
            foreach (var mat in new[] { sideAsphalt, northAsphalt })
                EditorUtility.SetDirty(mat);
        }

        // --- Buildings -----------------------------------------------------------------

        // Two-storey ruko along the far side of the jalan raya, facing the capital.
        private void RukoRow()
        {
            string[] names =
            {
                "TOKO KELONTONG", "FOTOKOPI 24 JAM", "APOTEK WARGA", "RUMAH MAKAN BU SRI",
                "BENGKEL MOTOR", "KONTER PULSA", "LAUNDRY KILAT", "TOKO BANGUNAN",
                "WARUNG NASI", "SALON RAKYAT", "TOKO EMAS", "SERVIS HP",
                "MIE AYAM", "TOKO BESI", "BIRO JASA*", "PANGKAS RAMBUT"
            };
            const float width = 7.6f, depth = 8f, height = 7f, z = -70.8f;
            for (int i = 0; i < names.Length; i++)
            {
                float x = -60f + i * 8f;
                // Yaw 180: the shop fronts (local -z) face the road and the capital (+z).
                int first = root.childCount;
                var ruko = Group("Kota ruko", new Vector3(x, 0f, z), 180f);
                Material wall = wallTints[i % wallTints.Length];
                LocalPart(ruko, "Kota ruko dinding", PrimitiveType.Cube, new Vector3(0f, height * .5f, 0f), new Vector3(width, height, depth), wall);
                LocalPart(ruko, "Kota ruko rolling door", PrimitiveType.Cube, new Vector3(0f, 1.45f, -depth * .5f - .02f), new Vector3(width - 1.2f, 2.9f, .05f), shutter);
                LocalPart(ruko, "Kota ruko kanopi", PrimitiveType.Cube, new Vector3(0f, 3.25f, -depth * .5f - .7f), new Vector3(width, .08f, 1.5f), awnings[i % awnings.Length])
                    .transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
                foreach (int w in new[] { -1, 1 })
                    LocalPart(ruko, "Kota ruko jendela", PrimitiveType.Cube, new Vector3(w * 1.8f, 5.1f, -depth * .5f - .02f), new Vector3(1.6f, 1.4f, .05f), windowGlass);
                LocalPart(ruko, "Kota ruko atap", PrimitiveType.Cube, new Vector3(0f, height + .45f, .3f), new Vector3(width + .2f, .14f, depth + .9f), genteng)
                    .transform.localRotation = Quaternion.Euler(-7f, 0f, 0f);
                LocalPart(ruko, "Kota ruko papan nama", PrimitiveType.Cube, new Vector3(0f, 3.95f, -depth * .5f - .05f), new Vector3(width - .6f, .75f, .06f), flagWhite);
                // 0.2.7 details: stone plinth, side pilasters, second-floor balcony rail,
                // an AC unit and a downpipe.
                LocalPart(ruko, "Kota ruko plint", PrimitiveType.Cube, new Vector3(0f, .2f, 0f), new Vector3(width + .08f, .4f, depth + .08f), stone);
                foreach (int s in new[] { -1, 1 })
                    LocalPart(ruko, "Kota ruko pilaster", PrimitiveType.Cube, new Vector3(s * (width * .5f - .12f), height * .5f, -depth * .5f - .06f), new Vector3(.3f, height, .14f), flagWhite);
                LocalPart(ruko, "Kota ruko balkon", PrimitiveType.Cube, new Vector3(0f, 4.55f, -depth * .5f - .35f), new Vector3(width - .8f, .08f, .7f), concrete);
                LocalPart(ruko, "Kota ruko pagar balkon", PrimitiveType.Cube, new Vector3(0f, 5.0f, -depth * .5f - .68f), new Vector3(width - .8f, .8f, .04f), steel);
                LocalPart(ruko, "Kota ruko AC", PrimitiveType.Cube, new Vector3(width * .3f, 6.3f, -depth * .5f - .25f), new Vector3(.8f, .55f, .45f), cartWhite);
                LocalPart(ruko, "Kota ruko talang", PrimitiveType.Cylinder, new Vector3(-width * .5f + .3f, height * .5f, -depth * .5f - .12f), new Vector3(.1f, height * .5f, .1f), concrete);
                Label(ruko, names[i], new Vector3(0f, 3.95f, -depth * .5f - .1f), .3f, new Color(.55f, .08f, .06f));
                CampaignModelSlots.Apply("Ruko", root, first, ruko.position, 180f);
            }
        }

        // Kampung houses with genteng pelana roofs along the outside of the side roads.
        private void KampungHouses()
        {
            int n = 0;
            foreach (int side in new[] { -1, 1 })
            {
                for (float z = -48f; z <= 62f; z += 11f, n++)
                {
                    // Front door towards the road (towards the capital).
                    int first = root.childCount;
                    var house = Group("Kota rumah", new Vector3(side * HouseRowX, 0f, z), side > 0 ? 90f : -90f);
                    Material wall = wallTints[(n * 3 + 1) % wallTints.Length];
                    LocalPart(house, "Kota rumah dinding", PrimitiveType.Cube, new Vector3(0f, 1.6f, 0f), new Vector3(6.5f, 3.2f, 5.5f), wall);
                    // Pelana roof: ridge along the house (local x), eaves at 3.2 m front and back.
                    foreach (int roofSide in new[] { -1, 1 })
                        LocalPart(house, "Kota rumah genteng", PrimitiveType.Cube, new Vector3(0f, 4.03f, roofSide * 1.55f), new Vector3(6.9f, .14f, 3.5f), genteng)
                            .transform.localRotation = Quaternion.Euler(roofSide * 28f, 0f, 0f);
                    LocalPart(house, "Kota rumah pintu", PrimitiveType.Cube, new Vector3(0f, 1.05f, -2.77f), new Vector3(.95f, 2.1f, .05f), timber);
                    foreach (int w in new[] { -1, 1 })
                        LocalPart(house, "Kota rumah jendela", PrimitiveType.Cube, new Vector3(w * 1.9f, 1.7f, -2.77f), new Vector3(1.1f, 1f, .05f), windowGlass);
                    LocalPart(house, "Kota rumah teras", PrimitiveType.Cube, new Vector3(0f, .1f, -3.6f), new Vector3(6.5f, .2f, 1.6f), concrete);
                    LocalPart(house, "Kota rumah pagar", PrimitiveType.Cube, new Vector3(0f, .5f, -4.55f), new Vector3(6.5f, 1f, .1f), flagWhite);
                    HouseDetails(house, wall);
                    CampaignModelSlots.Apply("RumahKampung", root, first, house.position, side > 0 ? 90f : -90f);
                }
            }
        }

        // 0.2.7: what makes a kampung house read as real: closed gables, a stone plinth,
        // white window and door frames with sills, a ridge cap, fascia boards under the eaves,
        // a small tiled porch roof on timber posts, and potted plants by the door.
        private void HouseDetails(Transform house, Material wall)
        {
            // Gables: a diamond (cube turned 45 degrees) squashed by its parent to the roof
            // pitch; the lower half hides inside the wall.
            const float halfDepth = 2.75f, eave = 3.2f, rise = 1.46f;
            foreach (int end in new[] { -1, 1 })
            {
                var gable = new GameObject("Kota rumah pelana").transform;
                gable.SetParent(house, false);
                gable.localPosition = new Vector3(end * 3.2f, eave, 0f);
                gable.localScale = new Vector3(1f, rise / halfDepth, 1f);
                LocalPart(gable, "Kota rumah dinding pelana", PrimitiveType.Cube, Vector3.zero,
                    new Vector3(.1f, halfDepth * 1.414f, halfDepth * 1.414f), wall).transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            }
            LocalPart(house, "Kota rumah plint", PrimitiveType.Cube, new Vector3(0f, .18f, 0f), new Vector3(6.6f, .36f, 5.6f), stone);
            LocalPart(house, "Kota rumah bubungan", PrimitiveType.Cube, new Vector3(0f, 4.86f, 0f), new Vector3(7f, .16f, .3f), genteng);
            foreach (int s in new[] { -1, 1 })
                LocalPart(house, "Kota rumah lisplang", PrimitiveType.Cube, new Vector3(0f, 3.13f, s * 3.12f), new Vector3(6.95f, .18f, .05f), flagWhite);
            foreach (int w in new[] { -1, 1 })
            {
                LocalPart(house, "Kota rumah kusen", PrimitiveType.Cube, new Vector3(w * 1.9f, 1.7f, -2.765f), new Vector3(1.26f, 1.16f, .04f), flagWhite);
                LocalPart(house, "Kota rumah ambang", PrimitiveType.Cube, new Vector3(w * 1.9f, 1.15f, -2.83f), new Vector3(1.32f, .07f, .16f), flagWhite);
            }
            LocalPart(house, "Kota rumah kusen pintu", PrimitiveType.Cube, new Vector3(0f, 1.12f, -2.765f), new Vector3(1.12f, 2.26f, .04f), flagWhite);
            // Porch roof on two posts.
            LocalPart(house, "Kota rumah atap teras", PrimitiveType.Cube, new Vector3(0f, 2.72f, -3.55f), new Vector3(6.6f, .1f, 1.9f), genteng)
                .transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            foreach (int s in new[] { -1, 1 })
            {
                LocalPart(house, "Kota rumah tiang teras", PrimitiveType.Cube, new Vector3(s * 3f, 1.4f, -4.3f), new Vector3(.12f, 2.6f, .12f), timber);
                LocalPart(house, "Kota rumah pot", PrimitiveType.Cylinder, new Vector3(s * 1f, .34f, -3.25f), new Vector3(.34f, .14f, .34f), genteng);
                LocalPart(house, "Kota rumah tanaman", PrimitiveType.Sphere, new Vector3(s * 1f, .62f, -3.25f), new Vector3(.5f, .5f, .5f), canopyLight);
            }
        }

        // Red-white flags at the boulevard and the plaza entrance, and vertical banners with
        // the fictional Konoha crest (gold star in a gold ring) on the boulevard.
        private void FlagsAndBanners()
        {
            foreach (int side in new[] { -1, 1 })
            {
                foreach (float z in new[] { -52f, -44f })
                    MerahPutih(new Vector3(side * 5.4f, 0f, z));
                foreach (float z in new[] { -33f, -26f, -14f })
                    CrestBanner(new Vector3(side * 5.3f, 0f, z));
            }
            MerahPutih(new Vector3(-9f, 0f, -10.8f));
            MerahPutih(new Vector3(9f, 0f, -10.8f));
        }

        private void MerahPutih(Vector3 p)
        {
            var flag = Group("KotaTall bendera merah putih", p, 0f);
            LocalPart(flag, "KotaTall tiang bendera", PrimitiveType.Cylinder, new Vector3(0f, 4.5f, 0f), new Vector3(.1f, 4.5f, .1f), chrome);
            LocalPart(flag, "KotaTall kain merah", PrimitiveType.Cube, new Vector3(.95f, 8.45f, 0f), new Vector3(1.8f, .6f, .03f), flagRed);
            LocalPart(flag, "KotaTall kain putih", PrimitiveType.Cube, new Vector3(.95f, 7.85f, 0f), new Vector3(1.8f, .6f, .03f), flagWhite);
            LocalPart(flag, "KotaTall puncak tiang", PrimitiveType.Sphere, new Vector3(0f, 9.05f, 0f), new Vector3(.18f, .18f, .18f), bronze);
        }

        private void CrestBanner(Vector3 p)
        {
            var banner = Group("KotaTall spanduk lambang Konoha", p, 0f);
            LocalPart(banner, "KotaTall tiang spanduk", PrimitiveType.Cylinder, new Vector3(0f, 3.4f, 0f), new Vector3(.08f, 3.4f, .08f), bronze);
            LocalPart(banner, "KotaTall palang spanduk", PrimitiveType.Cube, new Vector3(0f, 6.6f, 0f), new Vector3(1.3f, .06f, .06f), bronze);
            LocalPart(banner, "KotaTall kain spanduk merah", PrimitiveType.Cube, new Vector3(0f, 5.5f, 0f), new Vector3(1.1f, 2.1f, .03f), flagRed);
            LocalPart(banner, "KotaTall kain spanduk putih", PrimitiveType.Cube, new Vector3(0f, 4.05f, 0f), new Vector3(1.1f, .8f, .03f), flagWhite);
            // Fictional crest on both faces: gold ring + gold 4-point star (two rotated squares).
            foreach (int face in new[] { -1, 1 })
            {
                var ring = LocalPart(banner, "KotaTall lambang cincin", PrimitiveType.Cylinder, new Vector3(0f, 5.7f, face * .025f), new Vector3(.62f, .01f, .62f), bronze);
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                LocalPart(banner, "KotaTall lambang dasar", PrimitiveType.Cylinder, new Vector3(0f, 5.7f, face * .03f), new Vector3(.5f, .01f, .5f), flagRed)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                foreach (float twist in new[] { 45f, 0f })
                    LocalPart(banner, "KotaTall lambang bintang", PrimitiveType.Cube, new Vector3(0f, 5.7f, face * .04f), new Vector3(.26f, .26f, .01f), bronze)
                        .transform.localRotation = Quaternion.Euler(0f, 0f, twist);
            }
        }

        // A bay north of the capital with an arched bridge, lighthouses and sailboats.
        private void BayAndBridge()
        {
            var water = Block("Kota teluk", new Vector3(0f, -.07f, 165f), new Vector3(420f, .01f, 110f), bay);
            water.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            const float bridgeZ = 128f, deck = 4.2f;
            Block("Kota jembatan lengkung dek", new Vector3(0f, deck, bridgeZ), new Vector3(260f, .5f, 5f), ivory);
            foreach (int rail in new[] { -1, 1 })
                Block("Kota jembatan lengkung pagar", new Vector3(0f, deck + .6f, bridgeZ + rail * 2.4f), new Vector3(260f, .7f, .2f), ivory);
            for (int i = 0; i <= 20; i++)
            {
                float x = -125f + i * 12.5f;
                Block("Kota jembatan lengkung pilar", new Vector3(x, deck * .5f, bridgeZ), new Vector3(1.6f, deck, 4.6f), ivory);
                // Spandrel under the deck with a shallow arch shoulder at each pier.
                Block("Kota jembatan lengkung bahu", new Vector3(x, deck - .6f, bridgeZ), new Vector3(4.2f, .7f, 4.6f), stone);
            }
            Lighthouse(new Vector3(-70f, 0f, 140f));
            Lighthouse(new Vector3(95f, 0f, 118f));
            Sailboat(new Vector3(-30f, 0f, 150f), 20f);
            Sailboat(new Vector3(18f, 0f, 142f), -35f);
            Sailboat(new Vector3(60f, 0f, 160f), 60f);
            Sailboat(new Vector3(-110f, 0f, 120f), 110f);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer.gameObject.name.StartsWith("Kota jembatan") || renderer.gameObject.name.StartsWith("Kota mercusuar")
                    || renderer.gameObject.name.StartsWith("Kota perahu"))
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void Lighthouse(Vector3 p)
        {
            Cylinder("Kota mercusuar batu", p + Vector3.up * 1f, new Vector3(6f, 1f, 6f), stone);
            for (int i = 0; i < 4; i++)
                Cylinder("Kota mercusuar menara", p + Vector3.up * (4.5f + i * 5f), new Vector3(3.2f - i * .3f, 2.5f, 3.2f - i * .3f),
                    i % 2 == 0 ? lighthouseWhite : flagRed);
            Cylinder("Kota mercusuar lampu", p + Vector3.up * 23f, new Vector3(2f, 1f, 2f), lampLit);
            Ellipsoid("Kota mercusuar kubah", p + Vector3.up * 24.2f, new Vector3(2.3f, 1.4f, 2.3f), flagRed);
        }

        private void Sailboat(Vector3 p, float yaw)
        {
            var boat = Group("Kota perahu layar", p, yaw);
            LocalPart(boat, "Kota perahu lambung", PrimitiveType.Cube, new Vector3(0f, .35f, 0f), new Vector3(1.6f, .7f, 5f), flagWhite);
            LocalPart(boat, "Kota perahu tiang", PrimitiveType.Cylinder, new Vector3(0f, 3.2f, .3f), new Vector3(.12f, 2.6f, .12f), timber);
            LocalPart(boat, "Kota perahu layar", PrimitiveType.Cube, new Vector3(0f, 3.4f, -.7f), new Vector3(.04f, 4f, 2f), sail)
                .transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
        }

        // --- Traffic ------------------------------------------------------------------

        private void Traffic(CampaignCityLife life)
        {
            var vehicles = new List<CampaignCityLife.Vehicle>();
            float perimeter = CampaignCityLife.RingLength(RingRoad);
            // Counter-clockwise lane (inner), 5 cars at 8 m/s; clockwise lane (outer), 3 cars at 7 m/s.
            string[] ccw = { "sedan", "angkot", "pikap", "sedan", "angkot" };
            int[] ccwPaint = { 0, 4, 1, 2, 5 };
            for (int i = 0; i < ccw.Length; i++)
                vehicles.Add(Car(ccw[i], carPaints[ccwPaint[i]], 1f, 8f, 2.1f, perimeter * i / ccw.Length + 10f, false));
            string[] cw = { "sedan", "angkot", "sedan" };
            int[] cwPaint = { 1, 6, 3 };
            for (int i = 0; i < cw.Length; i++)
                vehicles.Add(Car(cw[i], carPaints[cwPaint[i]], -1f, 7f, 2.1f, perimeter * (i + .5f) / cw.Length, false));
            // Motor near the kerb in both directions (same speed per lane, so no overtaking through cars).
            for (int i = 0; i < 3; i++)
                vehicles.Add(Car("motor", carPaints[(i * 2 + 3) % carPaints.Length], i == 2 ? -1f : 1f, 9.5f, 3.4f,
                    perimeter * (i * .37f + .2f), true, i != 1));
            life.vehicles = vehicles.ToArray();
        }

        private CampaignCityLife.Vehicle Car(string kind, Material paint, float direction, float speed, float lane,
            float distance, bool motor, bool boncengTiga = false)
        {
            Vector3[] laneRing = CampaignCityLife.LaneRing(RingRoad, lane * direction);
            Vector3 point = CampaignCityLife.RingPoint(laneRing, distance, out Vector3 heading);
            heading *= direction;
            var body = new GameObject("Kota kendaraan " + kind).transform;
            body.SetParent(kotaRoot, false);
            body.position = point;
            body.rotation = Quaternion.LookRotation(heading, Vector3.up);

            switch (kind)
            {
                case "angkot":
                    KotaPart(body, "Kota angkot bodi", PrimitiveType.Cube, new Vector3(0f, 1.05f, 0f), new Vector3(1.7f, 1.5f, 4f), paint, true);
                    KotaPart(body, "Kota angkot kaca", PrimitiveType.Cube, new Vector3(0f, 1.45f, .1f), new Vector3(1.72f, .5f, 3.4f), windowGlass);
                    KotaPart(body, "Kota angkot list", PrimitiveType.Cube, new Vector3(0f, .75f, 0f), new Vector3(1.72f, .12f, 4.02f), flagWhite);
                    KotaPart(body, "Kota angkot pintu", PrimitiveType.Cube, new Vector3(-.86f, 1f, -1.2f), new Vector3(.02f, 1.2f, .8f), rubber);
                    break;
                case "pikap":
                    KotaPart(body, "Kota pikap kabin", PrimitiveType.Cube, new Vector3(0f, 1.15f, 1.2f), new Vector3(1.7f, 1.5f, 1.6f), paint, true);
                    KotaPart(body, "Kota pikap kaca", PrimitiveType.Cube, new Vector3(0f, 1.5f, 1.95f), new Vector3(1.5f, .55f, .1f), windowGlass);
                    KotaPart(body, "Kota pikap bak", PrimitiveType.Cube, new Vector3(0f, .8f, -.9f), new Vector3(1.7f, .7f, 2.5f), paint);
                    KotaPart(body, "Kota pikap muatan", PrimitiveType.Cube, new Vector3(0f, 1.3f, -.9f), new Vector3(1.4f, .5f, 2f), terpal);
                    break;
                case "motor":
                    KotaPart(body, "Kota motor bodi", PrimitiveType.Cube, new Vector3(0f, .55f, 0f), new Vector3(.34f, .34f, 1.15f), paint, true);
                    KotaPart(body, "Kota motor tameng", PrimitiveType.Cube, new Vector3(0f, .75f, .52f), new Vector3(.36f, .6f, .12f), paint);
                    KotaPart(body, "Kota motor setang", PrimitiveType.Cube, new Vector3(0f, 1.08f, .52f), new Vector3(.62f, .04f, .05f), chrome);
                    foreach (float z in new[] { -.48f, .52f })
                        KotaPart(body, "Kota motor roda", PrimitiveType.Cylinder, new Vector3(0f, .28f, z), new Vector3(.5f, .05f, .5f), rubber)
                            .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    // Rider with a helmet, arms on the handlebar.
                    KotaPart(body, "Kota pengendara badan", PrimitiveType.Cube, new Vector3(0f, 1.25f, -.15f), new Vector3(.4f, .55f, .24f), shirts[(int)(distance) % shirts.Length], true);
                    KotaPart(body, "Kota pengendara paha", PrimitiveType.Cube, new Vector3(0f, .92f, .05f), new Vector3(.38f, .16f, .5f), pants);
                    KotaPart(body, "Kota pengendara helm", PrimitiveType.Sphere, new Vector3(0f, 1.7f, -.12f), new Vector3(.3f, .3f, .32f), helmet);
                    foreach (int s in new[] { -1, 1 })
                        KotaPart(body, "Kota pengendara lengan", PrimitiveType.Cube, new Vector3(s * .24f, 1.3f, .22f), new Vector3(.1f, .1f, .55f), skins[0])
                            .transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
                    if (boncengTiga)
                    {
                        // 0.5.0 bonceng tiga: a child in front on the footboard, an ibu behind side-saddle.
                        KotaPart(body, "Kota bonceng anak", PrimitiveType.Capsule, new Vector3(0f, 1.05f, .32f), new Vector3(.24f, .2f, .18f), shirts[((int)distance + 3) % shirts.Length]);
                        KotaPart(body, "Kota bonceng anak kepala", PrimitiveType.Sphere, new Vector3(0f, 1.36f, .34f), new Vector3(.18f, .2f, .18f), skins[1 % skins.Length]);
                        KotaPart(body, "Kota bonceng ibu", PrimitiveType.Capsule, new Vector3(.08f, 1.22f, -.6f), new Vector3(.38f, .3f, .26f), hijabCloth);
                        KotaPart(body, "Kota bonceng ibu jilbab", PrimitiveType.Sphere, new Vector3(.08f, 1.68f, -.6f), new Vector3(.26f, .3f, .27f), hijabCloth);
                        KotaPart(body, "Kota bonceng ibu kaki", PrimitiveType.Capsule, new Vector3(.32f, .82f, -.55f), new Vector3(.14f, .3f, .14f), hijabCloth)
                            .transform.localRotation = Quaternion.Euler(0f, 0f, 80f);
                    }
                    break;
                default: // sedan
                    KotaPart(body, "Kota sedan bodi", PrimitiveType.Cube, new Vector3(0f, .6f, 0f), new Vector3(1.8f, .65f, 4.3f), paint, true);
                    KotaPart(body, "Kota sedan kabin", PrimitiveType.Cube, new Vector3(0f, 1.15f, -.2f), new Vector3(1.6f, .5f, 2.2f), windowGlass);
                    KotaPart(body, "Kota sedan atap", PrimitiveType.Cube, new Vector3(0f, 1.42f, -.25f), new Vector3(1.62f, .07f, 1.8f), paint);
                    break;
            }

            // Light positions just in front of each body's nose and tail.
            float front = kind == "sedan" ? 2.17f : 2.02f;
            float back = kind == "angkot" ? 2.02f : 2.17f;
            if (!motor)
            {
                foreach (int x in new[] { -1, 1 })
                {
                    foreach (int z in new[] { -1, 1 })
                        KotaPart(body, "Kota roda", PrimitiveType.Cylinder, new Vector3(x * .86f, .32f, z * 1.35f), new Vector3(.64f, .1f, .64f), rubber)
                            .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    KotaPart(body, "Kota lampu depan", PrimitiveType.Cube, new Vector3(x * .6f, .7f, front), new Vector3(.32f, .14f, .04f), lampLit);
                    KotaPart(body, "Kota lampu belakang", PrimitiveType.Cube, new Vector3(x * .65f, .75f, -back), new Vector3(.3f, .14f, .04f), flagRed);
                }
            }

            return new CampaignCityLife.Vehicle
            {
                body = body, speed = speed, direction = direction, distance = distance,
                laneOffset = lane, motor = motor, ring = true
            };
        }

        // --- Warga ----------------------------------------------------------------------

        private void Warga(CampaignCityLife life)
        {
            var walkers = new List<CampaignCityLife.Walker>();
            int seed = 0;
            void Walk(Vector3 from, Vector3 to, float progress)
            {
                var w = Person(from, 0f, seed++, false);
                w.from = from; w.to = to; w.progress = progress;
                w.speed = 1.05f + (seed % 4) * .12f;
                w.direction = seed % 2 == 0 ? 1f : -1f;
                w.root.position = Vector3.Lerp(from, to, progress);
                w.root.rotation = Quaternion.LookRotation((to - from).normalized * w.direction, Vector3.up);
                walkers.Add(w);
            }
            void Stand(Vector3 at, float yaw)
            {
                var w = Person(at, yaw, seed++, true);
                w.from = at; w.to = at; w.idle = true;
                walkers.Add(w);
            }

            // Pavements of the jalan raya (near and far side).
            Walk(new Vector3(-60f, 0f, -54.2f), new Vector3(-7f, 0f, -54.2f), .3f);
            Walk(new Vector3(7f, 0f, -54.2f), new Vector3(60f, 0f, -54.2f), .7f);
            Walk(new Vector3(-55f, 0f, FarTrotoarZ), new Vector3(55f, 0f, FarTrotoarZ), .15f);
            Walk(new Vector3(-55f, 0f, FarTrotoarZ), new Vector3(55f, 0f, FarTrotoarZ), .55f);
            Walk(new Vector3(-55f, 0f, FarTrotoarZ), new Vector3(55f, 0f, FarTrotoarZ), .85f);
            foreach (int side in new[] { -1, 1 })
            {
                // Grass paths beside the boulevard, clear of palms (x ±6.8) and parked motors.
                Walk(new Vector3(side * 8.6f, 0f, -52f), new Vector3(side * 8.6f, 0f, -24f), side > 0 ? .2f : .6f);
                Walk(new Vector3(side * 8.6f, 0f, -52f), new Vector3(side * 8.6f, 0f, -24f), side > 0 ? .75f : .1f);
                // Garden promenades and crosswalks of the Plaza Aspirasi.
                Walk(new Vector3(side * 20f, 0f, -11f), new Vector3(side * 20f, 0f, 17f), .25f);
                Walk(new Vector3(side * 20f, 0f, -11f), new Vector3(side * 20f, 0f, 17f), .8f);
                Walk(new Vector3(side * 9f, 0f, -12.5f), new Vector3(side * 21f, 0f, -12.5f), .5f);
                // Pavements of the side roads, in front of the kampung houses.
                Walk(new Vector3(side * (SideRoadX + 5.5f), 0f, -50f), new Vector3(side * (SideRoadX + 5.5f), 0f, 62f), .4f);
            }
            Walk(new Vector3(-40f, 0f, NorthRoadZ + 5.5f), new Vector3(40f, 0f, NorthRoadZ + 5.5f), .5f);

            // 0.6.2: children walking along with the grown-ups (a kampung is never only adults).
            void Kid(Vector3 from, Vector3 to, float progress)
            {
                Walk(from, to, progress);
                CampaignCityLife.Walker kid = walkers[walkers.Count - 1];
                kid.root.localScale = new Vector3(.62f, .62f, .62f);
                kid.root.name = "Kota anak jalan";
                kid.speed *= 1.25f;
            }
            Kid(new Vector3(-8f, 0f, -52f), new Vector3(-8f, 0f, -24f), .64f);
            Kid(new Vector3(8f, 0f, -52f), new Vector3(8f, 0f, -24f), .24f);
            Kid(new Vector3(-20.7f, 0f, -11f), new Vector3(-20.7f, 0f, 17f), .3f);
            Kid(new Vector3(9f, 0f, -13.2f), new Vector3(21f, 0f, -13.2f), .45f);

            // Vendors and their customers, onlookers at the pavilions and at the baliho.
            Stand(new Vector3(-12.6f, .1f, -40.5f), 90f);  // penjual kopi behind the warung counter (on its floor)
            Stand(new Vector3(-9.9f, 0f, -41f), -90f);     // pembeli kopi, in front of the bench
            Stand(new Vector3(-9.95f, 0f, -40f), -100f);
            Stand(new Vector3(11.8f, 0f, -39.3f), 180f);   // tukang bakso at the cart handles
            Stand(new Vector3(10.4f, 0f, -41.6f), 80f);    // pembeli bakso
            Stand(new Vector3(10.5f, 0f, -40.6f), 100f);
            foreach (int side in new[] { -1, 1 })
            {
                Stand(new Vector3(side * 16.4f, 0f, -11.3f), side * -60f);
                Stand(new Vector3(side * 17.2f, 0f, -10.6f), side * -80f);
            }
            Stand(new Vector3(-14.6f, 0f, -44.8f), 20f);
            Stand(new Vector3(-13.8f, 0f, -45.4f), 10f);

            // 0.5.0 Jalan Nyaleg: everyday Indonesian scenes. Seeds pick the look:
            // seed % 5 == 1 wears a jilbab, 2 a peci, 4 a topi.
            void Crowd(Vector3 centre, Vector3[] offsets, int[] seeds, Material shirt, bool filming)
            {
                for (int i = 0; i < offsets.Length; i++)
                {
                    Vector3 at = centre + offsets[i];
                    Vector3 look = centre - at;
                    float yaw = look.sqrMagnitude > .01f ? Quaternion.LookRotation(look).eulerAngles.y : 0f;
                    var w = Person(at, yaw, seeds[i % seeds.Length], true, shirt, filming);
                    w.from = at; w.to = at; w.idle = true;
                    walkers.Add(w);
                }
            }
            Vector3[] circle4 = { new Vector3(-.75f, 0f, .55f), new Vector3(.7f, 0f, .6f), new Vector3(.75f, 0f, -.6f), new Vector3(-.7f, 0f, -.65f) };
            Vector3[] circle3 = { new Vector3(-.8f, 0f, .4f), new Vector3(.8f, 0f, .45f), new Vector3(0f, 0f, -.85f) };

            // Blusukan 1: bapak-bapak at the pos ronda (left of the boulevard).
            PosRonda(new Vector3(-11.3f, 0f, -26f));
            Crowd(new Vector3(-8.6f, 0f, -26f), circle3, new[] { 2, 7, 3 }, null, false);
            // Blusukan 2: ibu-ibu ngerumpi around the tukang sayur (right).
            GerobakSayur(new Vector3(10.9f, 0f, -26.3f));
            Crowd(new Vector3(8.6f, 0f, -26f), circle4, new[] { 1, 6, 11, 16 }, null, false);
            Crowd(new Vector3(11.9f, 0f, -26.3f), new[] { Vector3.zero }, new[] { 4 }, null, false);
            // Blusukan 3: pangkalan ojol beside the garden (green jackets, parked motors).
            foreach (float z in new[] { -19.9f, -21f, -22.1f })
                Motor(new Vector3(15f, 0f, z), 90f, flagGreen);
            Crowd(new Vector3(13f, 0f, -21f), circle3, new[] { 4, 9, 14 }, flagGreen, false);

            // Rumah duka in the kampung (west side road): bendera kuning, tenda, kursi plastik, pelayat.
            RumahDuka(new Vector3(-46.3f, 0f, 7f));
            Crowd(new Vector3(-46.3f, 0f, 7f), new[] { new Vector3(-.9f, 0f, -1.6f), new Vector3(.6f, 0f, -1.7f),
                new Vector3(-1.1f, 0f, 1.5f), new Vector3(.8f, 0f, 1.6f), new Vector3(1.5f, 0f, 0f), new Vector3(-1.6f, 0f, .1f) },
                new[] { 2, 1, 7, 6, 12, 3 }, pants, false);

            // Motor jatuh on the verge of the jalan raya: warga crowd round and record it.
            MotorJatuh(new Vector3(30f, 0f, -53.1f));
            Crowd(new Vector3(30f, 0f, -53.1f), new[] { new Vector3(-1.7f, 0f, .2f), new Vector3(-1f, 0f, 1.3f),
                new Vector3(.4f, 0f, 1.6f), new Vector3(1.6f, 0f, .7f), new Vector3(1.8f, 0f, -.6f) },
                new[] { 0, 3, 5, 8, 10 }, null, true);
            life.walkers = walkers.ToArray();
        }

        // Primitive Indonesian passer-by (about 1.7 m) with pivoted limbs for the walk cycle.
        // Seed picks skin, clothes and headwear: hair, jilbab, peci or topi.
        private Material wargaEyeWhite, wargaLips;

        // --- 0.5.0 everyday scenes -------------------------------------------------------

        // Pos ronda: a small open hut with a tiled roof, a bench and a kentongan.
        private void PosRonda(Vector3 p)
        {
            var hut = new GameObject("Kota pos ronda").transform;
            hut.SetParent(kotaRoot, false);
            hut.position = p;
            foreach (int x in new[] { -1, 1 })
                foreach (int z in new[] { -1, 1 })
                    KotaPart(hut, "Kota pos ronda tiang", PrimitiveType.Cylinder, new Vector3(x * 1.05f, 1.1f, z * 1.05f), new Vector3(.12f, 1.1f, .12f), timber);
            foreach (int side in new[] { -1, 1 })
                KotaPart(hut, "Kota pos ronda atap", PrimitiveType.Cube, new Vector3(0f, 2.45f, side * .62f), new Vector3(2.6f, .08f, 1.45f), genteng)
                    .transform.localRotation = Quaternion.Euler(side * 24f, 0f, 0f);
            KotaPart(hut, "Kota pos ronda lantai", PrimitiveType.Cube, new Vector3(0f, .45f, 0f), new Vector3(2.1f, .1f, 2.1f), timber, true);
            KotaPart(hut, "Kota pos ronda kaki", PrimitiveType.Cube, new Vector3(0f, .2f, 0f), new Vector3(1.9f, .4f, 1.9f), concrete);
            KotaPart(hut, "Kota pos ronda kentongan", PrimitiveType.Cylinder, new Vector3(1.05f, 1.55f, -1.15f), new Vector3(.16f, .32f, .16f), bark);
            KotaPart(hut, "Kota pos ronda papan", PrimitiveType.Cube, new Vector3(0f, 2.1f, -1.12f), new Vector3(1.4f, .32f, .04f), flagWhite);
            Label(hut, "POS RONDA RT 03", new Vector3(0f, 2.1f, -1.15f), .16f, new Color(.15f, .15f, .15f));
        }

        // Gerobak sayur: the tukang sayur's cart with vegetables under a small canopy.
        private void GerobakSayur(Vector3 p)
        {
            var cart = new GameObject("Kota gerobak sayur").transform;
            cart.SetParent(kotaRoot, false);
            cart.position = p;
            cart.rotation = Quaternion.Euler(0f, 90f, 0f);
            KotaPart(cart, "Kota gerobak sayur bak", PrimitiveType.Cube, new Vector3(0f, .8f, 0f), new Vector3(1.5f, .5f, .9f), timber, true);
            foreach (int z in new[] { -1, 1 })
                KotaPart(cart, "Kota gerobak sayur roda", PrimitiveType.Cylinder, new Vector3(.45f, .32f, z * .5f), new Vector3(.6f, .04f, .6f), rubber)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            KotaPart(cart, "Kota gerobak sayur tiang", PrimitiveType.Cylinder, new Vector3(0f, 1.5f, 0f), new Vector3(.05f, .7f, .05f), steel);
            KotaPart(cart, "Kota gerobak sayur payung", PrimitiveType.Cube, new Vector3(0f, 2.2f, 0f), new Vector3(1.7f, .05f, 1.2f), terpalOrange);
            Material[] produce = { canopyLight, flagRed, canopy, terpalOrange, flagYellow };
            for (int i = 0; i < 10; i++)
                KotaPart(cart, "Kota gerobak sayur sayuran", PrimitiveType.Sphere,
                    new Vector3(-.55f + (i % 5) * .27f, 1.12f, i < 5 ? -.2f : .2f), new Vector3(.22f, .16f, .22f), produce[i % produce.Length]);
        }

        // Rumah duka: blue tent, rows of plastic chairs, bendera kuning and a karangan bunga board.
        private void RumahDuka(Vector3 p)
        {
            var duka = new GameObject("Kota rumah duka").transform;
            duka.SetParent(kotaRoot, false);
            duka.position = p;
            foreach (int x in new[] { -1, 1 })
                foreach (int z in new[] { -1, 1 })
                    KotaPart(duka, "Kota tenda tiang", PrimitiveType.Cylinder, new Vector3(x * 1.7f, 1.25f, z * 2.3f), new Vector3(.08f, 1.25f, .08f), steel);
            KotaPart(duka, "Kota tenda atap", PrimitiveType.Cube, new Vector3(0f, 2.55f, 0f), new Vector3(3.7f, .06f, 4.9f), terpal);
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 3; i++)
                {
                    Vector3 seat = new Vector3(-1f + i * 1f, 0f, -1f + row * 1f);
                    Material plastic = (row + i) % 2 == 0 ? flagRed : flagGreen;
                    KotaPart(duka, "Kota kursi plastik", PrimitiveType.Cube, seat + new Vector3(0f, .42f, 0f), new Vector3(.42f, .05f, .42f), plastic);
                    KotaPart(duka, "Kota kursi plastik sandaran", PrimitiveType.Cube, seat + new Vector3(0f, .66f, .2f), new Vector3(.42f, .45f, .04f), plastic);
                    KotaPart(duka, "Kota kursi plastik kaki", PrimitiveType.Cube, seat + new Vector3(0f, .2f, 0f), new Vector3(.36f, .4f, .36f), plastic);
                }
            KotaPart(duka, "Kota bendera kuning tiang", PrimitiveType.Cylinder, new Vector3(1.9f, 1.1f, -2.9f), new Vector3(.05f, 1.1f, .05f), bamboo);
            KotaPart(duka, "Kota bendera kuning", PrimitiveType.Cube, new Vector3(2.15f, 1.9f, -2.9f), new Vector3(.5f, .34f, .02f), flagYellow);
            KotaPart(duka, "Kota karangan bunga", PrimitiveType.Cube, new Vector3(-1.9f, 1.05f, -2.9f), new Vector3(1.5f, 1.1f, .06f), flagWhite);
            KotaPart(duka, "Kota karangan bunga kaki", PrimitiveType.Cube, new Vector3(-1.9f, .25f, -2.9f), new Vector3(.08f, .5f, .3f), bamboo);
            Label(duka, "TURUT BERDUKA CITA\nKELUARGA BESAR RT 03", new Vector3(-1.9f, 1.1f, -2.94f), .12f, new Color(.15f, .15f, .15f));
        }

        // A motor lying on the verge after a fall (the rider has already been taken home).
        private void MotorJatuh(Vector3 p)
        {
            var motor = new GameObject("Kota motor jatuh").transform;
            motor.SetParent(kotaRoot, false);
            motor.position = p;
            motor.rotation = Quaternion.Euler(0f, 30f, 78f);
            KotaPart(motor, "Kota motor jatuh bodi", PrimitiveType.Cube, new Vector3(0f, .55f, 0f), new Vector3(.34f, .34f, 1.15f), flagBlue);
            KotaPart(motor, "Kota motor jatuh tameng", PrimitiveType.Cube, new Vector3(0f, .75f, .52f), new Vector3(.36f, .6f, .12f), flagBlue);
            foreach (float z in new[] { -.48f, .52f })
                KotaPart(motor, "Kota motor jatuh roda", PrimitiveType.Cylinder, new Vector3(0f, .28f, z), new Vector3(.5f, .05f, .5f), rubber)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            KotaPart(kotaRoot, "Kota helm tergeletak", PrimitiveType.Sphere, p + new Vector3(.9f, .15f, -.6f), new Vector3(.3f, .28f, .32f), helmet);
            KotaPart(kotaRoot, "Kota sandal tertinggal", PrimitiveType.Cube, p + new Vector3(-.7f, .03f, .5f), new Vector3(.12f, .04f, .26f), rubber);
        }

        private CampaignCityLife.Walker Person(Vector3 at, float yaw, int seed, bool idle,
            Material shirtOverride = null, bool filming = false)
        {
            if (wargaEyeWhite == null)
            {
                wargaEyeWhite = CampaignRigBuilder.Lit("RigMataPutih", new Color(.96f, .95f, .92f), .5f);
                wargaLips = CampaignRigBuilder.Lit("RigBibir", new Color(.42f, .20f, .17f), .3f);
            }
            var person = new GameObject(idle ? "Kota warga diam" : "Kota warga jalan").transform;
            person.SetParent(kotaRoot, false);
            person.position = at;
            person.rotation = Quaternion.Euler(0f, yaw, 0f);
            Material skin = skins[seed % skins.Length];
            Material shirt = shirtOverride != null ? shirtOverride : shirts[(seed * 3 + 1) % shirts.Length];
            int headwear = seed % 5; // 0,3 hair; 1 jilbab; 2 peci; 4 topi
            bool gamis = headwear == 1;
            // 0.6.2 variety: scarf colour, some grey heads, slightly different heights and builds.
            Material hijabCloth = hijabs != null && hijabs.Length > 0 ? hijabs[(seed / 5 + seed) % hijabs.Length] : this.hijabCloth;
            Material hair = seed % 7 == 3 && hairGrey != null ? hairGrey : this.hair;
            float tall = .93f + (seed * 37 % 13) * .011f;
            float wide = .94f + (seed * 53 % 11) * .012f;
            person.localScale = new Vector3(wide, tall, wide);

            Transform Pivot(string name, Vector3 local)
            {
                var pivot = new GameObject(name).transform;
                pivot.SetParent(person, false);
                pivot.localPosition = local;
                return pivot;
            }

            var legLeft = Pivot("Kota warga pinggul kiri", new Vector3(-.1f, .9f, 0f));
            var legRight = Pivot("Kota warga pinggul kanan", new Vector3(.1f, .9f, 0f));
            // 0.3.4: rounded limbs and torso (capsules) instead of boxes, like the hero bodies.
            foreach (var leg in new[] { legLeft, legRight })
            {
                KotaPart(leg, "Kota warga kaki", PrimitiveType.Capsule, new Vector3(0f, -.43f, 0f), new Vector3(.15f, .45f, .15f), gamis ? hijabCloth : pants);
                KotaPart(leg, "Kota warga sandal", PrimitiveType.Sphere, new Vector3(0f, -.87f, .05f), new Vector3(.13f, .07f, .26f), rubber);
            }
            KotaPart(person, "Kota warga badan", PrimitiveType.Capsule,
                gamis ? new Vector3(0f, 1.08f, 0f) : new Vector3(0f, 1.2f, 0f),
                gamis ? new Vector3(.44f, .5f, .28f) : new Vector3(.42f, .33f, .26f), gamis ? hijabCloth : shirt, true);
            if (!gamis)
                KotaPart(person, "Kota warga pinggang", PrimitiveType.Sphere, new Vector3(0f, .93f, 0f), new Vector3(.38f, .2f, .25f), pants);

            var armLeft = Pivot("Kota warga bahu kiri", new Vector3(-.27f, 1.44f, 0f));
            var armRight = Pivot("Kota warga bahu kanan", new Vector3(.27f, 1.44f, 0f));
            foreach (var arm in new[] { armLeft, armRight })
            {
                KotaPart(arm, "Kota warga lengan", PrimitiveType.Capsule, new Vector3(0f, -.16f, 0f), new Vector3(.12f, .18f, .12f), gamis ? hijabCloth : shirt);
                KotaPart(arm, "Kota warga lengan bawah", PrimitiveType.Capsule, new Vector3(0f, -.45f, 0f), new Vector3(.1f, .17f, .1f), gamis ? hijabCloth : skin);
                KotaPart(arm, "Kota warga tangan", PrimitiveType.Sphere, new Vector3(0f, -.64f, .01f), new Vector3(.085f, .1f, .09f), skin);
            }

            KotaPart(person, "Kota warga leher", PrimitiveType.Capsule, new Vector3(0f, 1.53f, -.01f), new Vector3(.09f, .06f, .09f), skin);
            KotaPart(person, "Kota warga kepala", PrimitiveType.Sphere, new Vector3(0f, 1.66f, -.02f), new Vector3(.22f, .26f, .23f), skin);
            // 0.3.4: a face (owner: "warganya kayak nggak punya muka"): blinking cartoon eyes,
            // eyebrows, nose, mouth, ears. Head front is at z ~0.095.
            var eyes = new List<Transform>();
            foreach (int s in new[] { -1, 1 })
            {
                var eye = KotaPart(person, "Kota warga mata", PrimitiveType.Sphere, new Vector3(s * .048f, 1.685f, .088f), new Vector3(.05f, .04f, .028f), wargaEyeWhite).transform;
                KotaPart(eye, "Kota warga pupil", PrimitiveType.Sphere, new Vector3(0f, -.05f, .42f), new Vector3(.52f, .66f, .5f), hair);
                eyes.Add(eye);
                KotaPart(person, "Kota warga alis", PrimitiveType.Capsule, new Vector3(s * .052f, 1.72f, .086f), new Vector3(.014f, .032f, .014f), hair)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 90f - s * 8f);
                KotaPart(person, "Kota warga telinga", PrimitiveType.Sphere, new Vector3(s * .112f, 1.66f, -.02f), new Vector3(.04f, .07f, .05f), skin);
            }
            KotaPart(person, "Kota warga hidung", PrimitiveType.Capsule, new Vector3(0f, 1.65f, .1f), new Vector3(.04f, .033f, .045f), skin);
            KotaPart(person, "Kota warga mulut", PrimitiveType.Sphere, new Vector3(0f, 1.59f, .093f), new Vector3(.06f, .015f, .02f), wargaLips);
            person.gameObject.AddComponent<CampaignBlink>().eyes = eyes.ToArray();
            switch (headwear)
            {
                case 1:
                    // 0.3.4: the jilbab frames the face (it used to cover it completely).
                    KotaPart(person, "Kota warga jilbab", PrimitiveType.Sphere, new Vector3(0f, 1.69f, -.06f), new Vector3(.27f, .31f, .26f), hijabCloth);
                    KotaPart(person, "Kota warga jilbab kerudung", PrimitiveType.Sphere, new Vector3(0f, 1.49f, -.02f), new Vector3(.38f, .22f, .3f), hijabCloth);
                    break;
                case 2:
                    KotaPart(person, "Kota warga rambut", PrimitiveType.Sphere, new Vector3(0f, 1.73f, -.035f), new Vector3(.235f, .19f, .24f), hair);
                    KotaPart(person, "Kota warga peci", PrimitiveType.Cylinder, new Vector3(0f, 1.8f, .01f), new Vector3(.22f, .05f, .22f), hair);
                    break;
                case 4:
                    KotaPart(person, "Kota warga rambut", PrimitiveType.Sphere, new Vector3(0f, 1.73f, -.035f), new Vector3(.235f, .19f, .24f), hair);
                    KotaPart(person, "Kota warga topi", PrimitiveType.Cylinder, new Vector3(0f, 1.79f, -.05f), new Vector3(.3f, .02f, .34f), shirts[(seed + 2) % shirts.Length]);
                    break;
                default:
                    KotaPart(person, "Kota warga rambut", PrimitiveType.Sphere, new Vector3(0f, 1.73f, -.035f), new Vector3(.235f, .19f, .24f), hair);
                    break;
            }

            // 0.5.0 kepo: a phone in the right hand, shown only while recording.
            var phone = KotaPart(armRight, "Kota warga HP", PrimitiveType.Cube, new Vector3(0f, -.66f, .07f),
                new Vector3(.07f, .13f, .016f), rubber);
            phone.SetActive(false);

            // Rest pose for onlookers: arms slightly forward.
            if (idle)
            {
                armLeft.localRotation = Quaternion.Euler(-8f, 0f, 0f);
                armRight.localRotation = Quaternion.Euler(-20f, 0f, 0f);
            }

            return new CampaignCityLife.Walker
            {
                root = person, legLeft = legLeft, legRight = legRight, armLeft = armLeft, armRight = armRight,
                phone = phone, filming = filming
            };
        }

        // --- Fountains ----------------------------------------------------------------

        // Four small jets per plaza canal, in the open water between the bridge and the coping.
        private void Fountains(CampaignCityLife life)
        {
            var jets = new List<Transform>();
            var nozzles = new List<Vector3>();
            foreach (int side in new[] { -1, 1 })
            {
                var canal = new Vector3(side * 10f, 0f, 5.8f);
                foreach (int x in new[] { -1, 1 })
                {
                    foreach (int z in new[] { -1, 1 })
                    {
                        // (1.4, 1.65) is the existing bronze fountain finial: that jet sprays from it.
                        Vector3 at = canal + new Vector3(x * 1.4f, 0f, z * 1.65f);
                        if (x < 0 || z < 0)
                            KotaPart(kotaRoot, "Kota air mancur dasar", PrimitiveType.Cylinder, at + Vector3.up * .06f, new Vector3(.36f, .06f, .36f), ivory);
                        float top = .03f + CampaignCityLife.NozzleHeight;
                        var jet = KotaPart(kotaRoot, "Kota air mancur semburan", PrimitiveType.Cylinder, at + Vector3.up * top * .5f, new Vector3(.09f, top * .5f, .09f), water);
                        jets.Add(jet.transform);
                        nozzles.Add(at + Vector3.up * top);
                    }
                }
            }
            var droplets = new List<Transform>();
            const int perFountain = 6;
            for (int i = 0; i < nozzles.Count * perFountain; i++)
            {
                var drop = KotaPart(kotaRoot, "Kota air mancur percikan", PrimitiveType.Sphere, nozzles[i / perFountain], Vector3.one * .09f, water);
                drop.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                droplets.Add(drop.transform);
            }
            life.jets = jets.ToArray();
            life.nozzles = nozzles.ToArray();
            life.droplets = droplets.ToArray();
            life.dropletReach = .45f;
            life.dropletHeight = .55f;
        }

        // Moving part: no collider, not static; only the main body casts a shadow.
        private GameObject KotaPart(Transform parent, string name, PrimitiveType type, Vector3 local, Vector3 scale,
            Material mat, bool castsShadow = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = castsShadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
    }
}
