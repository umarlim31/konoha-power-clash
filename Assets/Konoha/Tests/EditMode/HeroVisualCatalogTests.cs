using System.IO;
using Konoha.Character;
using Konoha.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Konoha.Tests
{
    public sealed class HeroVisualCatalogTests
    {
        [Test]
        public void ClipPathsFollowNamingConvention()
        {
            Assert.That(HeroVisualCatalog.TryParseClipPath("Assets/Konoha/Art/Heroes/Mega/Mega@Run.fbx",
                out string hero, out string clip), Is.True);
            Assert.That(hero, Is.EqualTo("Mega"));
            Assert.That(clip, Is.EqualTo("Run"));
            Assert.That(HeroVisualCatalog.TryParseClipPath("Assets/Konoha/Art/Heroes/Mega/Mega.fbx", out _, out _), Is.False);
            Assert.That(HeroVisualCatalog.TryParseClipPath("Assets/Konoha/Art/Heroes/Mega/Mega@Dance.fbx", out _, out _), Is.False);
            Assert.That(HeroVisualCatalog.TryParseClipPath("Assets/Konoha/Art/Heroes/Abah/Mega@Idle.fbx", out _, out _), Is.False);
            Assert.That(HeroVisualCatalog.TryParseClipPath("Assets/Other/Mega/Mega@Idle.fbx", out _, out _), Is.False);
            Assert.That(HeroVisualCatalog.IsLoopingClip("Idle") && HeroVisualCatalog.IsLoopingClip("Run"), Is.True);
            Assert.That(HeroVisualCatalog.IsLoopingClip("Attack"), Is.False);
            Assert.That(HeroVisualCatalog.ModelPath(3), Is.EqualTo("Assets/Konoha/Art/Heroes/PakWi/PakWi.fbx"));
        }

        [Test]
        public void MissingModelsKeepPrimitiveVisualsAndCapsule()
        {
            SpikeProject.Prepare();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpikeProject.Generated + "/NetworkPlayer.prefab");
            // Looked up by name: the test assembly does not reference Netcode, which the
            // presentation component derives from.
            string[] names = { "HeroVisual_MEGA", "HeroVisual_GEMOY", "HeroVisual_ABAH", "HeroVisual_PAK_WI" };
            for (int i = 0; i < 4; i++)
            {
                var visual = prefab.transform.Find(names[i]);
                Assert.That(visual, Is.Not.Null, names[i]);
                bool hasModel = File.Exists(HeroVisualCatalog.ModelPath(i));
                Assert.That(HeroAnimatorDriver.UsesModel(visual.gameObject), Is.EqualTo(hasModel), names[i]);
                if (!hasModel) continue;
                Assert.That(visual.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(visual.GetComponentInChildren<Animator>().runtimeAnimatorController, Is.Not.Null);
            }
        }

        [Test]
        public void AnimatorDriverIsSafeWithoutAnimator()
        {
            var visual = new GameObject("Driver without animator");
            try
            {
                var driver = visual.AddComponent<HeroAnimatorDriver>();
                Assert.DoesNotThrow(() =>
                {
                    driver.PlayAttack();
                    driver.PlaySkill();
                    driver.PlayHit();
                    driver.PlayJump();
                    driver.SetRuntuh(true);
                });
                Assert.That(HeroAnimatorDriver.UsesModel(visual), Is.True);
                Assert.That(HeroAnimatorDriver.UsesModel(null), Is.False);
            }
            finally { Object.DestroyImmediate(visual); }
        }
    }
}
