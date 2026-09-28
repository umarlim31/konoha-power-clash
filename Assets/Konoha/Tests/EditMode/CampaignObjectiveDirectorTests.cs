using System.Collections.Generic;
using Konoha.Campaign;
using NUnit.Framework;
using UnityEngine;

namespace Konoha.Tests
{
    // Guards the 0.0.8.2 slice feel after moving objective rules out of the controller.
    public sealed class CampaignObjectiveDirectorTests
    {
        private static readonly Vector3 Plaza = new Vector3(0, 0, -5);
        private static readonly Vector3 Majelis = new Vector3(-9, 0, -6);
        private static readonly Vector3 Biro = new Vector3(9, 0, -6);
        private static readonly Vector3 Garda = new Vector3(4.4f, 0, 7.4f);
        private static readonly Vector3 Chair = Vector3.zero;
        private const float Frame = 1f / 60f;

        private static CampaignObjectiveDirector Create() =>
            new CampaignObjectiveDirector(new CampaignObjectiveLayout(Plaza, Majelis, Biro, Garda, Chair));

        private static void Hold(CampaignObjectiveDirector director, Vector3 position, float seconds)
        {
            for (float t = 0; t < seconds; t += Frame) director.Tick(position, Frame);
        }

        [Test]
        public void MajelisSealNeedsTwoAndHalfSecondsInsideRing()
        {
            var director = Create();
            var messages = new List<string>();
            director.Notified += messages.Add;
            director.Tick(Plaza, Frame);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.PlazaAspirasi));
            Hold(director, Majelis, 2.3f);
            Assert.That(director.Run.HasSeal(CampaignSector.MajelisDaun), Is.False);
            Hold(director, Majelis, 0.3f);
            Assert.That(director.Run.HasSeal(CampaignSector.MajelisDaun), Is.True);
            Assert.That(messages, Contains.Item("Segel Majelis Daun diperoleh"));
        }

        [Test]
        public void MajelisProgressDecaysSlowlyOutsideRing()
        {
            var director = Create();
            director.Tick(Plaza, Frame);
            Hold(director, Majelis, 2f);
            Hold(director, Plaza, 2f);
            Assert.That(director.MajelisHold, Is.EqualTo(1.5f).Within(0.05f));
        }

        [Test]
        public void BiroNeedsThreeSpacedConfirmations()
        {
            var director = Create();
            director.Tick(Plaza, Frame);
            Assert.That(director.Interact(Biro, 10f), Is.True);
            Assert.That(director.Interact(Biro, 10.3f), Is.False, "Confirmation interval is 0.65 s");
            Assert.That(director.Interact(Biro, 10.7f), Is.True);
            Assert.That(director.Run.HasSeal(CampaignSector.BiroProsedur), Is.False);
            Assert.That(director.Interact(Biro, 11.4f), Is.True);
            Assert.That(director.Run.HasSeal(CampaignSector.BiroProsedur), Is.True);
            Assert.That(director.Interact(Plaza, 20f), Is.False);
        }

        [Test]
        public void FullSliceRouteReachesVictoryWithOneCounterattack()
        {
            var director = Create();
            var spawnedHealth = new List<int>();
            director.GuardSpawned += spawnedHealth.Add;
            director.Tick(Plaza, Frame);
            Hold(director, Majelis, 2.6f);
            director.Interact(Biro, 1f);
            director.Interact(Biro, 2f);
            director.Interact(Biro, 3f);
            director.Tick(Plaza, Frame);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.GerbangDalam));

            director.Tick(Garda, Frame);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.GardaTakhta));
            Assert.That(spawnedHealth, Is.EqualTo(new[] { 100 }));
            Assert.That(director.Interact(Chair, 5f), Is.False, "Seat stays locked during the guard fight");
            for (int i = 0; i < 4; i++) director.DamageGuard(25);
            Assert.That(director.GuardActive, Is.False);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.KursiTerbuka));

            Assert.That(director.Interact(Chair, 6f), Is.True);
            Hold(director, Chair, 3.1f);
            Assert.That(spawnedHealth, Is.EqualTo(new[] { 100, 75 }));
            Assert.That(director.Run.Power, Is.EqualTo(15));
            Hold(director, Chair, 5f);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.Menang));
            Assert.That(director.Run.Power, Is.EqualTo(35));
            Assert.That(director.GuardActive, Is.False, "Remaining counterattack leaves on victory");
        }

        [Test]
        public void LeavingSeatStopsPowerAndRuntuhIsCounted()
        {
            var director = Create();
            director.Tick(Plaza, Frame);
            Hold(director, Majelis, 2.6f);
            director.Interact(Biro, 1f);
            director.Interact(Biro, 2f);
            director.Interact(Biro, 3f);
            director.Tick(Plaza, Frame);
            director.Tick(Garda, Frame);
            director.HandleRuntuh();
            Assert.That(director.Run.RuntuhCount, Is.EqualTo(1));
            Assert.That(director.GuardHealth, Is.EqualTo(100), "Guard is restored after a collapse in the fight");
            director.DamageGuard(100);
            director.Interact(Chair, 5f);
            Hold(director, Chair, 1.05f);
            int power = director.Run.Power;
            director.Tick(Plaza, Frame);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.KursiTerbuka));
            Hold(director, Plaza, 2f);
            Assert.That(director.Run.Power, Is.EqualTo(power));

            director.Restart();
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.GerbangRakyat));
            Assert.That(director.Run.RuntuhCount, Is.Zero);
            Assert.That(director.BiroSteps, Is.Zero);
        }
    }
}
