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

        private static Material peciBlack;

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
            var root = new GameObject(style.name).transform;
            root.SetParent(parent, false);
            root.localScale = new Vector3(style.width, style.height, style.width);
            var parts = new Parts();
            var rig = root.gameObject.AddComponent<CampaignHumanoid>();
            parts.rig = rig;
            Material sleeve = style.sleeve != null ? style.sleeve : style.shirt;

            var pelvis = Pivot("Pinggul", root, new Vector3(0f, .95f, 0f));
            Part(parts, "Pinggul", PrimitiveType.Cube, pelvis, Vector3.zero, new Vector3(.38f, .18f, .24f), style.pants, false);

            Transform[] hips = new Transform[2], knees = new Transform[2];
            for (int s = 0; s < 2; s++)
            {
                float x = s == 0 ? -.1f : .1f;
                hips[s] = Pivot(s == 0 ? "Paha kiri" : "Paha kanan", pelvis, new Vector3(x, -.04f, 0f));
                Part(parts, "Paha", PrimitiveType.Cylinder, hips[s], new Vector3(0f, -.23f, 0f), new Vector3(.17f, .23f, .17f), style.pants, style.heroShadows);
                knees[s] = Pivot(s == 0 ? "Lutut kiri" : "Lutut kanan", hips[s], new Vector3(0f, -.46f, 0f));
                Part(parts, "Betis", PrimitiveType.Cylinder, knees[s], new Vector3(0f, -.21f, 0f), new Vector3(.14f, .21f, .14f), style.pants, style.heroShadows);
                Part(parts, "Sepatu", PrimitiveType.Cube, knees[s], new Vector3(0f, -.43f, .05f), new Vector3(.14f, .08f, .27f), style.shoes, false);
            }
            if (style.skirt != null)
                Part(parts, "Kain", PrimitiveType.Cylinder, pelvis, new Vector3(0f, -.43f, 0f), new Vector3(.46f, .44f, .36f), style.skirt, style.heroShadows);

            var spine = Pivot("Punggung", pelvis, new Vector3(0f, .06f, 0f));
            parts.primary.Add(Part(parts, "Badan", PrimitiveType.Cube, spine, new Vector3(0f, .28f, 0f), new Vector3(.44f, .5f, .25f), style.shirt, true));
            if (style.belly > 0f)
                parts.primary.Add(Part(parts, "Perut", PrimitiveType.Sphere, spine, new Vector3(0f, .16f, .05f),
                    new Vector3(.44f + .06f * style.belly, .38f, .3f + .12f * style.belly), style.shirt, false));
            Part(parts, "Leher", PrimitiveType.Cylinder, spine, new Vector3(0f, .56f, 0f), new Vector3(.1f, .05f, .1f), style.skin, false);

            var head = Pivot("Kepala", spine, new Vector3(0f, .62f, 0f));
            parts.head = head;
            Part(parts, "Wajah", PrimitiveType.Sphere, head, new Vector3(0f, .13f, .01f), new Vector3(.23f, .27f, .25f), style.skin, style.heroShadows);
            Part(parts, "Hidung", PrimitiveType.Cube, head, new Vector3(0f, .12f, .13f), new Vector3(.045f, .07f, .05f), style.skin, false);
            foreach (int s in new[] { -1, 1 })
            {
                Part(parts, "Mata", PrimitiveType.Sphere, head, new Vector3(s * .055f, .16f, .115f), Vector3.one * .035f, style.hair, false);
                Part(parts, "Alis", PrimitiveType.Cube, head, new Vector3(s * .055f, .2f, .12f), new Vector3(.07f, .015f, .02f), style.hair, false);
                Part(parts, "Telinga", PrimitiveType.Sphere, head, new Vector3(s * .12f, .13f, 0f), new Vector3(.05f, .08f, .04f), style.skin, false);
            }
            Part(parts, "Rambut", PrimitiveType.Sphere, head, new Vector3(0f, .19f, -.02f), new Vector3(.25f, .2f, .27f), style.hair, false);
            if (style.headwear == Headwear.Bun)
                Part(parts, "Sanggul", PrimitiveType.Sphere, head, new Vector3(0f, .18f, -.17f), new Vector3(.17f, .15f, .15f), style.hair, false);
            else if (style.headwear == Headwear.Peci)
                Part(parts, "Peci", PrimitiveType.Cylinder, head, new Vector3(0f, .27f, -.01f), new Vector3(.25f, .06f, .26f), peciBlack, false);

            Transform[] shoulders = new Transform[2], elbows = new Transform[2];
            for (int s = 0; s < 2; s++)
            {
                float x = s == 0 ? -.28f : .28f;
                shoulders[s] = Pivot(s == 0 ? "Bahu kiri" : "Bahu kanan", spine, new Vector3(x, .47f, 0f));
                parts.primary.Add(Part(parts, "Pundak", PrimitiveType.Sphere, shoulders[s], Vector3.zero, new Vector3(.17f, .14f, .17f), style.shirt, false));
                Renderer upper = Part(parts, "Lengan atas", PrimitiveType.Cylinder, shoulders[s], new Vector3(0f, -.15f, 0f), new Vector3(.12f, .15f, .12f), sleeve, false);
                if (sleeve == style.shirt) parts.primary.Add(upper);
                elbows[s] = Pivot(s == 0 ? "Siku kiri" : "Siku kanan", shoulders[s], new Vector3(0f, -.3f, 0f));
                Renderer fore = Part(parts, "Lengan bawah", PrimitiveType.Cylinder, elbows[s], new Vector3(0f, -.14f, 0f),
                    new Vector3(.105f, .14f, .105f), style.longSleeves ? sleeve : style.skin, false);
                if (style.longSleeves && sleeve == style.shirt) parts.primary.Add(fore);
                Part(parts, "Tangan", PrimitiveType.Sphere, elbows[s], new Vector3(0f, -.31f, .01f), new Vector3(.1f, .11f, .1f), style.skin, false);
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
                pants = Lit("RigKainBatik", new Color(.30f, .16f, .09f), .15f), shoes = shoes,
                skirt = Lit("RigKainBatik", new Color(.30f, .16f, .09f), .15f),
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
            Accent(gemoy, "Sabuk", PrimitiveType.Cube, new Vector3(0f, .04f, 0f), new Vector3(.47f, .06f, .36f), gold, Vector3.zero);

            // ABAH: dark green jas over a white koko collar, peci, grey temples.
            var abah = Build(parent, new Style
            {
                name = "BodyTemplate ABAH", skin = langsat, hair = greyHair,
                shirt = Lit("RigJasHijau", new Color(.08f, .26f, .20f), .3f),
                pants = darkPants, shoes = shoes, headwear = Headwear.Peci, width = .96f
            });
            Accent(abah, "Kerah koko", PrimitiveType.Cube, new Vector3(0f, .42f, .1f), new Vector3(.16f, .2f, .06f), Lit("RigPutih", new Color(.93f, .92f, .88f), .2f), Vector3.zero);
            Accent(abah, "Syal", PrimitiveType.Cube, new Vector3(-.08f, .3f, .135f), new Vector3(.07f, .45f, .02f), Lit("RigSyalHijau", new Color(.20f, .62f, .42f), .2f), new Vector3(0f, 0f, -4f));

            // PAK WI: white shirt with rolled sleeves, black trousers, orange project vest.
            var pakWi = Build(parent, new Style
            {
                name = "BodyTemplate PAK WI", skin = sawo, hair = hair,
                shirt = Lit("RigPutih", new Color(.93f, .92f, .88f), .2f),
                pants = darkPants, shoes = shoes, longSleeves = false, width = .95f
            });
            Accent(pakWi, "Rompi proyek", PrimitiveType.Cube, new Vector3(0f, .3f, 0f), new Vector3(.46f, .38f, .27f), Lit("RigRompiOranye", new Color(.95f, .45f, .10f), .3f), Vector3.zero);
            Accent(pakWi, "Pita reflektor", PrimitiveType.Cube, new Vector3(0f, .24f, 0f), new Vector3(.47f, .04f, .28f), Lit("RigReflektor", new Color(.85f, .88f, .82f), .8f), Vector3.zero);

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
            parts.accent.Add(Accent(parts, "Sabuk", PrimitiveType.Cube, new Vector3(0f, .04f, 0f), new Vector3(.45f, .06f, .26f), accent, Vector3.zero));
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
    }
}
