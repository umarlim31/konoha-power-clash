using System;
using System.IO;
using Konoha.Character;
using Konoha.Core;
using Konoha.Data;
using Konoha.Diagnostics;
using Konoha.Input;
using Konoha.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Konoha.Editor
{
    public static class SpikeProject
    {
        public const string UnityVersion = "6000.0.60f1";
        public const string Generated = "Assets/Konoha/Generated";
        public const string ScenePath = Generated + "/SpikeArena.unity";

        [MenuItem("Konoha/Prepare Build 0.0.1 (regenerates greybox)")]
        public static void Prepare()
        {
            if (Application.unityVersion != UnityVersion)
                throw new InvalidOperationException("Expected Unity " + UnityVersion + ", got " + Application.unityVersion);
            Directory.CreateDirectory(Generated);
            Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.Refresh();
            ConfigureAndroid();
            ConfigureRendering();
            CreateArena();
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureAndroid()
        {
            PlayerSettings.companyName = "KonohaPrototype";
            PlayerSettings.productName = "KONOHA Spike";
            PlayerSettings.bundleVersion = "0.0.1";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.konoha.powerclash.spike");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.useCustomKeystore = false;
            // Activity avoids the documented GameActivity dev-build issue on this pinned editor.
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.runInBackground = false;
            PlayerSettings.defaultIsNativeResolution = true;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var inputHandler = settings.FindProperty("activeInputHandler");
            if (inputHandler == null) throw new InvalidOperationException("Missing Unity activeInputHandler setting.");
            inputHandler.intValue = 0; // Legacy touch/EventSystem for this isolated input adapter.
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRendering()
        {
            var renderer = LoadOrCreate<UniversalRendererData>(Generated + "/SpikeRenderer.asset");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Generated + "/SpikeURP.asset");
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, Generated + "/SpikeURP.asset");
            }
            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = 2;
            pipeline.renderScale = 1f;
            pipeline.shadowDistance = 0f;
            pipeline.useSRPBatcher = true;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            // Prevent a default quality tier from overriding URP on Android.
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(0, false);
            QualitySettings.vSyncCount = 0;
            EditorUtility.SetDirty(pipeline);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value != null) return value;
            value = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(value, path);
            return value;
        }

        private static Material Material(string name, Color color)
        {
            string path = Generated + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader is unavailable.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(material);
            return material;
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

        private static void CreateArena()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Arena Negara Konoha v0.1 — playable Nusantara power-complex greybox.
            var stone = Material("Floor", new Color(0.18f, 0.20f, 0.19f));
            var wall = Material("Wall", new Color(0.38f, 0.40f, 0.37f));
            var red = Material("KonohaRed", new Color(0.72f, 0.08f, 0.07f));
            var ivory = Material("KonohaIvory", new Color(0.86f, 0.82f, 0.70f));
            var gold = Material("PowerGold", new Color(0.72f, 0.48f, 0.12f));
            var water = Material("WaterMarker", new Color(0.10f, 0.35f, 0.42f));
            var heroMaterial = Material("TemporaryHero", new Color(0.15f, 0.9f, 0.8f));

            var arena = new GameObject("ARENA_NEGARA_KONOHA_V01");
            Box("Plaza_Floor", new Vector3(0f, -0.5f, 0f), new Vector3(52f, 1f, 42f), stone).transform.SetParent(arena.transform);
            Box("NorthBoundary", new Vector3(0f, 1.5f, 21f), new Vector3(53f, 3f, 1f), wall).transform.SetParent(arena.transform);
            Box("SouthBoundary", new Vector3(0f, 1.5f, -21f), new Vector3(53f, 3f, 1f), wall).transform.SetParent(arena.transform);
            Box("EastBoundary", new Vector3(26f, 1.5f, 0f), new Vector3(1f, 3f, 42f), wall).transform.SetParent(arena.transform);
            Box("WestBoundary", new Vector3(-26f, 1.5f, 0f), new Vector3(1f, 3f, 42f), wall).transform.SetParent(arena.transform);

            // Central Kursi Kekuasaan objective + original Konoha Garuda landmark silhouette.
            Box("CentralDais", new Vector3(0f, 0.5f, 0f), new Vector3(10f, 1f, 10f), ivory).transform.SetParent(arena.transform);
            Box("PowerCore", new Vector3(0f, 1.35f, 0f), new Vector3(5f, 0.7f, 5f), gold).transform.SetParent(arena.transform);
            Box("KursiKekuasaan_Back", new Vector3(0f, 3.1f, 0.8f), new Vector3(2.4f, 4.2f, 0.7f), gold).transform.SetParent(arena.transform);
            Box("KursiKekuasaan_Seat", new Vector3(0f, 1.8f, -0.2f), new Vector3(2.4f, 0.6f, 2.2f), gold).transform.SetParent(arena.transform);
            Box("GarudaMonument_Pillar", new Vector3(0f, 4.5f, 5.7f), new Vector3(1.5f, 9f, 1.5f), ivory).transform.SetParent(arena.transform);
            Box("GarudaKonoha_Body", new Vector3(0f, 9.4f, 5.7f), new Vector3(1.3f, 2.0f, 0.8f), gold).transform.SetParent(arena.transform);
            var wingL=Box("GarudaKonoha_WingL", new Vector3(-2.1f, 9.7f, 5.7f), new Vector3(3.4f, 0.55f, 0.65f), gold); wingL.transform.rotation=Quaternion.Euler(0f,0f,18f); wingL.transform.SetParent(arena.transform);
            var wingR=Box("GarudaKonoha_WingR", new Vector3(2.1f, 9.7f, 5.7f), new Vector3(3.4f, 0.55f, 0.65f), gold); wingR.transform.rotation=Quaternion.Euler(0f,0f,-18f); wingR.transform.SetParent(arena.transform);

            // Four approach lanes, ring cover and flank channels.
            for (int i = -1; i <= 1; i += 2)
            {
                Box("EastWestCover", new Vector3(i * 12f, 0.75f, 0f), new Vector3(4f, 1.5f, 2f), wall).transform.SetParent(arena.transform);
                Box("NorthSouthCover", new Vector3(0f, 0.75f, i * 13f), new Vector3(2f, 1.5f, 4f), wall).transform.SetParent(arena.transform);
                Box("FlankBridge", new Vector3(i * 19f, 1.0f, 0f), new Vector3(4f, 2f, 10f), ivory).transform.SetParent(arena.transform);
                var canal=Box("CanalMarker", new Vector3(i * 19f, 0.03f, 12f), new Vector3(4f, 0.05f, 10f), water);
                UnityEngine.Object.DestroyImmediate(canal.GetComponent<Collider>()); canal.transform.SetParent(arena.transform);
            }

            // Six fictional institutional sectors from the Power Blueprint.
            string[] sectorNames = { "MajelisDaun", "KomisiSuara", "BiroProsedur", "KonsorsiumModal", "MenaraNarasi", "GardaTakhta" };
            Vector3[] sectorPos = {
                new Vector3(-19f,2f,15f), new Vector3(0f,2f,17f), new Vector3(19f,2f,15f),
                new Vector3(-19f,2f,-15f), new Vector3(0f,2f,-17f), new Vector3(19f,2f,-15f)
            };
            for (int i=0;i<sectorNames.Length;i++)
            {
                Box("Sector_"+sectorNames[i], sectorPos[i], new Vector3(9f,4f,5f), i%2==0 ? wall : ivory).transform.SetParent(arena.transform);
                Box("Gate_"+sectorNames[i], sectorPos[i] + new Vector3(0f,-0.5f, sectorPos[i].z>0 ? -3f : 3f), new Vector3(3f,3f,1f), red).transform.SetParent(arena.transform);
            }

            // Spawn territories and readable red/blue team approach markers.
            Box("Spawn_A", new Vector3(-22f,0.05f,0f), new Vector3(5f,0.1f,7f), red).transform.SetParent(arena.transform);
            var blue = Material("TeamBlue", new Color(0.08f,0.30f,0.68f));
            Box("Spawn_B", new Vector3(22f,0.05f,0f), new Vector3(5f,0.1f,7f), blue).transform.SetParent(arena.transform);

            var hero = new GameObject("TemporaryPlayableCharacter");
            hero.transform.position = new Vector3(-20f, 0.1f, 0f);
            var controller = hero.AddComponent<CharacterController>();
            controller.height = 2f; controller.center = Vector3.up; controller.radius = 0.45f;
            controller.stepOffset = 0.2f; controller.slopeLimit = 45f; controller.minMoveDistance = 0f;
            var model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = "PlaceholderSilhouette"; model.transform.SetParent(hero.transform, false);
            model.transform.localPosition = Vector3.up; model.GetComponent<Renderer>().sharedMaterial = heroMaterial;
            UnityEngine.Object.DestroyImmediate(model.GetComponent<Collider>());
            var nose = Box("FacingMarker", Vector3.zero, new Vector3(0.22f, 0.25f, 0.6f), gold);
            UnityEngine.Object.DestroyImmediate(nose.GetComponent<Collider>()); nose.transform.SetParent(hero.transform, false);
            nose.transform.localPosition = new Vector3(0f, 1.35f, 0.5f);
            var motor = hero.AddComponent<CharacterMotor>();
            motor.definition = LoadOrCreate<LocomotionDefinition>(Generated + "/Locomotion.asset");

            var cameraObject = new GameObject("MobileCamera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.10f,0.12f,0.14f);
            camera.fieldOfView = 55f; camera.nearClipPlane = 0.1f; camera.farClipPlane = 110f;
            var follow = cameraObject.AddComponent<MobileCombatCamera>(); follow.target = hero.transform;
            cameraObject.transform.position = hero.transform.position + follow.offset;
            cameraObject.transform.rotation = Quaternion.LookRotation(-follow.offset);
            var light = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.15f; light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.58f,0.55f,0.48f);

            var joystick = CreateHud(motor);
            var driver = new GameObject("OfflineSpikeDriver").AddComponent<OfflineSpikeDriver>();
            driver.joystick = joystick; driver.motor = motor;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Text Label(RectTransform rect, string text, int size)
        {
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = size;
            label.color = Color.white;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private static TouchJoystick CreateHud(CharacterMotor motor)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var canvasObject = new GameObject("TouchCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            var safe = Rect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            safe.anchorMax = Vector2.one;
            var pad = Rect("MovePad", safe, Vector2.zero, Vector2.one * 0.5f, new Vector2(165f, 135f), new Vector2(200f, 200f));
            pad.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.33f, 0.4f, 0.8f);
            var handle = Rect("Handle", pad, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(72f, 72f));
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(0.2f, 0.95f, 0.85f, 0.95f);
            handleImage.raycastTarget = false;
            var joystick = pad.gameObject.AddComponent<TouchJoystick>();
            joystick.pad = pad;
            joystick.handle = handle;
            var layout = canvasObject.AddComponent<SafeAreaLayout>();
            layout.safeRoot = safe;
            layout.joystick = pad;
            Label(Rect("Instruction", safe, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 15f), new Vector2(460f, 32f)),
                "0.0.1 | Geser joystick untuk bergerak", 21).alignment = TextAnchor.MiddleCenter;
            var diagnostics = Label(Rect("Diagnostics", safe, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -18f), new Vector2(690f, 230f)), "Loading diagnostics...", 18);
            var buttonRect = Rect("DebugToggle", safe, Vector2.one, Vector2.one, new Vector2(-20f, -18f), new Vector2(112f, 58f));
            buttonRect.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.3f, 0.35f, 0.95f);
            var button = buttonRect.gameObject.AddComponent<Button>();
            var buttonLabel = Label(Rect("Label", buttonRect, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, buttonRect.sizeDelta), "DEBUG", 20);
            buttonLabel.alignment = TextAnchor.MiddleCenter;
            var hud = canvasObject.AddComponent<SpikeDebugHud>();
            hud.display = diagnostics;
            hud.toggle = button;
            hud.motor = motor;
            return joystick;
        }
    }
}
