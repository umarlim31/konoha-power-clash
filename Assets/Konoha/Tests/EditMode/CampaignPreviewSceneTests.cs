using Konoha.Campaign;
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
                Assert.That(preview.chairBarrier.GetComponentsInChildren<BoxCollider>().Length,
                    Is.EqualTo(4));
                Assert.That(GameObject.Find("Circular plaza stone"), Is.Not.Null);
                Assert.That(GameObject.Find("CyanSpawnBay"), Is.Null);
            }
            finally
            {
                // Other EditMode tests and regular 4v4 builds still prepare the PvP scene.
                SpikeProject.Prepare();
            }
        }
    }
}
