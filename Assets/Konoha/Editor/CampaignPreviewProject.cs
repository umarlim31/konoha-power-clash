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

        [MenuItem("Konoha/Prepare Jalur Takhta Preview 0.6.0")]
        public static void Prepare()
        {
            // Generate a separate scene using the same Android, URP, input, camera and
            // networked hero prefab as 4v4. Do not change its existing scene or prefabs.
            SpikeProject.Prepare();
            var scene = EditorSceneManager.OpenScene(SpikeProject.ScenePath, OpenSceneMode.Single);
            PlayerSettings.productName = "KONOHA Jalur Takhta Preview";
            PlayerSettings.bundleVersion = "0.6.0";
            PlayerSettings.Android.bundleVersionCode = 46;
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
                new Vector2(0, 165), new Vector2(760, 76), 23); // Two lines: RESTU/SIDANG messages.
            feedback.alignment = TextAnchor.MiddleCenter;
            feedback.color = new Color(1f, 0.82f, 0.42f);
            feedback.gameObject.AddComponent<Outline>().effectColor = Color.black;
            var heroText = Text("CampaignHero", safe, new Vector2(0f, 1f),
                new Vector2(14, -128), new Vector2(430, 28), 16);
            heroText.alignment = TextAnchor.MiddleLeft;
            heroText.gameObject.AddComponent<Outline>().effectColor = Color.black;
            // STEMPEL TUNDA debuff line (0.0.9.3), empty when not slowed.
            var debuff = Text("CampaignDebuff", safe, new Vector2(0f, 1f),
                new Vector2(14, -154), new Vector2(430, 26), 15);
            debuff.alignment = TextAnchor.MiddleLeft;
            debuff.color = new Color(1f, 0.55f, 0.45f);
            debuff.gameObject.AddComponent<Outline>().effectColor = Color.black;

            // Real hero controls. The names are the ones the networked hero components bind
            // to (NetworkPlayerCombat, NetworkHeroKit, NetworkPlayerMovement).
            // 0.2.9: round icon buttons in an arc around a big BASIC (CampaignSkillHud draws them).
            var basic = Button("AttackButton", "BASIC", safe, new Vector2(1f, 0f),
                new Vector2(-30, 34), new Vector2(150, 150));
            var ultimate = Button("UltimateButton", "ULT 0%", safe, new Vector2(1f, 0f),
                new Vector2(-60, 218), new Vector2(100, 100));
            var s1 = Button("S1Button", "S1", safe, new Vector2(1f, 0f),
                new Vector2(-205, 30), new Vector2(100, 100));
            var s2 = Button("S2Button", "S2", safe, new Vector2(1f, 0f),
                new Vector2(-190, 162), new Vector2(100, 100));
            var dodge = Button("DodgeButton", "DODGE", safe, new Vector2(1f, 0f),
                new Vector2(-330, 30), new Vector2(88, 88));
            var jump = Button("CampaignJump", "LOMPAT", safe, new Vector2(1f, 0f),
                new Vector2(-318, 146), new Vector2(88, 88));
            var sit = Button("CampaignSit", "DUDUK", safe, new Vector2(1f, 0f),
                new Vector2(-300, 262), new Vector2(88, 88));
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
            var skillHud = new GameObject("CampaignSkillHud").AddComponent<CampaignSkillHud>();
            skillHud.buttons = new[]
            {
                new CampaignSkillHud.Entry { slot = CampaignSkillHud.Slot.Basic, button = basic },
                new CampaignSkillHud.Entry { slot = CampaignSkillHud.Slot.Skill1, button = s1 },
                new CampaignSkillHud.Entry { slot = CampaignSkillHud.Slot.Skill2, button = s2 },
                new CampaignSkillHud.Entry { slot = CampaignSkillHud.Slot.Ultimate, button = ultimate },
                new CampaignSkillHud.Entry { slot = CampaignSkillHud.Slot.Dodge, button = dodge },
                new CampaignSkillHud.Entry { slot = CampaignSkillHud.Slot.Jump, button = jump },
                new CampaignSkillHud.Entry { slot = CampaignSkillHud.Slot.Seat, button = sit }
            };

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
            footer.text = "JALUR TAKHTA 0.6.0  •  SOLO PREVIEW";
            // 0.3.1: which owner models (Art/Models/<Slot>) are in this build; hidden when none.
            string modelSummary = CampaignModelSlots.Summary();
            if (modelSummary.Length > 0)
            {
                var modelLine = Text("CampaignModelSlots", safe, new Vector2(0.5f, 0f),
                    new Vector2(0, 26), new Vector2(620, 20), 11);
                modelLine.alignment = TextAnchor.MiddleCenter;
                modelLine.horizontalOverflow = HorizontalWrapMode.Overflow;
                modelLine.color = new Color(1f, 1f, 1f, .75f);
                modelLine.raycastTarget = false;
                modelLine.text = modelSummary;
            }

            var stage = new GameObject("CampaignStage").AddComponent<CampaignStage>();
            stage.plaza = capital.Plaza;
            stage.majelis = capital.Majelis;
            stage.biro = capital.Biro;
            stage.garda = capital.Garda;
            stage.chair = chair.transform;
            stage.startPoint = CampaignCapitalArt.SpawnPoint;
            stage.terraceHeight = CampaignCapitalArt.TerraceHeight;
            stage.innerGateZ = CampaignCapitalArt.InnerGateZ;
            stage.loketPoints = new Vector3[CampaignCapitalArt.LoketCenters.Length];
            for (int i = 0; i < stage.loketPoints.Length; i++)
                stage.loketPoints[i] = CampaignCapitalArt.LoketCenters[i] + Vector3.up * 0.1f;
            stage.officeMin = new Vector2(CampaignCapitalArt.OfficeWestX, CampaignCapitalArt.OfficeSouthZ);
            stage.officeMax = new Vector2(CampaignCapitalArt.OfficeEastX, CampaignCapitalArt.OfficeNorthZ);

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
            var occluders = new GameObject("CampaignOccluders").AddComponent<CampaignOccluders>();
            occluders.viewCamera = previewCamera;
            occluders.candidates = capital.CameraOccluders();
            session.occluders = occluders;
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
            preview.biroDoorClosed = capital.BiroDoorClosed;
            preview.biroDoorOpen = capital.BiroDoorOpen;
            preview.loketZones = capital.LoketZones;
            preview.loketLabels = capital.LoketLabels;
            preview.debuffText = debuff;
            preview.gardaLockdown = capital.GardaLockdown;
            preview.traversal = traversal;

            var viewButton = Button("ArenaViewButton", "LIHAT ARENA", safe, new Vector2(0f, 1f),
                new Vector2(14f, -14f), new Vector2(164f, 44f));
            viewButton.GetComponentInChildren<Text>().fontSize = 16;
            var arenaView = preview.gameObject.AddComponent<CampaignArenaView>();
            arenaView.follow = follow;
            arenaView.campaign = preview;
            arenaView.traversal = traversal;
            arenaView.safeRoot = safe;
            arenaView.viewButton = viewButton;
            arenaView.occluders = occluders;
            arenaView.viewFocus = new Vector3(0f, 2f, 4f);
            arenaView.viewOffset = new Vector3(0f, 62f, -70f);

            // RESTU RAKYAT marker under the local hero (0.0.9.2.1).
            var aura = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            aura.name = "RestuRakyatAura";
            UnityEngine.Object.DestroyImmediate(aura.GetComponent<Collider>());
            aura.transform.localScale = new Vector3(1.8f, 0.006f, 1.8f);
            var auraRenderer = aura.GetComponent<Renderer>();
            auraRenderer.sharedMaterial = Mat("CampaignRestuAura", new Color(0.95f, 0.74f, 0.26f));
            auraRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            aura.SetActive(false);
            preview.restuAura = aura.transform;

            // 0.5.0 Jalan Nyaleg: the blusukan rings. 0.6.0: the gold light pillar is gone (owner:
            // "bikin visual kurang"); a small chevron at the hero's feet and a low dashed ring on
            // the target show the way, with a phone "ting" for every new target (CampaignGuideArrow).
            CreateGuideArrow();
            var zoneGlow = CampaignRigBuilder.Unlit("BlusukanZona", new Color(0.95f, 0.70f, 0.22f));
            var zones = new Renderer[stage.blusukanPoints.Length];
            var zoneLabels = new TextMesh[stage.blusukanPoints.Length];
            for (int i = 0; i < stage.blusukanPoints.Length; i++)
            {
                Vector3 point = stage.blusukanPoints[i];
                float diameter = CampaignTuning.Blusukan.ZoneRadius * 2f;
                zones[i] = Glow(null, "Blusukan zona " + (i + 1), PrimitiveType.Cylinder, point + Vector3.up * 0.06f,
                    new Vector3(diameter, 0.01f, diameter), zoneGlow);
                var label = new GameObject("Blusukan label " + (i + 1));
                label.transform.position = point + Vector3.up * 2.9f;
                label.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
                label.transform.localScale = Vector3.one * 0.32f;
                var text = label.AddComponent<TextMesh>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
                text.characterSize = 0.25f;
                text.fontSize = 48;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.fontStyle = FontStyle.Bold;
                text.color = new Color(1f, 0.86f, 0.42f);
                text.text = "SAPA WARGA";
                zoneLabels[i] = text;
                zones[i].gameObject.SetActive(false);
                label.SetActive(false);
            }
            preview.blusukanZones = zones;
            preview.blusukanLabels = zoneLabels;

            var heroSelect = CreateHeroSelect(safe);

            // Sound effects (0.0.9.4): synthesised at runtime, SUARA toggle under KAMERA AWAL.
            var mute = Button("SoundToggle", "SUARA: NYALA", safe, new Vector2(1f, 1f),
                new Vector2(-14, -64), new Vector2(164, 44));
            mute.GetComponentInChildren<Text>().fontSize = 16;
            var audio = new GameObject("CampaignAudio").AddComponent<CampaignAudio>();
            audio.muteButton = mute;
            // 0.2.8: KAMERA JAUH / DEKAT under SUARA (close street camera, remembered).
            var cameraModeButton = Button("CameraModeToggle", "KAMERA: JAUH", safe, new Vector2(1f, 1f),
                new Vector2(-14, -114), new Vector2(164, 44));
            cameraModeButton.GetComponentInChildren<Text>().fontSize = 16;
            var cameraMode = new GameObject("CampaignCameraMode").AddComponent<CampaignCameraMode>();
            cameraMode.follow = follow;
            cameraMode.view = previewCamera;
            cameraMode.button = cameraModeButton;
            // 0.2.3: human bodies, hit feel and skill effects (campaign only; PvP unchanged).
            var combatFeel = new GameObject("CampaignCombatFeel");
            var fx = combatFeel.AddComponent<CampaignCombatFx>();
            fx.glow = CampaignRigBuilder.Unlit("FxGlow", Color.white);
            fx.dust = CampaignRigBuilder.Lit("FxDebu", new Color(.72f, .66f, .56f), 0f);
            fx.kerbauHide = CampaignRigBuilder.Lit("FxKerbauKulit", new Color(.17f, .16f, .17f), .25f);
            fx.kerbauHorn = CampaignRigBuilder.Lit("FxKerbauTanduk", new Color(.78f, .74f, .64f), .5f);
            fx.skin = CampaignRigBuilder.Lit("RigKulitSawo", new Color(.62f, .43f, .30f), .3f);
            fx.cloth = CampaignRigBuilder.Lit("FxKain", Color.white, .2f);
            fx.pants = CampaignRigBuilder.Lit("RigCelanaGelap", new Color(.10f, .11f, .14f), .15f);
            fx.hair = CampaignRigBuilder.Lit("RigRambut", new Color(.04f, .035f, .03f), .45f);
            fx.wood = CampaignRigBuilder.Lit("FxKayuPodium", new Color(.40f, .24f, .12f), .35f);
            fx.cone = CampaignRigBuilder.Lit("FxOranyeProyek", new Color(.98f, .45f, .08f), .35f);
            fx.white = CampaignRigBuilder.Lit("FxPutih", new Color(.94f, .94f, .92f), .3f);
            var bodies = combatFeel.AddComponent<CampaignBodies>();
            bodies.fx = fx;
            var templates = new GameObject("CampaignBodyTemplates");
            templates.transform.SetParent(combatFeel.transform, false);
            bodies.heroTemplates = CampaignRigBuilder.HeroTemplates(templates.transform);
            // 0.6.0 KARIER: the player's own citizen (character creator).
            bodies.avatarTemplate = CampaignRigBuilder.AvatarTemplate(templates.transform);
            (follow != null ? follow.gameObject : previewCamera.gameObject).AddComponent<CampaignCameraShake>();

            var lobi = CreateLobiPanel(safe);
            CreateChecklist(safe, lobi.panel);
            // 0.6.0 KARIER Level 1 "Warga Biasa": HUD, phone, police, Grup WA, crowd, job props,
            // and the character creator (below the hero screen and the mode menu).
            var karierScene = capital.BuildKarier();
            var karier = CreateKarier(safe, objective, status, waypoint, heroText, feedback, fillImage, traversal, bodies,
                new[] { s1, s2, ultimate, heroButton }, karierScene, stage);
            var avatarPanel = CreateAvatarPanel(safe, karier, follow);
            var result = CreateResultScreen(safe);
            // Draw order on top of the HUD: result screen < hero screen < mode menu.
            result.panel.transform.SetAsLastSibling();
            heroSelect.panel.transform.SetAsLastSibling();
            var menu = CreateModeMenu(safe);
            menu.session = session;
            menu.avatarPanel = avatarPanel;
            menu.pvpScene = SpikeProject.ScenePath;
            session.waitForMenu = true;

            audio.clickButtons = new[] { sit, viewButton, resetCamera, cameraModeButton,
                heroSelect.heroButtons[0], heroSelect.heroButtons[1], heroSelect.heroButtons[2],
                heroSelect.heroButtons[3], heroSelect.startButton,
                result.retryButton, result.changeHeroButton, menu.soloButton, menu.pvpButton,
                lobi.lawanButton, lobi.rangkulButton, menu.karierButton,
                karier.phoneButton, karier.closePhoneButton, karier.ojolButton, karier.kuliButton, karier.buzzerButton,
                karier.rebahanButton, karier.actionButton, karier.kaburButton, karier.damaiButton, karier.polsekButton,
                karier.waCloseButton, avatarPanel.genderButton, avatarPanel.skinButton, avatarPanel.hairButton,
                avatarPanel.bodyButton, avatarPanel.shirtButton, avatarPanel.startButton, avatarPanel.resetButton };

            EditorSceneManager.SaveScene(scene, ScenePath);
            // 0.1.0: the campaign scene opens first (mode menu); the untouched PvP scene is
            // second and loaded by REBUT KURSI.
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(SpikeProject.ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("Jalur Takhta preview prepared: " + ScenePath);
        }

        // --- 0.6.0 KARIER ---------------------------------------------------------------

        // Small gold chevron (two bars, doubled) for the hero's feet, a dashed ring and a label
        // for the target. CampaignGuideArrow places them; all start hidden, no colliders.
        private static CampaignGuideArrow CreateGuideArrow()
        {
            var glow = CampaignRigBuilder.Unlit("PanduArah", new Color(1f, 0.82f, 0.30f));
            var guide = new GameObject("CampaignGuideArrow").AddComponent<CampaignGuideArrow>();
            var arrow = new GameObject("PanduArah panah").transform;
            arrow.SetParent(guide.transform, false);
            foreach (float back in new[] { 0f, -0.3f })
                foreach (int side in new[] { -1, 1 })
                    Glow(arrow, "PanduArah sayap", PrimitiveType.Cube, new Vector3(side * 0.15f, 0f, 0.03f + back),
                        new Vector3(0.09f, 0.02f, 0.46f), glow).transform.localRotation = Quaternion.Euler(0f, -side * 40.6f, 0f);
            arrow.gameObject.SetActive(false);

            var ring = new GameObject("PanduArah cincin").transform;
            ring.SetParent(guide.transform, false);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f;
                Vector3 at = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 1.25f;
                Glow(ring, "PanduArah garis", PrimitiveType.Cube, at, new Vector3(0.34f, 0.02f, 0.08f), glow)
                    .transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            }
            ring.gameObject.SetActive(false);

            var labelObject = new GameObject("PanduArah label");
            labelObject.transform.SetParent(guide.transform, false);
            labelObject.transform.localScale = Vector3.one * 0.3f;
            var label = labelObject.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelObject.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            label.fontSize = 48;
            label.characterSize = 0.22f;
            label.anchor = TextAnchor.LowerCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(1f, 0.88f, 0.50f);
            labelObject.SetActive(false);

            guide.arrow = arrow;
            guide.ring = ring;
            guide.ringLabel = label;
            return guide;
        }

        // KARIER HUD: target list, message box, HP button, action button and the phone,
        // police, Grup WA, rebahan and fade panels, all under one root shown by the controller.
        private static KarierController CreateKarier(Transform safe, Text objective, Text status, Text waypoint, Text heroText,
            Text feedback, Image progress, CampaignTraversal traversal, CampaignBodies bodies, Button[] hidden,
            CampaignCapitalArt.KarierScene scene, CampaignStage stage)
        {
            var rootObject = new GameObject("KarierHud", typeof(RectTransform));
            var root = (RectTransform)rootObject.transform;
            root.SetParent(safe, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;

            var karier = new GameObject("KarierController").AddComponent<KarierController>();
            karier.hudRoot = rootObject;
            karier.objectiveText = objective;
            karier.statusText = status;
            karier.waypointText = waypoint;
            karier.heroText = heroText;
            karier.feedbackText = feedback;
            karier.objectiveProgress = progress;
            karier.traversal = traversal;
            karier.bodies = bodies;
            karier.hiddenInKarier = hidden;

            var target = UiBox("KarierTarget", root, new Vector2(0f, 1f), new Vector2(14, -232), new Vector2(380, 168),
                new Color(0.04f, 0.05f, 0.06f, 0.6f), false);
            var targetText = Text("KarierTargetText", target, new Vector2(0f, 1f), new Vector2(12, -8), new Vector2(360, 154), 15);
            targetText.alignment = TextAnchor.UpperLeft;
            targetText.supportRichText = true;
            targetText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            karier.targetText = targetText;

            var message = UiBox("KarierPesan", root, new Vector2(0.5f, 1f), new Vector2(0, -128), new Vector2(540, 96),
                new Color(0.03f, 0.04f, 0.05f, 0.86f), false);
            var messageText = Text("KarierPesanText", message, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(516, 88), 16);
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.color = new Color(1f, 0.92f, 0.72f);
            message.gameObject.SetActive(false);
            karier.messagePanel = message.gameObject;
            karier.messageText = messageText;

            var phoneButton = Button("KarierHP", "HP • KERJA", root, new Vector2(0f, 1f), new Vector2(14, -66), new Vector2(164, 52));
            phoneButton.GetComponentInChildren<Text>().fontSize = 18;
            phoneButton.GetComponent<Image>().color = new Color(0.16f, 0.42f, 0.30f, 0.97f);
            karier.phoneButton = phoneButton;

            var action = Button("KarierAksi", "AKSI", root, new Vector2(1f, 0f), new Vector2(-200, 30), new Vector2(120, 100));
            action.GetComponentInChildren<Text>().fontSize = 17;
            action.GetComponent<Image>().color = new Color(0.20f, 0.50f, 0.32f, 0.97f);
            action.gameObject.SetActive(false);
            karier.actionButton = action;

            // KONOHA KERJA phone.
            var phone = UiBox("KarierHPPanel", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(470, 590),
                new Color(0.05f, 0.07f, 0.08f, 0.97f), true);
            var phoneTitle = Text("KarierHPJudul", phone, new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(440, 40), 26);
            phoneTitle.alignment = TextAnchor.MiddleCenter;
            phoneTitle.fontStyle = FontStyle.Bold;
            phoneTitle.color = new Color(0.55f, 0.92f, 0.62f);
            phoneTitle.text = "KONOHA KERJA";
            var phoneInfo = Text("KarierHPInfo", phone, new Vector2(0.5f, 1f), new Vector2(0, -58), new Vector2(440, 60), 15);
            phoneInfo.alignment = TextAnchor.MiddleCenter;
            karier.phoneInfo = phoneInfo;
            karier.ojolButton = PhoneButton("KarierKerjaOjol", "OJOL", phone, -126, new Color(0.16f, 0.42f, 0.30f, 0.98f));
            karier.kuliButton = PhoneButton("KarierKerjaKuli", "KULI BANGUNAN", phone, -220, new Color(0.45f, 0.33f, 0.16f, 0.98f));
            karier.buzzerButton = PhoneButton("KarierKerjaBuzzer", "BUZZER HOAKS", phone, -314, new Color(0.50f, 0.13f, 0.12f, 0.98f));
            karier.rebahanButton = PhoneButton("KarierRebahanTombol", "REBAHAN", phone, -408, new Color(0.20f, 0.24f, 0.36f, 0.98f));
            var closePhone = Button("KarierHPTutup", "TUTUP", phone, new Vector2(0.5f, 0f), new Vector2(0, 16), new Vector2(420, 52));
            closePhone.GetComponentInChildren<Text>().fontSize = 18;
            karier.closePhoneButton = closePhone;
            phone.gameObject.SetActive(false);
            karier.phonePanel = phone.gameObject;

            // Police choices.
            var police = UiBox("KarierPolisi", root, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(700, 330),
                new Color(0.06f, 0.07f, 0.12f, 0.96f), true);
            var policeTitle = Text("KarierPolisiJudul", police, new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(660, 40), 28);
            policeTitle.alignment = TextAnchor.MiddleCenter;
            policeTitle.fontStyle = FontStyle.Bold;
            policeTitle.color = new Color(0.75f, 0.85f, 1f);
            policeTitle.text = "POLISI DATANG";
            var policeText = Text("KarierPolisiIsi", police, new Vector2(0.5f, 1f), new Vector2(0, -62), new Vector2(660, 150), 17);
            policeText.alignment = TextAnchor.UpperCenter;
            karier.policeText = policeText;
            karier.kaburButton = ChoiceButton("KarierKabur", "KABUR", police, -230, new Color(0.45f, 0.12f, 0.10f, 0.98f));
            karier.damaiButton = ChoiceButton("KarierDamai", "DAMAI", police, 0, new Color(0.55f, 0.42f, 0.14f, 0.98f));
            karier.polsekButton = ChoiceButton("KarierPolsek", "POLSEK", police, 230, new Color(0.18f, 0.28f, 0.40f, 0.98f));
            police.gameObject.SetActive(false);
            karier.policePanel = police.gameObject;

            // Grup WA after registering for Ketua RT.
            var wa = UiBox("KarierGrupWA", root, new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(640, 470),
                new Color(0.92f, 0.95f, 0.90f, 0.98f), true);
            var waHeader = UiBox("KarierWAHeader", wa, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(640, 54),
                new Color(0.07f, 0.37f, 0.30f, 1f), false);
            var waTitle = Text("KarierWAJudul", waHeader, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 44), 22);
            waTitle.alignment = TextAnchor.MiddleCenter;
            waTitle.fontStyle = FontStyle.Bold;
            waTitle.text = "GRUP WA • RT 03 KONOHA";
            var waText = Text("KarierWAIsi", wa, new Vector2(0.5f, 1f), new Vector2(0, -66), new Vector2(600, 330), 16);
            waText.alignment = TextAnchor.UpperLeft;
            waText.supportRichText = true;
            waText.color = new Color(0.10f, 0.12f, 0.10f);
            karier.waText = waText;
            var waClose = Button("KarierWATutup", "TUTUP", wa, new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(300, 52));
            waClose.GetComponentInChildren<Text>().fontSize = 18;
            karier.waCloseButton = waClose;
            wa.gameObject.SetActive(false);
            karier.waPanel = wa.gameObject;

            // Rebahan feed.
            var rebahan = UiBox("KarierRebahan", root, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(560, 170),
                new Color(0.08f, 0.06f, 0.12f, 0.93f), false);
            var rebahanText = Text("KarierRebahanIsi", rebahan, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(530, 150), 18);
            rebahanText.alignment = TextAnchor.MiddleCenter;
            karier.rebahanText = rebahanText;
            rebahan.gameObject.SetActive(false);
            karier.rebahanPanel = rebahan.gameObject;

            // Black fade (IKUT KE POLSEK); blocks touches while shown.
            var fadeObject = new GameObject("KarierGelap", typeof(RectTransform), typeof(Image));
            var fadeRect = (RectTransform)fadeObject.transform;
            fadeRect.SetParent(root, false);
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = fadeRect.offsetMax = Vector2.zero;
            fadeObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            fadeObject.SetActive(false);
            karier.fade = fadeObject.GetComponent<Image>();

            // World: job places (tested walkable ground), preman spot, crowd and job props.
            karier.placeNames = (string[])CampaignCapitalArt.KarierPlaceNames.Clone();
            karier.placePoints = CampaignCapitalArt.KarierPlacePoints(stage.plaza.position);
            karier.kuliPickup = CampaignCapitalArt.KarierKuliPickup;
            karier.kuliDrop = CampaignCapitalArt.KarierKuliDrop;
            karier.warkop = CampaignCapitalArt.KarierWarkop;
            karier.posRt = CampaignCapitalArt.KarierPosRt;
            karier.sapaNames = new[] { "BAPAK-BAPAK", "IBU-IBU", "DRIVER OJOL" };
            karier.sapaPoints = (Vector3[])stage.blusukanPoints.Clone();
            karier.premanCenter = CampaignCapitalArt.KarierPremanCenter;
            karier.premanPoints = (Vector3[])CampaignCapitalArt.KarierPremanPoints.Clone();
            karier.premanLook = CampaignCapitalArt.KarierPremanLook;
            karier.homePoint = CampaignCapitalArt.SpawnPoint + Vector3.up * 0.25f;
            karier.crowd = scene.crowd;
            karier.ojolMotor = scene.ojolMotor;
            karier.ojolPassenger = scene.ojolPassenger;
            karier.sack = scene.sack;
            rootObject.SetActive(false);
            return karier;
        }

        private static Button PhoneButton(string name, string caption, RectTransform phone, float y, Color color)
        {
            var button = Button(name, caption, phone, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(420, 86));
            button.GetComponentInChildren<Text>().fontSize = 18;
            button.GetComponent<Image>().color = color;
            return button;
        }

        private static Button ChoiceButton(string name, string caption, RectTransform panel, float x, Color color)
        {
            var button = Button(name, caption, panel, new Vector2(0.5f, 0f), new Vector2(x, 18), new Vector2(214, 90));
            button.GetComponentInChildren<Text>().fontSize = 16;
            button.GetComponent<Image>().color = color;
            return button;
        }

        // Character creator on the left; the hero stays visible on the right.
        private static KarierAvatarPanel CreateAvatarPanel(Transform safe, KarierController karier, MobileCombatCamera follow)
        {
            var panelObject = new GameObject("KarierAvatarPanel", typeof(RectTransform), typeof(Image));
            var panel = (RectTransform)panelObject.transform;
            panel.SetParent(safe, false);
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(440f, 0f);
            panelObject.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.07f, 0.9f);

            var title = Text("KarierAvatarJudul", panel, new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(420, 40), 26);
            title.alignment = TextAnchor.MiddleCenter;
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, 0.86f, 0.52f);
            title.text = "BUAT WARGA KONOHA";
            var subtitle = Text("KarierAvatarSub", panel, new Vector2(0.5f, 1f), new Vector2(0, -56), new Vector2(420, 40), 14);
            subtitle.alignment = TextAnchor.MiddleCenter;
            subtitle.text = "Jadi dirimu sendiri di Negara Konoha.\nTokoh, lembaga, dan peristiwanya fiksi.";
            var nameLabel = Text("KarierAvatarNamaLabel", panel, new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(380, 22), 15);
            nameLabel.alignment = TextAnchor.MiddleLeft;
            nameLabel.text = "NAMA";

            var avatar = new GameObject("KarierAvatar").AddComponent<KarierAvatarPanel>();
            avatar.panel = panelObject;
            avatar.controller = karier;
            avatar.follow = follow;
            avatar.nameField = NameField(panel, new Vector2(0, -124));
            avatar.genderButton = CreatorButton("KarierAvatarJenis", panel, -184);
            avatar.skinButton = CreatorButton("KarierAvatarKulit", panel, -244);
            avatar.hairButton = CreatorButton("KarierAvatarKepala", panel, -304);
            avatar.bodyButton = CreatorButton("KarierAvatarBadan", panel, -364);
            avatar.shirtButton = CreatorButton("KarierAvatarBaju", panel, -424);
            var info = Text("KarierAvatarInfo", panel, new Vector2(0.5f, 1f), new Vector2(0, -484), new Vector2(400, 64), 15);
            info.alignment = TextAnchor.MiddleCenter;
            avatar.infoText = info;
            var start = Button("KarierAvatarMulai", "MULAI HIDUP", panel, new Vector2(0.5f, 0f), new Vector2(0, 84), new Vector2(380, 64));
            start.GetComponentInChildren<Text>().fontSize = 24;
            start.GetComponent<Image>().color = new Color(0.62f, 0.44f, 0.16f, 0.98f);
            avatar.startButton = start;
            var reset = Button("KarierAvatarBaru", "MULAI DARI NOL", panel, new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(380, 50));
            reset.GetComponentInChildren<Text>().fontSize = 17;
            reset.GetComponent<Image>().color = new Color(0.40f, 0.14f, 0.12f, 0.96f);
            avatar.resetButton = reset;
            panelObject.SetActive(false);
            return avatar;
        }

        private static Button CreatorButton(string name, RectTransform panel, float y)
        {
            var button = Button(name, "...", panel, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(380, 52));
            button.GetComponentInChildren<Text>().fontSize = 19;
            button.GetComponent<Image>().color = new Color(0.14f, 0.22f, 0.26f, 0.97f);
            return button;
        }

        private static InputField NameField(RectTransform parent, Vector2 offset)
        {
            var rect = new GameObject("KarierNama", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(380, 48);
            var image = rect.GetComponent<Image>();
            image.color = new Color(0.12f, 0.16f, 0.18f, 1f);
            Text text = FieldText("Teks", rect, 22, Color.white);
            Text placeholder = FieldText("Petunjuk", rect, 20, new Color(1f, 1f, 1f, 0.45f));
            placeholder.fontStyle = FontStyle.Italic;
            placeholder.text = "Tulis namamu...";
            var field = rect.gameObject.AddComponent<InputField>();
            field.targetGraphic = image;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.characterLimit = KarierAvatar.MaxNameLength;
            field.lineType = InputField.LineType.SingleLine;
            return field;
        }

        private static Text FieldText(string name, RectTransform parent, int size, Color color)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(14f, 6f);
            rect.offsetMax = new Vector2(-14f, -6f);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAnchor.MiddleLeft;
            label.supportRichText = false;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform UiBox(string name, Transform parent, Vector2 anchor, Vector2 offset, Vector2 size,
            Color color, bool blocksTouches)
        {
            var box = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)box.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            var image = box.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = blocksTouches;
            return rect;
        }

        private static RectTransform FullPanel(string name, Transform safe, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(safe, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();
            panel.GetComponent<Image>().color = color;
            return rect;
        }

        // §10 result screen. 0.4.0: KORAN KONOHA, a cream front page over a dark backdrop.
        private static CampaignResultScreen CreateResultScreen(Transform safe)
        {
            var panel = FullPanel("ResultPanel", safe, new Color(0.05f, 0.05f, 0.04f, 0.90f));
            var paperObject = new GameObject("KoranKertas", typeof(RectTransform), typeof(Image));
            var paper = (RectTransform)paperObject.transform;
            paper.SetParent(panel, false);
            paper.anchorMin = paper.anchorMax = new Vector2(0.5f, 0.5f);
            paper.pivot = new Vector2(0.5f, 0.5f);
            paper.anchoredPosition = new Vector2(0, 40);
            paper.sizeDelta = new Vector2(980, 560);
            paperObject.GetComponent<Image>().color = new Color(0.93f, 0.90f, 0.81f, 1f);
            Color ink = new Color(0.12f, 0.10f, 0.09f);
            Color red = new Color(0.62f, 0.10f, 0.08f);

            var masthead = Text("KoranMasthead", paper, new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(940, 54), 42);
            masthead.alignment = TextAnchor.MiddleCenter;
            masthead.fontStyle = FontStyle.Bold;
            masthead.color = ink;
            masthead.text = "KORAN KONOHA";
            var edition = Text("KoranEdisi", paper, new Vector2(0.5f, 1f), new Vector2(0, -68), new Vector2(940, 22), 14);
            edition.alignment = TextAnchor.MiddleCenter;
            edition.color = new Color(0.35f, 0.30f, 0.26f);
            Rule(paper, "KoranGarisAtas", -92, ink);
            var headline = Text("KoranJudul", paper, new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(940, 64), 32);
            headline.alignment = TextAnchor.MiddleCenter;
            headline.fontStyle = FontStyle.Bold;
            headline.color = red;
            var subhead = Text("KoranSubjudul", paper, new Vector2(0.5f, 1f), new Vector2(0, -164), new Vector2(900, 44), 17);
            subhead.alignment = TextAnchor.MiddleCenter;
            subhead.fontStyle = FontStyle.Italic;
            subhead.color = ink;
            Rule(paper, "KoranGarisTengah", -212, ink);
            var news = Text("KoranBerita", paper, new Vector2(0.5f, 1f), new Vector2(0, -222), new Vector2(920, 250), 16);
            news.alignment = TextAnchor.UpperLeft;
            news.color = ink;
            var archetype = Text("KoranArketipe", paper, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(940, 30), 20);
            archetype.alignment = TextAnchor.MiddleCenter;
            archetype.fontStyle = FontStyle.Bold;
            archetype.color = red;
            var stats = Text("ResultStats", paper, new Vector2(0.5f, 0f), new Vector2(0, 12), new Vector2(940, 24), 13);
            stats.alignment = TextAnchor.MiddleCenter;
            stats.color = new Color(0.30f, 0.26f, 0.22f);

            var retry = Button("ResultRetry", "ULANG", panel, new Vector2(0.5f, 0f), new Vector2(-150, 20), new Vector2(260, 64));
            retry.GetComponentInChildren<Text>().fontSize = 24;
            retry.GetComponent<Image>().color = new Color(0.62f, 0.44f, 0.16f, 0.98f);
            var change = Button("ResultChangeHero", "GANTI HERO", panel, new Vector2(0.5f, 0f), new Vector2(150, 20), new Vector2(260, 64));
            change.GetComponentInChildren<Text>().fontSize = 24;

            var screen = new GameObject("CampaignResultScreen").AddComponent<CampaignResultScreen>();
            screen.panel = panel.gameObject;
            screen.statsText = stats;
            screen.editionText = edition;
            screen.headlineText = headline;
            screen.subheadText = subhead;
            screen.newsText = news;
            screen.archetypeText = archetype;
            screen.retryButton = retry;
            screen.changeHeroButton = change;
            return screen;
        }

        private static void Rule(RectTransform paper, string name, float y, Color color)
        {
            var line = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)line.transform;
            rect.SetParent(paper, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(940, 3);
            line.GetComponent<Image>().color = color;
            line.GetComponent<Image>().raycastTarget = false;
        }

        private static Renderer Glow(Transform parent, string name, PrimitiveType type, Vector3 local, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return renderer;
        }

        // 0.5.0 Jalan Nyaleg: the step list on the left, under the hero line.
        private static CampaignChecklist CreateChecklist(Transform safe, GameObject lobiPanel)
        {
            var panelObject = new GameObject("ChecklistPanel", typeof(RectTransform), typeof(Image));
            var panel = (RectTransform)panelObject.transform;
            panel.SetParent(safe, false);
            panel.anchorMin = panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(14, -184);
            panel.sizeDelta = new Vector2(400, 178);
            var image = panelObject.GetComponent<Image>();
            image.color = new Color(0.04f, 0.05f, 0.06f, 0.55f);
            image.raycastTarget = false;
            var list = Text("ChecklistText", panel, new Vector2(0f, 1f), new Vector2(12, -8), new Vector2(380, 166), 15);
            list.alignment = TextAnchor.UpperLeft;
            list.supportRichText = true;
            list.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            var checklist = new GameObject("CampaignChecklist").AddComponent<CampaignChecklist>();
            checklist.panel = panelObject;
            checklist.listText = list;
            checklist.hideWhenActive = lobiPanel;
            return checklist;
        }

        // 0.4.0 Musim Pemilu: the LAWAN / RANGKUL panel (left of the hero, above the joystick).
        private static CampaignLobiPanel CreateLobiPanel(Transform safe)
        {
            var panelObject = new GameObject("LobiPanel", typeof(RectTransform), typeof(Image));
            var panel = (RectTransform)panelObject.transform;
            panel.SetParent(safe, false);
            panel.anchorMin = panel.anchorMax = new Vector2(0f, 0.5f);
            panel.pivot = new Vector2(0f, 0.5f);
            panel.anchoredPosition = new Vector2(24, 30);
            panel.sizeDelta = new Vector2(520, 250);
            panelObject.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.12f, 0.92f);
            var title = Text("LobiJudul", panel, new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(490, 34), 24);
            title.alignment = TextAnchor.MiddleCenter;
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, 0.82f, 0.40f);
            var body = Text("LobiIsi", panel, new Vector2(0.5f, 1f), new Vector2(0, -48), new Vector2(490, 96), 15);
            body.alignment = TextAnchor.UpperCenter;
            var lawan = Button("LobiLawan", "LAWAN\nmasuk aula, bertarung", panel, new Vector2(0.5f, 0f),
                new Vector2(-126, 16), new Vector2(236, 78));
            lawan.GetComponentInChildren<Text>().fontSize = 17;
            lawan.GetComponent<Image>().color = new Color(0.55f, 0.12f, 0.10f, 0.98f);
            var rangkul = Button("LobiRangkul", "RANGKUL", panel, new Vector2(0.5f, 0f),
                new Vector2(126, 16), new Vector2(236, 78));
            rangkul.GetComponentInChildren<Text>().fontSize = 17;
            rangkul.GetComponent<Image>().color = new Color(0.62f, 0.44f, 0.16f, 0.98f);

            var lobi = new GameObject("CampaignLobiPanel").AddComponent<CampaignLobiPanel>();
            lobi.panel = panelObject;
            lobi.titleText = title;
            lobi.bodyText = body;
            lobi.lawanButton = lawan;
            lobi.rangkulButton = rangkul;
            lobi.rangkulLabel = rangkul.GetComponentInChildren<Text>();
            return lobi;
        }

        // 0.1.0 mode menu, the first screen of the APK.
        private static CampaignModeMenu CreateModeMenu(Transform safe)
        {
            var panel = FullPanel("ModeMenuPanel", safe, new Color(0.05f, 0.07f, 0.08f, 0.97f));
            var title = Text("ModeMenuTitle", panel, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(760, 60), 36);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1f, 0.84f, 0.46f);
            title.text = "NEGARA KONOHA: POWER CLASH";
            title.gameObject.AddComponent<Outline>().effectColor = Color.black;
            var tagline = Text("ModeMenuTagline", panel, new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(760, 30), 17);
            tagline.alignment = TextAnchor.MiddleCenter;
            tagline.text = "Karya satir fiksi. Tokoh, lembaga, dan peristiwa Negara Konoha adalah rekaan.";

            // 0.6.0: KARIER first (Level 1, the new main mode); Jalur Takhta becomes MODE PRESIDEN.
            var karier = Button("ModeKarier", "KARIER  (BARU)\nWarga biasa > Ketua RT", panel, new Vector2(0.5f, 0.5f),
                new Vector2(-325, -10), new Vector2(300, 130));
            karier.GetComponentInChildren<Text>().fontSize = 22;
            karier.GetComponent<Image>().color = new Color(0.62f, 0.44f, 0.16f, 0.98f);
            var solo = Button("ModeSolo", "MODE PRESIDEN\nJalur Takhta (cepat)", panel, new Vector2(0.5f, 0.5f),
                new Vector2(0, -10), new Vector2(300, 130));
            solo.GetComponentInChildren<Text>().fontSize = 22;
            solo.GetComponent<Image>().color = new Color(0.40f, 0.28f, 0.12f, 0.98f);
            var pvp = Button("ModePvp", "REBUT KURSI\nPvP 4v4", panel, new Vector2(0.5f, 0.5f),
                new Vector2(325, -10), new Vector2(300, 130));
            pvp.GetComponentInChildren<Text>().fontSize = 22;
            var hint = Text("ModeMenuHint", panel, new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(900, 26), 15);
            hint.alignment = TextAnchor.MiddleCenter;
            hint.color = new Color(1f, 1f, 1f, 0.75f);
            hint.text = "KARIER: hidup dari nol, cari duit bersih atau kotor, progres tersimpan.  •  MODE PRESIDEN: rebut Kursi dalam satu perjalanan.";
            var version = Text("ModeMenuVersion", panel, new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(400, 24), 14);
            version.alignment = TextAnchor.MiddleCenter;

            var menu = new GameObject("CampaignModeMenu").AddComponent<CampaignModeMenu>();
            menu.panel = panel.gameObject;
            menu.soloButton = solo;
            menu.pvpButton = pvp;
            menu.karierButton = karier;
            menu.versionText = version;
            return menu;
        }

        // Hero screen shown before each run (0.0.9.2.1). Last sibling: covers and blocks the HUD.
        private static CampaignHeroSelect CreateHeroSelect(Transform safe)
        {
            var panel = new GameObject("HeroSelectPanel", typeof(RectTransform), typeof(Image));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(safe, false);
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            panelRect.SetAsLastSibling();
            panel.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.07f, 0.86f);

            var title = Text("HeroSelectTitle", panelRect, new Vector2(0.5f, 1f), new Vector2(0, -34), new Vector2(620, 48), 30);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1f, 0.86f, 0.52f);
            title.text = "PILIH HERO";
            title.gameObject.AddComponent<Outline>().effectColor = Color.black;
            var rule = Text("HeroSelectRule", panelRect, new Vector2(0.5f, 1f), new Vector2(0, -84), new Vector2(720, 28), 16);
            rule.alignment = TextAnchor.MiddleCenter;
            rule.text = "Hero terkunci selama perjalanan. Bisa diganti setelah ULANG.";

            // The controller lives outside the panel: a component on the panel itself stops
            // running once the panel is hidden, and the screen could never reopen (ULANG).
            var select = new GameObject("CampaignHeroSelect").AddComponent<CampaignHeroSelect>();
            select.panel = panel;
            string[] names = { "MEGA", "GEMOY", "ABAH", "PAK WI" };
            for (int i = 0; i < names.Length; i++)
            {
                var button = Button("HeroPick_" + names[i].Replace(" ", string.Empty), names[i], panelRect,
                    new Vector2(0.5f, 0.5f), new Vector2(-300f + i * 200f, 70f), new Vector2(184, 84));
                button.GetComponentInChildren<Text>().fontSize = 24;
                select.heroButtons[i] = button;
            }

            var detail = Text("HeroSelectDetail", panelRect, new Vector2(0.5f, 0.5f), new Vector2(0, -52), new Vector2(680, 130), 17);
            detail.alignment = TextAnchor.MiddleCenter;
            detail.gameObject.AddComponent<Outline>().effectColor = Color.black;
            select.detailText = detail;

            var start = Button("HeroSelectStart", "MULAI", panelRect, new Vector2(0.5f, 0f),
                new Vector2(0, 28), new Vector2(260, 70));
            start.GetComponentInChildren<Text>().fontSize = 26;
            select.startButton = start;
            return select;
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
            // 0.2.3: a human body (jas, dasi, sabuk take the faction colours) replaces the capsule.
            var member = CampaignRigBuilder.Member(visual.transform, body, accent);
            member.rig.motionSource = root.transform;
            var capsule = member.primary[0];
            var sash = member.accent[0];
            // Faction headwear: peci (Majelis), kepet cap (Biro), baret (Garda / others).
            var peci = CampaignRigBuilder.HeadPiece(member, "Peci Majelis", PrimitiveType.Cylinder,
                new Vector3(0f, .27f, -.01f), new Vector3(.25f, .06f, .26f), CampaignRigBuilder.Lit("RigPeciHitam", new Color(.05f, .05f, .06f), .35f), Vector3.zero);
            var kepet = CampaignRigBuilder.HeadPiece(member, "Topi Biro", PrimitiveType.Cylinder,
                new Vector3(0f, .27f, 0f), new Vector3(.27f, .05f, .28f), CampaignRigBuilder.Lit("RigTopiBiro", new Color(.22f, .30f, .45f), .3f), Vector3.zero);
            CampaignRigBuilder.HeadPiece(member, "Pet topi", PrimitiveType.Cube, new Vector3(0f, .24f, .15f), new Vector3(.2f, .02f, .12f),
                CampaignRigBuilder.Lit("RigTopiBiro", new Color(.22f, .30f, .45f), .3f), new Vector3(-10f, 0f, 0f)).transform.SetParent(kepet.transform, true);
            var baret = CampaignRigBuilder.HeadPiece(member, "Baret Garda", PrimitiveType.Sphere,
                new Vector3(.03f, .25f, -.02f), new Vector3(.28f, .09f, .29f), CampaignRigBuilder.Lit("RigBaret", new Color(.10f, .12f, .20f), .2f), new Vector3(0f, 0f, -12f));
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
            // In the raised right hand of the body (CampaignHumanoid hold pose).
            gavelPivot.transform.localPosition = new Vector3(0.3f, 1.2f, 0.5f);
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

            // STEMPEL TUNDA radius (8 m) under a Biro Pengawas (0.0.9.3).
            float stempel = CampaignTuning.Biro.StempelTundaRadius * 2f;
            var stempelAura = Part("StempelTundaArea", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.04f, 0f),
                new Vector3(stempel, 0.004f, stempel), Mat("CampaignStempelAura", new Color(0.30f, 0.38f, 0.52f)));
            stempelAura.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            stempelAura.SetActive(false);

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
            enemy.primaryRenderers = member.primary.ToArray();
            enemy.accentRenderers = member.accent.ToArray();
            enemy.body = member.rig;
            enemy.headwearMajelis = peci;
            enemy.headwearBiro = kepet;
            enemy.headwearGarda = baret;
            enemy.nameplate = label;
            enemy.blockRing = blockRing;
            enemy.gavel = gavelPivot;
            enemy.gavelPivot = gavelPivot.transform;
            enemy.telegraphOuter = telegraphOuter.transform;
            enemy.telegraphFill = telegraphFill.transform;
            enemy.stempelAura = stempelAura;
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
