using System.Collections.Generic;
using Konoha.Campaign;
using NUnit.Framework;
using UnityEngine;

namespace Konoha.Tests
{
    // Objective rules of the solo slice. Since 0.0.9 the fights use real enemies: the
    // director announces encounters (events) and is told when they are cleared.
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

        // Gerbang Rakyat defenders already down.
        private static CampaignObjectiveDirector Cleared()
        {
            var director = Create();
            director.MarkGateCleared();
            return director;
        }

        private static void Hold(CampaignObjectiveDirector director, Vector3 position, float seconds)
        {
            for (float t = 0; t < seconds; t += Frame) director.Tick(position, Frame);
        }

        private static void CollectBothSeals(CampaignObjectiveDirector director)
        {
            director.Tick(Plaza, Frame);
            director.Tick(Majelis, Frame);
            Assert.That(director.CompleteMajelis(), Is.True);
            director.Interact(Biro, 1f);
            director.Interact(Biro, 2f);
            director.Interact(Biro, 3f);
            director.Tick(Plaza, Frame);
        }

        [Test]
        public void PlazaStaysClosedUntilGateDefendersAreDown()
        {
            var director = Create();
            var messages = new List<string>();
            director.Notified += messages.Add;
            director.Tick(Plaza, Frame);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.GerbangRakyat));
            director.MarkGateCleared();
            director.MarkGateCleared();
            Assert.That(messages.FindAll(m => m.StartsWith("Gerbang Rakyat terbuka")), Has.Count.EqualTo(1));
            director.Tick(Plaza, Frame);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.PlazaAspirasi));
        }

        [Test]
        public void MajelisSidangIsPlacedOnceWhenPlazaIsReached()
        {
            var director = Cleared();
            int requests = 0;
            director.MajelisRequested += () => requests++;
            Hold(director, Plaza, 0.5f);
            Assert.That(requests, Is.EqualTo(1));
            Assert.That(director.MajelisEngaged, Is.False);
            Assert.That(director.Run.GetSectorState(CampaignSector.MajelisDaun), Is.EqualTo(SectorState.Tersedia));

            director.Restart();
            director.MarkGateCleared();
            director.Tick(Plaza, Frame);
            Assert.That(requests, Is.EqualTo(2), "A restarted run places a fresh sidang");
        }

        [Test]
        public void ApproachingTheHallStartsTheSidangAndMovesTheCheckpoint()
        {
            var director = Cleared();
            var messages = new List<string>();
            director.Notified += messages.Add;
            director.Tick(Plaza, Frame);
            director.Tick(Majelis + new Vector3(CampaignTuning.Majelis.SectorEngageRadius + 1f, 0, 0), Frame);
            Assert.That(director.MajelisEngaged, Is.False);
            director.Tick(Majelis + new Vector3(CampaignTuning.Majelis.SectorEngageRadius - 1f, 0, 0), Frame);
            Assert.That(director.MajelisEngaged, Is.True);
            Assert.That(director.Run.GetSectorState(CampaignSector.MajelisDaun), Is.EqualTo(SectorState.Berlangsung));
            Assert.That(director.Run.Checkpoint, Is.EqualTo(CampaignCheckpoint.MajelisDaun));
            Assert.That(messages.FindAll(m => m.StartsWith("SIDANG MAJELIS")), Has.Count.EqualTo(1));
            Hold(director, Majelis, 3f);
            Assert.That(director.Run.HasSeal(CampaignSector.MajelisDaun), Is.False, "Standing in the hall no longer earns the seal");
        }

        [Test]
        public void MajelisSealComesOnlyFromTheClearedSidang()
        {
            var director = Cleared();
            var messages = new List<string>();
            director.Notified += messages.Add;
            Assert.That(director.CompleteMajelis(), Is.False, "Plaza not reached yet");
            director.Tick(Plaza, Frame);
            director.Tick(Majelis, Frame);
            Assert.That(director.CompleteMajelis(), Is.True);
            Assert.That(director.Run.HasSeal(CampaignSector.MajelisDaun), Is.True);
            Assert.That(director.CompleteMajelis(), Is.False, "Only once");
            Assert.That(messages.FindAll(m => m.StartsWith("SEGEL MAJELIS")), Has.Count.EqualTo(1));
        }

        [Test]
        public void BiroNeedsThreeSpacedConfirmations()
        {
            var director = Cleared();
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
            var director = Cleared();
            int gardaRequests = 0, counterattacks = 0, wins = 0;
            director.GardaRequested += () => gardaRequests++;
            director.CounterattackRequested += () => counterattacks++;
            director.Won += () => wins++;

            CollectBothSeals(director);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.GerbangDalam));
            Assert.That(gardaRequests, Is.EqualTo(1), "Garda defenders are placed when the inner gate opens");
            Hold(director, Plaza, 0.5f);
            Assert.That(gardaRequests, Is.EqualTo(1));

            director.Tick(Garda, Frame);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.GardaTakhta));
            Assert.That(director.Run.Checkpoint, Is.EqualTo(CampaignCheckpoint.GardaTakhta));
            Assert.That(director.Interact(Chair, 5f), Is.False, "Seat stays locked during the guard fight");
            Assert.That(director.CompleteGarda(), Is.True);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.KursiTerbuka));

            Assert.That(director.Interact(Chair, 6f), Is.True);
            Hold(director, Chair, 3.1f);
            Assert.That(counterattacks, Is.EqualTo(1));
            Assert.That(director.Run.Power, Is.EqualTo(15));
            Hold(director, Chair, 5f);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.Menang));
            Assert.That(director.Run.Power, Is.EqualTo(35));
            Assert.That(counterattacks, Is.EqualTo(1));
            Assert.That(wins, Is.EqualTo(1));
        }

        [Test]
        public void GardaClearedBeforeEngagingStillOpensTheSeat()
        {
            var director = Cleared();
            CollectBothSeals(director);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.GerbangDalam));
            Assert.That(director.CompleteGarda(), Is.True);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.KursiTerbuka));
            Assert.That(director.CompleteGarda(), Is.False, "Only once");
        }

        [Test]
        public void LeavingSeatStopsPowerAndRuntuhIsCounted()
        {
            var director = Cleared();
            CollectBothSeals(director);
            director.Tick(Garda, Frame);
            director.HandleRuntuh();
            Assert.That(director.Run.RuntuhCount, Is.EqualTo(1));
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.GardaTakhta), "Defenders persist after a collapse");
            director.CompleteGarda();
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
            Assert.That(director.GateCleared, Is.False);
            Assert.That(director.GardaPrepared, Is.False);
            Assert.That(director.MajelisPrepared, Is.False);
            Assert.That(director.MajelisEngaged, Is.False);
        }
    }
}
