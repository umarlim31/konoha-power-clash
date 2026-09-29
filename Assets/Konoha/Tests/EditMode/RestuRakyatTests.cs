using Konoha.Campaign;
using NUnit.Framework;

namespace Konoha.Tests
{
    // 0.0.9.2.1 RESTU RAKYAT blessing (reward of the Gerbang Rakyat).
    public sealed class RestuRakyatTests
    {
        [Test]
        public void BlessedHeroHitsHarderAndTakesLess()
        {
            Assert.That(RestuRakyat.DamageMultiplier(false, false), Is.EqualTo(1f));
            Assert.That(RestuRakyat.DamageMultiplier(true, false), Is.EqualTo(1.30f).Within(0.0001f));
            Assert.That(RestuRakyat.DamageMultiplier(false, true), Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void BlessingStillNeedsTheSeniorsToBreakTheBlock()
        {
            // Hero hit on a blocked Majelis member: 1.30 x 0.60 = 0.78 of the base damage.
            float blocked = RestuRakyat.DamageMultiplier(true, false) * MajelisEncounter.IncomingMultiplier(true);
            Assert.That(blocked, Is.EqualTo(0.78f).Within(0.0001f));
            Assert.That(blocked, Is.LessThan(1f), "Breaking the block must stay worthwhile");
        }

        [Test]
        public void CrowdPacingAndRewardAreSane()
        {
            Assert.That(CampaignTuning.Encounters.TargetHitSpacingSeconds, Is.GreaterThan(0f));
            Assert.That(CampaignTuning.Encounters.TargetHitSpacingSeconds,
                Is.LessThan(CampaignTuning.Encounters.EnemyAttackIntervalSeconds));
            Assert.That(CampaignTuning.Restu.PengaruhBonus, Is.InRange(1, CampaignTuning.ResourceRules.MaxPengaruh));
        }
    }
}
