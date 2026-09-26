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

        [MenuItem("Konoha/Prepare Jalur Takhta Preview 0.0.8.2")]
        public static void Prepare()
        {
            // Generate a separate scene using the same Android, URP, input, camera and
            // prototype hero silhouettes as 4v4. Do not change its existing scene.
            SpikeProject.Prepare();
            var scene = EditorSceneManager.OpenScene(SpikeProject.ScenePath, OpenSceneMode.Single);
            PlayerSettings.productName = "KONOHA Jalur Takhta Preview";
            PlayerSettings.bundleVersion = "0.0.8.2";
            PlayerSettings.Android.bundleVersionCode = 19;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.konoha.powerclash.jalurtakhta");

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

            // Replace only the offline solo input driver. PvP prefabs keep their original controls.
            var offline = UnityEngine.Object.FindFirstObjectByType<Konoha.Core.OfflineSpikeDriver>();
            offline.enabled = false;
            var traversal = player.gameObject.AddComponent<CampaignTraversal>();
            traversal.motor = player;
            traversal.joystick = pad.GetComponent<Konoha.Input.TouchJoystick>();
            traversal.movementCamera = previewCamera.transform;
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
            var guardColor = Mat("CampaignGuard", new Color(0.24f, 0.29f, 0.36f));
            var plaza = capital.Plaza;
            var majelis = capital.Majelis;
            var biro = capital.Biro;
            var garda = capital.Garda;
            var chair = new GameObject("Kursi Kekuasaan");
            var barrier = new GameObject("Gerbang Takhta - locked");
            GateSeal("North Gate", new Vector3(0, 0, 2.35f), false, barrier.transform, gateColor, gold);
            GateSeal("South Gate", new Vector3(0, 0, -2.35f), false, barrier.transform, gateColor, gold);
            GateSeal("East Gate", new Vector3(2.35f, 0, 0), true, barrier.transform, gateColor, gold);
            GateSeal("West Gate", new Vector3(-2.35f, 0, 0), true, barrier.transform, gateColor, gold);

            var guard = new GameObject("GardaTakhta");
            guard.transform.position = garda.position;
            var guardBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            guardBody.name = "Guard Silhouette";
            guardBody.transform.SetParent(guard.transform, false);
            guardBody.transform.localPosition = Vector3.up;
            guardBody.GetComponent<Renderer>().sharedMaterial = guardColor;
            UnityEngine.Object.DestroyImmediate(guardBody.GetComponent<Collider>());
            var guardCrest = Deco("Guard crest", Vector3.zero, new Vector3(0.26f, 0.30f, 0.17f), gateColor);
            guardCrest.transform.SetParent(guard.transform, false);
            guardCrest.transform.localPosition = new Vector3(0f, 2.07f, 0.22f);
            var guardShield = Deco("Guard shield", Vector3.zero, new Vector3(0.75f, 0.92f, 0.15f), gold);
            guardShield.transform.SetParent(guard.transform, false);
            guardShield.transform.localPosition = new Vector3(-0.56f, 1.02f, 0.28f);
            var guardCape = Deco("Guard cape", Vector3.zero, new Vector3(1.08f, 1.05f, 0.12f), gateColor);
            guardCape.transform.SetParent(guard.transform, false);
            guardCape.transform.localPosition = new Vector3(0f, 1.03f, -0.42f);
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
                visuals[i].transform.localScale = Vector3.one * 1.12f;
            }
            var placeholder = player.transform.Find("PlaceholderSilhouette");
            if (placeholder != null)
                placeholder.GetComponent<Renderer>().sharedMaterial = Mat("CampaignHeroBody", new Color(0.17f, 0.20f, 0.24f));
            var oldFacing = player.transform.Find("FacingMarker");
            if (oldFacing != null) UnityEngine.Object.DestroyImmediate(oldFacing.gameObject);
            var selection = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            selection.name = "Hero Ground Marker";
            selection.transform.SetParent(player.transform, false);
            selection.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            selection.transform.localScale = new Vector3(1.65f, 0.012f, 1.65f);
            selection.GetComponent<Renderer>().sharedMaterial = gold;
            UnityEngine.Object.DestroyImmediate(selection.GetComponent<Collider>());

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
            var heroText = Text("CampaignHero", safe, new Vector2(0.5f, 0f),
                new Vector2(0, 42), new Vector2(480, 34), 18);
            heroText.alignment = TextAnchor.MiddleCenter;
            var heroButton = Button("HeroSelector", "GANTI HERO", safe, new Vector2(1f, 0f),
                new Vector2(-226, 196), new Vector2(136, 48));
            var skill = Button("CampaignSkill", "PERISAI", safe, new Vector2(1f, 0f),
                new Vector2(-74, 196), new Vector2(136, 48));
            var basic = Button("CampaignBasic", "BASIC", safe, new Vector2(1f, 0f),
                new Vector2(-74, 132), new Vector2(136, 54));
            var sit = Button("CampaignSit", "DUDUK", safe, new Vector2(1f, 0f),
                new Vector2(-226, 132), new Vector2(136, 54));
            skill.GetComponent<Image>().color = new Color(0.50f, 0.16f, 0.18f, 0.96f);
            sit.GetComponent<Image>().color = new Color(0.59f, 0.42f, 0.18f, 0.96f);
            heroButton.GetComponent<Image>().color = new Color(0.18f, 0.25f, 0.29f, 0.96f);
            var jump = Button("CampaignJump", "LOMPAT", safe, new Vector2(1f, 0f),
                new Vector2(-74, 65), new Vector2(136, 54));
            jump.GetComponent<Image>().color = new Color(.24f,.40f,.36f,.96f);
            traversal.jumpButton = jump;
            var resetCamera = Button("ResetCamera", "KAMERA AWAL", safe, new Vector2(1f, 1f),
                new Vector2(-14, -14), new Vector2(164, 44));
            resetCamera.GetComponentInChildren<Text>().fontSize = 16;
            var cameraReset = resetCamera.gameObject.AddComponent<CampaignCameraReset>();
            cameraReset.follow = follow;
            var gestureHint = Text("CameraGestureHint", safe, new Vector2(1f,0f),
                new Vector2(-20,265), new Vector2(305,28), 13);
            gestureHint.alignment = TextAnchor.MiddleRight;
            gestureHint.text = "GESER LAYAR KANAN: KAMERA • CUBIT: ZOOM";
            var footer = Text("CampaignRevision", safe, new Vector2(0.5f, 0f),
                new Vector2(0, 10), new Vector2(560, 26), 13);
            footer.alignment = TextAnchor.MiddleCenter;
            footer.text = "JALUR TAKHTA 0.0.8.2  •  SOLO PREVIEW";

            var preview = new GameObject("JalurTakhtaPreview").AddComponent<CampaignPreviewController>();
            preview.player = player;
            preview.centralMonument = UnityEngine.Object.FindFirstObjectByType<CampaignMonument>();
            preview.plaza = plaza;
            preview.majelis = majelis;
            preview.biro = biro;
            preview.garda = garda;
            preview.chair = chair.transform;
            preview.chairBarrier = barrier;
            preview.guardVisual = guard;
            preview.guardRenderer = guardBody.GetComponent<Renderer>();
            preview.heroMarker = selection.transform;
            preview.heroVisuals = visuals;
            preview.objectiveText = objective;
            preview.statusText = status;
            preview.waypointText = waypoint;
            preview.heroText = heroText;
            preview.feedbackText = feedback;
            preview.objectiveProgress = fillImage;
            preview.attackButton = basic;
            preview.sitButton = sit;
            preview.heroButton = heroButton;
            preview.skillButton = skill;

            var viewButton = Button("ArenaViewButton", "LIHAT ARENA", safe, new Vector2(0f, 1f),
                new Vector2(14f, -14f), new Vector2(164f, 44f));
            viewButton.GetComponentInChildren<Text>().fontSize = 16;
            var arenaView = preview.gameObject.AddComponent<CampaignArenaView>();
            arenaView.follow = follow;
            arenaView.campaign = preview;
            arenaView.traversal = traversal;
            arenaView.safeRoot = safe;
            arenaView.viewButton = viewButton;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Jalur Takhta preview prepared: " + ScenePath);
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
