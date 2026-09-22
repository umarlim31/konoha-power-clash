using System;
using System.IO;
using Konoha.Character;
using Konoha.Core;
using Konoha.Data;
using Konoha.Diagnostics;
using Konoha.Input;
using Konoha.Networking;
using Konoha.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Konoha.Editor
{
    public static class SpikeProject
    {
        public const string UnityVersion = "6000.0.60f1";
        public const string Generated = "Assets/Konoha/Generated";
        public const string ScenePath = Generated + "/SpikeArena.unity";

        [MenuItem("Konoha/Prepare Build 0.0.4A (regenerates greybox)")]
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

        // Unity Build Automation resolves the configured Pre-Export method by exact name.
        // Keep this compatibility entry point while the build target is configured with
        // Konoha.Editor.SpikeProject.prepare.
        public static void prepare()
        {
            Prepare();
        }

        private static void ConfigureAndroid()
        {
            PlayerSettings.companyName = "KonohaPrototype";
            PlayerSettings.productName = "KONOHA Spike";
            PlayerSettings.bundleVersion = "0.0.4";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.konoha.powerclash.spike");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = 6;
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

        private static GameObject CreateMatchManagerPrefab()
        {
            const string path = Generated + "/NetworkMatchManager.prefab";

            var root = new GameObject("NetworkMatchManager");
            root.AddComponent<NetworkObject>();
            var match = root.AddComponent<NetworkMatchManager>();
            match.chairPosition = Vector3.zero;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);

            if (prefab == null)
                throw new InvalidOperationException("Failed to create match manager prefab at " + path);

            return prefab;
        }

        private static GameObject CreateNetworkPlayerPrefab()
        {
            const string path = Generated + "/NetworkPlayer.prefab";

            var root = new GameObject("NetworkPlayer");
            root.AddComponent<NetworkObject>();
            var networkTransform = root.AddComponent<OwnerNetworkTransform>();
            networkTransform.Interpolate = true;

            var controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.center = Vector3.up;
            controller.radius = 0.45f;
            controller.stepOffset = 0.2f;
            controller.slopeLimit = 45f;
            controller.minMoveDistance = 0f;

            var motor = root.AddComponent<CharacterMotor>();
            motor.definition = LoadOrCreate<LocomotionDefinition>(Generated + "/Locomotion.asset");

            var movement = root.AddComponent<NetworkPlayerMovement>();
            movement.motor = motor;

            var identity = root.AddComponent<NetworkPlayerIdentity>();
            var combat = root.AddComponent<NetworkPlayerCombat>();

            var neutralMaterial = Material("NetworkPlayerNeutral", new Color(0.72f, 0.78f, 0.84f));
            var facingMaterial = Material("NetworkPlayerFacing", new Color(0.95f, 0.75f, 0.25f));
            var ownerMaterial = Material("NetworkPlayerOwner", new Color(0.35f, 1.00f, 0.45f));

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up;
            body.GetComponent<Renderer>().sharedMaterial = neutralMaterial;
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());

            var facing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            facing.name = "FacingMarker";
            facing.transform.SetParent(root.transform, false);
            facing.transform.localPosition = new Vector3(0f, 1.35f, 0.55f);
            facing.transform.localScale = new Vector3(0.22f, 0.25f, 0.6f);
            facing.GetComponent<Renderer>().sharedMaterial = facingMaterial;
            UnityEngine.Object.DestroyImmediate(facing.GetComponent<Collider>());

            var ownerMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ownerMarker.name = "LocalOwnerMarker";
            ownerMarker.transform.SetParent(root.transform, false);
            ownerMarker.transform.localPosition = new Vector3(0f, 2.65f, 0f);
            ownerMarker.transform.localScale = Vector3.one * 0.32f;
            ownerMarker.GetComponent<Renderer>().sharedMaterial = ownerMaterial;
            UnityEngine.Object.DestroyImmediate(ownerMarker.GetComponent<Collider>());

            var labelObject = new GameObject("OwnershipLabel");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 2.25f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = "PLAYER";
            label.fontSize = 42;
            label.characterSize = 0.055f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
            var labelRenderer = labelObject.GetComponent<MeshRenderer>();
            if (label.font != null && labelRenderer != null)
                labelRenderer.sharedMaterial = label.font.material;

            var healthObject = new GameObject("HealthLabel");
            healthObject.transform.SetParent(root.transform, false);
            healthObject.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            var healthLabel = healthObject.AddComponent<TextMesh>();
            healthLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            healthLabel.text = "WIBAWA 100/100";
            healthLabel.fontSize = 38;
            healthLabel.characterSize = 0.05f;
            healthLabel.anchor = TextAnchor.MiddleCenter;
            healthLabel.alignment = TextAlignment.Center;
            healthLabel.color = new Color(0.55f, 1f, 0.55f);
            var healthRenderer = healthObject.GetComponent<MeshRenderer>();
            if (healthLabel.font != null && healthRenderer != null)
                healthRenderer.sharedMaterial = healthLabel.font.material;

            identity.bodyRenderer = body.GetComponent<Renderer>();
            identity.facingRenderer = facing.GetComponent<Renderer>();
            identity.localOwnerMarker = ownerMarker;
            identity.ownershipLabel = label;
            combat.healthLabel = healthLabel;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);

            if (prefab == null)
                throw new InvalidOperationException("Failed to create network player prefab at " + path);

            return prefab;
        }

        private static void CreateArena()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var ground = Material("Floor", new Color(0.12f, 0.18f, 0.24f));
            var wall = Material("Wall", new Color(0.35f, 0.43f, 0.50f));
            var accent = Material("Marker", new Color(0.95f, 0.57f, 0.15f));
            var heroMaterial = Material("TemporaryHero", new Color(0.15f, 0.9f, 0.8f));
            var cyanMaterial = Material("TeamCyan", NetworkTeamUtility.GetTeamColor(NetworkTeamUtility.CyanTeam));
            var orangeMaterial = Material("TeamOrange", NetworkTeamUtility.GetTeamColor(NetworkTeamUtility.OrangeTeam));
            var chairMaterial = Material("ChairGold", new Color(0.95f, 0.72f, 0.18f));
            var zoneMaterial = Material("ChairZone", new Color(0.22f, 0.42f, 0.48f));
            var networkPlayerPrefab = CreateNetworkPlayerPrefab();
            var matchManagerPrefab = CreateMatchManagerPrefab();
            Box("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(32f, 1f, 24f), ground);
            Box("NorthBoundary", new Vector3(0f, 0.6f, 12f), new Vector3(33f, 1.2f, 1f), wall);
            Box("SouthBoundary", new Vector3(0f, 0.6f, -12f), new Vector3(33f, 1.2f, 1f), wall);
            Box("EastBoundary", new Vector3(16f, 0.6f, 0f), new Vector3(1f, 1.2f, 24f), wall);
            Box("WestBoundary", new Vector3(-16f, 0.6f, 0f), new Vector3(1f, 1.2f, 24f), wall);
            Box("ObstacleA", new Vector3(-5f, 0.6f, 2f), new Vector3(3f, 1.2f, 3f), wall);
            Box("ObstacleB", new Vector3(5f, 0.6f, -2f), new Vector3(3f, 1.2f, 3f), wall);

            var chairZone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chairZone.name = "KursiCaptureZone";
            chairZone.transform.position = new Vector3(0f, 0.025f, 0f);
            chairZone.transform.localScale = new Vector3(
                NetworkMatchManager.CaptureRadius * 2f,
                0.025f,
                NetworkMatchManager.CaptureRadius * 2f);
            chairZone.GetComponent<Renderer>().sharedMaterial = zoneMaterial;
            UnityEngine.Object.DestroyImmediate(chairZone.GetComponent<Collider>());

            var chairSeat = Box("KursiSeat", new Vector3(0f, 0.45f, 0f), new Vector3(1.25f, 0.28f, 1.15f), chairMaterial);
            var chairBack = Box("KursiBack", new Vector3(0f, 1.15f, 0.48f), new Vector3(1.25f, 1.35f, 0.22f), chairMaterial);
            var chairLeftArm = Box("KursiLeftArm", new Vector3(-0.73f, 0.75f, 0f), new Vector3(0.18f, 0.7f, 1.1f), chairMaterial);
            var chairRightArm = Box("KursiRightArm", new Vector3(0.73f, 0.75f, 0f), new Vector3(0.18f, 0.7f, 1.1f), chairMaterial);

            var chairVisualObject = new GameObject("NetworkChairVisual");
            var chairVisual = chairVisualObject.AddComponent<NetworkChairVisual>();
            chairVisual.zoneRenderer = chairZone.GetComponent<Renderer>();
            chairVisual.chairRenderers = new[]
            {
                chairSeat.GetComponent<Renderer>(),
                chairBack.GetComponent<Renderer>(),
                chairLeftArm.GetComponent<Renderer>(),
                chairRightArm.GetComponent<Renderer>()
            };

            for (ulong clientId = 0; clientId < 8; clientId++)
            {
                int team = NetworkTeamUtility.GetTeam(clientId);
                var spawnMarker = Box(
                    "TeamSpawn_" + clientId,
                    NetworkTeamUtility.GetSpawnPosition(clientId) + new Vector3(0f, -0.085f, 0f),
                    new Vector3(1.4f, 0.04f, 1.4f),
                    team == NetworkTeamUtility.CyanTeam ? cyanMaterial : orangeMaterial);
                UnityEngine.Object.DestroyImmediate(spawnMarker.GetComponent<Collider>());
            }

            for (int x = -10; x <= 10; x += 5)
            {
                var marker = Box("NavigationMarker", new Vector3(x, 0.006f, 6f), new Vector3(1f, 0.01f, 1f), accent);
                UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
            }
            var hero = new GameObject("TemporaryPlayableCharacter");
            hero.transform.position = new Vector3(0f, 0.1f, -4f);
            var controller = hero.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.center = Vector3.up;
            controller.radius = 0.45f;
            controller.stepOffset = 0.2f;
            controller.slopeLimit = 45f;
            controller.minMoveDistance = 0f;
            var model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = "PlaceholderSilhouette";
            model.transform.SetParent(hero.transform, false);
            model.transform.localPosition = Vector3.up;
            model.GetComponent<Renderer>().sharedMaterial = heroMaterial;
            UnityEngine.Object.DestroyImmediate(model.GetComponent<Collider>());
            var nose = Box("FacingMarker", Vector3.zero, new Vector3(0.22f, 0.25f, 0.6f), accent);
            UnityEngine.Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.transform.SetParent(hero.transform, false);
            nose.transform.localPosition = new Vector3(0f, 1.35f, 0.5f);
            var motor = hero.AddComponent<CharacterMotor>();
            motor.definition = LoadOrCreate<LocomotionDefinition>(Generated + "/Locomotion.asset");

            var cameraObject = new GameObject("MobileCamera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.07f, 0.1f);
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            var follow = cameraObject.AddComponent<MobileCombatCamera>();
            follow.target = hero.transform;
            cameraObject.transform.position = hero.transform.position + follow.offset;
            cameraObject.transform.rotation = Quaternion.LookRotation(-follow.offset);
            var light = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.7f);

            var driverObject = new GameObject("OfflineSpikeDriver");
            var driver = driverObject.AddComponent<OfflineSpikeDriver>();
            driver.motor = motor;
            var joystick = CreateHud(motor, hero, driverObject, networkPlayerPrefab, matchManagerPrefab);
            driver.joystick = joystick;
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

        private static TouchJoystick CreateHud(
            CharacterMotor motor,
            GameObject offlineHero,
            GameObject offlineDriver,
            GameObject networkPlayerPrefab,
            GameObject matchManagerPrefab)
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
            Label(Rect("Instruction", safe, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 15f), new Vector2(560f, 32f)),
                "0.0.4A | Rebut Kursi Greybox Match Loop", 21).alignment = TextAnchor.MiddleCenter;

            Button MakeActionButton(string name, string caption, Vector2 position, Vector2 size)
            {
                var rect = Rect(name, safe, new Vector2(1f, 0f), new Vector2(1f, 0f), position, size);
                var image = rect.gameObject.AddComponent<Image>();
                image.color = new Color(0.12f, 0.34f, 0.40f, 0.96f);
                image.raycastTarget = true;

                var actionButton = rect.gameObject.AddComponent<Button>();
                actionButton.targetGraphic = image;
                actionButton.interactable = false;

                var actionText = Label(Rect("Label", rect, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, size), caption, 18);
                actionText.alignment = TextAnchor.MiddleCenter;
                actionText.raycastTarget = false;
                return actionButton;
            }

            var dodgeButton = MakeActionButton("DodgeButton", "DODGE", new Vector2(-250f, 78f), new Vector2(150f, 66f));
            var attackButton = MakeActionButton("AttackButton", "ATTACK", new Vector2(-84f, 78f), new Vector2(150f, 66f));
            var chairButton = MakeActionButton("ChairButton", "DUDUK", new Vector2(-84f, 158f), new Vector2(150f, 58f));

            CreateMatchHud(safe, chairButton);
            CreateNetworkingProof(safe, networkPlayerPrefab, matchManagerPrefab, offlineHero, offlineDriver);

            // Keep gameplay actions above the rest of the HUD.
            dodgeButton.transform.SetAsLastSibling();
            attackButton.transform.SetAsLastSibling();
            chairButton.transform.SetAsLastSibling();
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

        private static void CreateMatchHud(RectTransform safe, Button chairButton)
        {
            var panel = Rect(
                "MatchPanel",
                safe,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(20f, -18f),
                new Vector2(720f, 145f));
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.035f, 0.07f, 0.10f, 0.90f);
            panelImage.raycastTarget = false;

            var matchText = Label(
                Rect("MatchState", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -10f), new Vector2(690f, 32f)),
                "REBUT KURSI | WAITING", 18);
            matchText.alignment = TextAnchor.MiddleLeft;

            var scoreText = Label(
                Rect("MatchScore", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -46f), new Vector2(690f, 30f)),
                "POWER CYAN 0/100 | ORANGE 0/100", 18);
            scoreText.alignment = TextAnchor.MiddleLeft;

            var objectiveText = Label(
                Rect("ObjectiveState", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -80f), new Vector2(690f, 54f)),
                "KURSI: NETRAL", 17);
            objectiveText.alignment = TextAnchor.UpperLeft;

            var startRect = Rect(
                "StartMatchButton",
                safe,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(20f, -175f),
                new Vector2(175f, 52f));
            var startImage = startRect.gameObject.AddComponent<Image>();
            startImage.color = new Color(0.12f, 0.34f, 0.40f, 0.96f);
            startImage.raycastTarget = true;
            var startButton = startRect.gameObject.AddComponent<Button>();
            startButton.targetGraphic = startImage;
            startButton.interactable = false;
            var startLabel = Label(
                Rect("Label", startRect, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, startRect.sizeDelta),
                "START MATCH", 17);
            startLabel.alignment = TextAnchor.MiddleCenter;

            var matchHud = safe.gameObject.AddComponent<NetworkMatchHud>();
            matchHud.matchText = matchText;
            matchHud.scoreText = scoreText;
            matchHud.objectiveText = objectiveText;
            matchHud.startButton = startButton;
            matchHud.chairButton = chairButton;

            panel.transform.SetAsFirstSibling();
        }

        private static void CreateNetworkingProof(
            RectTransform safe,
            GameObject playerPrefab,
            GameObject matchPrefab,
            GameObject offlineHero,
            GameObject offlineDriver)
        {
            var networkObject = new GameObject("RuntimeNetworkingProof", typeof(NetworkManager), typeof(UnityTransport), typeof(RuntimeNetworkingProof));
            var proof = networkObject.GetComponent<RuntimeNetworkingProof>();
            proof.playerPrefab = playerPrefab;
            proof.matchPrefab = matchPrefab;
            proof.offlineHero = offlineHero;
            proof.offlineDriver = offlineDriver;

            var panel = Rect("NetworkPanel", safe, new Vector2(1f, 1f), Vector2.one, new Vector2(-20f, -92f), new Vector2(470f, 185f));
            panel.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.09f, 0.13f, 0.9f);

            var statusRect = Rect("NetworkStatus", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(440f, 34f));
            var status = Label(statusRect, "NETWORK BOOTING...", 18);
            status.alignment = TextAnchor.MiddleCenter;

            var addressRect = Rect("Address", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(440f, 40f));
            addressRect.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.18f, 0.22f, 1f);
            var input = addressRect.gameObject.AddComponent<InputField>();
            var inputText = Label(Rect("Text", addressRect, Vector2.zero, Vector2.zero, new Vector2(10f, 5f), new Vector2(420f, 30f)), "127.0.0.1", 18);
            inputText.alignment = TextAnchor.MiddleLeft;
            input.textComponent = inputText;
            input.text = "127.0.0.1";

            Button MakeButton(string name, string caption, float x)
            {
                var rect = Rect(name, panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 12f), new Vector2(135f, 52f));
                rect.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.3f, 0.35f, 0.95f);
                var button = rect.gameObject.AddComponent<Button>();
                var text = Label(Rect("Label", rect, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, rect.sizeDelta), caption, 18);
                text.alignment = TextAnchor.MiddleCenter;
                return button;
            }

            proof.status = status;
            proof.addressInput = input;
            proof.hostButton = MakeButton("Host", "HOST", -145f);
            proof.clientButton = MakeButton("Client", "CLIENT", 0f);
            proof.shutdownButton = MakeButton("Shutdown", "STOP", 145f);

            var legend = Label(
                Rect("NetworkLegend", safe, Vector2.one, Vector2.one, new Vector2(-20f, -292f), new Vector2(470f, 54f)),
                "WIBAWA 0 = RUNTUH | RESPAWN 6s | KURSI +1 POWER/DETIK | FIRST 100 POWER WINS",
                16);
            legend.alignment = TextAnchor.MiddleCenter;

            proof.Initialize();
        }
    }
}
