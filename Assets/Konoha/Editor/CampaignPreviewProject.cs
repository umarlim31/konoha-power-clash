using System;
using Konoha.Campaign;
using Konoha.Character;
using Konoha.Diagnostics;
using Konoha.Networking;
using Konoha.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Editor
{
    public static class CampaignPreviewProject
    {
        public const string ScenePath = SpikeProject.Generated + "/JalurTakhtaPreview.unity";

        [MenuItem("Konoha/Prepare Jalur Takhta Preview 0.0.7")]
        public static void Prepare()
        {
            // Generate a separate scene using the same Android, URP, input, camera and
            // prototype hero silhouettes as 4v4. Do not change its existing scene.
            SpikeProject.Prepare();
            var scene = EditorSceneManager.OpenScene(SpikeProject.ScenePath, OpenSceneMode.Single);
            PlayerSettings.productName = "KONOHA Jalur Takhta Preview";
            PlayerSettings.bundleVersion = "0.0.7.0";
            PlayerSettings.Android.bundleVersionCode = 13;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.konoha.powerclash.jalurtakhta");

            var network = GameObject.Find("RuntimeNetworkingProof");
            if (network != null) UnityEngine.Object.DestroyImmediate(network);
            var player = UnityEngine.Object.FindFirstObjectByType<CharacterMotor>();
            var canvas = GameObject.Find("TouchCanvas");
            if (player == null || canvas == null)
                throw new InvalidOperationException("Expected offline player and mobile canvas from SpikeProject.");
            player.transform.position = new Vector3(0f, 0.1f, -9f);

            var debug = canvas.GetComponent<SpikeDebugHud>();
            if (debug != null) UnityEngine.Object.DestroyImmediate(debug);
            var feed = canvas.GetComponent<CombatFeedHud>();
            if (feed != null) UnityEngine.Object.DestroyImmediate(feed);
            var safe = canvas.transform.Find("SafeArea");
            var matchHud = safe.GetComponent<NetworkMatchHud>();
            if (matchHud != null) UnityEngine.Object.DestroyImmediate(matchHud);
            var pad = safe.Find("MovePad");
            for (int i = safe.childCount - 1; i >= 0; i--)
                if (safe.GetChild(i) != pad) UnityEngine.Object.DestroyImmediate(safe.GetChild(i).gameObject);
            var layout = canvas.GetComponent<SafeAreaLayout>();
            layout.safeRoot = (RectTransform)safe;
            layout.joystick = (RectTransform)pad;

            var stone = Mat("CampaignStone", new Color(0.54f, 0.52f, 0.46f));
            var gold = Mat("CampaignGold", new Color(0.82f, 0.59f, 0.25f));
            var burgundy = Mat("CampaignMajelis", new Color(0.48f, 0.14f, 0.25f));
            var biroColor = Mat("CampaignBiro", new Color(0.43f, 0.47f, 0.49f));
            var gateColor = Mat("CampaignGate", new Color(0.82f, 0.22f, 0.18f));
            var guardColor = Mat("CampaignGuard", new Color(0.17f, 0.22f, 0.32f));
            var green = Mat("CampaignSeal", new Color(0.22f, 0.68f, 0.45f));

            // The chair stays visible but is surrounded by colliders until the guard falls.
            var barrier = new GameObject("Gerbang Takhta - locked");
            Box("North Gate", new Vector3(0, 0.75f, 2.35f), new Vector3(5.1f, 1.5f, 0.26f), gateColor).transform.SetParent(barrier.transform);
            Box("South Gate", new Vector3(0, 0.75f, -2.35f), new Vector3(5.1f, 1.5f, 0.26f), gateColor).transform.SetParent(barrier.transform);
            Box("East Gate", new Vector3(2.35f, 0.75f, 0), new Vector3(0.26f, 1.5f, 5.1f), gateColor).transform.SetParent(barrier.transform);
            Box("West Gate", new Vector3(-2.35f, 0.75f, 0), new Vector3(0.26f, 1.5f, 5.1f), gateColor).transform.SetParent(barrier.transform);
            var plaza = Marker("Plaza Aspirasi", new Vector3(0, 0, -6.5f), green);
            var majelis = Institution("Majelis Daun", new Vector3(-9f, 0f, -6f), burgundy, stone);
            var biro = Institution("Biro Prosedur", new Vector3(9f, 0f, -6f), biroColor, stone);
            var garda = Marker("Garda Takhta", new Vector3(0, 0, 5.5f), gold);
            var chair = new GameObject("Kursi Kekuasaan");
            chair.transform.position = Vector3.zero;
            WorldLabel("ISTANA TAKHTA", new Vector3(0, 2.8f, 0));
            // Distinct fictional bird monument rather than an official coat of arms.
            Box("Monumen Burung Konoha Plinth", new Vector3(0, 0.4f, 9.5f),
                new Vector3(2.8f, 0.8f, 1.5f), stone);
            Deco("Abstract Bird Body", new Vector3(0, 2.2f, 9.5f), new Vector3(0.48f, 2.8f, 0.5f), gold);
            for (int i = 0; i < 3; i++)
                foreach (int side in new[] { -1, 1 })
                    Deco("Abstract Wing", new Vector3(side * (0.58f + i * 0.5f), 3.0f + i * 0.17f, 9.5f),
                        new Vector3(0.88f, 0.13f, 0.3f), gold);
            for (int side = -1; side <= 1; side += 2)
            {
                Deco("Reflecting Pool", new Vector3(side * 10, 0.035f, 7.7f),
                    new Vector3(3.2f, 0.035f, 2.2f), Mat("CampaignWater", new Color(0.10f, 0.33f, 0.39f)));
                Deco("Ceremonial Banner", new Vector3(side * 14.5f, 1.8f, 6f),
                    new Vector3(0.12f, 3.2f, 0.75f), burgundy);
            }

            var guard = new GameObject("GardaTakhta_Greybox");
            guard.transform.position = garda.position;
            var guardBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            guardBody.name = "Guard Silhouette";
            guardBody.transform.SetParent(guard.transform, false);
            guardBody.transform.localPosition = Vector3.up;
            guardBody.GetComponent<Renderer>().sharedMaterial = guardColor;
            UnityEngine.Object.DestroyImmediate(guardBody.GetComponent<Collider>());
            WorldLabel("GARDA", garda.position + new Vector3(0, 2.5f, 0)).transform.SetParent(guard.transform);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpikeProject.Generated + "/NetworkPlayer.prefab");
            var presentation = prefab != null ? prefab.GetComponent<NetworkActorPresentation>() : null;
            if (presentation == null || presentation.heroVisuals == null || presentation.heroVisuals.Length != 4)
                throw new InvalidOperationException("Four generated hero visuals are required for campaign preview.");
            var visuals = new GameObject[4];
            for (int i = 0; i < visuals.Length; i++)
            {
                visuals[i] = UnityEngine.Object.Instantiate(presentation.heroVisuals[i], player.transform, false);
                visuals[i].name = "Campaign " + new[] { "Mega", "Gemoy", "Abah", "Pak Wi" }[i];
            }
            var placeholder = player.transform.Find("PlaceholderSilhouette");
            if (placeholder != null)
                placeholder.GetComponent<Renderer>().sharedMaterial = Mat("CampaignHeroBody", new Color(0.17f, 0.20f, 0.24f));

            var objective = Text("CampaignObjective", safe, new Vector2(0.5f, 1f),
                new Vector2(0, -32), new Vector2(790, 60), 22);
            objective.alignment = TextAnchor.MiddleCenter;
            var status = Text("CampaignStatus", safe, new Vector2(0.5f, 1f),
                new Vector2(0, -88), new Vector2(750, 36), 18);
            status.alignment = TextAnchor.MiddleCenter;
            var heroText = Text("CampaignHero", safe, new Vector2(0.5f, 0f),
                new Vector2(0, 55), new Vector2(480, 34), 18);
            heroText.alignment = TextAnchor.MiddleCenter;
            var heroButton = Button("HeroSelector", "GANTI HERO", safe, new Vector2(1f, 0f),
                new Vector2(-282, 224), new Vector2(176, 52));
            var basic = Button("CampaignBasic", "BASIC", safe, new Vector2(1f, 0f),
                new Vector2(-106, 92), new Vector2(164, 66));
            var sit = Button("CampaignSit", "DUDUK", safe, new Vector2(1f, 0f),
                new Vector2(-282, 92), new Vector2(164, 66));
            var footer = Text("CampaignRevision", safe, new Vector2(0.5f, 0f),
                new Vector2(0, 10), new Vector2(560, 26), 13);
            footer.alignment = TextAnchor.MiddleCenter;
            footer.text = "JALUR TAKHTA 0.0.7 - SOLO GREYBOX PREVIEW";

            var preview = new GameObject("JalurTakhtaPreview").AddComponent<CampaignPreviewController>();
            preview.player = player;
            preview.plaza = plaza;
            preview.majelis = majelis;
            preview.biro = biro;
            preview.garda = garda;
            preview.chair = chair.transform;
            preview.chairBarrier = barrier;
            preview.guardVisual = guard;
            preview.heroVisuals = visuals;
            preview.objectiveText = objective;
            preview.statusText = status;
            preview.heroText = heroText;
            preview.attackButton = basic;
            preview.sitButton = sit;
            preview.heroButton = heroButton;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Jalur Takhta preview prepared: " + ScenePath);
        }

        private static Transform Institution(string name, Vector3 location, Material accent, Material stone)
        {
            var marker = Marker(name, location, accent);
            Box(name + " Facade", location + new Vector3(0, 1.25f, 1.5f), new Vector3(3.8f, 2.5f, 0.45f), stone);
            Box(name + " Roof", location + new Vector3(0, 2.6f, 1.5f), new Vector3(4.2f, 0.3f, 0.9f), accent);
            foreach (int side in new[] { -1, 1 })
                Box(name + " Column", location + new Vector3(side * 1.5f, 0.9f, 0.55f),
                    new Vector3(0.3f, 1.8f, 0.4f), stone);
            WorldLabel(name.ToUpperInvariant(), location + new Vector3(0, 3.25f, 1.5f));
            return marker;
        }

        private static Transform Marker(string name, Vector3 location, Material material)
        {
            var marker = new GameObject(name);
            marker.transform.position = location;
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = name + " Zone";
            disc.transform.SetParent(marker.transform, false);
            disc.transform.localPosition = new Vector3(0, 0.02f, 0);
            disc.transform.localScale = new Vector3(2.2f, 0.015f, 2.2f);
            disc.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(disc.GetComponent<Collider>());
            return marker.transform;
        }

        private static GameObject WorldLabel(string caption, Vector3 position)
        {
            var root = new GameObject("Sign " + caption);
            root.transform.position = position;
            var label = root.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 36;
            label.characterSize = 0.055f;
            label.anchor = TextAnchor.MiddleCenter;
            label.text = caption;
            label.color = Color.white;
            if (label.font != null)
                root.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            root.transform.rotation = Quaternion.Euler(30, 0, 0);
            return root;
        }

        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        private static GameObject Deco(string name, Vector3 position, Vector3 scale, Material material)
        {
            var result = Box(name, position, scale, material);
            UnityEngine.Object.DestroyImmediate(result.GetComponent<Collider>());
            return result;
        }

        private static Material Mat(string name, Color color)
        {
            string path = SpikeProject.Generated + "/" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader unavailable");
                result = new Material(shader);
                AssetDatabase.CreateAsset(result, path);
            }
            result.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(result);
            return result;
        }

        private static Text Text(string name, Transform parent, Vector2 anchor, Vector2 offset,
            Vector2 size, int fontSize)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.color = Color.white;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            return label;
        }

        private static Button Button(string name, string caption, Transform parent, Vector2 anchor,
            Vector2 offset, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.14f, 0.30f, 0.33f, 0.95f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var captionText = Text("Caption", rect, Vector2.one * 0.5f, Vector2.zero, size, 20);
            captionText.rectTransform.pivot = Vector2.one * 0.5f;
            captionText.alignment = TextAnchor.MiddleCenter;
            captionText.text = caption;
            return button;
        }
    }
}
