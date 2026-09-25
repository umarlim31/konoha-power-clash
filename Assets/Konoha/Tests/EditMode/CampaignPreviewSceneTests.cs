using Konoha.Campaign;
using Konoha.Character;
using Konoha.Editor;
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
                Assert.That(preview.heroVisuals, Has.Length.EqualTo(4));
                Assert.That(preview.attackButton, Is.Not.Null);
                Assert.That(preview.sitButton, Is.Not.Null);
                Assert.That(preview.skillButton, Is.Not.Null);
                Assert.That(preview.feedbackText, Is.Not.Null);
                Assert.That(preview.waypointText, Is.Not.Null);
                Assert.That(preview.objectiveProgress, Is.Not.Null);
                Assert.That(preview.chairBarrier.GetComponentsInChildren<BoxCollider>(), Has.Length.EqualTo(4));
                Assert.That(preview.centralMonument, Is.Not.Null);
                Assert.That(preview.centralMonument.solid, Is.Not.Null);
                Assert.That(preview.centralMonument.transparentMaterials,
                    Has.Length.EqualTo(preview.centralMonument.surfaces.Length));
                foreach (var material in preview.centralMonument.transparentMaterials)
                {
                    Assert.That(AssetDatabase.Contains(material), Is.True);
                    Assert.That(material.GetFloat("_Surface"), Is.EqualTo(1f));
                }
                foreach (string obsolete in new[] { "CyanSpawnBay", "CyanChevron_-3_A", "SpawnArrow_0_A",
                    "NorthWestCover_Body", "SouthEastCover_Body", "NorthWestRelay", "SouthEastRelay" })
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
                Assert.That(Object.FindFirstObjectByType<MobileCombatCamera>().limitFocusToArena, Is.True);

                // Sample actual collision along the complete campaign route, excluding the player's own capsule.
                preview.chairBarrier.SetActive(false);
                Physics.SyncTransforms();
                Vector3[] route = { new Vector3(0,0,-9), preview.plaza.position, new Vector3(-9,0,-6),
                    new Vector3(9,0,-6), new Vector3(4.4f,0,-6), new Vector3(4.4f,0,7.4f), preview.garda.position,
                    new Vector3(4.4f,0,7.4f), new Vector3(4.4f,0,-3.3f), new Vector3(0,0,-3.3f), Vector3.zero };
                for (int segment = 1; segment < route.Length; segment++)
                    for (float distance = 0; distance <= Vector3.Distance(route[segment-1],route[segment]); distance += .2f)
                    {
                        Vector3 p = Vector3.MoveTowards(route[segment-1],route[segment],distance);
                        foreach (var hit in Physics.OverlapCapsule(p+Vector3.up*.5f,p+Vector3.up*1.5f,.42f))
                            Assert.That(hit.transform.IsChildOf(preview.player.transform), Is.True,
                                "Blocked campaign route at " + p + " by " + hit.name);
                    }
            }
            finally { SpikeProject.Prepare(); }
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
