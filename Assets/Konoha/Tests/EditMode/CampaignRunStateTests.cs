using Konoha.Campaign;
using NUnit.Framework;

namespace Konoha.Tests
{
    public sealed class CampaignRunStateTests
    {
        [Test]
        public void ChairRequiresDistinctSealsAndFinalGuard()
        {
            var run = new CampaignRunState(requiredSeals: 2);
            Assert.That(run.TrySit(), Is.False);
            run.ReachPlaza();
            Assert.That(run.AwardSeal(CampaignSector.MajelisDaun), Is.True);
            Assert.That(run.AwardSeal(CampaignSector.MajelisDaun), Is.False);
            Assert.That(run.TryOpenInnerGate(), Is.False);
            Assert.That(run.AwardSeal(CampaignSector.BiroProsedur), Is.True);
            Assert.That(run.TryOpenInnerGate(), Is.True);
            Assert.That(run.TrySit(), Is.False);
            Assert.That(run.TryStartGuard(), Is.True);
            Assert.That(run.TrySit(), Is.False);
            Assert.That(run.DefeatGuard(), Is.True);
            Assert.That(run.TrySit(), Is.True);
        }

        [Test]
        public void OptionalSectorsCanMeetThresholdWithoutClearingAllFive()
        {
            var run = new CampaignRunState(requiredSeals: 3);
            run.ReachPlaza();
            run.AwardSeal(CampaignSector.MajelisDaun);
            run.AwardSeal(CampaignSector.KomisiSuara);
            Assert.That(run.TryOpenInnerGate(), Is.False);
            run.AwardSeal(CampaignSector.MenaraNarasi);
            Assert.That(run.TryOpenInnerGate(), Is.True);
            Assert.That(run.SealCount, Is.EqualTo(3));
        }

        [Test]
        public void KnockedOffSeatStopsPowerUntilReseated()
        {
            var run = new CampaignRunState(requiredSeals: 1, targetPower: 5);
            run.ReachPlaza();
            run.AwardSeal(CampaignSector.MajelisDaun);
            run.TryOpenInnerGate();
            run.TryStartGuard();
            run.DefeatGuard();
            run.TrySit();
            Assert.That(run.GainPower(2), Is.True);
            run.LoseSeat();
            Assert.That(run.GainPower(3), Is.False);
            Assert.That(run.Power, Is.EqualTo(2));
            Assert.That(run.TrySit(), Is.True);
            Assert.That(run.GainPower(3), Is.True);
            Assert.That(run.Phase, Is.EqualTo(CampaignPhase.Menang));
        }
    }
}
