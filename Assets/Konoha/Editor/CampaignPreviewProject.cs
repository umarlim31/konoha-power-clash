using System;
using Konoha.Campaign;
using Konoha.Character;
using Konoha.Data;
using Konoha.Diagnostics;
using Konoha.Networking;
using Konoha.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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

        [MenuItem("Konoha/Prepare Jalur Takhta Preview 0.0.9.2")]
        public static void Prepare()
        {
            // Generate a separate scene using the same Android, URP, input, camera and
            // networked hero prefab as 4v4. Do not change its existing scene or prefabs.
            SpikeProject.Prepare();
            var scene = EditorSceneManager.OpenScene(SpikeProject.ScenePath, OpenSceneMode.Single);
            PlayerSettings.productName = "KONOHA Jalur Takhta Preview";
            PlayerSettings.bundleVersion = "0.0.9.2";
            PlayerSettings.Android.bundleVersionCode = 23;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.konoha.powerclash.jalurtakhta");

            // The PvP host/client panel is replaced by CampaignSession (automatic local host).
            var network = GameObject.Find("RuntimeNetworkingProof");
            if (network != null) UnityEngine.Object.DestroyImmediate(network);
            var player = UnityEngine.Object.FindFirstObjectByType<CharacterMotor>();
            var canvas = GameObject.Find("TouchCanvas");
            if (player == null || canvas == null)
                throw new InvalidOperationException("Expected offline player and mobile canvas from SpikeProject.");
            player.transform.position = new Vector3(0f, 0.1f, -9f);
            var follow = UnityEngine.Object.FindFirstObjectByType<MobileCombatCamera>();
            if (follow != null)
            {
                follow.offset = new Vector3(0f, 13.8f, -17.5f);
                follow.crowdedOffset = follow.offset;
                follow.limitFocusToArena = false;
                follow.allowOrbit = true;
                // A lower default pitch keeps the distant Istana Takhta in frame from the spawn
                // (vertical FOV 50: the frame top sits 3 degrees above the horizon).
                follow.resetPitch = 22f;
                follow.minPitch = 16f;
                follow.resetDistance = 22f;
                follow.ResetOrbit();
                follow.focusXLimits = new Vector2(-9f, 9f);
                follow.focusZLimits = new Vector2(-7.5f, 7f);
            }
            var previewCamera = Camera.main;
            if (previewCamera != null) previewCamera.fieldOfView = 50f;

            var debug = canvas.GetComponent<SpikeDebugHud>();
            if (debug != null) UnityEngine.Object.DestroyImmediate(debug);
            var feed = canvas.GetComponent<CombatFeedHud>();
            if (feed != null) UnityEngine.Object.DestroyImmediate(feed);
            var safe = canvas.transform.Find("SafeArea");
            var matchHud = safe.GetComponent<NetworkMatchHud>();
            if (matchHud != null) UnityEngine.Object.DestroyImmediate(matchHud);
            // The PvP chair script recolours the seat grey when no network match is
            // present. The solo objective must retain its gold focal colour.
            var networkChair = UnityEngine.Object.FindFirstObjectByType<NetworkChairVisual>();
            if (networkChair != null) UnityEngine.Object.DestroyImmediate(networkChair);
            var pad = safe.Find("MovePad");
            for (int i = safe.childCount - 1; i >= 0; i--)
                if (safe.GetChild(i) != pad) UnityEngine.Object.DestroyImmediate(safe.GetChild(i).gameObject);
            var layout = canvas.GetComponent<SafeAreaLayout>();
            layout.safeRoot = (RectTransform)safe;
            layout.joystick = (RectTransform)pad;
            layout.compactJoystick = true;

            // The offline character stays in the scene as a collision probe for editor tests;
            // CampaignSession hides it once the networked hero exists.
            var offline = UnityEngine.Object.FindFirstObjectByType<Konoha.Core.OfflineSpikeDriver>();
            offline.enabled = false;
            var traversal = new GameObject("CampaignLocomotion").AddComponent<CampaignTraversal>();
            traversal.motor = player;
            traversal.joystick = pad.GetComponent<Konoha.Input.TouchJoystick>();
            traversal.movementCamera = previewCamera.transform;
            traversal.boundaryCenter = CampaignCapitalArt.BoundaryCenter;
            traversal.boundaryRadii = CampaignCapitalArt.BoundaryRadii;
            player.allowJump = true;
            player.jumpHeight = 1.25f;
            player.GetComponent<CharacterController>().stepOffset = .25f;

            var dragObject = new GameObject("Camera drag surface", typeof(RectTransform), typeof(Image), typeof(CampaignCameraDrag));
            var dragRect = (RectTransform)dragObject.transform;
            dragRect.SetParent(safe, false);
            dragRect.anchorMin = new Vector2(.35f, 0f);
            dragRect.anchorMax = Vector2.one;
            dragRect.offsetMin = dragRect.offsetMax = Vector2.zero;
            dragRect.SetAsFirstSibling();
            dragObject.GetComponent<Image>().color = Color.clear;
            dragObject.GetComponent<CampaignCameraDrag>().follow = follow;

            var capital = CampaignCapitalArt.Build();
            var gold = capital.Gold;
            var gateColor = capital.Red;
            // The seat stands on the Istana Takhta terrace at the north end of the route.
            var chair = new GameObject("Kursi Kekuasaan");
            chair.transform.position = capital.Throne.position;
            Vector3 seat = chair.transform.position;
            var barrier = new GameObject("Gerbang Takhta - locked");
            GateSeal("North Gate", seat + new Vector3(0, 0, 2.35f), false, barrier.transform, gateColor, gold);
            GateSeal("South Gate", seat + new Vector3(0, 0, -2.35f), false, barrier.transform, gateColor, gold);
            GateSeal("East Gate", seat + new Vector3(2.35f, 0, 0), true, barrier.transform, gateColor, gold);
            GateSeal("West Gate", seat + new Vector3(-2.35f, 0, 0), true, barrier.transform, gateColor, gold);

            var placeholder = player.transform.Find("PlaceholderSilhouette");
            if (placeholder != null)
                placeholder.GetComponent<Renderer>().sharedMaterial = Mat("CampaignHeroBody", new Color(0.17f, 0.20f, 0.24f));
            var oldFacing = player.transform.Find("FacingMarker");
            if (oldFacing != null) UnityEngine.Object.DestroyImmediate(oldFacing.gameObject);

            var headingBackdrop = new GameObject("Objective backdrop", typeof(RectTransform), typeof(Image));
            var backdropRect = (RectTransform)headingBackdrop.transform;
            backdropRect.SetParent(safe, false);
            backdropRect.anchorMin = backdropRect.anchorMax = new Vector2(0.5f, 1f);
            backdropRect.pivot = new Vector2(0.5f, 1f);
            backdropRect.anchoredPosition = new Vector2(0f, -6f);
            backdropRect.sizeDelta = new Vector2(600f, 95f);
            headingBackdrop.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.10f, 0.84f);
            headingBackdrop.GetComponent<Image>().raycastTarget = false;
            var objective = Text("CampaignObjective", safe, new Vector2(0.5f, 1f),
                new Vector2(0, -11), new Vector2(560, 34), 18);
            objective.alignment = TextAnchor.MiddleCenter;
            objective.color = new Color(1f, 0.89f, 0.65f);
            objective.gameObject.AddComponent<Outline>().effectColor = new Color(0.10f, 0.10f, 0.10f, 0.85f);
            var status = Text("CampaignStatus", safe, new Vector2(0.5f, 1f),
                new Vector2(0, -47), new Vector2(560, 24), 16);
            status.alignment = TextAnchor.MiddleCenter;
            status.gameObject.AddComponent<Outline>().effectColor = Color.black;
            var progressTrack = new GameObject("ObjectiveProgressTrack", typeof(RectTransform), typeof(Image));
            var progressRect = (RectTransform)progressTrack.transform;
            progressRect.SetParent(safe, false);
            progressRect.anchorMin = progressRect.anchorMax = new Vector2(0.5f, 1f);
            progressRect.pivot = new Vector2(0.5f, 1f);
            progressRect.anchoredPosition = new Vector2(0f, -78f);
            progressRect.sizeDelta = new Vector2(480f, 7f);
            progressTrack.GetComponent<Image>().color = new Color(0.22f, 0.22f, 0.21f, 0.9f);
            progressTrack.GetComponent<Image>().raycastTarget = false;
            var progressFill = new GameObject("ObjectiveProgressFill", typeof(RectTransform), typeof(Image));
            var progressFillRect = (RectTransform)progressFill.transform;
            progressFillRect.SetParent(progressRect, false);
            progressFillRect.anchorMin = Vector2.zero;
            progressFillRect.anchorMax = Vector2.one;
            progressFillRect.offsetMin = progressFillRect.offsetMax = Vector2.zero;
            var fillImage = progressFill.GetComponent<Image>();
            fillImage.color = new Color(0.88f, 0.65f, 0.31f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 0f;
            fillImage.raycastTarget = false;
            var waypoint = Text("CampaignWaypoint", safe, new Vector2(0.5f, 1f),
                new Vector2(0, -108), new Vector2(700, 30), 18);
            waypoint.alignment = TextAnchor.MiddleCenter;
            waypoint.color = new Color(1f, 0.86f, 0.58f);
            waypoint.gameObject.AddComponent<Outline>().effectColor = Color.black;
            var feedback = Text("CampaignFeedback", safe, new Vector2(0.5f, 0.5f),
                new Vector2(0, 165), new Vector2(720, 48), 23);
            feedback.alignment = TextAnchor.MiddleCenter;
            feedback.color = new Color(1f, 0.82f, 0.42f);
            feedback.gameObject.AddComponent<Outline>().effectColor = Color.black;
            var heroText = Text("CampaignHero", safe, new Vector2(0f, 1f),
                new Vector2(14, -128), new Vector2(430, 28), 16);
            heroText.alignment = TextAnchor.MiddleLeft;
            heroText.gameObject.AddComponent<Outline>().effectColor = Color.black;

            // Real hero controls. The names are the ones the networked hero components bind
            // to (NetworkPlayerCombat, NetworkHeroKit, NetworkPlayerMovement).
            var basic = Button("AttackButton", "BASIC", safe, new Vector2(1f, 0f),
                new Vector2(-24, 30), new Vector2(150, 96));
            var ultimate = Button("UltimateButton", "ULT 0%", safe, new Vector2(1f, 0f),
                new Vector2(-24, 136), new Vector2(150, 60));
            var s1 = Button("S1Button", "S1", safe, new Vector2(1f, 0f),
                new Vector2(-186, 30), new Vector2(140, 60));
            var s2 = Button("S2Button", "S2", safe, new Vector2(1f, 0f),
                new Vector2(-186, 100), new Vector2(140, 60));
            var dodge = Button("DodgeButton", "DODGE", safe, new Vector2(1f, 0f),
                new Vector2(-338, 30), new Vector2(140, 60));
            var jump = Button("CampaignJump", "LOMPAT", safe, new Vector2(1f, 0f),
                new Vector2(-338, 100), new Vector2(140, 60));
            var sit = Button("CampaignSit", "DUDUK", safe, new Vector2(1f, 0f),
                new Vector2(-186, 170), new Vector2(140, 56));
            var heroButton = Button("HeroButton", "GANTI HERO", safe, new Vector2(0f, 1f),
                new Vector2(14, -66), new Vector2(164, 52));
            foreach (var small in new[] { ultimate, s1, s2, dodge, jump, sit, heroButton })
                small.GetComponentInChildren<Text>().fontSize = 17;
            basic.GetComponent<Image>().color = new Color(0.50f, 0.16f, 0.18f, 0.96f);
            sit.GetComponent<Image>().color = new Color(0.59f, 0.42f, 0.18f, 0.96f);
            jump.GetComponent<Image>().color = new Color(.24f, .40f, .36f, .96f);
            dodge.GetComponent<Image>().color = new Color(0.18f, 0.25f, 0.29f, 0.96f);
            heroButton.GetComponent<Image>().color = new Color(0.18f, 0.25f, 0.29f, 0.96f);
            traversal.jumpButton = jump;

            var resetCamera = Button("ResetCamera", "KAMERA AWAL", safe, new Vector2(1f, 1f),
                new Vector2(-14, -14), new Vector2(164, 44));
            resetCamera.GetComponentInChildren<Text>().fontSize = 16;
            var cameraReset = resetCamera.gameObject.AddComponent<CampaignCameraReset>();
            cameraReset.follow = follow;
            var gestureHint = Text("CameraGestureHint", safe, new Vector2(1f,0f),
                new Vector2(-20,236), new Vector2(305,28), 13);
            gestureHint.alignment = TextAnchor.MiddleRight;
            gestureHint.text = "GESER LAYAR KANAN: KAMERA • CUBIT: ZOOM";
            var footer = Text("CampaignRevision", safe, new Vector2(0.5f, 0f),
                new Vector2(0, 6), new Vector2(360, 22), 13);
            footer.alignment = TextAnchor.MiddleCenter;
            footer.text = "JALUR TAKHTA 0.0.9.2  •  SOLO PREVIEW";

            var stage = new GameObject("CampaignStage").AddComponent<CampaignStage>();
            stage.plaza = capital.Plaza;
            stage.majelis = capital.Majelis;
            stage.biro = capital.Biro;
            stage.garda = capital.Garda;
            stage.chair = chair.transform;
            stage.startPoint = CampaignCapitalArt.SpawnPoint;
            stage.terraceHeight = CampaignCapitalArt.TerraceHeight;
            stage.innerGateZ = CampaignCapitalArt.InnerGateZ;

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpikeProject.Generated + "/NetworkPlayer.prefab");
            if (playerPrefab == null)
                throw new InvalidOperationException("NetworkPlayer.prefab is required for the campaign host.");
            var enemyPrefab = CreateEnemyPrefab();
            var directorPrefab = CreateDirectorPrefab(enemyPrefab);

            var networkObject = new GameObject("CampaignNetwork", typeof(NetworkManager), typeof(UnityTransport), typeof(CampaignSession));
            var session = networkObject.GetComponent<CampaignSession>();
            session.playerPrefab = playerPrefab;
            session.directorPrefab = directorPrefab;
            session.enemyPrefab = enemyPrefab;
            session.stage = stage;
            session.traversal = traversal;
            session.monument = UnityEngine.Object.FindFirstObjectByType<CampaignMonument>();
            session.movementCamera = previewCamera.transform;
            session.offlineHero = player.gameObject;

            var preview = new GameObject("JalurTakhtaPreview").AddComponent<CampaignPreviewController>();
            preview.stage = stage;
            preview.chairBarrier = barrier;
            preview.innerGateClosed = capital.InnerGateClosed;
            preview.innerGateOpen = capital.InnerGateOpen;
            preview.objectiveText = objective;
            preview.statusText = status;
            preview.waypointText = waypoint;
            preview.heroText = heroText;
            preview.feedbackText = feedback;
            preview.objectiveProgress = fillImage;
            preview.sitButton = sit;

            var viewButton = Button("ArenaViewButton", "LIHAT ARENA", safe, new Vector2(0f, 1f),
                new Vector2(14f, -14f), new Vector2(164f, 44f));
            viewButton.GetComponentInChildren<Text>().fontSize = 16;
            var arenaView = preview.gameObject.AddComponent<CampaignArenaView>();
            arenaView.follow = follow;
            arenaView.campaign = preview;
            arenaView.traversal = traversal;
            arenaView.safeRoot = safe;
            arenaView.viewButton = viewButton;
            arenaView.viewFocus = new Vector3(0f, 2f, 4f);
            arenaView.viewOffset = new Vector3(0f, 62f, -70f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Jalur Takhta preview prepared: " + ScenePath);
        }

        // Server-driven organisation member. Shares the hero combat components so every
        // ability works on it; role/faction colours and scale are applied at runtime.
        private static GameObject CreateEnemyPrefab()
        {
            const string path = SpikeProject.Generated + "/CampaignEnemy.prefab";

            var root = new GameObject("CampaignEnemy");
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
            motor.definition = AssetDatabase.LoadAssetAtPath<LocomotionDefinition>(SpikeProject.Generated + "/Locomotion.asset");
            if (motor.definition == null)
                throw new InvalidOperationException("Locomotion.asset is required for campaign enemies.");

            root.AddComponent<NetworkHeroKit>();
            var combat = root.AddComponent<NetworkPlayerCombat>();
            combat.healthLabel = null;
            var enemy = root.AddComponent<CampaignEnemy>();
            enemy.motor = motor;

            var body = Mat("CampaignEnemyBody", new Color(0.30f, 0.32f, 0.36f));
            var accent = Mat("CampaignEnemyAccent", new Color(0.86f, 0.66f, 0.28f));
            var ringMaterial = Mat("CampaignEnemyRing", new Color(0.62f, 0.12f, 0.16f));

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var capsule = Part("Body", PrimitiveType.Capsule, visual.transform, Vector3.up, Vector3.one, body);
            var sash = Part("Sash", PrimitiveType.Cube, visual.transform, new Vector3(0f, 1.18f, 0.43f),
                new Vector3(0.52f, 0.72f, 0.08f), accent);
            Part("Crest", PrimitiveType.Cube, visual.transform, new Vector3(0f, 2.06f, 0f),
                new Vector3(0.34f, 0.16f, 0.34f), accent);
            Part("ShoulderL", PrimitiveType.Cube, visual.transform, new Vector3(-0.50f, 1.50f, 0f),
                new Vector3(0.26f, 0.14f, 0.40f), accent);
            Part("ShoulderR", PrimitiveType.Cube, visual.transform, new Vector3(0.50f, 1.50f, 0f),
                new Vector3(0.26f, 0.14f, 0.40f), accent);
            // Raised above the thin floor inlays (corridor top .043 m) so it is never hidden.
            Part("SistemRing", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.075f, 0f),
                new Vector3(1.2f, 0.012f, 1.2f), ringMaterial);

            // 0.0.9.2 Majelis Daun readability (§8.1). Shared opaque materials, no lights.
            var blockMaterial = Mat("CampaignMajelisBlock", new Color(0.46f, 0.08f, 0.16f));
            var gavelWood = Mat("CampaignGavelWood", new Color(0.36f, 0.21f, 0.11f));
            var warnOuter = Mat("CampaignTelegraphOuter", new Color(0.36f, 0.05f, 0.05f));
            var warnFill = Mat("CampaignTelegraphFill", new Color(0.95f, 0.30f, 0.12f));

            // Voting block: a wider burgundy disc under the red Sistem ring.
            var blockRing = Part("BlockRing", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.05f, 0f),
                new Vector3(1.9f, 0.006f, 1.9f), blockMaterial);
            blockRing.SetActive(false);

            // Ketua's gavel on the right hand; the pivot swings it overhead during KETOK PALU.
            var gavelPivot = new GameObject("GavelPivot");
            gavelPivot.transform.SetParent(visual.transform, false);
            gavelPivot.transform.localPosition = new Vector3(0.62f, 1.35f, 0.15f);
            Part("GavelHandle", PrimitiveType.Cylinder, gavelPivot.transform, new Vector3(0f, 0f, 0.38f),
                new Vector3(0.07f, 0.38f, 0.07f), gavelWood).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part("GavelHead", PrimitiveType.Cylinder, gavelPivot.transform, new Vector3(0f, 0f, 0.78f),
                new Vector3(0.20f, 0.17f, 0.20f), gavelWood);
            Part("GavelBand", PrimitiveType.Cylinder, gavelPivot.transform, new Vector3(0f, 0f, 0.78f),
                new Vector3(0.21f, 0.04f, 0.21f), accent);
            gavelPivot.SetActive(false);

            // KETOK PALU warning circle; placed in world space by CampaignEnemy while active.
            var telegraphOuter = Part("KetokPaluWarning", PrimitiveType.Cylinder, root.transform, Vector3.zero,
                new Vector3(6f, 0.006f, 6f), warnOuter);
            var telegraphFill = Part("KetokPaluFill", PrimitiveType.Cylinder, root.transform, Vector3.zero,
                new Vector3(0.3f, 0.006f, 0.3f), warnFill);
            foreach (var disc in new[] { blockRing, telegraphOuter, telegraphFill })
                disc.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            telegraphOuter.SetActive(false);
            telegraphFill.SetActive(false);

            var labelObject = new GameObject("Nameplate");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 3.0f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = "SISTEM";
            label.fontSize = 32;
            label.characterSize = 0.045f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
            var labelRenderer = labelObject.GetComponent<MeshRenderer>();
            if (label.font != null && labelRenderer != null)
                labelRenderer.sharedMaterial = label.font.material;

            enemy.visualRoot = visual.transform;
            enemy.bodyRenderer = capsule.GetComponent<Renderer>();
            enemy.accentRenderer = sash.GetComponent<Renderer>();
            enemy.nameplate = label;
            enemy.blockRing = blockRing;
            enemy.gavel = gavelPivot;
            enemy.gavelPivot = gavelPivot.transform;
            enemy.telegraphOuter = telegraphOuter.transform;
            enemy.telegraphFill = telegraphFill.transform;
            SpikeProject.CreateWorldWibawaBar(root);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
                throw new InvalidOperationException("Failed to create campaign enemy prefab at " + path);
            return prefab;
        }

        private static GameObject CreateDirectorPrefab(GameObject enemyPrefab)
        {
            const string path = SpikeProject.Generated + "/CampaignDirector.prefab";

            var root = new GameObject("CampaignDirector");
            root.AddComponent<NetworkObject>();
            root.AddComponent<CampaignDirector>().enemyPrefab = enemyPrefab;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
                throw new InvalidOperationException("Failed to create campaign director prefab at " + path);
            return prefab;
        }

        private static GameObject Part(string name, PrimitiveType primitive, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }

        private static void GateSeal(string name, Vector3 center, bool alongZ,
            Transform parent, Material red, Material gold)
        {
            Vector3 extent = alongZ ? new Vector3(0.26f, 4f, 5.1f)
                : new Vector3(5.1f, 4f, 0.26f);
            var collision = Box(name, center + Vector3.up * 2f, extent, red);
            collision.transform.SetParent(parent);
            collision.layer = 2; // Ignore Raycast: camera orbit should not zoom into invisible gate seals.
            collision.GetComponent<Renderer>().enabled = false;
            Vector3 thin = alongZ ? new Vector3(0.10f, 0.10f, 5.1f)
                : new Vector3(5.1f, 0.10f, 0.10f);
            for (int i = 0; i < 2; i++)
            {
                var seal = Deco(name + " ritual seal", center + Vector3.up * (0.48f + i * 0.54f),
                    thin, i == 0 ? red : gold);
                seal.transform.SetParent(parent);
            }
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
