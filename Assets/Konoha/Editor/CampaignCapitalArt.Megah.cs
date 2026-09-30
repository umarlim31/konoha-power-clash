using Konoha.Campaign;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Konoha.Editor
{
    // 0.2.5 "Nusantara Megah": the owner's concept images applied to the tested route
    // (nothing on the walking route moves or gains collision):
    //  - ornamental navy-and-gold tiles on the boulevard and the Istana forecourt,
    //  - red-white banners with the FICTIONAL Konoha crest along the Gerbang Dalam wall and on
    //    the Istana colonnade (never the state emblem, see CLAUDE.md),
    //  - towers with pavilion roofs around the Istana and at the ends of the inner wall,
    //  - two cascades falling from the Istana terrace into reflecting pools,
    //  - flamboyan trees and bougainvillea along the boulevard,
    //  - clouds, a coastal town on the bay, and islands.
    // Static decor is named "Megah ..." (tall pieces "MegahTall ..." for the camera occluder).
    internal sealed partial class CampaignCapitalArt
    {
        private Material motif, flamboyan, cloud, cascade;

        private void BuildNusantaraMegah()
        {
            motif = Surface("MegahUbinMotif", Color.white, .35f);
            motif.SetTexture("_BaseMap", ColorTexture("MegahUbinMotif", MotifTile));
            flamboyan = Surface("MegahFlamboyan", new Color(.72f, .14f, .08f), .15f);
            cloud = Surface("MegahAwan", new Color(.97f, .97f, .98f), 0f);
            cascade = Surface("MegahAirTerjun", new Color(.62f, .84f, .92f), .9f);
            cascade.SetTexture("_BaseMap", Texture("Water"));
            foreach (var mat in new[] { motif, flamboyan, cloud, cascade })
                EditorUtility.SetDirty(mat);

            MotifPaving();
            WallBanners();
            IstanaBanners();
            Towers();
            Cascades();
            BoulevardGreenery();
            Clouds();
            CoastalTown();
            Islands();
            CandiBentar();
        }

        // --- Ornamental tiles -------------------------------------------------------------

        private void MotifPaving()
        {
            // 1.5 m tiles (0.2.6, was 3 m): boulevard 6 x 43 m, forecourt 18 x 5 m (top face: x -> u, z -> v).
            SetMotif("Gerbang Rakyat boulevard", new Vector2(4f, 43f / 1.5f));
            SetMotif("Civic forecourt inlay", new Vector2(12f, 5f / 1.5f));
        }

        private void SetMotif(string blockName, Vector2 tiles)
        {
            Transform inlay = root.Find(blockName);
            if (inlay == null)
                return;
            // One material per tiling so both surfaces keep square 3 m tiles.
            string path = CampaignCapitalMeshes.Folder + "/MegahUbinMotif_" + blockName.Replace(' ', '_') + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(motif); AssetDatabase.CreateAsset(mat, path); }
            else mat.CopyPropertiesFromMaterial(motif);
            mat.name = System.IO.Path.GetFileNameWithoutExtension(path);
            mat.SetTextureScale("_BaseMap", tiles);
            EditorUtility.SetDirty(mat);
            inlay.GetComponent<Renderer>().sharedMaterial = mat;
        }

        // Navy tile with a gold eight-point star, gold grid lines and cream kawung circles
        // that meet at the tile corners.
        private static Color MotifTile(int x, int y)
        {
            // 0.2.6: muted like weathered glazed tiles (0.2.5 was too loud).
            Color navy = new Color(.22f, .27f, .36f), gold = new Color(.70f, .58f, .36f), cream = new Color(.78f, .74f, .64f);
            float n = Mathf.PerlinNoise(x * .08f, y * .08f) * .05f;
            if (x < 5 || y < 5 || x > 250 || y > 250)
                return gold * (.9f + n);
            float cx = x - 128f, cy = y - 128f;
            float r = Mathf.Sqrt(cx * cx + cy * cy);
            // Kawung: quarter circles in every corner.
            for (int k = 0; k < 4; k++)
            {
                float qx = (k % 2 == 0 ? 0f : 256f) - x, qy = (k < 2 ? 0f : 256f) - y;
                float d = Mathf.Sqrt(qx * qx + qy * qy);
                if (d < 44f)
                    return d > 38f ? gold : cream * (.95f + n);
            }
            // Eight-point star: a square and a 45 degree square, radius 70.
            bool square = Mathf.Abs(cx) < 50f && Mathf.Abs(cy) < 50f;
            bool diamond = Mathf.Abs(cx) + Mathf.Abs(cy) < 70f;
            if (r < 14f) return gold;
            if (r < 30f) return navy * (1f + n);
            if (square || diamond)
                return (Mathf.Abs(cx) > 44f || Mathf.Abs(cy) > 44f || Mathf.Abs(cx) + Mathf.Abs(cy) > 64f) ? cream : gold * (.95f + n);
            return new Color(navy.r + n, navy.g + n, navy.b + n);
        }

        // --- Banners ------------------------------------------------------------------------

        // Hanging on the south face of the Gerbang Dalam wall (z 16.5), both wings.
        private void WallBanners()
        {
            const float face = InnerGateZ - .5f - .04f;
            foreach (int side in new[] { -1, 1 })
                for (int i = 0; i < 6; i++)
                    HangingBanner(new Vector3(side * (7.5f + i * 5f), 3.45f, face), 1.1f, 1.5f, .7f, "MegahTall spanduk dinding");
        }

        // In front of the Istana colonnade (entablature front at z 53.6, y 10.2).
        private void IstanaBanners()
        {
            foreach (float x in new[] { -6.6f, -2.5f, 2.5f, 6.6f })
                HangingBanner(new Vector3(x, 10f, 53.45f), 1.2f, 3f, 1.2f, "MegahTall spanduk istana");
        }

        // A red-over-white cloth hanging from `top`, facing -z, with the gold Konoha crest.
        private void HangingBanner(Vector3 top, float width, float redLength, float whiteLength, string name)
        {
            var banner = Group(name, top, 0f);
            LocalPart(banner, name + " batang", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(width + .2f, .08f, .08f), bronze);
            LocalPart(banner, name + " merah", PrimitiveType.Cube, new Vector3(0f, -redLength * .5f, 0f), new Vector3(width, redLength, .03f), flagRed);
            LocalPart(banner, name + " putih", PrimitiveType.Cube, new Vector3(0f, -redLength - whiteLength * .5f, 0f), new Vector3(width, whiteLength, .03f), flagWhite);
            float crestY = -Mathf.Min(redLength * .45f, .9f);
            float size = width * .55f;
            LocalPart(banner, name + " lambang cincin", PrimitiveType.Cylinder, new Vector3(0f, crestY, -.025f), new Vector3(size, .01f, size), bronze)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            LocalPart(banner, name + " lambang dasar", PrimitiveType.Cylinder, new Vector3(0f, crestY, -.03f), new Vector3(size * .8f, .01f, size * .8f), flagRed)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            foreach (float twist in new[] { 45f, 0f })
                LocalPart(banner, name + " lambang bintang", PrimitiveType.Cube, new Vector3(0f, crestY, -.04f), new Vector3(size * .42f, size * .42f, .01f), bronze)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, twist);
        }

        // --- Towers -------------------------------------------------------------------------

        private void Towers()
        {
            foreach (int side in new[] { -1, 1 })
            {
                // Flush against the terrace side (x ±11): no narrow gap a hero could get stuck in.
                Tower(new Vector3(side * 12.2f, 0f, 43.5f), 15f);
                Tower(new Vector3(side * 12.2f, 0f, 62f), 13f);
                Tower(new Vector3(side * 34.2f, 0f, InnerGateZ), 10f);
            }
        }

        // Square ivory shaft with bronze bands and a pavilion (four posts + tiered genteng roof).
        private void Tower(Vector3 p, float height)
        {
            var shaft = Block("MegahTall menara", p + Vector3.up * (height * .5f), new Vector3(2.4f, height, 2.4f), ivory, true);
            shaft.layer = 2; // Solid, but the orbit camera looks through it (like the walls).
            // 0.2.7: stone base and a crowning cornice so the shaft stops reading as a chimney.
            Block("MegahTall menara plint", p + Vector3.up * .6f, new Vector3(2.9f, 1.2f, 2.9f), stone);
            Block("MegahTall menara kornis", p + Vector3.up * (height - .15f), new Vector3(2.8f, .3f, 2.8f), ivory);
            foreach (float y in new[] { height * .35f, height * .7f })
                Block("MegahTall menara pita", p + Vector3.up * y, new Vector3(2.5f, .22f, 2.5f), bronze);
            foreach (int f in new[] { -1, 1 })
            {
                Block("MegahTall menara jendela", p + new Vector3(0f, height * .82f, f * 1.21f), new Vector3(.55f, 1.2f, .04f), dark);
                Block("MegahTall menara jendela", p + new Vector3(f * 1.21f, height * .82f, 0f), new Vector3(.04f, 1.2f, .55f), dark);
            }
            Block("MegahTall menara lantai", p + Vector3.up * (height + .15f), new Vector3(3.2f, .3f, 3.2f), ivory);
            foreach (int x in new[] { -1, 1 })
                foreach (int z in new[] { -1, 1 })
                    Block("MegahTall menara tiang", p + new Vector3(x * 1.2f, height + 1.3f, z * 1.2f), new Vector3(.24f, 2.1f, .24f), ivory);
            Roof(p + Vector3.up * (height + 2.45f), new Vector3(2.3f, 1.9f, 2.3f), red);
            // Small red-white flag on top.
            Block("MegahTall menara tiang bendera", p + Vector3.up * (height + 5.2f), new Vector3(.07f, 3.2f, .07f), chrome);
            Block("MegahTall menara bendera merah", p + new Vector3(.55f, height + 6.45f, 0f), new Vector3(1f, .35f, .03f), flagRed);
            Block("MegahTall menara bendera putih", p + new Vector3(.55f, height + 6.1f, 0f), new Vector3(1f, .35f, .03f), flagWhite);
        }

        // --- Cascades -----------------------------------------------------------------------

        // Water falls from the Istana terrace (front face z 42) into two reflecting pools.
        private void Cascades()
        {
            var flowObject = new GameObject("Megah air terjun istana");
            flowObject.transform.SetParent(root);
            var flow = flowObject.AddComponent<CampaignWaterFlow>();
            var falls = new System.Collections.Generic.List<Renderer>();
            var foam = new System.Collections.Generic.List<Transform>();
            Material white = Surface("MegahBuih", new Color(.95f, .97f, .98f), .6f);
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * 7.5f;
                Block("Megah air terjun corong", new Vector3(x, TerraceHeight - .15f, 41.75f), new Vector3(2.7f, .25f, .55f), bronze);
                var fall = KotaPart(flowObject.transform, "Megah air terjun tirai", PrimitiveType.Cube,
                    new Vector3(x, TerraceHeight * .5f, 41.86f), new Vector3(2.2f, TerraceHeight - .2f, .06f), cascade);
                falls.Add(fall.GetComponent<Renderer>());
                var pool = Block("Megah kolam air terjun", new Vector3(x, .03f, 40.75f), new Vector3(3.4f, .02f, 2.2f), water);
                pool.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                Block("Megah kolam tepi", new Vector3(x, .07f, 39.6f), new Vector3(3.6f, .14f, .2f), ivory);
                foreach (int s in new[] { -1, 1 })
                    Block("Megah kolam tepi", new Vector3(x + s * 1.75f, .07f, 40.75f), new Vector3(.2f, .14f, 2.4f), ivory);
                for (int i = -1; i <= 1; i++)
                    foam.Add(KotaPart(flowObject.transform, "Megah air terjun buih", PrimitiveType.Sphere,
                        new Vector3(x + i * .7f, .15f, 41.55f), new Vector3(.8f, .35f, .55f), white).transform);
            }
            flow.falls = falls.ToArray();
            flow.foam = foam.ToArray();
        }

        // --- Greenery -----------------------------------------------------------------------

        private void BoulevardGreenery()
        {
            foreach (int side in new[] { -1, 1 })
            {
                foreach (float z in new[] { -49f, -31f, -24f })
                    Flamboyan(new Vector3(side * 10.5f, 0f, z), z * 13f + side * 40f);
                foreach (float z in new[] { -46f, -35.5f, -28.5f })
                    Bugenvil(new Vector3(side * 5.3f, 0f, z));
            }
        }

        // Flame tree (flamboyan): the red-flowering shade tree of Indonesian streets.
        // 0.2.6: a wide, flat umbrella of many small clumps, mostly green leaves with patches
        // of deep red flowers (0.2.5 used a few huge orange blobs that looked like toys).
        private void Flamboyan(Vector3 p, float twist)
        {
            Cylinder("MegahTall batang flamboyan", p + Vector3.up * 1.6f, new Vector3(.3f, 1.6f, .3f), bark);
            for (int i = 0; i < 4; i++)
            {
                var direction = Quaternion.Euler(0f, twist + i * 90f, 0f) * Vector3.forward;
                Stick("MegahTall dahan flamboyan", p + Vector3.up * 2.8f, p + Vector3.up * 3.9f + direction * 1.7f, .12f, bark);
            }
            var random = new System.Random(Mathf.RoundToInt(p.x * 31f + p.z * 7f));
            for (int i = 0; i < 11; i++)
            {
                float angle = (twist + i * 32.7f) * Mathf.Deg2Rad;
                float radius = i == 0 ? 0f : .9f + (float)random.NextDouble() * 1.4f;
                var at = p + new Vector3(Mathf.Sin(angle) * radius, 4.1f + (float)random.NextDouble() * .5f - radius * .12f, Mathf.Cos(angle) * radius);
                bool blossom = i % 3 == 1;
                Ellipsoid("MegahTall tajuk flamboyan", at, blossom ? new Vector3(1f, .45f, 1f) : new Vector3(1.5f, .6f, 1.5f),
                    blossom ? flamboyan : (i % 2 == 0 ? canopy : canopyLight));
            }
        }

        private void Bugenvil(Vector3 p)
        {
            Ellipsoid("Megah bugenvil daun", p + new Vector3(0f, .35f, 0f), new Vector3(1.1f, .7f, 1f), leaf);
            Ellipsoid("Megah bugenvil bunga", p + new Vector3(.15f, .62f, .1f), new Vector3(.8f, .4f, .75f), flower);
            Ellipsoid("Megah bugenvil bunga", p + new Vector3(-.25f, .5f, -.2f), new Vector3(.5f, .3f, .5f), flower);
        }

        // --- Candi bentar ------------------------------------------------------------------

        // 0.2.6: a split gate of red brick (candi bentar, as in Majapahit Trowulan and Bali)
        // welcoming the hero at the mouth of the boulevard, on the pavement by the road.
        private void CandiBentar()
        {
            Material brick = Surface("MegahBataMerah", Color.white, .12f);
            brick.SetTexture("_BaseMap", ColorTexture("MegahBataMerah", (x, y) =>
            {
                int row = y / 32;
                bool mortar = y % 32 < 3 || (x + (row % 2) * 32) % 64 < 3;
                float n = Mathf.PerlinNoise(x * .09f + row, y * .09f) * .08f;
                float shade = ((x + (row % 2) * 32) / 64 + row) % 3 * .03f;
                return mortar ? new Color(.72f, .66f, .56f) : new Color(.60f + n - shade, .27f + n * .5f, .18f);
            }));
            EditorUtility.SetDirty(brick);
            const float z = -54.3f, inner = 3.4f;
            foreach (int side in new[] { -1, 1 })
            {
                // The inner face of every tier lines up at x = ±3.4 (the split); tiers step back outward.
                float[] widths = { 2.4f, 2f, 1.7f, 1.4f, 1.1f, .8f };
                float y = 0f;
                for (int i = 0; i < widths.Length; i++)
                {
                    float h = i == 0 ? .8f : .9f;
                    float w = widths[i];
                    Block("MegahTall candi bentar", new Vector3(side * (inner + w * .5f), y + h * .5f, z), new Vector3(w, h, w * .9f), brick);
                    if (i > 0)
                        Block("MegahTall candi bentar pelipit", new Vector3(side * (inner + w * .5f), y + .06f, z), new Vector3(w + .12f, .12f, w * .9f + .12f), stone);
                    y += h;
                }
                MeshObject("MegahTall candi bentar kemuncak", stupa, new Vector3(side * (inner + .4f), y, z), new Vector3(.32f, .45f, .32f), stone);
            }
        }

        // --- Horizon ------------------------------------------------------------------------

        private void Clouds()
        {
            var random = new System.Random(2505);
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2f / 16f + (float)random.NextDouble() * .3f;
                float radius = 200f + (float)random.NextDouble() * 60f;
                var center = new Vector3(Mathf.Sin(angle) * radius, 60f + (float)random.NextDouble() * 25f, 40f + Mathf.Cos(angle) * radius);
                for (int k = 0; k < 3; k++)
                {
                    var puff = Ellipsoid("Megah awan", center + new Vector3((k - 1) * 12f, (k == 1 ? 4f : 0f), (float)random.NextDouble() * 6f),
                        new Vector3(22f, 9f, 14f) * (k == 1 ? 1.2f : .9f), cloud);
                    puff.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        // A row of cream houses with genteng roofs and a few towers on the south shore of the bay.
        private void CoastalTown()
        {
            var random = new System.Random(1945);
            Material[] walls = wallTints != null && wallTints.Length > 0 ? wallTints : new[] { ivory };
            for (float x = -140f; x <= 140f; x += 11f)
            {
                float height = 5f + (float)random.NextDouble() * 7f;
                var p = new Vector3(x + (float)random.NextDouble() * 3f, 0f, 106f + (float)random.NextDouble() * 2f);
                var body = Block("Megah kota pesisir", p + Vector3.up * (height * .5f), new Vector3(8f, height, 6f), walls[random.Next(walls.Length)]);
                body.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                foreach (int s in new[] { -1, 1 })
                {
                    var slab = Block("Megah kota pesisir atap", p + new Vector3(0f, height + .9f, s * 1.6f), new Vector3(8.6f, .2f, 3.8f), genteng);
                    slab.transform.rotation = Quaternion.Euler(s * 28f, 0f, 0f);
                    slab.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
                if (random.NextDouble() < .25)
                {
                    var tower = Block("Megah kota pesisir menara", p + new Vector3(3f, height + 3f, 0f), new Vector3(2f, 6f, 2f), ivory);
                    tower.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    Roof(p + new Vector3(3f, height + 6.3f, 0f), new Vector3(1.8f, 1.6f, 1.8f), red);
                }
            }
        }

        private void Islands()
        {
            // Between the bridge (z 128) and the gunung (bases from z ~165).
            Vector3[] spots = { new Vector3(-150f, 0f, 150f), new Vector3(-12f, 0f, 150f), new Vector3(60f, 0f, 145f),
                new Vector3(150f, 0f, 140f), new Vector3(-80f, 0f, 160f) };
            for (int i = 0; i < spots.Length; i++)
            {
                Vector3 p = spots[i];
                var land = Ellipsoid("Megah pulau", p + Vector3.down * 1f, new Vector3(20f, 5f, 13f), canopy);
                var rock = Ellipsoid("Megah pulau batu", p + new Vector3(4f, .5f, 2f), new Vector3(7f, 6f, 6f), stone);
                var beach = Ellipsoid("Megah pulau pantai", p + Vector3.down * 1.6f, new Vector3(24f, 3f, 16f), Surface("MegahPasir", new Color(.86f, .79f, .62f), .1f));
                foreach (var part in new[] { land, rock, beach })
                    part.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                Palm(p + new Vector3(-4f, 1f, -1f), 6f + i % 2, i * 40f);
                Palm(p + new Vector3(2f, 1f, -3f), 5.5f, i * 70f);
            }
        }
    }
}
