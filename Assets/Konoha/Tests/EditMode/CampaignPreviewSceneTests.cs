using Konoha.Campaign;
using Konoha.Character;
using Konoha.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Konoha.Tests
{
    public sealed class CampaignPreviewSceneTests
    {
        [Test]
        public void ManualBuildPreparationCreatesIndependentSoloScene()
        {
            try
            {
                CampaignPreviewProject.Prepare();
                Assert.That(EditorBuildSettings.scenes[0].path,
                    Is.EqualTo(CampaignPreviewProject.ScenePath));
                Assert.That(GameObject.Find("RuntimeNetworkingProof"), Is.Null);
                var preview = UnityEngine.Object.FindFirstObjectByType<CampaignPreviewController>();
                Assert.That(preview, Is.Not.Null);
                Assert.That(preview.heroVisuals, Has.Length.EqualTo(4));
                Assert.That(preview.attackButton, Is.Not.Null);
                Assert.That(preview.sitButton, Is.Not.Null);
                Assert.That(preview.skillButton, Is.Not.Null);
                Assert.That(preview.feedbackText, Is.Not.Null);
                Assert.That(preview.waypointText, Is.Not.Null);
                Assert.That(preview.objectiveProgress, Is.Not.Null);
                Assert.That(preview.chairBarrier.GetComponentsInChildren<BoxCollider>().Length,
                    Is.EqualTo(4));
                Assert.That(GameObject.Find("Circular plaza stone"), Is.Not.Null);
                Assert.That(GameObject.Find("CyanSpawnBay"), Is.Null);
                Assert.That(GameObject.Find("CyanChevron_-3_A"), Is.Null);
                Assert.That(GameObject.Find("SpawnArrow_0_A"), Is.Null);
                Assert.That(GameObject.Find("Surrounding Konoha Landscape"), Is.Not.Null);
                Assert.That(GameObject.Find("Monumen Burung Konoha plinth"), Is.Not.Null);
                Assert.That(GameObject.Find("Fictional civic dome"), Is.Not.Null);
                Assert.That(GameObject.Find("Bridge across reflecting pool"), Is.Not.Null);
                Assert.That(GameObject.Find("Majelis Daun interaction boundary"), Is.Not.Null);
                Assert.That(GameObject.Find("Majelis Daun pitched assembly roof"), Is.Not.Null);
                Assert.That(GameObject.Find("Biro Prosedur archive lantern"), Is.Not.Null);
                Assert.That(GameObject.Find("Objective backdrop").GetComponent<RectTransform>().sizeDelta.y,
                    Is.LessThan(100f));
                Assert.That(GameObject.Find("TouchCanvas").GetComponent<Konoha.UI.SafeAreaLayout>().compactJoystick,
                    Is.True);
                Assert.That(GameObject.Find("North Gate").GetComponent<Renderer>().enabled, Is.False);
                Assert.That(GameObject.Find("North Gate ritual seal"), Is.Not.Null);
                Assert.That(GameObject.Find("Floor").GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap"),
                    Is.Not.Null);
                var camera = UnityEngine.Object.FindFirstObjectByType<MobileCombatCamera>();
                Assert.That(camera, Is.Not.Null);
                Assert.That(camera.limitFocusToArena, Is.True);
                Assert.That(camera.focusZLimits.x, Is.EqualTo(-7.5f));
            }
            finally
            {
                // Other EditMode tests and regular 4v4 builds still prepare the PvP scene.
                SpikeProject.Prepare();
            }
        }
    }
}
