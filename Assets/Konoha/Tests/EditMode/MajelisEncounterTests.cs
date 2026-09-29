using Konoha.Campaign;
using NUnit.Framework;

namespace Konoha.Tests
{
    // §8.1 Majelis Daun "Pecahkan Blok" rules (pure logic, host applies the changes).
    public sealed class MajelisEncounterTests
    {
        [Test]
        public void VotingBlockNeedsTwoSeniorsAndCutsFortyPercent()
        {
            Assert.That(MajelisEncounter.BlockActive(2), Is.True);
            Assert.That(MajelisEncounter.BlockActive(1), Is.False);
            Assert.That(MajelisEncounter.IncomingMultiplier(true), Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(MajelisEncounter.IncomingMultiplier(false), Is.EqualTo(1f));
        }

        [Test]
        public void FullRosterKeepsTheBlock()
        {
            var sidang = new MajelisEncounter();
            Assert.That(sidang.Evaluate(2, true, 3), Is.EqualTo(MajelisChange.None));
            Assert.That(sidang.BlockHolds, Is.True);
            Assert.That(sidang.IncomingDamageMultiplier, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void FirstSeniorDownBreaksTheBlockOnceAndForGood()
        {
            var sidang = new MajelisEncounter();
            sidang.Evaluate(2, true, 3);
            Assert.That(sidang.Evaluate(1, true, 2), Is.EqualTo(MajelisChange.BlockBroken));
            Assert.That(sidang.BlockHolds, Is.False);
            Assert.That(sidang.IncomingDamageMultiplier, Is.EqualTo(1f));
            Assert.That(sidang.Evaluate(1, true, 2), Is.EqualTo(MajelisChange.None), "Announced once");
            Assert.That(sidang.Evaluate(2, true, 3), Is.EqualTo(MajelisChange.None));
            Assert.That(sidang.BlockHolds, Is.False, "A broken block never reforms");
        }

        [Test]
        public void KetuaDownIsReportedOnceWhileOfficersStillStand()
        {
            var sidang = new MajelisEncounter();
            Assert.That(sidang.Evaluate(2, false, 3),
                Is.EqualTo(MajelisChange.LeaderDown), "Ketua may fall first; the block still holds");
            Assert.That(sidang.BlockHolds, Is.True);
            Assert.That(sidang.Cleared, Is.False);
            Assert.That(sidang.Evaluate(2, false, 3), Is.EqualTo(MajelisChange.None));
        }

        [Test]
        public void ClearedWhenKetuaAndEveryOfficerAreDown()
        {
            var sidang = new MajelisEncounter();
            sidang.Evaluate(2, true, 3);
            sidang.Evaluate(0, true, 1);
            Assert.That(sidang.Evaluate(0, true, 0), Is.EqualTo(MajelisChange.None), "Ketua still standing");
            MajelisChange change = sidang.Evaluate(0, false, 0);
            Assert.That(change, Is.EqualTo(MajelisChange.LeaderDown | MajelisChange.Cleared));
            Assert.That(sidang.Cleared, Is.True);
            Assert.That(sidang.Evaluate(0, false, 0), Is.EqualTo(MajelisChange.None));
        }

        [Test]
        public void ResetStartsAFreshSidang()
        {
            var sidang = new MajelisEncounter();
            sidang.Evaluate(0, false, 0);
            sidang.Reset();
            Assert.That(sidang.BlockHolds, Is.True);
            Assert.That(sidang.LeaderFallen, Is.False);
            Assert.That(sidang.Cleared, Is.False);
        }

        [Test]
        public void SoloRosterMatchesGameLogic()
        {
            var roster = EncounterComposer.Compose(FactionId.MajelisDaun, 1);
            Assert.That(EncounterComposer.TotalUnits(roster), Is.EqualTo(7));
            Assert.That(UnitRoleStats.For(UnitRole.Pemimpin).HasTelegraphedSpecial, Is.True);
            Assert.That(CampaignTuning.Majelis.KetokPaluDamage, Is.EqualTo(22));
            Assert.That(CampaignTuning.Majelis.KetokPaluRadius, Is.EqualTo(3f));
        }
    }
}
