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
            Assert.That(director.CompleteBiro(), Is.True);
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
        public void BiroHallOpensWithThePlazaAndStartsNearTheLokets()
        {
            var director = Cleared();
            int requests = 0;
            var messages = new List<string>();
            director.BiroRequested += () => requests++;
            director.Notified += messages.Add;
            Hold(director, Plaza, 0.5f);
            Assert.That(requests, Is.EqualTo(1));
            Assert.That(director.BiroEngaged, Is.False);
            director.Tick(Biro, Frame);
            Assert.That(director.BiroEngaged, Is.True);
            Assert.That(director.Run.Checkpoint, Is.EqualTo(CampaignCheckpoint.BiroProsedur));
            Assert.That(messages.FindAll(m => m.StartsWith("BIRO PROSEDUR")), Has.Count.EqualTo(1));
            Assert.That(director.Interact(Biro, 1f), Is.False, "No SAHKAN button any more");
            Assert.That(director.Run.HasSeal(CampaignSector.BiroProsedur), Is.False);
        }

        [Test]
        public void EitherSealOrderOpensTheInnerGate()
        {
            var director = Cleared();
            int gardaRequests = 0;
            director.GardaRequested += () => gardaRequests++;
            director.Tick(Plaza, Frame);
            Assert.That(director.CompleteBiro(), Is.True, "Biro first");
            Assert.That(director.CompleteBiro(), Is.False, "Only once");
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.PlazaAspirasi));
            Assert.That(director.CompleteMajelis(), Is.True);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.GerbangDalam));
            director.Tick(Plaza, Frame);
            Assert.That(gardaRequests, Is.EqualTo(1));
        }

        [Test]
        public void FullSliceRouteReachesVictoryWithThreeCounterattackWaves()
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

            int seated = 0;
            director.Seated += () => seated++;
            Assert.That(director.Interact(Chair, 6f), Is.True);
            Assert.That(seated, Is.EqualTo(1));
            Assert.That(director.ReignStarted, Is.True);
            // §9: Power +2/s, a counterattack wave every 15 s, 100 Power wins (~50 s).
            Hold(director, Chair, 15.05f);
            Assert.That(counterattacks, Is.EqualTo(1));
            Assert.That(director.Run.Power, Is.EqualTo(30));
            Hold(director, Chair, 35f);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.Menang));
            Assert.That(director.Run.Power, Is.EqualTo(100));
            Assert.That(counterattacks, Is.EqualTo(3));
            Assert.That(wins, Is.EqualTo(1));
        }

        private static CampaignObjectiveDirector Seated()
        {
            var director = Cleared();
            CollectBothSeals(director);
            director.Tick(Garda, Frame);
            director.CompleteGarda();
            Assert.That(director.Interact(Chair, 5f), Is.True);
            return director;
        }

        [Test]
        public void StandingUpStopsPowerWithoutLosingIt()
        {
            var director = Seated();
            Hold(director, Chair, 2.05f);
            Assert.That(director.Run.Power, Is.EqualTo(4));
            Assert.That(director.Interact(Chair, 9f), Is.True, "BERDIRI");
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.KursiTerbuka));
            Hold(director, Chair, 3f);
            Assert.That(director.Run.Power, Is.EqualTo(4));
            Assert.That(director.Interact(Chair, 12f), Is.True, "DUDUK again");
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.Memerintah));
        }

        [Test]
        public void ThreeRuntuhDuringTheReignIsAKudeta()
        {
            var director = Seated();
            int kudeta = 0;
            director.Kudeta += () => kudeta++;
            Hold(director, Chair, 5.05f);
            Assert.That(director.Run.Power, Is.EqualTo(10));
            director.HandleRuntuh();
            director.HandleRuntuh();
            Assert.That(kudeta, Is.Zero);
            Assert.That(director.ReignRuntuh, Is.EqualTo(2));
            Assert.That(director.Run.Power, Is.EqualTo(10), "Runtuh alone keeps Power");
            director.HandleRuntuh();
            Assert.That(kudeta, Is.EqualTo(1));
            Assert.That(director.Run.Power, Is.Zero);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.KursiTerbuka));
            Assert.That(director.ReignStarted, Is.False);
            Assert.That(director.ReignRuntuh, Is.Zero);
            Assert.That(director.Run.Checkpoint, Is.EqualTo(CampaignCheckpoint.GardaTakhta));
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
            Assert.That(director.BiroPrepared, Is.False);
            Assert.That(director.BiroEngaged, Is.False);
            Assert.That(director.GateCleared, Is.False);
            Assert.That(director.GardaPrepared, Is.False);
            Assert.That(director.MajelisPrepared, Is.False);
            Assert.That(director.MajelisEngaged, Is.False);
        }
    }
}
