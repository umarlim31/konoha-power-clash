using System;
using System.Collections.Generic;
using Konoha.Campaign;
using Konoha.Networking;
using NUnit.Framework;

namespace Konoha.Tests
{
    // 0.2.9: icon painting and caption parsing of the round skill buttons.
    public sealed class CampaignSkillHudTests
    {
        [Test]
        public void EveryIconIsAVisibleDistinctSilhouette()
        {
            var seen = new List<float[]>();
            foreach (CampaignIconPainter.Icon icon in Enum.GetValues(typeof(CampaignIconPainter.Icon)))
            {
                var canvas = CampaignIconPainter.Paint(icon);
                float coverage = canvas.Coverage();
                Assert.That(coverage, Is.InRange(.03f, .8f), icon + " coverage");
                foreach (float[] other in seen)
                {
                    float diff = 0f;
                    for (int i = 0; i < other.Length; i++) diff += Math.Abs(other[i] - canvas.alpha[i]);
                    Assert.That(diff / other.Length, Is.GreaterThan(.01f), icon + " looks like another icon");
                }
                seen.Add(canvas.alpha);
            }
        }

        [Test]
        public void EveryHeroSkillHasItsOwnIcon()
        {
            var icons = new HashSet<CampaignIconPainter.Icon>();
            foreach (PrototypeHero hero in Enum.GetValues(typeof(PrototypeHero)))
                foreach (var slot in new[] { CampaignSkillHud.Slot.Skill1, CampaignSkillHud.Slot.Skill2, CampaignSkillHud.Slot.Ultimate })
                    Assert.That(icons.Add(CampaignSkillHud.IconFor(slot, hero)), Is.True, hero + " " + slot);
            Assert.That(CampaignSkillHud.IconFor(CampaignSkillHud.Slot.Skill1, PrototypeHero.Mega), Is.EqualTo(CampaignIconPainter.Icon.Kerbau));
        }

        [Test]
        public void CaptionsParseInBothDecimalStyles()
        {
            CampaignSkillHud.Parse("SERUAN\nIBU\n9,1", out string name, out float remaining, out _, out _);
            Assert.That(name, Is.EqualTo("SERUAN IBU"));
            Assert.That(remaining, Is.EqualTo(9.1f).Within(.001f));
            CampaignSkillHud.Parse("PERISAI\nRAKYAT", out name, out remaining, out _, out _);
            Assert.That(name, Is.EqualTo("PERISAI RAKYAT"));
            Assert.That(remaining, Is.Zero);
            CampaignSkillHud.Parse("DODGE\n0.3", out name, out remaining, out _, out _);
            Assert.That(name, Is.EqualTo("DODGE"));
            Assert.That(remaining, Is.EqualTo(.3f).Within(.001f));
            CampaignSkillHud.Parse("ULT 63%", out _, out _, out float percent, out bool ready);
            Assert.That(percent, Is.EqualTo(63f));
            Assert.That(ready, Is.False);
            CampaignSkillHud.Parse("ULT\nMONCONG", out name, out _, out _, out ready);
            Assert.That(ready, Is.True);
            Assert.That(name, Is.EqualTo("MONCONG"));
        }
    }
}
