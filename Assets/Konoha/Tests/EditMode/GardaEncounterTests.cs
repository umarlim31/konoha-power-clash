using Konoha.Campaign;
using NUnit.Framework;

namespace Konoha.Tests
{
    // §8.3 Garda Takhta "Gerbang Terakhir" rules (pure logic).
    public sealed class GardaEncounterTests
    {
        [Test]
        public void ReinforcementArrivesOnceBelowHalfWibawa()
        {
            var garda = new GardaEncounter();
            Assert.That(garda.Evaluate(0.6f, true), Is.EqualTo(GardaChange.None));
            Assert.That(garda.Evaluate(0.49f, true), Is.EqualTo(GardaChange.Reinforce));
            Assert.That(garda.Evaluate(0.2f, true), Is.EqualTo(GardaChange.None), "Only one wave in solo");
        }

        [Test]
        public void PanglimaDownEndsTheEncounterAndOpensTheRing()
        {
            var garda = new GardaEncounter();
            garda.UpdateLockdown(true, false);
            Assert.That(garda.LockdownClosed, Is.True);
            Assert.That(garda.Evaluate(0f, false), Is.EqualTo(GardaChange.LeaderDown));
            Assert.That(garda.LockdownClosed, Is.False);
            Assert.That(garda.UpdateLockdown(true, false), Is.False, "Stays open after the Panglima falls");
            Assert.That(garda.Evaluate(0f, false), Is.EqualTo(GardaChange.None));
        }

        [Test]
        public void LockdownClosesInsideAndOpensWhenTheHeroCollapses()
        {
            var garda = new GardaEncounter();
            Assert.That(garda.UpdateLockdown(false, false), Is.False);
            Assert.That(garda.UpdateLockdown(true, false), Is.True);
            Assert.That(garda.UpdateLockdown(false, false), Is.False, "Closed stays closed while the hero fights");
            Assert.That(garda.LockdownClosed, Is.True);
            Assert.That(garda.UpdateLockdown(false, true), Is.True, "Runtuh reopens (revive outside the ring)");
            Assert.That(garda.LockdownClosed, Is.False);
            garda.Reset();
            Assert.That(garda.ReinforcementSent, Is.False);
            Assert.That(garda.LeaderFallen, Is.False);
        }
    }
}
