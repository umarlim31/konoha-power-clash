using System;
using Konoha.Character;
using Konoha.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Konoha.Editor
{
    public static class ArenaPrototypeProject
    {
        public const string ScenePath = SpikeProject.Generated + "/PowerRouteArena.unity";

        [MenuItem("Konoha/Prepare Arena Prototype 0.0.2 (regenerates scene)")]
        public static void Prepare()
        {
            // Reuse the pinned Android, URP, input and camera configuration of the 0.0.1 spike.
            SpikeProject.Prepare();
            var scene = EditorSceneManager.OpenScene(SpikeProject.ScenePath, OpenSceneMode.Single);
            PlayerSettings.productName = "NEGARA KONOHA Arena Prototype";
            PlayerSettings.bundleVersion = "0.0.2";
            PlayerSettings.Android.bundleVersionCode = 2;

            Destroy("ObstacleA");
            Destroy("ObstacleB");
            foreach (var marker in GameObject.FindGameObjectsWithTag("Untagged"))
                if (marker.name == "NavigationMarker") UnityEngine.Object.DestroyImmediate(marker);

            var stone = Mat("Andesite", new Color(0.33f, 0.37f, 0.38f));
            var cream = Mat("Sandstone", new Color(0.72f, 0.66f, 0.53f));
            var red = Mat("CeremonialRed", new Color(0.64f, 0.17f, 0.17f));
            var gold = Mat("Brass", new Color(0.83f, 0.58f, 0.22f));
            var pool = Mat("Water", new Color(0.10f, 0.34f, 0.43f));
            var dark = Mat("Avenue", new Color(0.19f, 0.23f, 0.25f));
            var green = Mat("Garden", new Color(0.19f, 0.35f, 0.23f));

            // Clear routes remain at x = +/- 3 and along the two side approaches.
            Box("CentralAvenue", new Vector3(0, 0.015f, 0), new Vector3(6, 0.02f, 22), dark, false);
            Box("CrossAvenue", new Vector3(0, 0.018f, 0), new Vector3(28, 0.02f, 4), dark, false);
            foreach (int side in new[] { -1, 1 })
            {
                Box("ReflectingPool", new Vector3(side * 8, 0.025f, 5), new Vector3(3, 0.02f, 3), pool, false);
                Box("Garden", new Vector3(side * 11, 0.023f, 6), new Vector3(2, 0.02f, 2), green, false);
                for (int z = -9; z <= 9; z += 6)
                    Box("StoneCover", new Vector3(side * 13, 0.45f, z), new Vector3(1.2f, 0.9f, 1.5f), stone);
                Box("CeremonialBanner", new Vector3(side * 15, 2, 0), new Vector3(0.08f, 3.7f, 0.7f), red, false);
            }

            // Original stylized bird silhouette: visual placeholder, no national or party logo asset.
            Box("MonumentPlinth", new Vector3(0, 0.4f, 10), new Vector3(2.6f, 0.8f, 1.6f), stone);
            Box("MonumentPillar", new Vector3(0, 1.7f, 10), new Vector3(0.7f, 2.1f, 0.7f), cream);
            Box("BirdBody", new Vector3(0, 3.15f, 10), new Vector3(0.5f, 0.8f, 0.35f), gold, false);
            for (int i = 0; i < 3; i++)
                foreach (int side in new[] { -1, 1 })
                    Box("AbstractWing", new Vector3(side * (0.65f + i * 0.48f), 3.45f + i * 0.2f, 10),
                        new Vector3(0.9f, 0.16f, 0.3f), gold, false);

            var offices = new[]
            {
                Office("KPU", new Vector3(-9, 0, -6), red, cream, gold),
                Office("Komisi", new Vector3(9, 0, -6), red, cream, gold),
                Office("Parlemen", new Vector3(0, 0, 6), red, cream, gold)
            };
            var seat = new GameObject("Takhta - locked until 3 institutions");
            seat.transform.position = new Vector3(0, 0, 1);
            Box("SeatDais", new Vector3(0, 0.15f, 1), new Vector3(2, 0.3f, 2), cream);
            Box("SeatBack", new Vector3(0, 1.1f, 1.5f), new Vector3(1.3f, 1.8f, 0.3f), gold);
            var throneRing = Box("ThroneRing", new Vector3(0, 0.032f, 1), new Vector3(3.5f, 0.03f, 3.5f), red, false);

            var player = UnityEngine.Object.FindFirstObjectByType<CharacterMotor>();
            player.transform.position = new Vector3(0, 0.1f, -9);
            var route = new GameObject("PowerRoute - local prototype").AddComponent<PowerRoute>();
            route.player = player;
            route.offices = offices;
            route.throne = seat.transform;
            route.throneIndicator = throneRing.GetComponent<Renderer>();

            var canvas = GameObject.Find("TouchCanvas");
            var safe = canvas.transform.Find("SafeArea");
            var instruction = safe.Find("Instruction").GetComponent<Text>();
            instruction.text = "ARENA 0.0.2  |  Jelajahi tiga institusi, lalu duduki takhta";
            instruction.rectTransform.sizeDelta = new Vector2(770, 38);
            var banner = new GameObject("PowerRouteStatus", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            banner.transform.SetParent(safe, false);
            banner.rectTransform.anchorMin = banner.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            banner.rectTransform.pivot = new Vector2(0.5f, 1f);
            banner.rectTransform.anchoredPosition = new Vector2(0, -22);
            banner.rectTransform.sizeDelta = new Vector2(700, 90);
            banner.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            banner.fontSize = 21;
            banner.alignment = TextAnchor.UpperCenter;
            banner.color = Color.white;
            banner.raycastTarget = false;
            route.status = banner;

            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            camera.farClipPlane = 100f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.65f, 0.65f, 0.62f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Arena 0.0.2 prepared. Scene: " + ScenePath);
        }

        private static InstitutionObjective Office(string title, Vector3 position,
            Material red, Material cream, Material gold)
        {
            var root = new GameObject(title + " Objective");
            root.transform.position = position;
            Box(title + " Steps", position + new Vector3(0, 0.15f, 0), new Vector3(3.2f, 0.3f, 3.2f), cream);
            Box(title + " Facade", position + new Vector3(0, 1.45f, 1.05f), new Vector3(2.9f, 2.3f, 0.35f), cream);
            Box(title + " Pediment", position + new Vector3(0, 2.75f, 1.05f), new Vector3(3.3f, 0.35f, 0.8f), red);
            foreach (int side in new[] { -1, 1 })
                Box(title + " Column", position + new Vector3(side * 1.1f, 1.2f, -0.2f),
                    new Vector3(0.28f, 2.1f, 0.3f), gold);
            var indicator = Box(title + " CaptureZone", position + new Vector3(0, 0.035f, -1.1f),
                new Vector3(2.5f, 0.04f, 1.5f), red, false);
            var office = root.AddComponent<InstitutionObjective>();
            office.displayName = title;
            office.indicator = indicator.GetComponent<Renderer>();
            office.radius = 2.4f;
            return office;
        }

        private static Material Mat(string name, Color color)
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
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material mat, bool solid = true)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = mat;
            if (!solid) UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            return box;
        }

        private static void Destroy(string name)
        {
            var target = GameObject.Find(name);
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
