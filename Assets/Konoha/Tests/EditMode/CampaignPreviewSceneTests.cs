using Konoha.Campaign;
using Konoha.Character;
using Konoha.Editor;
using Konoha.Input;
using UnityEngine.EventSystems;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Konoha.Tests
{
    public sealed class CampaignPreviewSceneTests
    {
        // 0.3.1: owner models under Assets/Konoha/Art/Models replace some code-built decor.
        private static bool Modelled(string slot) => Konoha.Editor.CampaignModelSlots.FindModelPath(slot) != null;

        private static bool AnyModel()
        {
            foreach (var slot in Konoha.Editor.CampaignModelSlots.Slots)
                if (Modelled(slot.name)) return true;
            return false;
        }

        [Test]
        public void ManualBuildPreparationPersistsCapitalAssetsAndPlayableRoutes()
        {
            try
            {
                CampaignPreviewProject.Prepare();
                // Reload to detect meshes/materials or component references that only existed in editor memory.
                EditorSceneManager.OpenScene(CampaignPreviewProject.ScenePath, OpenSceneMode.Single);
                Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo(CampaignPreviewProject.ScenePath));
                Assert.That(GameObject.Find("RuntimeNetworkingProof"), Is.Null);
                var preview = Object.FindFirstObjectByType<CampaignPreviewController>();
                Assert.That(preview, Is.Not.Null);
                Assert.That(GameObject.Find("CampaignRevision").GetComponent<UnityEngine.UI.Text>().text,
                    Is.EqualTo("JALUR TAKHTA 0.3.4  •  SOLO PREVIEW"));
                Assert.That(PlayerSettings.bundleVersion, Is.EqualTo("0.3.4"));
                Assert.That(PlayerSettings.Android.bundleVersionCode, Is.EqualTo(43));
                Assert.That(preview.sitButton, Is.Not.Null);
                Assert.That(preview.feedbackText, Is.Not.Null);
                Assert.That(preview.waypointText, Is.Not.Null);
                Assert.That(preview.objectiveProgress, Is.Not.Null);
                Assert.That(preview.chairBarrier.GetComponentsInChildren<BoxCollider>(), Has.Length.EqualTo(4));

                // 0.0.9: the solo scene is a local Netcode host that spawns the shared hero,
                // a director and organisation enemies from generated prefabs.
                var session = Object.FindFirstObjectByType<CampaignSession>();
                Assert.That(session, Is.Not.Null);
                Assert.That(session.GetComponent("NetworkManager"), Is.Not.Null);
                Assert.That(session.GetComponent("UnityTransport"), Is.Not.Null);
                foreach (var prefab in new[] { session.playerPrefab, session.directorPrefab, session.enemyPrefab })
                {
                    Assert.That(prefab, Is.Not.Null);
                    Assert.That(AssetDatabase.Contains(prefab), Is.True);
                    Assert.That(prefab.GetComponent("NetworkObject"), Is.Not.Null, prefab.name);
                }
                Assert.That(session.playerPrefab.GetComponent<Konoha.Networking.NetworkHeroKit>(), Is.Not.Null);
                Assert.That(session.directorPrefab.GetComponent<CampaignDirector>().enemyPrefab, Is.SameAs(session.enemyPrefab));
                var enemy = session.enemyPrefab.GetComponent<CampaignEnemy>();
                Assert.That(enemy, Is.Not.Null);
                Assert.That(enemy.visualRoot, Is.Not.Null);
                Assert.That(enemy.bodyRenderer, Is.Not.Null);
                Assert.That(enemy.nameplate, Is.Not.Null);
                Assert.That(enemy.motor.definition, Is.Not.Null);
                Assert.That(enemy.GetComponent<Konoha.Networking.NetworkPlayerCombat>(), Is.Not.Null);
                Assert.That(enemy.GetComponent<Konoha.Networking.NetworkHeroKit>(), Is.Not.Null);
                Assert.That(enemy.GetComponent<Konoha.Networking.NetworkWibawaBar>(), Is.Not.Null);
                foreach (var collider in enemy.GetComponentsInChildren<Collider>(true))
                    Assert.That(collider, Is.InstanceOf<CharacterController>(), "Decor collider on enemy: " + collider.name);
                // 0.2.3: organisation members have a human body with joints and faction headwear.
                Assert.That(enemy.body, Is.Not.Null);
                AssertJoints(enemy.body);
                Assert.That(enemy.body.motionSource, Is.SameAs(enemy.transform));
                Assert.That(enemy.primaryRenderers.Length, Is.GreaterThan(3));
                Assert.That(enemy.accentRenderers.Length, Is.GreaterThan(0));
                Assert.That(enemy.headwearMajelis, Is.Not.Null);
                Assert.That(enemy.headwearBiro, Is.Not.Null);
                Assert.That(enemy.headwearGarda, Is.Not.Null);
                Assert.That(enemy.visualRoot.Find("Body"), Is.Null, "The old capsule body is gone");
                foreach (string control in new[] { "AttackButton", "S1Button", "S2Button", "UltimateButton",
                    "DodgeButton", "HeroButton", "CampaignJump", "CampaignSit" })
                    Assert.That(GameObject.Find(control).GetComponent<UnityEngine.UI.Button>(), Is.Not.Null, control);

                // 0.0.9.2.1 hero screen (covers the HUD until MULAI) and RESTU RAKYAT marker.
                var heroSelect = Object.FindFirstObjectByType<CampaignHeroSelect>(FindObjectsInactive.Include);
                Assert.That(heroSelect, Is.Not.Null);
                Assert.That(heroSelect.heroButtons, Has.Length.EqualTo(4));
                foreach (var pick in heroSelect.heroButtons) Assert.That(pick, Is.Not.Null);
                Assert.That(heroSelect.startButton, Is.Not.Null);
                Assert.That(heroSelect.detailText, Is.Not.Null);
                Assert.That(heroSelect.panel, Is.Not.SameAs(heroSelect.gameObject),
                    "The controller must stay active while its panel is hidden");
                // 0.1.0: mode menu first (on top), then hero screen, then the result screen.
                var menu = Object.FindFirstObjectByType<CampaignModeMenu>();
                Assert.That(menu, Is.Not.Null);
                Assert.That(menu.session, Is.SameAs(session));
                Assert.That(session.waitForMenu, Is.True, "Solo must not host before JALUR TAKHTA is chosen");
                Assert.That(menu.pvpScene, Is.EqualTo(SpikeProject.ScenePath));
                Transform hud = menu.panel.transform.parent;
                Assert.That(menu.panel.transform.GetSiblingIndex(), Is.EqualTo(hud.childCount - 1));
                Assert.That(heroSelect.panel.transform.GetSiblingIndex(), Is.EqualTo(hud.childCount - 2));
                var result = Object.FindFirstObjectByType<CampaignResultScreen>();
                Assert.That(result, Is.Not.Null);
                Assert.That(result.panel, Is.Not.SameAs(result.gameObject));
                Assert.That(result.retryButton, Is.Not.Null);
                Assert.That(result.changeHeroButton, Is.Not.Null);
                Assert.That(EditorBuildSettings.scenes, Has.Length.EqualTo(2));
                Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo(CampaignPreviewProject.ScenePath));
                Assert.That(EditorBuildSettings.scenes[1].path, Is.EqualTo(SpikeProject.ScenePath));
                Assert.That(preview.gardaLockdown, Is.Not.Null);
                Assert.That(preview.gardaLockdown.activeSelf, Is.False, "LOCKDOWN starts open");
                Assert.That(preview.traversal, Is.Not.Null);
                Assert.That(preview.restuAura, Is.Not.Null);
                // 0.0.9.4: roofs/gates/palms hide while covering the hero; sound effects with a toggle.
                Assert.That(session.occluders, Is.Not.Null);
                Assert.That(session.occluders.candidates.Length, Is.GreaterThan(20));
                foreach (var occluder in session.occluders.candidates)
                {
                    Assert.That(occluder, Is.Not.Null);
                    Assert.That(occluder.bounds.max.y, Is.GreaterThan(.5f), "Floor-level decor is never an occluder: " + occluder.name);
                }
                // 0.2.0 Suasana Nusantara: the street dressing exists and never blocks movement.
                foreach (string landmark in new[] { "NusantaraGround jalan raya", "NusantaraTall warung kopi",
                    "NusantaraTall gerobak bakso", "Nusantara gunung", "NusantaraGround sawah", "Nusantara motor bebek" })
                    Assert.That(GameObject.Find(landmark), Is.Not.Null, landmark);
                int nusantara = 0;
                foreach (var decor in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!decor.name.StartsWith("Nusantara")) continue;
                    nusantara++;
                    Assert.That(decor.GetComponent<Collider>(), Is.Null, "Decor must not collide: " + decor.name);
                }
                Assert.That(nusantara, Is.InRange(150, 900), "Decor budget for the tablet");
                // 0.2.1: no 17-an decoration; surfaces carry normal maps; colour grading on the camera.
                Assert.That(GameObject.Find("NusantaraTall umbul-umbul"), Is.Null);
                Assert.That(GameObject.Find("Nusantara bendera segitiga"), Is.Null);
                var roofTile = GameObject.Find("Tiered Nusantara roof").GetComponent<Renderer>().sharedMaterial;
                Assert.That(roofTile.name, Is.EqualTo("GentengTanahLiat"));
                Assert.That(roofTile.GetTexture("_BumpMap"), Is.Not.Null, "Genteng needs a normal map");
                var grading = GameObject.Find("NusantaraColourGrading");
                Assert.That(grading, Is.Not.Null);
                Assert.That(grading.GetComponent<Volume>().sharedProfile, Is.Not.Null);
                Assert.That(Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing, Is.True);
                // 0.2.5 Nusantara Megah: banners, towers, cascades, horizon.
                // 0.3.0: roads are plain stone paving again (no batik/motif tiles).
                Assert.That(GameObject.Find("Gerbang Rakyat boulevard").GetComponent<Renderer>().sharedMaterial.name,
                    Is.EqualTo("MegahJalanBatu"));
                Assert.That(GameObject.Find("Civic forecourt inlay").GetComponent<Renderer>().sharedMaterial.name,
                    Is.EqualTo("MegahPelataranBatu"));
                var flow = Object.FindFirstObjectByType<CampaignWaterFlow>();
                Assert.That(flow, Is.Not.Null);
                Assert.That(flow.falls, Has.Length.EqualTo(2));
                Assert.That(flow.foam, Has.Length.EqualTo(6));
                int megah = 0, towers = 0, wallBanners = 0;
                foreach (var decor in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!decor.name.StartsWith("Megah")) continue;
                    megah++;
                    if (decor.name == "MegahTall menara")
                    {
                        towers++;
                        Assert.That(decor.gameObject.layer, Is.EqualTo(2), "Towers are solid but ignored by the orbit camera");
                        Assert.That(Mathf.Abs(decor.position.x), Is.GreaterThan(12f), "Towers stay off the route");
                        continue;
                    }
                    if (decor.name == "MegahTall spanduk dinding") wallBanners++;
                    Assert.That(decor.GetComponent<Collider>(), Is.Null, "Megah decor must not collide: " + decor.name);
                }
                Assert.That(towers, Is.EqualTo(6));
                Assert.That(wallBanners, Is.EqualTo(12));
                Assert.That(megah, Is.InRange(AnyModel() ? 50 : 200, 900), "Megah decor budget for the tablet");
                Assert.That(Camera.main.farClipPlane, Is.GreaterThanOrEqualTo(RenderSettings.fogEndDistance));
                // 0.2.6 Rasa Nusantara: stupa instead of the guardian figure, heritage buildings,
                // lotus inlay, candi bentar at the boulevard mouth.
                Assert.That(GameObject.Find("Guardian head"), Is.Null);
                Assert.That(GameObject.Find("Stupa genta"), Is.Not.Null);
                Assert.That(GameObject.Find(Modelled("GedungLama") ? "Model GedungLama" : "Gedung lama jendela"), Is.Not.Null);
                Assert.That(GameObject.Find("Tower recessed arcade"), Is.Null);
                Assert.That(GameObject.Find("MegahTall candi bentar"), Is.Not.Null);
                // 0.2.7 flicker fixes: overview never runs the occluder, near plane for depth precision,
                // ground layers at least ~1.5 cm apart where they overlap.
                Assert.That(Object.FindFirstObjectByType<CampaignArenaView>().occluders, Is.Not.Null);
                Assert.That(Camera.main.nearClipPlane, Is.GreaterThanOrEqualTo(.5f));
                float Top(string n) => GameObject.Find(n).GetComponent<Renderer>().bounds.max.y;
                Assert.That(Top("Gerbang Rakyat boulevard") - Top("Ceremonial main lane"), Is.GreaterThan(.015f));
                Assert.That(Top("Civic forecourt inlay") - Top("Ceremonial main lane"), Is.GreaterThan(.015f));
                Assert.That(Top("Ceremonial main lane"), Is.GreaterThan(.015f), "Above the oval promenade (y 0)");
                Assert.That(Top("Surrounding Konoha Landscape"), Is.LessThan(-.05f));
                Assert.That(GameObject.Find(Modelled("RumahKampung") ? "Model RumahKampung" : "Kota rumah pelana"), Is.Not.Null);
                Assert.That(Object.FindFirstObjectByType<CampaignCityLife>().bowlSpots, Is.Not.Empty);
                // 0.2.8: close street camera, opt-in; the JAUH preset equals the tested default (§13).
                var cameraMode = Object.FindFirstObjectByType<CampaignCameraMode>();
                Assert.That(cameraMode, Is.Not.Null);
                Assert.That(cameraMode.button, Is.Not.Null);
                Assert.That(cameraMode.follow, Is.Not.Null);
                Assert.That(cameraMode.far.pitch, Is.EqualTo(cameraMode.follow.resetPitch));
                Assert.That(cameraMode.far.distance, Is.EqualTo(cameraMode.follow.resetDistance));
                Assert.That(cameraMode.close.distance, Is.LessThan(cameraMode.far.distance));
                Assert.That(cameraMode.close.minDistance, Is.GreaterThanOrEqualTo(.8f + 1f), "Never inside the hero");
                // 0.2.9: round icon buttons; every control is square and wired.
                var skillHud = Object.FindFirstObjectByType<CampaignSkillHud>();
                Assert.That(skillHud, Is.Not.Null);
                Assert.That(skillHud.buttons, Has.Length.EqualTo(7));
                foreach (var entry in skillHud.buttons)
                {
                    Assert.That(entry.button, Is.Not.Null, entry.slot.ToString());
                    var size = ((RectTransform)entry.button.transform).sizeDelta;
                    Assert.That(size.x, Is.EqualTo(size.y), "Round buttons are square: " + entry.slot);
                    Assert.That(entry.button.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>(), Is.Not.Null,
                        "The caption stays the first child (the hero code writes cooldowns there)");
                }
                // 0.2.8: tumpang roof on the Istana, kampung street furniture without colliders.
                Assert.That(GameObject.Find("Fictional civic dome"), Is.Null);
                foreach (string piece in new[] { "Kampung gapura", "Kampung becak", Modelled("PohonPisang") ? "Model PohonPisang" : "Kampung pisang batang", "Kampung PJU" })
                    Assert.That(GameObject.Find(piece), Is.Not.Null, piece);
                foreach (var decor in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (decor.name.StartsWith("Kampung"))
                        Assert.That(decor.GetComponent<Collider>(), Is.Null, "Kampung decor must not collide: " + decor.name);

                // 0.2.3: hero bodies (one per hero, inactive templates), effects and camera shake.
                var bodies = Object.FindFirstObjectByType<CampaignBodies>();
                Assert.That(bodies, Is.Not.Null);
                Assert.That(bodies.heroTemplates, Has.Length.EqualTo(4));
                foreach (var template in bodies.heroTemplates)
                {
                    Assert.That(template, Is.Not.Null);
                    Assert.That(template.gameObject.activeSelf, Is.False, "Templates stay hidden");
                    AssertJoints(template);
                    foreach (var part in template.GetComponentsInChildren<Transform>(true))
                        Assert.That(part.GetComponent<Collider>(), Is.Null, "Body parts never collide: " + part.name);
                }
                // 0.3.0: rounded bodies; MEGA's kain is a saved lathe mesh with a batik texture.
                Transform kain = null, torso = null;
                foreach (var part in bodies.heroTemplates[0].GetComponentsInChildren<Transform>(true))
                {
                    if (part.name == "Kain") kain = part;
                    if (part.name == "Badan") torso = part;
                }
                Assert.That(kain, Is.Not.Null, "MEGA wears a kain");
                Assert.That(kain.GetComponent<MeshFilter>().sharedMesh.name, Is.EqualTo("RigKain"));
                Assert.That(AssetDatabase.Contains(kain.GetComponent<MeshFilter>().sharedMesh), Is.True);
                Assert.That(kain.GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap"), Is.Not.Null, "Batik on the kain");
                Assert.That(torso.GetComponent<MeshFilter>().sharedMesh.name, Is.EqualTo("RigTorso"));
                Assert.That(GameObject.Find(Modelled("Lentera") ? "Model Lentera" : "Lentera atap"), Is.Not.Null);
                Assert.That(GameObject.Find("Civic lamp glass"), Is.Null);
                Assert.That(GameObject.Find("Radial stone inlay"), Is.Null);
                Assert.That(GameObject.Find(Modelled("PohonKetapang") ? "Model PohonKetapang" : "NusantaraTall tajuk ketapang"), Is.Not.Null);
                Assert.That(bodies.fx, Is.Not.Null);
                foreach (var material in new[] { bodies.fx.glow, bodies.fx.dust, bodies.fx.kerbauHide, bodies.fx.kerbauHorn,
                    bodies.fx.skin, bodies.fx.cloth, bodies.fx.pants, bodies.fx.hair, bodies.fx.wood, bodies.fx.cone, bodies.fx.white })
                {
                    Assert.That(material, Is.Not.Null);
                    Assert.That(AssetDatabase.Contains(material), Is.True, "Effect materials are saved assets: " + material.name);
                }
                Assert.That(bodies.fx.glow.shader.name, Is.EqualTo("Universal Render Pipeline/Unlit"));
                Assert.That(Camera.main.GetComponent<CampaignCameraShake>(), Is.Not.Null);

                // 0.2.2 Kota Hidup: traffic, warga and fountains exist, never collide, stay within budget.
                var city = Object.FindFirstObjectByType<CampaignCityLife>();
                Assert.That(city, Is.Not.Null);
                Assert.That(city.ringCorners, Has.Length.EqualTo(4));
                Assert.That(city.vehicles.Length, Is.InRange(8, 14));
                Assert.That(city.walkers.Length, Is.InRange(20, 36));
                Assert.That(city.jets.Length, Is.EqualTo(city.nozzles.Length));
                Assert.That(city.droplets.Length, Is.GreaterThan(0));
                foreach (var vehicle in city.vehicles) Assert.That(vehicle.body, Is.Not.Null);
                foreach (var walker in city.walkers)
                {
                    Assert.That(walker.root, Is.Not.Null);
                    Assert.That(walker.legLeft, Is.Not.Null);
                    Assert.That(walker.armRight, Is.Not.Null);
                }
                foreach (Vector3 corner in city.ringCorners)
                    Assert.That(Mathf.Abs(corner.x) > 34f || Mathf.Abs(corner.z - 4f) > 58f, Is.True,
                        "Ring road stays outside the playable oval");
                int kota = 0;
                foreach (var decor in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!decor.name.StartsWith("Kota")) continue;
                    kota++;
                    Assert.That(decor.GetComponent<Collider>(), Is.Null, "Kota Hidup must not collide: " + decor.name);
                }
                Assert.That(kota, Is.InRange(AnyModel() ? 50 : 300, 2800), "Kota Hidup budget for the tablet (0.2.7 house details, 0.3.4 warga faces)");
                Assert.That(GameObject.Find("KotaTall spanduk lambang Konoha"), Is.Not.Null);
                Assert.That(GameObject.Find("Kota ruko"), Is.Not.Null);
                Assert.That(GameObject.Find("Kota teluk"), Is.Not.Null);
                var audio = Object.FindFirstObjectByType<CampaignAudio>();
                Assert.That(audio, Is.Not.Null);
                Assert.That(audio.muteButton, Is.Not.Null);
                foreach (var click in audio.clickButtons) Assert.That(click, Is.Not.Null);
                Assert.That(Camera.main.GetComponent<AudioListener>(), Is.Not.Null, "Sound needs a listener on the camera");
                Assert.That(preview.restuAura.GetComponent<Collider>(), Is.Null);

                var stage = preview.stage;
                Assert.That(stage, Is.SameAs(session.stage));
                Assert.That(stage.plaza, Is.Not.Null);
                Assert.That(stage.majelis, Is.Not.Null);
                Assert.That(stage.biro, Is.Not.Null);
                Assert.That(stage.garda, Is.Not.Null);
                Assert.That(stage.chair, Is.Not.Null);
                // 0.0.9.1 route: spawn far south, seat on the raised north terrace.
                Assert.That(stage.startPoint.z, Is.LessThan(-40f));
                Assert.That(stage.chair.position.z, Is.GreaterThan(40f));
                Assert.That(stage.chair.position.y, Is.EqualTo(stage.terraceHeight).Within(.01f));
                Assert.That(stage.majelis.position.x, Is.LessThan(-20f));
                Assert.That(stage.biro.position.x, Is.GreaterThan(20f));
                Assert.That(preview.innerGateClosed, Is.Not.Null);
                Assert.That(preview.innerGateOpen, Is.Not.Null);
                Assert.That(preview.innerGateClosed.activeSelf, Is.True);
                Assert.That(preview.innerGateOpen.activeSelf, Is.False);
                Assert.That(preview.innerGateClosed.GetComponentsInChildren<BoxCollider>(), Has.Length.EqualTo(2));
                Assert.That(preview.innerGateOpen.GetComponentsInChildren<Collider>(true), Is.Empty);
                foreach (string sign in new[] { "Sign GERBANG RAKYAT", "Sign PLAZA ASPIRASI", "Sign MAJELIS DAUN",
                    "Sign BIRO PROSEDUR", "Sign GERBANG DALAM", "Sign GARDA TAKHTA", "Sign ISTANA TAKHTA" })
                    Assert.That(GameObject.Find(sign), Is.Not.Null, sign);

                var monument = Object.FindFirstObjectByType<CampaignMonument>();
                Assert.That(session.monument, Is.SameAs(monument));
                Assert.That(monument.solid, Is.Not.Null);
                Assert.That(monument.transparentMaterials, Has.Length.EqualTo(monument.surfaces.Length));
                foreach (var material in monument.transparentMaterials)
                {
                    Assert.That(AssetDatabase.Contains(material), Is.True);
                    Assert.That(material.GetFloat("_Surface"), Is.EqualTo(1f));
                }
                foreach (string obsolete in new[] { "CyanSpawnBay", "CyanChevron_-3_A", "SpawnArrow_0_A",
                    "NorthWestCover_Body", "SouthEastCover_Body", "NorthWestRelay", "SouthEastRelay",
                    "NorthBoundary", "SouthBoundary", "EastBoundary", "WestBoundary",
                    "GardaTakhta", "HeroSelector", "CampaignSkill", "CampaignBasic", "Hero Ground Marker" })
                    Assert.That(GameObject.Find(obsolete), Is.Null, obsolete);
                foreach (string landmark in new[] { "Circular plaza stone", "Monumen Garuda Konoha",
                    "Istana mustaka", "Bridge across reflecting pool", "Majelis Daun interaction boundary",
                    "Biro Prosedur interaction boundary", "Tiered Nusantara roof", "Palm curved frond" })
                    Assert.That(GameObject.Find(landmark), Is.Not.Null, landmark);
                var capital = GameObject.Find("CapitalEnvironment");
                foreach (var mesh in capital.GetComponentsInChildren<MeshFilter>())
                {
                    Assert.That(mesh.sharedMesh, Is.Not.Null, mesh.name);
                    Assert.That(AssetDatabase.GetAssetPath(mesh.sharedMesh), Is.Not.Empty, mesh.name);
                }
                Assert.That(GameObject.Find("Floor").GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap"), Is.Not.Null);
                var pipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
                Assert.That(pipeline, Is.Not.Null);
                Assert.That(pipeline.supportsMainLightShadows, Is.True);
                Assert.That(pipeline.shadowDistance, Is.GreaterThan(30f));
                Assert.That(GameObject.Find("Sun").GetComponent<Light>().shadows, Is.EqualTo(LightShadows.Soft));
                var pvp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(SpikeProject.Generated + "/SpikeURP.asset");
                Assert.That(pvp, Is.Not.SameAs(pipeline));
                Assert.That(pvp.shadowDistance, Is.Zero);
                Assert.That(Object.FindFirstObjectByType<CampaignArenaView>().viewButton, Is.Not.Null);
                Assert.That(GameObject.Find("TouchCanvas").GetComponent<Konoha.UI.SafeAreaLayout>().compactJoystick, Is.True);
                var camera = Object.FindFirstObjectByType<MobileCombatCamera>();
                Assert.That(camera.limitFocusToArena, Is.False);
                Assert.That(camera.allowOrbit, Is.True);
                Assert.That(session.movementCamera, Is.EqualTo(camera.transform));

                // The offline character is the editor collision probe; the session hides it at runtime.
                var probe = session.offlineHero.GetComponent<CharacterMotor>();
                Assert.That(probe, Is.Not.Null);
                Assert.That(probe.allowJump, Is.True);
                Assert.That(Object.FindFirstObjectByType<Konoha.Core.OfflineSpikeDriver>().enabled, Is.False);
                var traversal = Object.FindFirstObjectByType<CampaignTraversal>();
                Assert.That(session.traversal, Is.SameAs(traversal));
                Assert.That(traversal.motor, Is.SameAs(probe));
                Assert.That(traversal.jumpButton, Is.Not.Null);
                Assert.That(traversal.movementCamera, Is.EqualTo(camera.transform));
                Assert.That(traversal.boundaryRadii.x, Is.GreaterThan(30));
                Assert.That(traversal.boundaryRadii.y, Is.GreaterThan(55));
                Assert.That(camera.resetPitch, Is.LessThan(25f));
                Assert.That(GameObject.Find("Camera drag surface").GetComponent<CampaignCameraDrag>().follow, Is.SameAs(camera));
                var view = Object.FindFirstObjectByType<CampaignArenaView>();
                float timeScale = Time.timeScale;
                view.Toggle();
                try
                {
                    Assert.That(Time.timeScale, Is.Zero);
                    Assert.That(traversal.enabled, Is.False);
                    Assert.That(camera.enabled, Is.False);
                    Assert.That(traversal.jumpButton.gameObject.activeSelf, Is.False);
                }
                finally { view.Toggle(); }
                Assert.That(Time.timeScale, Is.EqualTo(timeScale));
                Assert.That(traversal.enabled && camera.enabled, Is.True);
                Assert.That(traversal.jumpButton.gameObject.activeSelf, Is.True);

                // Sample actual collision along the complete campaign route, excluding the probe's own capsule.
                // Points carry their ground height (ramp and 5 m terrace).
                preview.chairBarrier.SetActive(false);
                preview.innerGateClosed.SetActive(false);
                Physics.SyncTransforms();
                Vector3 seat = stage.chair.position;
                Vector3[] route = { new Vector3(0,0,-44), new Vector3(0,0,-30), new Vector3(0,0,-8),
                    // 0.0.9.3: the Biro hall is walked through its loket row (the office in front
                    // of the hall at x 25..31 is walled).
                    new Vector3(-28,0,-5), new Vector3(22,0,-5), new Vector3(22,0,-9.5f), new Vector3(31.8f,0,-9.5f),
                    new Vector3(22,0,-9.5f), new Vector3(22,0,-5), new Vector3(4.4f,0,-5), new Vector3(4.4f,0,7.4f),
                    new Vector3(2.5f,0,13), new Vector3(0,0,15), new Vector3(0,0,19.5f), new Vector3(0,0,28),
                    stage.rampBase + Vector3.forward, stage.rampTop - Vector3.forward, seat - Vector3.forward * 1.2f };
                for (int segment = 1; segment < route.Length; segment++)
                    for (float distance = 0; distance <= Vector3.Distance(route[segment-1],route[segment]); distance += .2f)
                    {
                        Vector3 p = Vector3.MoveTowards(route[segment-1],route[segment],distance);
                        AssertFree(p, probe.transform, "Blocked campaign route at ");
                    }
                // Enemies and revived heroes must never appear inside geometry.
                foreach (var point in stage.gateSpawnPoints) AssertFree(Flat(point), probe.transform, "Gate spawn blocked at ");
                foreach (var point in stage.gardaSpawnPoints) AssertFree(Flat(point), probe.transform, "Garda spawn blocked at ");
                foreach (var point in stage.counterattackSpawnPoints)
                    AssertFree(Flat(point), probe.transform, "Counterattack spawn blocked at ");
                foreach (var point in stage.gardaReinforcePoints)
                    AssertFree(Flat(point), probe.transform, "Garda reinforcement blocked at ");
                // Garda points inside the LOCKDOWN ring; checkpoint 5 outside it (and clear of its walls).
                foreach (var point in stage.gardaSpawnPoints)
                    Assert.That(CampaignObjectiveDirector.Near(point, stage.garda.position, CampaignTuning.Garda.LockdownRadius - 1f), Is.True);
                Assert.That(CampaignObjectiveDirector.Near(stage.CheckpointPosition(CampaignCheckpoint.GardaTakhta),
                    stage.garda.position, CampaignTuning.Garda.LockdownRadius + .7f), Is.False);
                // With the ring closed, the checkpoint and the ring's inside stay free.
                preview.gardaLockdown.SetActive(true);
                Physics.SyncTransforms();
                AssertFree(Flat(stage.CheckpointPosition(CampaignCheckpoint.GardaTakhta)), probe.transform, "Checkpoint 5 inside the ring wall at ");
                foreach (var point in stage.gardaSpawnPoints) AssertFree(Flat(point), probe.transform, "Garda point inside the ring wall at ");
                preview.gardaLockdown.SetActive(false);
                Physics.SyncTransforms();
                // 0.0.9.2 Majelis Daun sidang: one point per roster unit, all clear and inside the hall leash.
                Assert.That(stage.majelisSpawnPoints.Length, Is.GreaterThanOrEqualTo(
                    EncounterComposer.TotalUnits(EncounterComposer.Compose(FactionId.MajelisDaun, 1))));
                foreach (var point in stage.majelisSpawnPoints)
                {
                    AssertFree(Flat(point), probe.transform, "Majelis spawn blocked at ");
                    Assert.That(CampaignObjectiveDirector.Near(point, stage.majelis.position, CampaignTuning.Majelis.LeashRadius),
                        Is.True, "Majelis member outside its hall leash: " + point);
                }
                // 0.0.9.3 Biro Prosedur: every loket centre, member point and door waypoint is clear.
                Assert.That(stage.loketPoints, Has.Length.EqualTo(CampaignTuning.Biro.LoketCount));
                foreach (var point in stage.loketPoints) AssertFree(Flat(point), probe.transform, "Loket blocked at ");
                foreach (var point in stage.biroArsipPoints) AssertFree(Flat(point), probe.transform, "Arsip spawn blocked at ");
                foreach (var point in new[] { stage.biroLeaderPoint, stage.biroSpecialistPoint, stage.biroGuardPoint,
                    stage.officeDoorOutside })
                    AssertFree(Flat(point), probe.transform, "Biro point blocked at ");
                Assert.That(stage.InOffice(stage.biroLeaderPoint), Is.True, "Kepala Biro starts in his office");
                Assert.That(stage.InOffice(stage.officeDoorOutside), Is.False);
                foreach (var point in stage.loketPoints)
                    Assert.That(stage.InOffice(point), Is.False, "Loket inside the office: " + point);
                Assert.That(preview.loketZones, Has.Length.EqualTo(stage.loketPoints.Length));
                Assert.That(preview.loketLabels, Has.Length.EqualTo(stage.loketPoints.Length));
                Assert.That(preview.biroDoorClosed, Is.Not.Null);
                Assert.That(preview.debuffText, Is.Not.Null);
                // Steering leaves the office through the door, never through a wall.
                Vector3 exit = stage.Steer(stage.biroLeaderPoint, stage.loketPoints[0]);
                Assert.That(Mathf.Abs(exit.x - stage.officeDoorInside.x), Is.LessThan(.01f));

                Assert.That(CampaignObjectiveDirector.Near(stage.CheckpointPosition(CampaignCheckpoint.MajelisDaun),
                    stage.majelis.position, CampaignTuning.Majelis.LeashRadius + .5f), Is.False,
                    "Majelis checkpoint must lie outside the sidang leash");
                foreach (CampaignCheckpoint checkpoint in System.Enum.GetValues(typeof(CampaignCheckpoint)))
                    AssertFree(Flat(stage.CheckpointPosition(checkpoint)), probe.transform, "Checkpoint " + checkpoint + " blocked at ");

                // §13: the seat is visible from the spawn with the default camera, above the
                // monument and inside the frame (Ignore Raycast walls/seals are see-through).
                Vector3 focus = stage.startPoint + Vector3.up * .8f;
                Vector3 eye = focus + Quaternion.Euler(camera.resetPitch, 0, 0) * Vector3.back * camera.resetDistance;
                Vector3 seatTop = seat + Vector3.up * .9f;
                Vector3 sight = seatTop - eye;
                bool hidden = Physics.Raycast(eye, sight.normalized, out RaycastHit blocker, sight.magnitude - .3f);
                Assert.That(hidden, Is.False, "Seat hidden from the spawn camera by " + (hidden ? blocker.collider.name : ""));
                Assert.That(Vector3.Angle(focus - eye, sight), Is.LessThan(camera.GetComponent<Camera>().fieldOfView * .5f - 1f),
                    "Seat outside the default spawn frame");
                float sculptureTop = 0f;
                foreach (var surface in monument.surfaces)
                    sculptureTop = Mathf.Max(sculptureTop, surface.bounds.max.y);
                float along = (monument.solid.bounds.center.z - eye.z) / sight.z;
                Assert.That(eye.y + sight.y * along, Is.GreaterThan(sculptureTop + .2f), "Sightline must clear the Garuda sculpture");

                VerifyWaterExitsAndJump(probe);
                // Jump must not bypass the locked seat enclosure on the terrace...
                preview.chairBarrier.SetActive(true);
                Physics.SyncTransforms();
                probe.Teleport(seat + new Vector3(0,.1f,-3.4f));
                Settle(probe);
                Assert.That(probe.TryJump(), Is.True);
                for(int i=0;i<90;i++) probe.Step(new MoveIntent(Vector2.up),1f/60f);
                Assert.That(probe.transform.position.z,Is.LessThan(seat.z - 2.7f));
                // ...nor the locked Gerbang Dalam.
                preview.innerGateClosed.SetActive(true);
                Physics.SyncTransforms();
                probe.Teleport(new Vector3(0,.1f,stage.innerGateZ - 2f));
                Settle(probe);
                Assert.That(probe.TryJump(), Is.True);
                for(int i=0;i<90;i++) probe.Step(new MoveIntent(Vector2.up),1f/60f);
                Assert.That(probe.transform.position.z,Is.LessThan(stage.innerGateZ - .5f));
            }
            finally { SpikeProject.Prepare(); }
        }

        // 'ground' is the walking surface height at that point.
        private static void AssertFree(Vector3 ground, Transform ignored, string message)
        {
            foreach (var hit in Physics.OverlapCapsule(ground+Vector3.up*.5f,ground+Vector3.up*1.5f,.42f))
                Assert.That(hit.transform.IsChildOf(ignored), Is.True, message + ground + " by " + hit.name);
        }

        private static Vector3 Flat(Vector3 p) => new Vector3(p.x, 0f, p.z);

        private static void Settle(CharacterMotor motor)
        {
            Physics.SyncTransforms();
            for(int i=0;i<45;i++) motor.Step(new MoveIntent(Vector2.zero),1f/60f);
            Assert.That(motor.Grounded,Is.True);
        }

        private static void VerifyWaterExitsAndJump(CharacterMotor motor)
        {
            foreach(int side in new[]{-1,1}) foreach(int exit in new[]{-1,1})
            {
                motor.Teleport(new Vector3(side*10,.12f,5.8f+exit*1.7f));
                Settle(motor);
                for(int i=0;i<35;i++) motor.Step(new MoveIntent(new Vector2(0,exit)),1f/60f);
                Assert.That((motor.transform.position.z-5.8f)*exit,Is.GreaterThan(3.2f),
                    "Could not walk out of canal without jumping");
            }
            motor.Teleport(new Vector3(0,.1f,-9));
            Settle(motor);
            float start=motor.transform.position.y, peak=start;
            Assert.That(motor.TryJump(),Is.True);
            Assert.That(motor.TryJump(),Is.False,"Duplicate launch in same frame");
            for(int i=0;i<120;i++)
            {
                motor.Step(new MoveIntent(Vector2.zero),1f/60f);
                peak=Mathf.Max(peak,motor.transform.position.y);
                if(i==10) Assert.That(motor.TryJump(),Is.False,"Air jump must be rejected");
            }
            Assert.That(peak-start,Is.InRange(1f,1.4f));
            Assert.That(motor.Grounded,Is.True);
            Assert.That(motor.TryJump(),Is.True,"Jump should become available again after landing");
            motor.Teleport(new Vector3(0,.1f,-9));
        }

        [Test]
        public void CameraRelativeMovementAndInvisibleOvalBoundaryRemainConsistent()
        {
            var east=CampaignTraversal.CameraRelative(Vector2.up,Vector3.right);
            Assert.That(east.x,Is.EqualTo(1).Within(.001f));
            Assert.That(east.y,Is.EqualTo(0).Within(.001f));
            Assert.That(CampaignTraversal.CameraRelative(Vector2.one,Vector3.back).magnitude,Is.LessThanOrEqualTo(1.0001f));
            var center=new Vector2(0,4); var radii=new Vector2(26,29);
            Vector3 outside=CampaignTraversal.ClampToCampus(new Vector3(70,1.2f,80),center,radii);
            float oval=outside.x*outside.x/(26*26)+(outside.z-4)*(outside.z-4)/(29*29);
            Assert.That(oval,Is.EqualTo(1).Within(.0001f));
            Assert.That(outside.y,Is.EqualTo(1.2f));
            Assert.That(CampaignTraversal.ClampToCampus(new Vector3(20,0,4),center,radii),Is.EqualTo(new Vector3(20,0,4)));
        }

        [Test]
        public void CameraGestureIgnoresUnownedFingerAndClampsPitchAndZoom()
        {
            var cameraObject=new GameObject("Test orbit camera");
            var dragObject=new GameObject("Test drag");
            try
            {
                var follow=cameraObject.AddComponent<MobileCombatCamera>(); follow.allowOrbit=true;
                var drag=dragObject.AddComponent<CampaignCameraDrag>(); drag.follow=follow;
                drag.OnPointerDown(new PointerEventData(null){pointerId=4,position=new Vector2(100,100)});
                drag.OnDrag(new PointerEventData(null){pointerId=9,position=new Vector2(400,100)});
                Assert.That(follow.orbitYaw,Is.Zero);
                drag.OnDrag(new PointerEventData(null){pointerId=4,position=new Vector2(200,100)});
                Assert.That(follow.orbitYaw,Is.GreaterThan(0));
                drag.OnPointerUp(new PointerEventData(null){pointerId=4});
                follow.RotateOrbit(new Vector2(0,100));
                Assert.That(follow.orbitPitch,Is.EqualTo(25));
                follow.ZoomOrbit(100);
                Assert.That(follow.orbitDistance,Is.EqualTo(13));
                follow.ResetOrbit();
                Assert.That(follow.orbitYaw,Is.Zero);
                Assert.That(follow.orbitDistance,Is.EqualTo(22));
            }
            finally { Object.DestroyImmediate(dragObject); Object.DestroyImmediate(cameraObject); }
        }

        [Test]
        public void GuardDetoursAroundCentralMonumentAndReachesThrone()
        {
            var bounds = new Bounds(new Vector3(0,2.9f,1.75f),new Vector3(1.35f,5.8f,1.35f));
            Vector3 position = new Vector3(0,0,7.4f);
            Vector3 target = Vector3.zero;
            var clearance = bounds;
            clearance.Expand(new Vector3(1f,0,1f));
            for (int i=0; i<400 && Vector3.Distance(position,target)>.1f; i++)
            {
                Vector3 next = CampaignMonument.GuardDestination(position,target,bounds);
                position = Vector3.MoveTowards(position,next,.07f);
                Assert.That(clearance.Contains(position+Vector3.up),Is.False,"Guard cut through monument");
            }
            Assert.That(Vector3.Distance(position,target),Is.LessThan(.15f));
        }

        private static void AssertJoints(CampaignHumanoid body)
        {
            foreach (var joint in new[] { body.pelvis, body.spine, body.head, body.shoulderLeft, body.shoulderRight,
                body.elbowLeft, body.elbowRight, body.hipLeft, body.hipRight, body.kneeLeft, body.kneeRight })
                Assert.That(joint, Is.Not.Null, body.name);
        }
    }
}
