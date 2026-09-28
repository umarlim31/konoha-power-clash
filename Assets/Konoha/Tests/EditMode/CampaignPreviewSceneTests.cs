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
                    Is.EqualTo("JALUR TAKHTA 0.0.9  •  SOLO PREVIEW"));
                Assert.That(PlayerSettings.bundleVersion, Is.EqualTo("0.0.9"));
                Assert.That(PlayerSettings.Android.bundleVersionCode, Is.EqualTo(21));
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
                foreach (string control in new[] { "AttackButton", "S1Button", "S2Button", "UltimateButton",
                    "DodgeButton", "HeroButton", "CampaignJump", "CampaignSit" })
                    Assert.That(GameObject.Find(control).GetComponent<UnityEngine.UI.Button>(), Is.Not.Null, control);

                var stage = preview.stage;
                Assert.That(stage, Is.SameAs(session.stage));
                Assert.That(stage.plaza, Is.Not.Null);
                Assert.That(stage.majelis, Is.Not.Null);
                Assert.That(stage.biro, Is.Not.Null);
                Assert.That(stage.garda, Is.Not.Null);
                Assert.That(stage.chair, Is.Not.Null);

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
                    "Fictional civic dome", "Bridge across reflecting pool", "Majelis Daun interaction boundary",
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
                Assert.That(traversal.boundaryRadii.x, Is.GreaterThan(20));
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
                preview.chairBarrier.SetActive(false);
                Physics.SyncTransforms();
                Vector3[] route = { new Vector3(0,0,-9), stage.plaza.position, new Vector3(-9,0,-6),
                    new Vector3(9,0,-6), new Vector3(4.4f,0,-6), new Vector3(4.4f,0,7.4f), stage.garda.position,
                    new Vector3(4.4f,0,7.4f), new Vector3(4.4f,0,-3.3f), new Vector3(0,0,-3.3f), Vector3.zero };
                for (int segment = 1; segment < route.Length; segment++)
                    for (float distance = 0; distance <= Vector3.Distance(route[segment-1],route[segment]); distance += .2f)
                    {
                        Vector3 p = Vector3.MoveTowards(route[segment-1],route[segment],distance);
                        AssertFree(p, probe.transform, "Blocked campaign route at ");
                    }
                // Enemies and revived heroes must never appear inside geometry.
                foreach (var point in stage.gateSpawnPoints) AssertFree(point, probe.transform, "Gate spawn blocked at ");
                foreach (var point in stage.gardaSpawnPoints) AssertFree(point, probe.transform, "Garda spawn blocked at ");
                AssertFree(stage.counterattackSpawnPoint, probe.transform, "Counterattack spawn blocked at ");
                foreach (CampaignCheckpoint checkpoint in System.Enum.GetValues(typeof(CampaignCheckpoint)))
                    AssertFree(stage.CheckpointPosition(checkpoint), probe.transform, "Checkpoint " + checkpoint + " blocked at ");
                VerifyWaterExitsAndJump(probe);
                // New jump must not bypass the locked mission enclosure.
                preview.chairBarrier.SetActive(true);
                Physics.SyncTransforms();
                probe.Teleport(new Vector3(0,.1f,-3.4f));
                Settle(probe);
                Assert.That(probe.TryJump(), Is.True);
                for(int i=0;i<90;i++) probe.Step(new MoveIntent(Vector2.up),1f/60f);
                Assert.That(probe.transform.position.z,Is.LessThan(-2.7f));
            }
            finally { SpikeProject.Prepare(); }
        }

        private static void AssertFree(Vector3 p, Transform ignored, string message)
        {
            Vector3 ground = new Vector3(p.x, 0f, p.z);
            foreach (var hit in Physics.OverlapCapsule(ground+Vector3.up*.5f,ground+Vector3.up*1.5f,.42f))
                Assert.That(hit.transform.IsChildOf(ignored), Is.True, message + p + " by " + hit.name);
        }

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
    }
}
