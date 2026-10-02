using Konoha.Campaign;
using Konoha.Networking;
using NUnit.Framework;
using UnityEngine;

namespace Konoha.Tests
{
    // 0.2.3: pure parts of the combat feel (meshes, colours, flat directions).
    public sealed class CampaignCombatFeelTests
    {
        [Test]
        public void RingMeshIsDoubleSidedAndFlat()
        {
            Mesh ring = CampaignCombatFx.BuildRing(.8f, 1f, 360f, 16);
            try
            {
                Assert.That(ring.vertexCount, Is.EqualTo(34));
                Assert.That(ring.triangles.Length, Is.EqualTo(16 * 12), "Both faces, so it shows from above and below");
                foreach (Vector3 v in ring.vertices)
                {
                    Assert.That(v.y, Is.EqualTo(0f));
                    Assert.That(new Vector2(v.x, v.z).magnitude, Is.InRange(.799f, 1.001f));
                }
            }
            finally
            {
                Object.DestroyImmediate(ring);
            }
        }

        [Test]
        public void ArcStaysInFrontOfTheAttacker()
        {
            Mesh arc = CampaignCombatFx.BuildRing(.6f, 1f, 130f, 10);
            try
            {
                foreach (Vector3 v in arc.vertices)
                    Assert.That(v.z, Is.GreaterThan(.2f));
            }
            finally
            {
                Object.DestroyImmediate(arc);
            }
        }

        [Test]
        public void EveryHeroHasItsOwnEffectColour()
        {
            var seen = new System.Collections.Generic.HashSet<Color>();
            foreach (PrototypeHero hero in System.Enum.GetValues(typeof(PrototypeHero)))
                Assert.That(seen.Add(CampaignCombatFx.HeroColor(hero)), Is.True, hero.ToString());
        }

        [Test]
        public void FlatDirectionIgnoresHeightAndNeverZero()
        {
            Assert.That(CampaignCombatFx.Flat(new Vector3(0f, 5f, 0f)), Is.EqualTo(Vector3.forward));
            Vector3 d = CampaignCombatFx.Flat(new Vector3(3f, 9f, 4f));
            Assert.That(d.y, Is.EqualTo(0f));
            Assert.That(d.magnitude, Is.EqualTo(1f).Within(.0001f));
        }
    }
}
