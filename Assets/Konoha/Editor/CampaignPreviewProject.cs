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

        [MenuItem("Konoha/Prepare Jalur Takhta Preview 0.0.7.2")]
        public static void Prepare()
        {
            // Generate a separate scene using the same Android, URP, input, camera and
            // prototype hero silhouettes as 4v4. Do not change its existing scene.
            SpikeProject.Prepare();
            var scene = EditorSceneManager.OpenScene(SpikeProject.ScenePath, OpenSceneMode.Single);
            PlayerSettings.productName = "KONOHA Jalur Takhta Preview";
            PlayerSettings.bundleVersion = "0.0.7.2";
            PlayerSettings.Android.bundleVersionCode = 15;
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
                follow.offset = new Vector3(0f, 14.6f, -10.8f);
                follow.crowdedOffset = follow.offset;
                follow.limitFocusToArena = true;
                follow.focusXLimits = new Vector2(-6.5f, 6.5f);
                follow.focusZLimits = new Vector2(-6f, 5f);
            }

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

            var stone = Mat("CampaignStone", new Color(0.54f, 0.52f, 0.46f));
            var gold = Mat("CampaignGold", new Color(0.82f, 0.59f, 0.25f));
            var burgundy = Mat("CampaignMajelis", new Color(0.48f, 0.14f, 0.25f));
            var biroColor = Mat("CampaignBiro", new Color(0.43f, 0.47f, 0.49f));
            var gateColor = Mat("CampaignGate", new Color(0.82f, 0.22f, 0.18f));
            var guardColor = Mat("CampaignGuard", new Color(0.17f, 0.22f, 0.32f));
            var green = Mat("CampaignSeal", new Color(0.22f, 0.68f, 0.45f));
            var pavement = Mat("CampaignPavement", new Color(0.44f, 0.46f, 0.43f));
            var route = Mat("CampaignRoute", new Color(0.63f, 0.58f, 0.48f));
            var charcoal = Mat("CampaignCharcoal", new Color(0.25f, 0.30f, 0.29f));
            var water = Mat("CampaignWater", new Color(0.10f, 0.33f, 0.39f));
            var landscape = Mat("CampaignLandscape", new Color(0.24f, 0.32f, 0.24f));
            var leaves = Mat("CampaignLeaves", new Color(0.17f, 0.40f, 0.27f));
            var leavesLight = Mat("CampaignLeavesLight", new Color(0.34f, 0.53f, 0.30f));
            var ivory = Mat("CampaignIvory", new Color(0.82f, 0.75f, 0.64f));

            // A ground plane outside the collision walls prevents the empty dark
            // void seen when the tablet camera followed a player to the north edge.
            Deco("Surrounding Konoha Landscape", new Vector3(0f, -0.65f, 0f),
                new Vector3(94f, 0.22f, 82f), landscape);

            // The 4v4 layout is still the collision foundation. Remove team-only
            // arrows and colours so the solo route reads as a single civic plaza.
            SetMaterial("Floor", pavement);
            foreach (string name in new[] {
                "NorthBoundary", "SouthBoundary", "EastBoundary", "WestBoundary",
                "NorthWestCover_Body", "SouthEastCover_Body", "NorthWestRelay",
                "SouthEastRelay"
            }) SetMaterial(name, stone);
            foreach (string name in new[] {
                "NorthTrim", "SouthTrim", "EastTrim", "WestTrim",
                "NorthWestCover_Top", "SouthEastCover_Top",
                "NorthWestCover_Slant", "SouthEastCover_Slant"
            }) SetMaterial(name, ivory);
            foreach (string name in new[] {
                "CyanSpawnBay", "OrangeSpawnBay", "CyanTeamWall", "OrangeTeamWall",
                "CyanWallStripe", "OrangeWallStripe", "NorthWestRelayCap",
                "SouthEastRelayCap", "NorthSideLane", "SouthSideLane", "CenterLane",
                "NorthWestCover_Accent", "SouthEastCover_Accent"
            }) RemoveObject(name);
            for (int z = -3; z <= 3; z += 2)
            {
                RemoveObject("CyanChevron_" + z + "_A");
                RemoveObject("CyanChevron_" + z + "_B");
                RemoveObject("OrangeChevron_" + z + "_A");
                RemoveObject("OrangeChevron_" + z + "_B");
            }
            for (int i = 0; i < 8; i++)
            {
                RemoveObject("TeamSpawn_" + i);
                RemoveObject("SpawnArrow_" + i + "_A");
                RemoveObject("SpawnArrow_" + i + "_B");
            }
            for (int z = -7; z <= 7; z += 7) RemoveObject("CenterGuide_" + z);
            // Low-cost scenery remains outside the walkable arena. The silhouettes
            // give the plaza a civic-garden setting even near camera boundaries.
            foreach (int side in new[] { -1, 1 })
            {
                for (int i = 0; i < 3; i++)
                    Tree("Garden tree", new Vector3(side * 20f, 0f, -9f + i * 9f),
                        stone, leaves, leavesLight);
                for (int i = 0; i < 2; i++)
                {
                    Tree("North garden tree", new Vector3(side * (5f + i * 7f), 0f, 17f),
                        stone, leaves, leavesLight);
                    Tree("South garden tree", new Vector3(side * (5f + i * 7f), 0f, -17f),
                        stone, leaves, leavesLight);
                }
                for (int i = 0; i < 2; i++)
                {
                    float z = i == 0 ? -7.5f : 7.5f;
                    Box("Courtyard planter", new Vector3(side * 14f, 0.20f, z),
                        new Vector3(1.2f, 0.40f, 2.0f), stone);
                    var shrub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    shrub.name = "Courtyard shrubs";
                    shrub.transform.position = new Vector3(side * 14f, 0.55f, z);
                    shrub.transform.localScale = new Vector3(1.0f, 0.62f, 1.72f);
                    shrub.GetComponent<Renderer>().sharedMaterial = leaves;
                    UnityEngine.Object.DestroyImmediate(shrub.GetComponent<Collider>());
                    Deco("Courtyard flowers", new Vector3(side * 14f, 0.88f, z),
                        new Vector3(0.36f, 0.10f, 0.36f), burgundy);
                }
            }
            Deco("Stone Processional", new Vector3(0, 0.012f, -5f),
                new Vector3(4.6f, 0.018f, 13f), route);
            Deco("Inner Plaza Paving", new Vector3(0, 0.012f, 4f),
                new Vector3(4.6f, 0.018f, 7f), route);
            foreach (int side in new[] { -1, 1 })
            {
                Deco("Side Processional", new Vector3(side * 5.5f, 0.014f, -6f),
                    new Vector3(9f, 0.019f, 2.8f), route);
                Deco("Courtyard Water", new Vector3(side * 11.3f, 0.036f, 5.8f),
                    new Vector3(3.8f, 0.018f, 4f), water);
                Box("Bridge to inner plaza", new Vector3(side * 7.9f, 0.15f, 5.8f),
                    new Vector3(1.35f, 0.3f, 3.4f), stone);
                Deco("Bridge parapet", new Vector3(side * 8.6f, 0.42f, 5.8f),
                    new Vector3(0.14f, 0.56f, 3.4f), gold);
                Deco("Konoha pennant", new Vector3(side * 13.8f, 1.75f, -8.2f),
                    new Vector3(0.11f, 3.1f, 0.75f), gateColor);
                Deco("Gateway pillar", new Vector3(side * 3.1f, 1.25f, 3.7f),
                    new Vector3(0.66f, 2.5f, 0.66f), stone);
                Deco("Gateway crown", new Vector3(side * 3.1f, 2.62f, 3.7f),
                    new Vector3(0.88f, 0.25f, 0.88f), gold);
            }
            Disc("Circular plaza border", new Vector3(0, 0.022f, 0), 8.2f, charcoal);
            Disc("Circular plaza stone", new Vector3(0, 0.042f, 0), 7.5f, stone);

            var sun = GameObject.Find("Sun")?.GetComponent<Light>();
            if (sun != null)
            {
                sun.color = new Color(1f, 0.87f, 0.67f);
                sun.intensity = 1.35f;
            }
            RenderSettings.ambientLight = new Color(0.53f, 0.53f, 0.48f);

            // The chair stays visible but is surrounded by colliders until the guard falls.
            var barrier = new GameObject("Gerbang Takhta - locked");
            Box("North Gate", new Vector3(0, 0.75f, 2.35f), new Vector3(5.1f, 1.5f, 0.26f), gateColor).transform.SetParent(barrier.transform);
            Box("South Gate", new Vector3(0, 0.75f, -2.35f), new Vector3(5.1f, 1.5f, 0.26f), gateColor).transform.SetParent(barrier.transform);
            Box("East Gate", new Vector3(2.35f, 0.75f, 0), new Vector3(0.26f, 1.5f, 5.1f), gateColor).transform.SetParent(barrier.transform);
            Box("West Gate", new Vector3(-2.35f, 0.75f, 0), new Vector3(0.26f, 1.5f, 5.1f), gateColor).transform.SetParent(barrier.transform);
            var plaza = Marker("Plaza Aspirasi", new Vector3(0, 0, -6.5f), green);
            var majelis = Institution("Majelis Daun", new Vector3(-9f, 0f, -6f), burgundy, stone, gold);
            var biro = Institution("Biro Prosedur", new Vector3(9f, 0f, -6f), biroColor, stone, gold);
            var garda = Marker("Garda Takhta", new Vector3(0, 0, 5.5f), gold);
            WorldLabel("PLAZA ASPIRASI", new Vector3(0, 0.45f, -6.5f));
            WorldLabel("ISTANA / GARDA", new Vector3(0, 0.45f, 5.5f));
            var chair = new GameObject("Kursi Kekuasaan");
            chair.transform.position = Vector3.zero;
            WorldLabel("ISTANA TAKHTA", new Vector3(0, 2.8f, 0));
            // Distinct fictional bird monument rather than an official coat of arms.
            Box("Monumen Burung Konoha Plinth", new Vector3(0, 0.4f, 9.5f),
                new Vector3(2.8f, 0.8f, 1.5f), stone);
            Deco("Abstract Bird Body", new Vector3(0, 2.15f, 9.5f), new Vector3(0.65f, 1.85f, 0.48f), gold);
            var birdHead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            birdHead.name = "Abstract Bird Head";
            birdHead.transform.position = new Vector3(0f, 3.18f, 9.5f);
            birdHead.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
            birdHead.GetComponent<Renderer>().sharedMaterial = gold;
            UnityEngine.Object.DestroyImmediate(birdHead.GetComponent<Collider>());
            for (int i = 0; i < 3; i++)
                foreach (int side in new[] { -1, 1 })
                {
                    var feather = Deco("Abstract Bird Feather", new Vector3(side * (0.72f + i * 0.67f),
                        2.65f + i * 0.27f, 9.5f), new Vector3(1.15f, 0.24f, 0.38f), gold);
                    feather.transform.rotation = Quaternion.Euler(0f, side * 12f, side * 19f);
                }
            Deco("Bird Tail", new Vector3(0f, 1.17f, 9.5f),
                new Vector3(0.36f, 0.85f, 0.24f), gold);
            for (int side = -1; side <= 1; side += 2)
            {
                Deco("Reflecting Pool", new Vector3(side * 10, 0.035f, 7.7f),
                    new Vector3(3.2f, 0.035f, 2.2f), water);
                Deco("Ceremonial Banner", new Vector3(side * 14.5f, 1.8f, 6f),
                    new Vector3(0.12f, 3.2f, 0.75f), burgundy);
                Deco("Red white civic ribbon", new Vector3(side * 14.3f, 2.35f, -3f),
                    new Vector3(0.12f, 0.44f, 2.1f), gateColor);
                Deco("Ivory civic ribbon", new Vector3(side * 14.3f, 1.93f, -3f),
                    new Vector3(0.12f, 0.40f, 2.1f), ivory);
            }

            var guard = new GameObject("GardaTakhta_Greybox");
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
            }
            var placeholder = player.transform.Find("PlaceholderSilhouette");
            if (placeholder != null)
                placeholder.GetComponent<Renderer>().sharedMaterial = Mat("CampaignHeroBody", new Color(0.17f, 0.20f, 0.24f));
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
            backdropRect.sizeDelta = new Vector2(820f, 93f);
            headingBackdrop.GetComponent<Image>().color = new Color(0.06f, 0.12f, 0.13f, 0.86f);
            headingBackdrop.GetComponent<Image>().raycastTarget = false;
            var objective = Text("CampaignObjective", safe, new Vector2(0.5f, 1f),
                new Vector2(0, -14), new Vector2(800, 43), 18);
            objective.alignment = TextAnchor.MiddleCenter;
            objective.color = new Color(1f, 0.89f, 0.65f);
            objective.gameObject.AddComponent<Outline>().effectColor = new Color(0.10f, 0.10f, 0.10f, 0.85f);
            var status = Text("CampaignStatus", safe, new Vector2(0.5f, 1f),
                new Vector2(0, -57), new Vector2(760, 28), 16);
            status.alignment = TextAnchor.MiddleCenter;
            status.gameObject.AddComponent<Outline>().effectColor = Color.black;
            var waypoint = Text("CampaignWaypoint", safe, new Vector2(0.5f, 1f),
                new Vector2(0, -112), new Vector2(600, 34), 17);
            waypoint.alignment = TextAnchor.MiddleCenter;
            waypoint.color = new Color(1f, 0.86f, 0.58f);
            waypoint.gameObject.AddComponent<Outline>().effectColor = Color.black;
            var feedback = Text("CampaignFeedback", safe, new Vector2(0.5f, 0.5f),
                new Vector2(0, 165), new Vector2(660, 44), 21);
            feedback.alignment = TextAnchor.MiddleCenter;
            feedback.color = new Color(1f, 0.82f, 0.42f);
            feedback.gameObject.AddComponent<Outline>().effectColor = Color.black;
            var heroText = Text("CampaignHero", safe, new Vector2(0.5f, 0f),
                new Vector2(0, 42), new Vector2(480, 34), 18);
            heroText.alignment = TextAnchor.MiddleCenter;
            var heroButton = Button("HeroSelector", "GANTI HERO", safe, new Vector2(1f, 0f),
                new Vector2(-270, 230), new Vector2(172, 56));
            var skill = Button("CampaignSkill", "PERISAI", safe, new Vector2(1f, 0f),
                new Vector2(-96, 230), new Vector2(172, 56));
            var basic = Button("CampaignBasic", "BASIC", safe, new Vector2(1f, 0f),
                new Vector2(-96, 154), new Vector2(172, 66));
            var sit = Button("CampaignSit", "DUDUK", safe, new Vector2(1f, 0f),
                new Vector2(-270, 154), new Vector2(172, 66));
            skill.GetComponent<Image>().color = new Color(0.50f, 0.16f, 0.18f, 0.96f);
            sit.GetComponent<Image>().color = new Color(0.59f, 0.42f, 0.18f, 0.96f);
            heroButton.GetComponent<Image>().color = new Color(0.18f, 0.25f, 0.29f, 0.96f);
            var footer = Text("CampaignRevision", safe, new Vector2(0.5f, 0f),
                new Vector2(0, 10), new Vector2(560, 26), 13);
            footer.alignment = TextAnchor.MiddleCenter;
            footer.text = "JALUR TAKHTA 0.0.7.2  •  SOLO PREVIEW";

            var preview = new GameObject("JalurTakhtaPreview").AddComponent<CampaignPreviewController>();
            preview.player = player;
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
            preview.attackButton = basic;
            preview.sitButton = sit;
            preview.heroButton = heroButton;
            preview.skillButton = skill;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Jalur Takhta preview prepared: " + ScenePath);
        }

        private static void RemoveObject(string name)
        {
            var item = GameObject.Find(name);
            if (item != null) UnityEngine.Object.DestroyImmediate(item);
        }

        private static void SetMaterial(string name, Material material)
        {
            var item = GameObject.Find(name);
            if (item != null && item.TryGetComponent<Renderer>(out var renderer))
                renderer.sharedMaterial = material;
        }

        private static GameObject Disc(string name, Vector3 center, float diameter, Material material)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = name;
            disc.transform.position = center;
            disc.transform.localScale = new Vector3(diameter, 0.02f, diameter);
            disc.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(disc.GetComponent<Collider>());
            return disc;
        }

        private static void Tree(string name, Vector3 position, Material trunk, Material canopy,
            Material highlight)
        {
            Deco(name + " trunk", position + new Vector3(0f, 1.05f, 0f),
                new Vector3(0.34f, 2.1f, 0.34f), trunk);
            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = name + " canopy";
            crown.transform.position = position + new Vector3(0f, 2.35f, 0f);
            crown.transform.localScale = new Vector3(2.5f, 1.15f, 2.5f);
            crown.GetComponent<Renderer>().sharedMaterial = canopy;
            UnityEngine.Object.DestroyImmediate(crown.GetComponent<Collider>());
            var top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            top.name = name + " highlight";
            top.transform.position = position + new Vector3(0f, 2.95f, 0f);
            top.transform.localScale = new Vector3(1.75f, 0.75f, 1.75f);
            top.GetComponent<Renderer>().sharedMaterial = highlight;
            UnityEngine.Object.DestroyImmediate(top.GetComponent<Collider>());
        }

        private static Transform Institution(string name, Vector3 location, Material accent,
            Material stone, Material gold)
        {
            var marker = Marker(name, location, accent);
            Box(name + " Facade", location + new Vector3(0, 1.65f, 1.6f), new Vector3(4.6f, 3.3f, 0.45f), stone);
            Box(name + " Roof", location + new Vector3(0, 3.45f, 1.45f), new Vector3(5.1f, 0.32f, 1.2f), accent);
            Deco(name + " Gold Cornice", location + new Vector3(0, 3.19f, 1.1f),
                new Vector3(4.8f, 0.12f, 0.16f), gold);
            foreach (int side in new[] { -1, 1 })
            {
                Box(name + " Column", location + new Vector3(side * 1.8f, 1.4f, 0.85f),
                    new Vector3(0.34f, 2.8f, 0.42f), stone);
                Deco(name + " Standard", location + new Vector3(side * 2.03f, 2.16f, 0.55f),
                    new Vector3(0.25f, 1.22f, 0.12f), accent);
            }
            WorldLabel(name.ToUpperInvariant(), location + new Vector3(0, 3.9f, 1.5f));
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
