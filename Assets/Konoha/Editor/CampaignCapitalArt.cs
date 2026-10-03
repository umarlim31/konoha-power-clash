using System;
using System.Collections.Generic;
using UnityEditor;
using Konoha.Campaign;
using Konoha.Character;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Konoha.Editor
{
    // Original civic architecture for the solo campaign. No PvP scene or material is edited.
    // 0.0.9.1 route layout (Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md §13), +Z = toward the seat:
    //   Gerbang Rakyat (spawn z -44) -> boulevard -> Plaza Aspirasi hub (origin) with
    //   Majelis Daun (x -28) and Biro Prosedur (x +28) wings -> Gerbang Dalam wall (z 17,
    //   locked gate) -> Garda Takhta parade ground (z 28) -> ramp -> Istana Takhta terrace
    //   (5 m high) with the Kursi at z 46.5, visible from the spawn over the monument.
    internal sealed partial class CampaignCapitalArt
    {
        internal const float TerraceHeight = 5f;
        internal const float InnerGateZ = 17f;
        internal const float WingCorridorZ = -5f;
        internal static readonly Vector3 ThronePosition = new Vector3(0f, TerraceHeight, 46.5f);
        internal static readonly Vector3 SpawnPoint = new Vector3(0f, 0.1f, -44f);
        internal static readonly Vector2 BoundaryCenter = new Vector2(0f, 4f);
        internal static readonly Vector2 BoundaryRadii = new Vector2(34f, 58f);

        private Transform root;
        private Material landscapeGrass;
        private Material stone, ivory, bronze, dark, red, green, leaf, paleLeaf, water, paving, flower;
        private Material majelisCloth, majelisTrim, biroCloth, biroTrim, gardaCloth;
        private Mesh column, dome, roof, arch, feather, frond, pedestal, rim, stupa, lotus;
        private Material shutterWood;
        internal Transform Plaza, Majelis, Biro, Garda, Throne;
        internal GameObject InnerGateClosed, InnerGateOpen;
        // 0.0.9.3 Biro Prosedur loket hall.
        internal GameObject BiroDoorClosed, BiroDoorOpen;
        // 0.1.0 Garda LOCKDOWN ring (inactive until the director closes it).
        internal GameObject GardaLockdown;
        internal Renderer[] LoketZones;
        internal TextMesh[] LoketLabels;
        // Loket centres on the ground; CampaignStage.loketPoints carries the same values.
        internal static readonly Vector3[] LoketCenters =
        {
            new Vector3(22.5f, 0f, -9f), new Vector3(27.5f, 0f, -10.5f), new Vector3(32f, 0f, -8.5f)
        };
        // Kepala Biro office in front of the hall: walls x 25..31, south wall z -6.6, door 2.2 m.
        internal const float OfficeWestX = 25f, OfficeEastX = 31f, OfficeSouthZ = -6.6f, OfficeNorthZ = -1.8f;
        internal const float OfficeDoorHalfWidth = 1.1f;
        internal Material Gold => bronze;
        internal Material Ivory => ivory;
        internal Material Red => red;

        internal static CampaignCapitalArt Build()
        {
            if (!AssetDatabase.IsValidFolder(CampaignCapitalMeshes.Folder))
                AssetDatabase.CreateFolder(SpikeProject.Generated, "Capital");
            var art = new CampaignCapitalArt();
            CampaignModelSlots.Begin();
            art.ClearSpikeScenery();
            art.CreatePalette();
            art.ApplyRealSurfaces();
            art.CreateMeshes();
            art.ConfigureLighting();
            art.root = new GameObject("CapitalEnvironment").transform;
            art.GroundAndStreets();
            art.BuildGerbangRakyat();
            art.BuildPlazaAspirasi();
            art.Majelis = art.BuildMajelisDaun();
            art.Biro = art.BuildBiroProsedur();
            art.BuildGerbangDalam();
            art.Garda = art.BuildPlazaTakhtaGarda();
            art.Throne = art.BuildIstanaTakhta();
            // 0.2.0 Suasana Nusantara: street life, utility poles, umbul-umbul, baliho, warung,
            // sawah and gunung (CampaignCapitalArt.Nusantara.cs).
            art.BuildNusantara();
            return art;
        }

        // Tall decor that may stand between the orbit camera and the hero: roofs, gate
        // lintels/walls/pylons, palm crowns and trunks, the Biro office and big gate signs.
        // CampaignOccluders hides them while they cover the hero (0.0.9.4).
        private static readonly string[] OccluderNames =
        {
            "Tiered Nusantara roof", "Roof bronze ridge", "Upturned roof finial",
            "Gerbang Rakyat", "Gerbang Dalam", "Kantor Kepala Biro", "Pintu Kepala Biro",
            "Palm curved frond", "Palm crown", "Palm tapered trunk",
            "Sign GERBANG", "Sign MENUJU", "Sign KANTOR", "Banner", "NusantaraTall", "KotaTall", "MegahTall"
        };

        internal Renderer[] CameraOccluders()
        {
            var result = new List<Renderer>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                string name = renderer.gameObject.name;
                if (name.IndexOf("boulevard", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue; // Floor inlay, never above the hero.
                foreach (string prefix in OccluderNames)
                {
                    if (name.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        result.Add(renderer);
                        break;
                    }
                }
            }
            return result.ToArray();
        }

        // A hero may cross the east-west wing corridor freely; solid decor stays out of it.
        private static bool InWingCorridor(float x, float z, float halfDepth) =>
            Mathf.Abs(x) > 4f && Mathf.Abs(z - WingCorridorZ) < halfDepth + 2.5f;

        // --- Sectors ------------------------------------------------------------------------

        private void BuildGerbangRakyat()
        {
            const float z = -37f;
            foreach (int side in new[] { -1, 1 })
            {
                // Pylons frame a 7.5 m opening on the boulevard axis; nothing blocks the lane.
                Block("Gerbang Rakyat pylon", new Vector3(side * 4.4f, 2.9f, z), new Vector3(1.3f, 5.8f, 1.3f), stone, true);
                Block("Gerbang Rakyat pylon cap", new Vector3(side * 4.4f, 5.86f, z), new Vector3(1.6f, .14f, 1.6f), bronze);
                Block("Gerbang Rakyat relief", new Vector3(side * 4.4f, 3.2f, z - .67f), new Vector3(.5f, 3.4f, .04f), bronze);
                Banner(new Vector3(side * 6.2f, 0, z - 1.4f), 3.8f);
                for (int i = 0; i < 4; i++)
                {
                    Palm(new Vector3(side * 6.8f, 0, -50 + i * 9), 5.2f + (i % 2) * .5f, i * 47 + side * 20);
                    Lamp(new Vector3(side * 4.1f, 0, -47.5f + i * 9));
                }
            }
            Block("Gerbang Rakyat lintel", new Vector3(0, 6.2f, z), new Vector3(10.4f, .75f, 1.5f), ivory);
            Roof(new Vector3(0, 6.55f, z), new Vector3(5.6f, 1.9f, 1.3f), red);
            Sign("GERBANG RAKYAT", new Vector3(0, 7.9f, z - .8f), 0f, 1.25f);
            // Hung under the lintel: the old spot (z -49, 1.9 m) sat between the spawn camera and the hero.
            Sign("MENUJU ISTANA TAKHTA", new Vector3(0, 5.15f, z - .8f), 0f, .55f);
        }

        private void BuildPlazaAspirasi()
        {
            Monument(new Vector3(0, 0, 1.75f));
            Plaza = Objective("Plaza Aspirasi", new Vector3(0, 0, -6.5f), 1.15f);
            foreach (int side in new[] { -1, 1 })
            {
                Canal(new Vector3(side * 10, 0, 5.8f));
                Statue(new Vector3(side * 18.6f, 0, 9.8f));
                for (int i = 0; i < 4; i++)
                {
                    Palm(new Vector3(side * 18.2f, 0, -9 + i * 7), 4.7f + i * 0.35f, i * 31);
                    float planterZ = -8 + i * 5.1f;
                    if (!InWingCorridor(side * 14, planterZ, 1.15f))
                        Planter(new Vector3(side * 14, 0, planterZ), new Vector3(1.4f, 0.55f, 2.3f));
                }
                for (int i = 0; i < 3; i++)
                {
                    Palm(new Vector3(side * (5 + i * 5), 0, 15.5f), 5.1f, i * 71);
                    Lamp(new Vector3(side * 3.9f, 0, -8.9f + i * 6));
                }
                Banner(new Vector3(side * 3.5f, 0, -10.5f), 3.1f);
                Banner(new Vector3(side * 15, 0, 9.8f), 3.8f);
                // City blocks behind the parade ground frame the north.
                for (int i = 0; i < 4; i++)
                    Skyline(new Vector3(side * (13 + i * 6.5f), 0, 28 + (i % 2) * 7), 5 + i * 2);
            }
            Sign("PLAZA ASPIRASI", new Vector3(0, 3.2f, -10.9f), 0f, .9f);
        }

        private Transform BuildMajelisDaun()
        {
            var sector = Institution("Majelis Daun", new Vector3(-28, 0, WingCorridorZ), false, majelisCloth, majelisTrim);
            Sign("MARKAS KOALISI", new Vector3(-28, 6.2f, -2.4f), 0f, 1f);
            Sign("< REKOMENDASI KOALISI", new Vector3(-6.5f, 2.6f, -3.6f), 0f, .7f);
            return sector;
        }

        private Transform BuildBiroProsedur()
        {
            var sector = Institution("Biro Prosedur", new Vector3(28, 0, WingCorridorZ), true, biroCloth, biroTrim);
            Sign("KANTOR KELURAHAN", new Vector3(28, 6.6f, -2.4f), 0f, 1f);
            Sign("BERKAS KELURAHAN >", new Vector3(6.5f, 2.6f, -3.6f), 0f, .7f);
            BuildBiroOffice();
            BuildLokets();
            return sector;
        }

        // §8.3 LOCKDOWN: a ring of invisible 5.5 m walls (tall enough to also close the ramp,
        // which is 2.5 m high where the ring crosses it) with visible posts and red-gold
        // barrier tape. Walls are on Ignore Raycast so the orbit camera looks through.
        private void BuildGardaLockdown(Vector3 post)
        {
            GardaLockdown = new GameObject("Garda Lockdown ring");
            GardaLockdown.transform.SetParent(root);
            const int segments = 28;
            float radius = CampaignTuning.Garda.LockdownRadius;
            float chord = 2f * Mathf.PI * radius / segments + .2f;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                Vector3 at = new Vector3(post.x, 0f, post.z) + direction * radius;
                var yaw = Quaternion.LookRotation(direction);
                var wall = Block("Garda lockdown wall", at + Vector3.up * 2.75f, new Vector3(chord, 5.5f, .4f), red, true);
                wall.transform.rotation = yaw;
                wall.layer = 2;
                wall.GetComponent<Renderer>().enabled = false;
                wall.transform.SetParent(GardaLockdown.transform, true);
                var postMesh = Block("Garda lockdown post", at + Vector3.up * 1.2f, new Vector3(.16f, 2.4f, .16f), bronze);
                postMesh.transform.SetParent(GardaLockdown.transform, true);
                foreach (float height in new[] { 1.1f, 2.0f })
                {
                    var tape = Block("Garda lockdown tape", at + Vector3.up * height, new Vector3(chord, .12f, .05f),
                        height < 1.5f ? red : bronze);
                    tape.transform.rotation = yaw;
                    tape.transform.SetParent(GardaLockdown.transform, true);
                }
            }
            GardaLockdown.SetActive(false);
        }

        // Walled office of the Kepala Biro (§8.2 "pintu berlapis"). Walls are 2.4 m (jump
        // height 1.25 m cannot clear them) and on Ignore Raycast so the orbit camera ignores them.
        private void BuildBiroOffice()
        {
            float centerX = (OfficeWestX + OfficeEastX) * .5f;
            float depth = OfficeNorthZ - OfficeSouthZ;
            foreach (float x in new[] { OfficeWestX, OfficeEastX })
            {
                var side = Block("Kantor Kepala Biro wall", new Vector3(x, 1.2f, OfficeSouthZ + depth * .5f),
                    new Vector3(.25f, 2.4f, depth), biroCloth, true);
                side.layer = 2;
                Block("Kantor Kepala Biro coping", new Vector3(x, 2.46f, OfficeSouthZ + depth * .5f),
                    new Vector3(.36f, .12f, depth), biroTrim);
            }
            float doorWest = centerX - OfficeDoorHalfWidth, doorEast = centerX + OfficeDoorHalfWidth;
            foreach (var span in new[] { new Vector2(OfficeWestX, doorWest), new Vector2(doorEast, OfficeEastX) })
            {
                float width = span.y - span.x;
                var front = Block("Kantor Kepala Biro wall", new Vector3(span.x + width * .5f, 1.2f, OfficeSouthZ),
                    new Vector3(width + .25f, 2.4f, .25f), biroCloth, true);
                front.layer = 2;
                Block("Kantor Kepala Biro coping", new Vector3(span.x + width * .5f, 2.46f, OfficeSouthZ),
                    new Vector3(width + .36f, .12f, .36f), biroTrim);
            }
            Sign("KANTOR PAK LURAH", new Vector3(centerX, 3.0f, OfficeSouthZ - .2f), 0f, .55f);

            // The door: solid until every loket is stamped, then two leaves fold outward (decor only).
            BiroDoorClosed = new GameObject("Pintu Kepala Biro - locked");
            BiroDoorClosed.transform.SetParent(root);
            BiroDoorOpen = new GameObject("Pintu Kepala Biro - open");
            BiroDoorOpen.transform.SetParent(root);
            var door = Block("Pintu Kepala Biro", new Vector3(centerX, 1.2f, OfficeSouthZ),
                new Vector3(OfficeDoorHalfWidth * 2f, 2.4f, .2f), biroTrim, true);
            door.layer = 2;
            door.transform.SetParent(BiroDoorClosed.transform, true);
            var stamp = Block("Pintu Kepala Biro stamp seal", new Vector3(centerX, 1.4f, OfficeSouthZ - .12f),
                new Vector3(.7f, .7f, .04f), red);
            stamp.transform.SetParent(BiroDoorClosed.transform, true);
            foreach (int side in new[] { -1, 1 })
            {
                var leaf = Block("Pintu Kepala Biro (open)",
                    new Vector3(centerX + side * (OfficeDoorHalfWidth + .1f), 1.2f, OfficeSouthZ - .55f),
                    new Vector3(.12f, 2.4f, 1.1f), biroTrim);
                leaf.transform.SetParent(BiroDoorOpen.transform, true);
            }
            BiroDoorOpen.SetActive(false);
        }

        // Three loket zones (radius 2 m) with a counter behind each. Zone discs and labels are
        // dynamic (colour/text change at runtime), so they are not batching-static.
        private void BuildLokets()
        {
            var zoneMaterial = Surface("BiroLoketZone", new Color(0.22f, 0.34f, 0.52f), 0.1f);
            LoketZones = new Renderer[LoketCenters.Length];
            LoketLabels = new TextMesh[LoketCenters.Length];
            for (int i = 0; i < LoketCenters.Length; i++)
            {
                Vector3 c = LoketCenters[i];
                var zone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Configure(zone, "Loket " + (i + 1) + " zone", c + Vector3.up * .05f,
                    new Vector3(CampaignTuning.Biro.LoketRadius * 2f, .01f, CampaignTuning.Biro.LoketRadius * 2f), zoneMaterial);
                UnityEngine.Object.DestroyImmediate(zone.GetComponent<Collider>());
                GameObjectUtility.SetStaticEditorFlags(zone, 0);
                zone.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                LoketZones[i] = zone.GetComponent<Renderer>();

                Vector3 counter = c + new Vector3(0f, 0f, -2.4f);
                Block("Loket counter", counter + Vector3.up * .55f, new Vector3(1.8f, 1.1f, .6f), biroCloth, true);
                Block("Loket counter top", counter + Vector3.up * 1.14f, new Vector3(1.95f, .08f, .72f), biroTrim);
                Block("Loket window", counter + new Vector3(0f, 1.7f, .05f), new Vector3(1.4f, 1f, .06f), dark);
                Block("Loket window frame", counter + new Vector3(0f, 2.24f, .05f), new Vector3(1.6f, .1f, .1f), biroTrim);
                LoketLabels[i] = Sign(CampaignDirector.LoketName(i), counter + new Vector3(0f, 2.8f, 0f), 0f, .5f);
            }
        }

        // The wall spans the whole campus width (the oval boundary is narrower here), so the
        // north is reachable only through the gate. Wall pieces use the Ignore Raycast layer
        // so the orbit camera does not collapse onto a hero standing beside it.
        private void BuildGerbangDalam()
        {
            const float z = InnerGateZ;
            foreach (int side in new[] { -1, 1 })
            {
                float inner = 4.25f, outer = 35f;
                var wall = Block("Gerbang Dalam wall", new Vector3(side * (inner + outer) * .5f, 1.75f, z),
                    new Vector3(outer - inner, 3.5f, 1f), stone, true);
                wall.layer = 2;
                Block("Gerbang Dalam coping", new Vector3(side * (inner + outer) * .5f, 3.56f, z),
                    new Vector3(outer - inner, .14f, 1.2f), ivory);
                Block("Gerbang Dalam bronze band", new Vector3(side * (inner + outer) * .5f, 2.7f, z - .52f),
                    new Vector3(outer - inner, .12f, .04f), bronze);
                var pylon = Block("Gerbang Dalam pylon", new Vector3(side * 3.6f, 3f, z), new Vector3(1.3f, 6f, 1.4f), ivory, true);
                pylon.layer = 2;
                Block("Gerbang Dalam pylon cap", new Vector3(side * 3.6f, 6.06f, z), new Vector3(1.6f, .14f, 1.7f), bronze);
                Banner(new Vector3(side * 5.6f, 0, z - 1.3f), 3.9f, gardaCloth, ivory);
            }
            Block("Gerbang Dalam lintel", new Vector3(0, 6.35f, z), new Vector3(8.6f, .7f, 1.5f), ivory);
            Roof(new Vector3(0, 6.7f, z), new Vector3(4.7f, 1.8f, 1.3f), red);
            Sign("GERBANG DALAM", new Vector3(0, 8f, z - .8f), 0f, 1.1f);

            // Closed leaves block the 5.9 m opening until the director opens the gate.
            InnerGateClosed = new GameObject("Gerbang Dalam - locked");
            InnerGateClosed.transform.SetParent(root);
            InnerGateOpen = new GameObject("Gerbang Dalam - open");
            InnerGateOpen.transform.SetParent(root);
            foreach (int side in new[] { -1, 1 })
            {
                var leaf = Block("Gerbang Dalam door", new Vector3(side * 1.475f, 2.3f, z), new Vector3(2.95f, 4.6f, .3f), red, true);
                leaf.transform.SetParent(InnerGateClosed.transform, true);
                var stud = Block("Gerbang Dalam door medallion", new Vector3(side * 1.475f, 2.6f, z - .17f), new Vector3(.9f, .9f, .04f), bronze);
                stud.transform.SetParent(InnerGateClosed.transform, true);
                var open = Block("Gerbang Dalam door (open)", new Vector3(side * 3.0f, 2.3f, z + 1.65f), new Vector3(.3f, 4.6f, 2.95f), red);
                open.transform.SetParent(InnerGateOpen.transform, true);
            }
            InnerGateOpen.SetActive(false);
        }

        private Transform BuildPlazaTakhtaGarda()
        {
            var post = new Vector3(0, 0, 28);
            Block("Garda parade ground", new Vector3(0, .008f, 27), new Vector3(22, .025f, 14), ivory);
            foreach (int side in new[] { -1, 1 })
            {
                Block("Parade bronze border", new Vector3(side * 11f, .05f, 27), new Vector3(.08f, .014f, 14), bronze);
                // Low cover (0.9 m): readable from the camera and jumpable.
                Block("Garda low cover", new Vector3(side * 5.5f, .45f, 24.5f), new Vector3(3.2f, .9f, .8f), stone, true);
                Block("Garda low cover coping", new Vector3(side * 5.5f, .93f, 24.5f), new Vector3(3.4f, .1f, 1f), ivory);
                Banner(new Vector3(side * 9f, 0, 21.5f), 3.6f, gardaCloth, bronze);
                Banner(new Vector3(side * 9f, 0, 33f), 3.6f, gardaCloth, bronze);
                Statue(new Vector3(side * 9.6f, 0, 36.5f));
            }
            Block("Takhta processional lane", new Vector3(0, .028f, 24), new Vector3(4.5f, .025f, 14), paving);
            Sign("GARDA ISTANA", new Vector3(0, 3.2f, 21.2f), 0f, .9f);
            BuildGardaLockdown(post);
            return Objective("Garda Takhta", post, 1.2f);
        }

        private Transform BuildIstanaTakhta()
        {
            float h = TerraceHeight;
            // Solid terrace; the only way up is the central ramp (24 degrees, below the
            // controllers' 45 degree slope limit, no step-height issue for enemies).
            Block("Istana terrace", new Vector3(0, h * .5f, 52.5f), new Vector3(22, h, 21), stone, true);
            // Only the front lip is raised; the walking surface stays flush with the collider.
            Block("Istana terrace cornice", new Vector3(0, h - .07f, 41.95f), new Vector3(22.4f, .14f, .22f), ivory);
            Block("Istana terrace floor", new Vector3(0, h + .012f, 49f), new Vector3(20, .02f, 12), paving);
            for (int i = 0; i < 2; i++)
                Block("Istana terrace relief band", new Vector3(0, 1.4f + i * 1.9f, 41.98f), new Vector3(22, .22f, .06f), bronze);

            Vector3 rampStart = new Vector3(0, 0, 31f), rampEnd = new Vector3(0, h, 42f);
            Vector3 along = rampEnd - rampStart;
            float angle = Mathf.Atan2(along.y, along.z) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(-angle, 0, 0);
            Vector3 normal = rotation * Vector3.up;
            var ramp = Block("Istana ceremonial ramp", (rampStart + rampEnd) * .5f - normal * .2f,
                new Vector3(8, .4f, along.magnitude), ivory, true);
            ramp.transform.rotation = rotation;
            for (int i = 1; i < 12; i++)
            {
                Vector3 point = Vector3.Lerp(rampStart, rampEnd, i / 12f);
                var tread = Block("Ramp carved tread line", point + normal * .012f, new Vector3(7.6f, .012f, .08f), dark);
                tread.transform.rotation = rotation;
            }
            foreach (int side in new[] { -1, 1 })
            {
                var rail = Block("Ramp bronze balustrade", (rampStart + rampEnd) * .5f + new Vector3(side * 4.1f, .9f, 0),
                    new Vector3(.12f, .12f, along.magnitude), bronze);
                rail.transform.rotation = rotation;
                for (int i = 0; i < 4; i++)
                    Column(Vector3.Lerp(rampStart, rampEnd, (i + .5f) / 4f) + new Vector3(side * 4.1f, 0, 0), .3f);
                Banner(new Vector3(side * 9f, h, 43.2f), 3.4f);
                Banner(new Vector3(side * 5.2f, h, 43.2f), 3.0f);
            }

            CivicHall(new Vector3(0, h, 58));
            Sign("ISTANA TAKHTA", ThronePosition + new Vector3(0, 11.6f, 2.45f), 0f, 1.3f);

            // Tall bronze-crowned pylons behind the seat read as the destination from the spawn.
            foreach (int side in new[] { -1, 1 })
            {
                Column(ThronePosition + new Vector3(side * 2.8f, 0, 3f), 3.9f);
                Block("Takhta pylon crown", ThronePosition + new Vector3(side * 2.8f, 11.2f, 3f), new Vector3(1.1f, .5f, 1.1f), bronze);
            }
            Block("Takhta crown lintel", ThronePosition + new Vector3(0, 11.6f, 3f), new Vector3(6.8f, .55f, .8f), bronze);
            Roof(ThronePosition + new Vector3(0, 11.9f, 3f), new Vector3(3.5f, 2.4f, 1.1f), red);
            MeshObject("Takhta dais ring", rim, ThronePosition + Vector3.up * .15f, Vector3.one * 2.6f, bronze);
            Block("Takhta crimson carpet", ThronePosition + new Vector3(0, .03f, -2.3f), new Vector3(2.2f, .02f, 3.6f), red);

            PlaceSpikeThrone();
            ThroneTrim();
            var marker = new GameObject("Istana Takhta");
            marker.transform.SetParent(root);
            marker.transform.position = ThronePosition;
            return marker.transform;
        }

        // The generated PvP seat (KursiSeat, KursiBack, ...) moves onto the terrace.
        private static void PlaceSpikeThrone()
        {
            foreach (string n in new[] { "KursiSeat", "KursiBack", "KursiLeftArm", "KursiRightArm", "KursiCrown" })
                GameObject.Find(n).transform.position += ThronePosition;
        }

        private TextMesh Sign(string text, Vector3 p, float yaw, float scale)
        {
            var go = new GameObject("Sign " + text);
            go.transform.SetParent(root);
            go.transform.position = p;
            go.transform.rotation = Quaternion.Euler(12, yaw, 0);
            go.transform.localScale = Vector3.one * scale;
            var label = go.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 48;
            label.characterSize = .09f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.text = text;
            label.color = new Color(1f, .86f, .52f);
            if (label.font != null)
                go.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            return label;
        }

        private void ClearSpikeScenery()
        {
            var keep = new HashSet<string> { "KursiSeat", "KursiBack", "KursiLeftArm", "KursiRightArm", "KursiCrown" };
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                // Every Spike arena primitive is a root renderer; player, input and cameras aren't.
                // Destroying geometry also removes its colliders, avoiding invisible old cover.
                if (go.GetComponent<Renderer>() != null && !keep.Contains(go.name))
                    UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private void CreatePalette()
        {
            stone = Surface("Andesite", new Color(0.40f, 0.43f, 0.43f), 0.18f);
            ivory = Surface("Limestone", new Color(0.80f, 0.77f, 0.65f), 0.32f);
            bronze = Surface("BrushedBronze", new Color(0.72f, 0.48f, 0.18f), 0.62f, 0.42f);
            dark = Surface("CarvedTimber", new Color(0.17f, 0.21f, 0.19f), 0.23f);
            red = Surface("CrimsonFabric", new Color(0.64f, 0.075f, 0.07f), 0.12f);
            green = Surface("PatinatedRoof", new Color(0.20f, 0.40f, 0.33f), 0.38f, 0.15f);
            leaf = Surface("PalmGreen", new Color(0.12f, 0.31f, 0.10f), 0.2f);
            paleLeaf = Surface("PalmSunlit", new Color(0.29f, 0.46f, 0.12f), 0.23f);
            flower = Surface("Bougainvillea", new Color(0.68f, 0.19f, 0.33f), 0.15f);
            water = Surface("TurquoiseWater", new Color(0.10f, 0.46f, 0.49f), 0.86f, 0.18f);
            paving = Surface("PlazaPaving", new Color(0.66f, 0.63f, 0.57f), 0.22f);
            // Faction identities (FactionDefinition colours) for banners and trims.
            majelisCloth = Surface("MajelisBurgundy", new Color(0.45f, 0.09f, 0.16f), 0.14f);
            majelisTrim = Surface("MajelisGold", new Color(0.86f, 0.66f, 0.28f), 0.5f, 0.3f);
            biroCloth = Surface("BiroArchiveBeige", new Color(0.72f, 0.68f, 0.60f), 0.2f);
            biroTrim = Surface("BiroArchiveBlue", new Color(0.20f, 0.36f, 0.58f), 0.25f);
            gardaCloth = Surface("GardaNavy", new Color(0.09f, 0.13f, 0.24f), 0.18f);
            foreach (var mat in new[] { stone, ivory, dark, green, paving, red, water })
            {
                mat.SetTexture("_BaseMap", Texture(mat == paving ? "Paving" : mat == water ? "Water" : "Grain"));
                if (mat == paving) mat.SetTextureScale("_BaseMap", new Vector2(16, 12));
                EditorUtility.SetDirty(mat);
            }
        }

        private void CreateMeshes()
        {
            column = CampaignCapitalMeshes.Lathe("ProfiledColumn", new[] {
                new Vector2(0,0), new Vector2(.32f,0), new Vector2(.32f,.12f),
                new Vector2(.25f,.18f), new Vector2(.19f,.27f), new Vector2(.16f,2.55f),
                new Vector2(.22f,2.63f), new Vector2(.30f,2.69f), new Vector2(.30f,2.85f), new Vector2(0,2.85f) }, 20);
            dome = CampaignCapitalMeshes.Lathe("CivicRibbedDome", new[] {
                new Vector2(0,0), new Vector2(1,0), new Vector2(1,.08f), new Vector2(.98f,.15f),
                new Vector2(.94f,.31f), new Vector2(.85f,.48f), new Vector2(.71f,.65f),
                new Vector2(.5f,.79f), new Vector2(.27f,.89f), new Vector2(0,.94f) }, 48);
            pedestal = CampaignCapitalMeshes.Lathe("OctagonalMonument", new[] {
                new Vector2(0,0), new Vector2(1.30f,0), new Vector2(1.30f,.18f),
                new Vector2(1.12f,.28f), new Vector2(1.12f,.55f), new Vector2(.86f,.70f),
                new Vector2(.69f,.9f), new Vector2(.39f,5.9f), new Vector2(.54f,6.05f),
                new Vector2(.54f,6.23f), new Vector2(0,6.23f) }, 8);
            roof = CampaignCapitalMeshes.Roof(); arch = CampaignCapitalMeshes.Arch();
            feather = CampaignCapitalMeshes.Feather("SculptedWingFeather", false);
            // 0.2.6: bell-shaped stupa and its lotus cushion (profiles in metres, scaled per use).
            stupa = CampaignCapitalMeshes.Lathe("CandiStupa", new[] {
                new Vector2(0f, 0f), new Vector2(.95f, 0f), new Vector2(1f, .12f), new Vector2(.97f, .45f),
                new Vector2(.85f, .9f), new Vector2(.6f, 1.25f), new Vector2(.32f, 1.42f), new Vector2(0f, 1.46f) }, 24);
            lotus = CampaignCapitalMeshes.Lathe("CandiTeratai", new[] {
                new Vector2(0f, 0f), new Vector2(1.05f, 0f), new Vector2(1.12f, .12f), new Vector2(1f, .26f),
                new Vector2(.95f, .38f), new Vector2(0f, .38f) }, 24);
            shutterWood = Surface("GedungLamaJendelaKayu", new Color(.16f, .30f, .22f), .3f);
            frond = CampaignCapitalMeshes.Feather("CurvedPalmFrond", true);
            rim = CampaignCapitalMeshes.Ring("ObjectiveRing", .96f, 1);
        }

        private void ConfigureLighting()
        {
            string path = CampaignCapitalMeshes.Folder + "/CampaignURP.asset";
            var source = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(SpikeProject.Generated + "/SpikeURP.asset");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (pipeline == null) { pipeline = UnityEngine.Object.Instantiate(source); AssetDatabase.CreateAsset(pipeline, path); }
            // URP 17 exposes shadow-support switches as read-only properties.
            var serialized = new SerializedObject(pipeline);
            SetBool(serialized, "m_MainLightShadowsSupported", true);
            SetBool(serialized, "m_SoftShadowsSupported", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            pipeline.shadowDistance = 48f;
            pipeline.mainLightShadowmapResolution = 2048;
            pipeline.shadowCascadeCount = 2;
            pipeline.msaaSampleCount = 2;
            pipeline.supportsHDR = false;
            pipeline.useSRPBatcher = true;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = pipeline; }
            QualitySettings.SetQualityLevel(0, false);
            EditorUtility.SetDirty(pipeline);
            var sun = GameObject.Find("Sun").GetComponent<Light>();
            sun.transform.rotation = Quaternion.Euler(48, -38, 0);
            sun.color = new Color(1, .92f, .78f); sun.intensity = 1.5f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = .8f;
            sun.shadowBias = .035f; sun.shadowNormalBias = .25f;
            RenderSettings.sun = sun;
            GameObject.Find("ArenaFill").GetComponent<Light>().intensity = .18f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.45f, .60f, .80f);
            RenderSettings.ambientEquatorColor = new Color(.42f, .44f, .40f);
            RenderSettings.ambientGroundColor = new Color(.23f, .23f, .18f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.72f, .84f, .94f);
            // 0.2.5: clearer tropical air (owner's concept images) so the bay, islands and
            // coastal town behind the Istana read; the gunung stay a soft silhouette.
            RenderSettings.fogStartDistance = 90; RenderSettings.fogEndDistance = 330;
            var sky = AssetDatabase.LoadAssetAtPath<Material>(CampaignCapitalMeshes.Folder + "/TropicalSky.mat");
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, CampaignCapitalMeshes.Folder + "/TropicalSky.mat");
            }
            sky.SetFloat("_AtmosphereThickness", .95f); sky.SetFloat("_Exposure", 1.25f);
            sky.SetColor("_SkyTint", new Color(.38f,.54f,.80f));
            RenderSettings.skybox = sky; EditorUtility.SetDirty(sky);
            Camera.main.clearFlags = CameraClearFlags.Skybox; Camera.main.farClipPlane = 340;
            // 0.2.7: depth precision for the far view (the orbit camera never gets closer than 0.8 m).
            Camera.main.nearClipPlane = .5f;
            ConfigurePostProcessing(Camera.main);
        }

        private static void SetBool(SerializedObject data, string name, bool value)
        {
            var property = data.FindProperty(name);
            if (property == null) throw new InvalidOperationException("URP 17 property missing: " + name);
            property.boolValue = value;
        }

        private void GroundAndStreets()
        {
            var grass=Surface("TropicalGardenGround",new Color(.24f,.34f,.16f),.1f);
            landscapeGrass=grass;
            grass.SetTexture("_BaseMap",Texture("Grain"));
            EditorUtility.SetDirty(grass);
            grass.SetTextureScale("_BaseMap",new Vector2(150,150));
            // 0.2.7: ground layers are at least ~2 cm apart; closer layers z-fought (flickered) at
            // a distance on the tablet (LIHAT ARENA). Landscape top -0.11, promenade 0, inlays above.
            Block("Surrounding Konoha Landscape", new Vector3(0,-.26f,4), new Vector3(600,.3f,600), grass);
            // A continuous collision floor extends beyond the logical oval campus boundary.
            // There is no perimeter wall or rendered boundary box.
            // Covers the whole route oval (radii 34 x 58 around z 4) with margin.
            var floor=Block("Floor", new Vector3(0,-.35f,4), new Vector3(96,.7f,136), paving, true);
            floor.GetComponent<Renderer>().enabled=false;
            // 0.6.5 KARIER: the walkable floor reaches the ring road and the jalan raya.
            var outer=Block("Floor jalan raya", new Vector3(0,-.354f,5), new Vector3(108,.7f,154), paving, true);
            outer.GetComponent<Renderer>().enabled=false;
            MeshObject("Oval capital promenade",CampaignCapitalMeshes.CampusGround(30,34),
                new Vector3(0,.001f,4),Vector3.one,paving);
            paving.SetTextureScale("_BaseMap", new Vector2(44,44));
            EditorUtility.SetDirty(paving);
            Block("Civic forecourt inlay", new Vector3(0,.028f,12), new Vector3(18,.025f,5), ivory);
            Block("Gerbang Rakyat boulevard", new Vector3(0,.028f,-30.5f), new Vector3(6,.025f,43), ivory);
            Block("Wing corridor", new Vector3(0,.028f,WingCorridorZ), new Vector3(62,.025f,2.7f), ivory);
            for (int side=-1;side<=1;side+=2)
            {
                Block("Garden promenade",new Vector3(side*20,.008f,1),new Vector3(3,.025f,35),ivory);
                Block("Garden crosswalk",new Vector3(side*15,.028f,-12.5f),new Vector3(13,.025f,2.1f),ivory);
                Pavilion(new Vector3(side*19.5f,0,-16));
                for(int i=0;i<4;i++)
                {
                    float islandZ=-9+i*8;
                    if(!InWingCorridor(side*24,islandZ,2.15f))
                    {
                        GardenIsland(new Vector3(side*24,0,islandZ),new Vector3(4.8f,.14f,4.3f));
                        Palm(new Vector3(side*24,0,islandZ),5.4f,i*53);
                    }
                    Lamp(new Vector3(side*18.3f,0,islandZ));
                }
                GardenIsland(new Vector3(side*8.5f,0,-18.5f),new Vector3(7,.14f,5.5f));
                Palm(new Vector3(side*8.5f,0,-18.5f),5.8f,side*42);
                Banner(new Vector3(side*3.8f,0,-20),3.8f);
                // Buildings continue outside the playable campus, maintaining a city silhouette in reverse views.
                for(int i=0;i<3;i++)
                    Skyline(new Vector3(side*(14+i*9),0,-32-(i%2)*5),5+i*2);
            }
            // Thin inlays sit above the floor and do not introduce invisible steps.
            Block("Ceremonial main lane",new Vector3(0,.008f,0),new Vector3(3.5f,.025f,23.2f),ivory);
            for(int side=-1;side<=1;side+=2)
                Block("Processional bronze inlay",new Vector3(side*1.72f,.05f,0),new Vector3(.035f,.015f,23),bronze);
            for (int i=0;i<18;i++)
                Block("Paving transverse joint",new Vector3(0,.05f,-11+i*1.25f),new Vector3(3.35f,.014f,.018f),stone);
            var plaza = CampaignCapitalMeshes.Lathe("PlazaSteppedDisc",new[]{ new Vector2(0,0),new Vector2(3.9f,0),
                new Vector2(3.9f,.06f),new Vector2(3.72f,.12f),new Vector2(0,.12f)},64);
            MeshObject("Circular plaza stone",plaza,Vector3.zero,Vector3.one,ivory);
            MeshObject("Plaza concentric carving",CampaignCapitalMeshes.Ring("PlazaRing",3.38f,3.53f),new Vector3(0,.124f,0),Vector3.one,bronze);
            // 0.3.0: no ornamental pattern on the walking surface (owner: plain roads); the
            // stepped disc and its single bronze ring stay.
        }

        private void Monument(Vector3 p)
        {
            int firstSurface = root.childCount;
            var baseGo = MeshObject("Monumen Garuda Konoha",pedestal,p,Vector3.one,ivory);
            // Narrow stem collision preserves a walkable ring and throne approach from the south.
            var collider=baseGo.AddComponent<BoxCollider>(); collider.center=new Vector3(0,2.9f,0); collider.size=new Vector3(1.35f,5.8f,1.35f);
            Block("Monument inset relief",p+new Vector3(0,3.25f,-.535f),new Vector3(.29f,4.15f,.035f),bronze);
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<7;i++)
                {
                    var wing=MeshObject("Garuda swept feather",feather,p+new Vector3(side*(.20f+i*.19f),6.45f+i*.10f,0),
                        new Vector3(1.5f,1.1f,1.6f+i*.07f),bronze);
                    wing.transform.rotation=Quaternion.Euler(-64+i*4,side*(52+i*7),0);
                }
                Ellipsoid("Garuda wing shoulder",p+new Vector3(side*.55f,6.6f,0),new Vector3(1.25f,.43f,.35f),bronze);
            }
            Ellipsoid("Garuda breast",p+new Vector3(0,6.62f,0),new Vector3(.66f,1.15f,.58f),bronze);
            Ellipsoid("Garuda head",p+new Vector3(0,7.30f,-.08f),new Vector3(.46f,.5f,.45f),bronze);
            var beak=MeshObject("Garuda beak",feather,p+new Vector3(0,7.25f,-.18f),new Vector3(.6f,.55f,.44f),bronze);
            beak.transform.rotation=Quaternion.Euler(14,180,0);
            for(int i=-1;i<=1;i++)
            {
                var tail=MeshObject("Garuda tail",feather,p+new Vector3(i*.16f,6.25f,.10f),new Vector3(.7f,.8f,.85f),bronze);
                tail.transform.rotation=Quaternion.Euler(80,i*18,0);
            }
            // Large lettered plaques use relief geometry; no floating debug label on the landmark.
            Block("Monument dedication plaque",p+new Vector3(0,.62f,-1.02f),new Vector3(.58f,.28f,.045f),dark);
            // 0.85 scale keeps the sculpture below the spawn camera's sightline to the seat
            // (§13); every piece, including the stem collider, scales about the base.
            const float scale = .85f;
            for (int i = firstSurface; i < root.childCount; i++)
            {
                Transform piece = root.GetChild(i);
                piece.position = p + (piece.position - p) * scale;
                piece.localScale *= scale;
            }
            var monument = baseGo.AddComponent<CampaignMonument>();
            monument.solid = collider;
            monument.player = UnityEngine.Object.FindFirstObjectByType<CharacterMotor>().transform;
            monument.viewCamera = Camera.main;
            int count = root.childCount - firstSurface;
            monument.surfaces = new Renderer[count];
            monument.opaqueMaterials = new Material[count];
            monument.transparentMaterials = new Material[count];
            var variants = new Dictionary<Material, Material>();
            for (int i = 0; i < count; i++)
            {
                var renderer = root.GetChild(firstSurface + i).GetComponent<Renderer>();
                var original = renderer.sharedMaterial;
                if (!variants.TryGetValue(original, out var faded))
                {
                    string path = CampaignCapitalMeshes.Folder + "/" + original.name + "Occluded.mat";
                    faded = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (faded == null) { faded = new Material(original); AssetDatabase.CreateAsset(faded, path); }
                    faded.CopyPropertiesFromMaterial(original);
                    faded.SetFloat("_Surface", 1);
                    faded.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    faded.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    faded.SetFloat("_ZWrite", 0);
                    faded.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    faded.SetOverrideTag("RenderType", "Transparent");
                    faded.renderQueue = (int)RenderQueue.Transparent;
                    EditorUtility.SetDirty(faded);
                    variants.Add(original, faded);
                }
                monument.surfaces[i] = renderer;
                monument.opaqueMaterials[i] = original;
                monument.transparentMaterials[i] = faded;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, 0);
            }
        }

        private void CivicHall(Vector3 p)
        {
            Steps(p+new Vector3(0,0,-7.45f),17,6,.17f,.55f);
            Block("Civic hall podium",p+new Vector3(0,.5f,0),new Vector3(21,1,9),stone,true);
            Block("Civic hall core",p+new Vector3(0,3.1f,1),new Vector3(17,4.3f,5.5f),ivory,true);
            Block("Deep colonnade shadow",p+new Vector3(0,3,-2),new Vector3(17,3.7f,.3f),dark,true);
            for(int i=-5;i<=5;i++) Column(p+new Vector3(i*1.65f,1,-3.4f),1.43f,true);
            Block("Civic entablature",p+new Vector3(0,5.2f,-.4f),new Vector3(21,.4f,8),ivory);
            Block("Bronze cornice",p+new Vector3(0,5.48f,-.4f),new Vector3(21.3f,.13f,8.2f),bronze);
            // 0.2.8: a three-tier tumpang roof (as on Javanese/Balinese halls and the owner's
            // concept palace) with a gold mustaka, instead of the green dome.
            Roof(p+new Vector3(0,5.7f,-.4f),new Vector3(11f,2.6f,4.5f),red);
            Roof(p+new Vector3(0,7.55f,-.4f),new Vector3(7.2f,2.1f,3.2f),red);
            Roof(p+new Vector3(0,9.2f,-.4f),new Vector3(4.2f,1.7f,2f),red);
            MeshObject("Istana mustaka",dome,p+new Vector3(0,10.55f,-.4f),new Vector3(.55f,1.1f,.55f),bronze);
            for(int side=-1;side<=1;side+=2)
            {
                Banner(p+new Vector3(side*5.6f,1,-3.65f),4.1f);
            }
            for(int i=-4;i<=4;i++)
                Block("Civic frieze lozenge",p+new Vector3(i*2.1f,5.21f,-4.43f),new Vector3(.26f,.26f,.05f),bronze).transform.rotation=Quaternion.Euler(0,0,45);
        }

        private Transform Institution(string name, Vector3 p, bool archive, Material cloth, Material trim)
        {
            var objective=Objective(name,p,2.8f);
            Vector3 b=p+new Vector3(0,0,2.9f);
            Block(name+" facade",b+new Vector3(0,1.35f,.8f),new Vector3(5.6f,2.7f,1.2f),ivory,true);
            Block(name+" carved doorway",b+new Vector3(0,1.15f,.17f),new Vector3(1.15f,2.25f,.06f),dark);
            Steps(b+new Vector3(0,0,-1.05f),5.5f,3,.12f,.28f);
            foreach(int side in new[]{-1,1})
            {
                Column(b+new Vector3(side*2.3f,.25f,-.4f),.96f,true);
                MeshObject(name+" carved window arch",arch,b+new Vector3(side*1.75f,1.65f,.17f),new Vector3(.55f,.65f,.45f),bronze);
                Block(name+" window recess",b+new Vector3(side*1.75f,1.2f,.17f),new Vector3(.85f,1.25f,.05f),dark);
            }
            Block(name+" carved lintel",b+new Vector3(0,3,.1f),new Vector3(6.3f,.3f,3.5f),ivory);
            Roof(b+new Vector3(0,3.25f,.1f),new Vector3(3.4f,2.2f,2.15f),archive?green:red);
            Roof(b+new Vector3(0,4.3f,.1f),new Vector3(2.1f,1.3f,1.3f),archive?green:red);
            if(archive) MeshObject(name+" archive crown",dome,b+new Vector3(0,4.75f,.1f),new Vector3(.72f,1,.72f),bronze);
            Banner(b+new Vector3(-3.15f,0,-.3f),2.7f,cloth,trim);
            Banner(b+new Vector3(3.15f,0,-.3f),2.7f,cloth,trim);
            Block(name+" faction band",b+new Vector3(0,2.78f,.16f),new Vector3(5.6f,.16f,.05f),trim);
            return objective;
        }

        private void Canal(Vector3 p)
        {
            Block("Canal dark basin",p+new Vector3(0,.012f,0),new Vector3(6.1f,.01f,5.5f),dark);
            var surface=Block("Canal reflective water",p+new Vector3(0,.034f,0),new Vector3(5.7f,.018f,5.1f),water);
            surface.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            foreach(int side in new[]{-1,1})
            {
                // Low, stepable coping and two broad open exits prevent the former .44 m trap.
                for(int end=-1;end<=1;end+=2)
                {
                    Block("Low canal coping",p+new Vector3(end*2.05f,.06f,side*2.7f),new Vector3(2.1f,.12f,.24f),ivory,true);
                    Block("Low canal return",p+new Vector3(side*2.95f,.06f,end*1.8f),new Vector3(.24f,.12f,1.7f),ivory,true);
                }
                // A 1.9 m wide opening; its shallow ramp crosses the water edge in both directions.
                var exit=Block("Canal walk-out ramp",p+new Vector3(0,.035f,side*2.7f),new Vector3(1.9f,.07f,1.5f),ivory,true);
                exit.transform.rotation=Quaternion.Euler(side*-3f,0,0);
                // Ramp rise .48 over run 1.8 (15 degrees), within the controller's slope limit.
                var ramp=Block("Bridge approach ramp",p+new Vector3(side*3.2f,.26f,0),new Vector3(1.9f,.12f,1.9f),ivory,true);
                ramp.transform.rotation=Quaternion.Euler(0,0,-side*15f);
                Block("Bridge handrail",p+new Vector3(0,1.23f,side*1.08f),new Vector3(4.65f,.12f,.13f),bronze);
                for(int i=-2;i<=2;i++) Column(p+new Vector3(i*1.1f,.5f,side*1.08f),.25f);
            }
            Block("Bridge across reflecting pool",p+new Vector3(0,.43f,0),new Vector3(4.65f,.18f,2.2f),ivory,true);
            MeshObject("Bridge carved arch",arch,p+new Vector3(0,.02f,-1.13f),new Vector3(2.05f,.4f,1),stone);
            MeshObject("Bridge carved arch",arch,p+new Vector3(0,.02f,1.13f),new Vector3(2.05f,.4f,1),stone);
            for(int i=0;i<3;i++)
            {
                var ripple=MeshObject("Fountain ripple",rim,p+new Vector3(1.4f,.044f,1.65f),Vector3.one*(.26f+i*.18f),ivory);
                ripple.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            Ellipsoid("Fountain finial",p+new Vector3(1.4f,.20f,1.65f),new Vector3(.24f,.3f,.24f),bronze);
        }

        private void ThroneTrim()
        {
            foreach(string n in new[]{"KursiSeat","KursiBack","KursiLeftArm","KursiRightArm","KursiCrown"})
                GameObject.Find(n).GetComponent<Renderer>().sharedMaterial=n=="KursiSeat"?red:bronze;
            for(int side=-1;side<=1;side+=2)
                Column(ThronePosition+new Vector3(side*.67f,.13f,.35f),.38f);
        }

        private Transform Objective(string name, Vector3 p, float radius)
        {
            var marker=new GameObject(name); marker.transform.SetParent(root); marker.transform.position=p;
            var ring=MeshObject(name+" interaction boundary",rim,p+Vector3.up*.056f,Vector3.one*radius,bronze);
            ring.transform.SetParent(marker.transform,true);
            return marker.transform;
        }

        private void Palm(Vector3 p,float height,float twist)
        {
            // Tapered trunk and pointed bent fronds replace the sphere-tree silhouette.
            int first=root.childCount;
            var trunk=CampaignCapitalMeshes.Lathe("PalmTrunk",new[]{new Vector2(0,0),new Vector2(.17f,0),new Vector2(.12f,1),new Vector2(0,1)},12);
            var palm = MeshObject("Palm tapered trunk",trunk,p,new Vector3(1,height,1),dark);
            var trunkCollider = palm.AddComponent<BoxCollider>();
            trunkCollider.center=new Vector3(0,.5f,0); trunkCollider.size=new Vector3(.26f,1,.26f);
            for(int i=0;i<10;i++)
            {
                var go=MeshObject("Palm curved frond",frond,p+Vector3.up*height,new Vector3(1.3f,1.35f,2.45f),i%2==0?leaf:paleLeaf);
                go.transform.rotation=Quaternion.Euler(i%2==0?-9:12,twist+i*36,0);
            }
            Ellipsoid("Palm crown",p+Vector3.up*(height-.05f),new Vector3(.48f,.55f,.48f),leaf);
            // 0.3.1: Art/Models/PohonPalem replaces what is drawn; the trunk collider stays.
            CampaignModelSlots.Apply("PohonPalem",root,first,p,twist,new Vector3(4.5f,height+.9f,4.5f));
        }

        private void Planter(Vector3 p,Vector3 size)
        {
            Block("Garden planter",p+Vector3.up*(size.y*.5f),size,stone,true);
            Block("Planter coping",p+Vector3.up*size.y,new Vector3(size.x+.14f,.1f,size.z+.14f),ivory);
            for(int i=0;i<3;i++)
            {
                Vector3 q=p+new Vector3(0,size.y+.14f,(i-1)*size.z*.28f);
                Ellipsoid("Garden shrub",q,new Vector3(size.x*.9f,.55f,size.z*.40f),i%2==0?leaf:paleLeaf);
                Ellipsoid("Bougainvillea flowers",q+new Vector3(.16f,.29f,0),new Vector3(.32f,.16f,.35f),flower);
            }
        }

        private void Statue(Vector3 p)
        {
            Block("Guardian pedestal",p+Vector3.up*1.1f,new Vector3(3.2f,2.2f,3),stone,true);
            Block("Guardian pedestal cornice",p+Vector3.up*2.25f,new Vector3(3.6f,.25f,3.4f),ivory);
            // 0.2.6: a stone stupa on a lotus cushion (as on the terraces of the old candi)
            // replaces the egg-headed guardian figure.
            MeshObject("Stupa bantalan teratai",lotus,p+Vector3.up*2.37f,new Vector3(1.35f,1f,1.35f),stone);
            MeshObject("Stupa genta",stupa,p+Vector3.up*2.75f,new Vector3(1.15f,1.25f,1.15f),stone);
            Block("Stupa harmika",p+Vector3.up*4.55f,new Vector3(.62f,.34f,.62f),stone);
            for(int i=0;i<3;i++)
                Block("Stupa yasti",p+Vector3.up*(4.8f+i*.22f),new Vector3(.42f-i*.1f,.2f,.42f-i*.1f),stone);
            Block("Stupa puncak",p+Vector3.up*5.55f,new Vector3(.1f,.35f,.1f),stone);
            Banner(p+new Vector3(-1.5f,0,-1.75f),2.1f);
        }

        // 0.2.6: a whitewashed Indonesian heritage building (gedung lama) instead of the tall
        // box with black slits: rows of shuttered windows, a wooden balcony band, a tiled
        // porch roof and a tiered genteng roof. Same 4 m solid footprint as before.
        private void Skyline(Vector3 p,float height)
        {
            int first=root.childCount;
            Block("Distant civic tower",p+Vector3.up*(height*.5f),new Vector3(4,height,4),ivory,true);
            Block("Gedung lama plint",p+Vector3.up*.35f,new Vector3(4.12f,.7f,4.12f),stone);
            Material shutter = shutterWood != null ? shutterWood : dark;
            for(float y=1.7f;y<height-.9f;y+=2.3f)
            {
                foreach(int s in new[]{-1,1})
                {
                    // Front (-z) and both sides.
                    Block("Gedung lama jendela",p+new Vector3(s*.9f,y,-2.02f),new Vector3(.62f,1.1f,.04f),dark);
                    Block("Gedung lama daun jendela",p+new Vector3(s*.9f+s*.42f,y,-2.04f),new Vector3(.2f,1.1f,.04f),shutter);
                    Block("Gedung lama ambang",p+new Vector3(s*.9f,y+.66f,-2.05f),new Vector3(.9f,.12f,.08f),ivory);
                    Block("Gedung lama jendela",p+new Vector3(s*2.02f,y,-.9f),new Vector3(.04f,1.1f,.62f),dark);
                    Block("Gedung lama jendela",p+new Vector3(s*2.02f,y,.9f),new Vector3(.04f,1.1f,.62f),dark);
                }
                if(y>2f) Block("Gedung lama balkon",p+new Vector3(0,y-.72f,-2.25f),new Vector3(4.2f,.1f,.5f),dark);
            }
            Block("Gedung lama pintu",p+new Vector3(0,1.1f,-2.02f),new Vector3(.9f,1.9f,.05f),dark);
            var porch=Block("Gedung lama atap teras",p+new Vector3(0,2.45f,-2.55f),new Vector3(2.4f,.1f,1.2f),genteng);
            porch.transform.rotation=Quaternion.Euler(-14,0,0);
            Roof(p+Vector3.up*height,new Vector3(2.9f,2.1f,2.9f),red);
            // 0.3.1: Art/Models/GedungLama; the solid 4 m block keeps colliding, so the footprint
            // (not the height) decides the fit: the model's walls must stand on that collider.
            CampaignModelSlots.Apply("GedungLama",root,first,p,0f,new Vector3(4.4f,40f,4.4f));
        }

        private void Banner(Vector3 p,float h) => Banner(p,h,red,ivory);

        private void Banner(Vector3 p,float h,Material upperCloth,Material lowerCloth)
        {
            Column(p,.20f);
            Block("Banner bronze staff",p+Vector3.up*(h*.5f),new Vector3(.07f,h,.07f),bronze);
            Block("Banner crossbar",p+new Vector3(.38f,h,0),new Vector3(.92f,.07f,.07f),bronze);
            // Staggered fabric strips create a folded silhouette and alternating lit normals.
            for(int i=0;i<5;i++)
            {
                float x=.1f+i*.13f;
                var upper=Block("Red civic cloth",p+new Vector3(x,h-.4f,Mathf.Sin(i*1.6f)*.045f),new Vector3(.135f,.74f,.025f),upperCloth);
                upper.transform.rotation=Quaternion.Euler(0,i%2==0?16:-16,0);
                var lower=Block("White civic cloth",p+new Vector3(x,h-1.09f,Mathf.Sin(i*1.6f)*.045f),new Vector3(.135f,.63f,.025f),lowerCloth);
                lower.transform.rotation=upper.transform.rotation;
            }
        }

        private Material lanternGlass;

        // 0.3.0: a square Javanese lantern (lentera) with a small pyramid roof replaces the
        // round glass globe under a dome cap (it read as a mushroom). Same post, no collider.
        private void Lamp(Vector3 p)
        {
            if(lanternGlass==null)
            {
                lanternGlass=Surface("LenteraKaca",new Color(1f,.88f,.62f),.65f);
                EditorUtility.SetDirty(lanternGlass);
            }
            int first=root.childCount;
            Column(p,.54f);
            Vector3 top=p+Vector3.up*1.54f;
            Block("Lentera dudukan",top+Vector3.up*.03f,new Vector3(.36f,.06f,.36f),bronze);
            Block("Lentera kaca",top+Vector3.up*.25f,new Vector3(.25f,.36f,.25f),lanternGlass);
            foreach(int x in new[]{-1,1}) foreach(int z in new[]{-1,1})
                Block("Lentera tiang",top+new Vector3(x*.135f,.25f,z*.135f),new Vector3(.035f,.4f,.035f),dark);
            Block("Lentera lis",top+Vector3.up*.46f,new Vector3(.34f,.04f,.34f),dark);
            MeshObject("Lentera atap",roof,top+Vector3.up*.48f,new Vector3(.22f,.17f,.22f),bronze);
            Ellipsoid("Lentera mustaka",top+Vector3.up*.66f,new Vector3(.06f,.09f,.06f),bronze);
            CampaignModelSlots.Apply("Lentera",root,first,p,0f);
        }

        private void Steps(Vector3 p,float width,int count,float rise,float run)
        {
            for(int i=0;i<count;i++)
                Block("Carved civic stair",p+new Vector3(0,(i+1)*rise*.5f,i*run),new Vector3(width,(i+1)*rise,run+.02f),ivory,true);
        }
        private void Column(Vector3 p,float scale,bool solid=false)
        {
            var go=MeshObject("Profiled civic column",column,p,Vector3.one*scale,ivory);
            if(solid)
            {
                var collider=go.AddComponent<BoxCollider>();
                collider.center=new Vector3(0,1.425f,0); collider.size=new Vector3(.46f,2.85f,.46f);
            }
        }

        private void GardenIsland(Vector3 p,Vector3 size)
        {
            Block("Low garden border",p+Vector3.up*(size.y*.5f),size,ivory,true);
            Block("Planted garden soil",p+Vector3.up*.15f,new Vector3(size.x-.22f,.025f,size.z-.22f),leaf);
            for(int i=-1;i<=1;i++)
            {
                Ellipsoid("Flower garden foliage",p+new Vector3(i*size.x*.25f,.34f,-size.z*.28f),new Vector3(1.15f,.42f,.8f),paleLeaf);
                Ellipsoid("Garden blossom cluster",p+new Vector3(i*size.x*.25f,.55f,-size.z*.28f),new Vector3(.60f,.18f,.48f),flower);
            }
        }

        private void Pavilion(Vector3 p)
        {
            int first=root.childCount;
            Block("Pavilion stone landing",p+Vector3.up*.075f,new Vector3(4.8f,.15f,4.8f),ivory,true);
            foreach(int x in new[]{-1,1}) foreach(int z in new[]{-1,1})
                Column(p+new Vector3(x*1.85f,.15f,z*1.85f),1.05f,true);
            Roof(p+Vector3.up*3.2f,new Vector3(2.65f,1.8f,2.65f),green);
            Roof(p+Vector3.up*4.15f,new Vector3(1.55f,1.1f,1.55f),green);
            foreach(int side in new[]{-1,1})
            {
                Block("Pavilion carved bench",p+new Vector3(side*1.25f,.52f,.65f),new Vector3(.45f,.20f,1.55f),dark,true);
                Block("Bench pedestal",p+new Vector3(side*1.25f,.25f,.65f),new Vector3(.32f,.5f,1.1f),stone,true);
            }
            // 0.3.1: Art/Models/Pendopo; landing, columns and benches keep their colliders.
            CampaignModelSlots.Apply("Pendopo",root,first,p,0f);
        }
        private void Roof(Vector3 p,Vector3 size,Material mat)
        {
            // 0.2.1: every tiered roof is clay genteng (the passed colour only tints trims).
            MeshObject("Tiered Nusantara roof",roof,p,size,genteng!=null?genteng:mat);
            Block("Roof bronze ridge",p+new Vector3(0,size.y*.65f,0),new Vector3(size.x*.96f,.085f,.10f),bronze);
            for(int side=-1;side<=1;side+=2)
            {
                var finial=MeshObject("Upturned roof finial",feather,p+new Vector3(side*size.x*.98f,.04f,0),new Vector3(.6f,.7f,.8f),bronze);
                finial.transform.rotation=Quaternion.Euler(-65,side*80,0);
            }
        }

        private GameObject Block(string name,Vector3 p,Vector3 scale,Material mat,bool solid=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); Configure(go,name,p,scale,mat);
            if(!solid) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        private GameObject Ellipsoid(string name,Vector3 p,Vector3 scale,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere); Configure(go,name,p,scale,mat);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        private GameObject MeshObject(string name,Mesh mesh,Vector3 p,Vector3 scale,Material mat)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh=mesh; Configure(go,name,p,scale,mat); return go;
        }
        private void Configure(GameObject go,string name,Vector3 p,Vector3 scale,Material mat)
        {
            go.name=name; go.transform.SetParent(root); go.transform.position=p; go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=mat;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
        }
        private Material Surface(string name,Color color,float smooth,float metal=0)
        {
            string path=CampaignCapitalMeshes.Folder+"/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.name=name;
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",smooth);mat.SetFloat("_Metallic",metal);
            mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
        }
        private Texture2D Texture(string name)
        {
            string path=CampaignCapitalMeshes.Folder+"/"+name+".asset";
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            const int size=256;
            if(tex==null){tex=new Texture2D(size,size,TextureFormat.RGBA32,true);AssetDatabase.CreateAsset(tex,path);}
            tex.name=name;tex.wrapMode=TextureWrapMode.Repeat;tex.filterMode=FilterMode.Trilinear;tex.anisoLevel=4;
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float grain=Mathf.PerlinNoise(x*.21f,y*.21f)*.07f;
                float value=.9f+grain;
                if(name=="Paving")
                {
                    // Conblock basketweave (trotoar khas Indonesia): 64 px cells of two bricks,
                    // horizontal and vertical cells alternating, each brick a slightly different tone.
                    int cx=x/64, cy=y/64, lx=x%64, ly=y%64;
                    bool horizontal=((cx+cy)&1)==0;
                    int brick=horizontal?ly/32:lx/32;
                    int along=horizontal?lx:ly, across=horizontal?ly%32:lx%32;
                    bool joint=along<2||across<2;
                    float tone=((cx*7+cy*13+brick*5)%4)*.025f;
                    value=joint?.60f:.84f+tone+grain*.8f;
                }
                if(name=="Water") value=.82f+.12f*Mathf.Sin(x*.17f+Mathf.Sin(y*.08f)*3)+grain;
                pixels[y*size+x]=new Color(value,value,value,1);
            }
            tex.SetPixels(pixels);tex.Apply(true,false);EditorUtility.SetDirty(tex);return tex;
        }
    }
}
