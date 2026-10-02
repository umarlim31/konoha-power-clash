using System;
using System.Collections.Generic;
using Konoha.Campaign;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Konoha.Editor
{
    // 0.2.3: builds the primitive human bodies of Jalur Takhta (heroes and organisation
    // members) with joint pivots for CampaignHumanoid. About 1.9 m tall, feet at the root,
    // facing +z. Clothes are generic Indonesian dress (kebaya, safari, koko + peci, kemeja
    // with a project vest, jas + dasi); no real faces, logos or party symbols.
    internal static class CampaignRigBuilder
    {
        internal enum Headwear { Hair, Bun, Peci }

        internal sealed class Style
        {
            public string name;
            public Material skin, shirt, sleeve, pants, shoes, hair, accent, skirt;
            public bool longSleeves = true;
            public float width = 1f, height = 1f, belly;
            public Headwear headwear = Headwear.Hair;
            public bool heroShadows = true;
        }

        internal sealed class Parts
        {
            public CampaignHumanoid rig;
            public Transform head;
            public readonly List<Renderer> primary = new List<Renderer>();
            public readonly List<Renderer> accent = new List<Renderer>();
        }

        private static Material peciBlack, lips, eyeWhite, cheek;
        private const string MeshFolder = SpikeProject.Generated + "/Rig";

        internal static Material Lit(string name, Color color, float smooth = .2f)
        {
            string path = SpikeProject.Generated + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader unavailable");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smooth);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // Unlit "glow" for effects: always bright, no lighting cost; colour per property block.
        internal static Material Unlit(string name, Color color)
        {
            string path = SpikeProject.Generated + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) throw new InvalidOperationException("URP Unlit shader unavailable");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        internal static Parts Build(Transform parent, Style style)
        {
            peciBlack = Lit("RigPeciHitam", new Color(.05f, .05f, .06f), .35f);
            lips = Lit("RigBibir", new Color(.42f, .20f, .17f), .3f);
            eyeWhite = Lit("RigMataPutih", new Color(.96f, .95f, .92f), .5f);
            cheek = Lit("RigPipi", new Color(.76f, .50f, .43f), .2f);
            var eyes = new List<Transform>();
            var root = new GameObject(style.name).transform;
            root.SetParent(parent, false);
            root.localScale = new Vector3(style.width, style.height, style.width);
            var parts = new Parts();
            var rig = root.gameObject.AddComponent<CampaignHumanoid>();
            parts.rig = rig;
            Material sleeve = style.sleeve != null ? style.sleeve : style.shirt;

            // 0.3.0: rounded body (owner: the kain and trousers still looked like blocks).
            // Pelvis is an ellipsoid, limbs are capsules, the torso and kain are lathe meshes
            // with a waist, chest and flared hem; shoes are flattened ellipsoids.
            var pelvis = Pivot("Pinggul", root, new Vector3(0f, .95f, 0f));
            Part(parts, "Pinggul", PrimitiveType.Sphere, pelvis, new Vector3(0f, -.02f, 0f), new Vector3(.37f, .24f, .25f), style.pants, false);

            Transform[] hips = new Transform[2], knees = new Transform[2];
            for (int s = 0; s < 2; s++)
            {
                float x = s == 0 ? -.1f : .1f;
                hips[s] = Pivot(s == 0 ? "Paha kiri" : "Paha kanan", pelvis, new Vector3(x, -.04f, 0f));
                Part(parts, "Paha", PrimitiveType.Capsule, hips[s], new Vector3(0f, -.22f, 0f), new Vector3(.18f, .25f, .18f), style.pants, style.heroShadows);
                knees[s] = Pivot(s == 0 ? "Lutut kiri" : "Lutut kanan", hips[s], new Vector3(0f, -.46f, 0f));
                Part(parts, "Betis", PrimitiveType.Capsule, knees[s], new Vector3(0f, -.2f, 0f), new Vector3(.14f, .22f, .14f), style.pants, style.heroShadows);
                Part(parts, "Sepatu", PrimitiveType.Sphere, knees[s], new Vector3(0f, -.42f, .05f), new Vector3(.13f, .1f, .27f), style.shoes, false);
            }
            if (style.skirt != null)
            {
                // Ankle-length wrapped kain: close at the ankles, soft hip, waist under the kebaya.
                MeshPart(parts, "Kain", KainMesh(), pelvis, new Vector3(0f, -.86f, 0f), new Vector3(.23f, .86f, .19f), style.skirt, style.heroShadows);
                // Kebaya tail falling over the hip.
                MeshPart(parts, "Ujung kebaya", KebayaTailMesh(), pelvis, new Vector3(0f, -.14f, 0f), new Vector3(.225f, .2f, .165f), style.shirt, false);
            }

            var spine = Pivot("Punggung", pelvis, new Vector3(0f, .06f, 0f));
            parts.primary.Add(MeshPart(parts, "Badan", TorsoMesh(), spine, Vector3.zero, new Vector3(.25f, .55f, .155f), style.shirt, true));
            if (style.belly > 0f)
                parts.primary.Add(Part(parts, "Perut", PrimitiveType.Sphere, spine, new Vector3(0f, .16f, .05f),
                    new Vector3(.42f + .06f * style.belly, .38f, .28f + .12f * style.belly), style.shirt, false));
            Part(parts, "Leher", PrimitiveType.Capsule, spine, new Vector3(0f, .56f, 0f), new Vector3(.1f, .06f, .1f), style.skin, false);

            var head = Pivot("Kepala", spine, new Vector3(0f, .62f, 0f));
            parts.head = head;
            Part(parts, "Wajah", PrimitiveType.Sphere, head, new Vector3(0f, .13f, .01f), new Vector3(.22f, .27f, .24f), style.skin, style.heroShadows);
            Part(parts, "Dagu", PrimitiveType.Sphere, head, new Vector3(0f, .04f, .05f), new Vector3(.13f, .1f, .13f), style.skin, false);
            Part(parts, "Hidung", PrimitiveType.Capsule, head, new Vector3(0f, .12f, .12f), new Vector3(.04f, .035f, .045f), style.skin, false);
            Part(parts, "Mulut", PrimitiveType.Sphere, head, new Vector3(0f, .06f, .118f), new Vector3(.065f, .016f, .02f), lips, false);
            foreach (int s in new[] { -1, 1 })
            {
                // 0.3.4: cartoon eyes (white with a dark pupil and a light glint) that blink.
                var eye = Part(parts, "Mata", PrimitiveType.Sphere, head, new Vector3(s * .05f, .155f, .113f), new Vector3(.052f, .042f, .03f), eyeWhite, false).transform;
                var pupil = Part(parts, "Pupil", PrimitiveType.Sphere, eye, new Vector3(0f, -.05f, .42f), new Vector3(.52f, .66f, .5f), style.hair, false).transform;
                Part(parts, "Kilau mata", PrimitiveType.Sphere, pupil, new Vector3(.25f, .3f, .55f), new Vector3(.3f, .3f, .3f), eyeWhite, false);
                eyes.Add(eye);
                Part(parts, "Alis", PrimitiveType.Capsule, head, new Vector3(s * .055f, .186f, .114f), new Vector3(.015f, .035f, .015f), style.hair, false)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 90f - s * 8f);
                Part(parts, "Telinga", PrimitiveType.Sphere, head, new Vector3(s * .115f, .13f, 0f), new Vector3(.04f, .075f, .05f), style.skin, false);
            }
            Part(parts, "Pipi", PrimitiveType.Sphere, head, new Vector3(-.066f, .1f, .1f), new Vector3(.045f, .026f, .02f), cheek, false);
            Part(parts, "Pipi", PrimitiveType.Sphere, head, new Vector3(.066f, .1f, .1f), new Vector3(.045f, .026f, .02f), cheek, false);
            root.gameObject.AddComponent<CampaignBlink>().eyes = eyes.ToArray();
            Part(parts, "Rambut", PrimitiveType.Sphere, head, new Vector3(0f, .18f, -.025f), new Vector3(.245f, .22f, .265f), style.hair, false);
            if (style.headwear == Headwear.Bun)
                Part(parts, "Sanggul", PrimitiveType.Sphere, head, new Vector3(0f, .16f, -.16f), new Vector3(.17f, .14f, .13f), style.hair, false);
            else if (style.headwear == Headwear.Peci)
                Part(parts, "Peci", PrimitiveType.Cylinder, head, new Vector3(0f, .27f, -.01f), new Vector3(.25f, .06f, .26f), peciBlack, false);

            Transform[] shoulders = new Transform[2], elbows = new Transform[2];
            for (int s = 0; s < 2; s++)
            {
                float x = s == 0 ? -.25f : .25f;
                shoulders[s] = Pivot(s == 0 ? "Bahu kiri" : "Bahu kanan", spine, new Vector3(x, .47f, 0f));
                parts.primary.Add(Part(parts, "Pundak", PrimitiveType.Sphere, shoulders[s], new Vector3(s == 0 ? .015f : -.015f, -.01f, 0f), new Vector3(.15f, .14f, .16f), style.shirt, false));
                Renderer upper = Part(parts, "Lengan atas", PrimitiveType.Capsule, shoulders[s], new Vector3(0f, -.15f, 0f), new Vector3(.12f, .17f, .12f), sleeve, false);
                if (sleeve == style.shirt) parts.primary.Add(upper);
                elbows[s] = Pivot(s == 0 ? "Siku kiri" : "Siku kanan", shoulders[s], new Vector3(0f, -.3f, 0f));
                Renderer fore = Part(parts, "Lengan bawah", PrimitiveType.Capsule, elbows[s], new Vector3(0f, -.14f, 0f),
                    new Vector3(.1f, .16f, .1f), style.longSleeves ? sleeve : style.skin, false);
                if (style.longSleeves && sleeve == style.shirt) parts.primary.Add(fore);
                Part(parts, "Tangan", PrimitiveType.Sphere, elbows[s], new Vector3(0f, -.31f, .01f), new Vector3(.085f, .11f, .095f), style.skin, false);
            }

            rig.pelvis = pelvis;
            rig.spine = spine;
            rig.head = head;
            rig.hipLeft = hips[0];
            rig.hipRight = hips[1];
            rig.kneeLeft = knees[0];
            rig.kneeRight = knees[1];
            rig.shoulderLeft = shoulders[0];
            rig.shoulderRight = shoulders[1];
            rig.elbowLeft = elbows[0];
            rig.elbowRight = elbows[1];
            return parts;
        }

        // The four hero bodies, inactive under one scene object; CampaignBodies copies them.
        internal static CampaignHumanoid[] HeroTemplates(Transform parent)
        {
            Material langsat = Lit("RigKulitLangsat", new Color(.80f, .62f, .47f), .3f);
            Material sawo = Lit("RigKulitSawo", new Color(.62f, .43f, .30f), .3f);
            Material hair = Lit("RigRambut", new Color(.04f, .035f, .03f), .45f);
            Material greyHair = Lit("RigRambutUban", new Color(.52f, .50f, .48f), .35f);
            Material shoes = Lit("RigSepatu", new Color(.06f, .05f, .05f), .6f);
            Material darkPants = Lit("RigCelanaGelap", new Color(.10f, .11f, .14f), .15f);
            Material gold = Lit("RigEmas", new Color(.90f, .66f, .25f), .7f);

            // MEGA: red kebaya, batik kain, hair bun, gold brooch and a red selendang.
            var mega = Build(parent, new Style
            {
                name = "BodyTemplate MEGA", skin = langsat, hair = hair,
                shirt = Lit("RigKebayaMerah", new Color(.60f, .08f, .12f), .35f),
                pants = Lit("RigKainPolos", new Color(.30f, .16f, .09f), .15f), shoes = shoes,
                skirt = KainBatik(),
                headwear = Headwear.Bun, width = 1.04f
            });
            Accent(mega, "Selendang", PrimitiveType.Cube, new Vector3(0f, .3f, 0f), new Vector3(.12f, .62f, .27f), Lit("RigSelendang", new Color(.85f, .20f, .20f), .3f), new Vector3(0f, 0f, 32f));
            Accent(mega, "Bros", PrimitiveType.Sphere, new Vector3(0f, .44f, .13f), new Vector3(.09f, .07f, .04f), gold, Vector3.zero);

            // GEMOY: cream safari shirt with pockets, peci, gold belt, stocky.
            Material safari = Lit("RigSafariKrem", new Color(.80f, .72f, .56f), .25f);
            var gemoy = Build(parent, new Style
            {
                name = "BodyTemplate GEMOY", skin = sawo, hair = hair, shirt = safari,
                pants = Lit("RigCelanaKhaki", new Color(.36f, .32f, .24f), .15f), shoes = shoes,
                longSleeves = false, headwear = Headwear.Peci, width = 1.12f, belly = .9f
            });
            foreach (int s in new[] { -1, 1 })
                Accent(gemoy, "Saku", PrimitiveType.Cube, new Vector3(s * .11f, .38f, .135f), new Vector3(.12f, .1f, .02f), Lit("RigSafariSaku", new Color(.68f, .60f, .45f), .2f), Vector3.zero);
            Accent(gemoy, "Sabuk", PrimitiveType.Cylinder, new Vector3(0f, .03f, .01f), new Vector3(.44f, .03f, .36f), gold, Vector3.zero);

            // ABAH: dark green jas over a white koko collar, peci, grey temples.
            var abah = Build(parent, new Style
            {
                name = "BodyTemplate ABAH", skin = langsat, hair = greyHair,
                shirt = Lit("RigJasHijau", new Color(.08f, .26f, .20f), .3f),
                pants = darkPants, shoes = shoes, headwear = Headwear.Peci, width = .96f
            });
            Accent(abah, "Kerah koko", PrimitiveType.Sphere, new Vector3(0f, .43f, .105f), new Vector3(.15f, .2f, .06f), Lit("RigPutih", new Color(.93f, .92f, .88f), .2f), Vector3.zero);
            Accent(abah, "Syal", PrimitiveType.Cube, new Vector3(-.08f, .3f, .135f), new Vector3(.07f, .45f, .02f), Lit("RigSyalHijau", new Color(.20f, .62f, .42f), .2f), new Vector3(0f, 0f, -4f));

            // PAK WI: white shirt with rolled sleeves, black trousers, orange project vest.
            var pakWi = Build(parent, new Style
            {
                name = "BodyTemplate PAK WI", skin = sawo, hair = hair,
                shirt = Lit("RigPutih", new Color(.93f, .92f, .88f), .2f),
                pants = darkPants, shoes = shoes, longSleeves = false, width = .95f
            });
            // Vest follows the torso shape (slightly larger), with a reflective band around it.
            MeshPart(pakWi, "Rompi proyek", TorsoMesh(), pakWi.rig.spine, new Vector3(0f, .1f, 0f), new Vector3(.265f, .4f, .17f),
                Lit("RigRompiOranye", new Color(.95f, .45f, .10f), .3f), false);
            Accent(pakWi, "Pita reflektor", PrimitiveType.Cylinder, new Vector3(0f, .25f, 0f), new Vector3(.415f, .02f, .275f), Lit("RigReflektor", new Color(.85f, .88f, .82f), .8f), Vector3.zero);

            var result = new[] { mega.rig, gemoy.rig, abah.rig, pakWi.rig };
            foreach (var rig in result)
                rig.gameObject.SetActive(false);
            return result;
        }

        // Organisation member body: jas/uniform and tie take the faction colours at runtime.
        internal static Parts Member(Transform parent, Material cloth, Material accent)
        {
            var parts = Build(parent, new Style
            {
                name = "Tubuh anggota", skin = Lit("RigKulitSawo", new Color(.62f, .43f, .30f), .3f),
                hair = Lit("RigRambut", new Color(.04f, .035f, .03f), .45f), shirt = cloth,
                pants = Lit("RigCelanaGelap", new Color(.10f, .11f, .14f), .15f),
                shoes = Lit("RigSepatu", new Color(.06f, .05f, .05f), .6f), heroShadows = false
            });
            parts.accent.Add(Accent(parts, "Dasi", PrimitiveType.Cube, new Vector3(0f, .3f, .13f), new Vector3(.07f, .32f, .02f), accent, Vector3.zero));
            parts.accent.Add(Accent(parts, "Sabuk", PrimitiveType.Cylinder, new Vector3(0f, .03f, 0f), new Vector3(.41f, .025f, .27f), accent, Vector3.zero));
            return parts;
        }

        // An extra piece on the chest (spine pivot).
        internal static Renderer Accent(Parts parts, string name, PrimitiveType type, Vector3 local, Vector3 scale, Material mat, Vector3 euler)
        {
            var renderer = Part(parts, name, type, parts.rig.spine, local, scale, mat, false);
            renderer.transform.localRotation = Quaternion.Euler(euler);
            return renderer;
        }

        internal static GameObject HeadPiece(Parts parts, string name, PrimitiveType type, Vector3 local, Vector3 scale, Material mat, Vector3 euler)
        {
            var renderer = Part(parts, name, type, parts.head, local, scale, mat, false);
            renderer.transform.localRotation = Quaternion.Euler(euler);
            return renderer.gameObject;
        }

        private static Transform Pivot(string name, Transform parent, Vector3 local)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = local;
            return pivot;
        }

        private static Renderer Part(Parts parts, string name, PrimitiveType type, Transform parent, Vector3 local, Vector3 scale,
            Material mat, bool shadow)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = shadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return renderer;
        }

        private static Renderer MeshPart(Parts parts, string name, Mesh mesh, Transform parent, Vector3 local, Vector3 scale,
            Material mat, bool shadow)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = shadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return renderer;
        }

        // Unit lathe profiles (radius, height), bottom to top so faces point outward.
        // Torso: waist, ribcage, chest, shoulder slope, neck opening.
        private static Mesh TorsoMesh() => Lathe("RigTorso", new[]
        {
            new Vector2(0f, 0f), new Vector2(.80f, 0f), new Vector2(.74f, .2f), new Vector2(.80f, .5f),
            new Vector2(.90f, .74f), new Vector2(.86f, .88f), new Vector2(.62f, .97f), new Vector2(.36f, 1f), new Vector2(0f, 1f)
        });

        // Kain: wrapped close at the ankles, widening over the knees to a soft hip, waist at the top.
        private static Mesh KainMesh() => Lathe("RigKain", new[]
        {
            new Vector2(0f, 0f), new Vector2(.84f, 0f), new Vector2(.87f, .03f), new Vector2(.85f, .15f),
            new Vector2(.9f, .45f), new Vector2(1f, .7f), new Vector2(1.06f, .84f), new Vector2(.94f, .96f), new Vector2(.8f, 1f), new Vector2(0f, 1f)
        });

        // Short kebaya tail: wider at the lower edge, hugging the waist at the top.
        private static Mesh KebayaTailMesh() => Lathe("RigUjungKebaya", new[]
        {
            new Vector2(0f, 0f), new Vector2(1.05f, 0f), new Vector2(1.04f, .15f), new Vector2(.95f, .6f),
            new Vector2(.86f, 1f), new Vector2(0f, 1f)
        });

        private static Mesh Lathe(string name, Vector2[] profile, int sides = 20)
        {
            EnsureMeshFolder();
            string path = MeshFolder + "/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh { name = name }; AssetDatabase.CreateAsset(mesh, path); }
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            for (int row = 0; row < profile.Length; row++)
                for (int i = 0; i <= sides; i++)
                {
                    float a = i * Mathf.PI * 2f / sides;
                    vertices.Add(new Vector3(Mathf.Cos(a) * profile[row].x, profile[row].y, Mathf.Sin(a) * profile[row].x));
                    uv.Add(new Vector2(i / (float)sides, profile[row].y));
                    if (row == 0 || i == sides) continue;
                    int b = row * (sides + 1) + i, p = b - sides - 1;
                    triangles.AddRange(new[] { p, b, p + 1, p + 1, b, b + 1 });
                }
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static void EnsureMeshFolder()
        {
            if (!AssetDatabase.IsValidFolder(MeshFolder))
                AssetDatabase.CreateFolder(SpikeProject.Generated, "Rig");
        }

        // MEGA's kain: brown sogan batik with diagonal parang-like bands and small cream dots.
        // Clothing only; roads stay plain (owner, 0.3.0).
        private static Material KainBatik()
        {
            EnsureMeshFolder();
            const int size = 128;
            string path = MeshFolder + "/RigKainBatikTex.asset";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) { tex = new Texture2D(size, size, TextureFormat.RGBA32, true); AssetDatabase.CreateAsset(tex, path); }
            tex.name = "RigKainBatikTex";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            var brown = new Color(.32f, .17f, .09f);
            var dark = new Color(.16f, .08f, .05f);
            var cream = new Color(.80f, .64f, .40f);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int d = (x + y) % 32;
                    Color c = brown;
                    if (d < 2 || (d > 13 && d < 16)) c = dark;
                    else if (d >= 4 && d <= 11)
                    {
                        // wavy cream band with a dark eye in the middle
                        float wave = Mathf.Sin((x - y) * Mathf.PI / 16f);
                        float centre = 7.5f + wave * 2.2f;
                        c = Mathf.Abs(d - centre) < 1.2f ? dark : cream;
                    }
                    else if (d >= 17 && d <= 29 && ((x / 4 + y / 4) % 4 == 0) && (x % 4 == 1) && (y % 4 == 1))
                        c = cream;
                    pixels[y * size + x] = c;
                }
            tex.SetPixels(pixels);
            tex.Apply(true, false);
            EditorUtility.SetDirty(tex);
            Material mat = Lit("RigKainBatik", Color.white, .15f);
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", new Vector2(6f, 4f));
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
