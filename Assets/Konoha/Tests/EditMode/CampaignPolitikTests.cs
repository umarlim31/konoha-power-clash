using Konoha.Campaign;
using NUnit.Framework;
using UnityEngine;

namespace Konoha.Tests
{
    // 0.4.0 Musim Pemilu: LAWAN / RANGKUL, Modal, Jatah, Restu and the Koran Konoha endings.
    public sealed class CampaignPolitikTests
    {
        private static readonly Vector3 Plaza = new Vector3(0, 0, -5);
        private static readonly Vector3 Majelis = new Vector3(-24, 0, -6);
        private static readonly Vector3 Biro = new Vector3(27, 0, -8);
        private const float Frame = 1f / 60f;

        private static CampaignObjectiveDirector AtPlaza()
        {
            var director = new CampaignObjectiveDirector(new CampaignObjectiveLayout(Plaza, Majelis, Biro,
                new Vector3(0, 0, 28), new Vector3(0, 0, 49)));
            director.MarkGateCleared();
            director.Tick(Plaza, Frame);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.PlazaAspirasi));
            return director;
        }

        [Test]
        public void GateGivesModalAndRestu()
        {
            var director = AtPlaza();
            Assert.That(director.Run.Modal, Is.EqualTo(CampaignTuning.Politik.ModalStart + CampaignTuning.Politik.ModalGateBonus));
            Assert.That(director.Run.Restu, Is.EqualTo(CampaignTuning.Politik.RestuStart + CampaignTuning.Politik.RestuGate));
        }

        [Test]
        public void OfferAppearsBeforeTheHallAndClosesWhenFighting()
        {
            var director = AtPlaza();
            Assert.That(director.OfferSector, Is.EqualTo(-1), "Plaza centre is outside both offers");
            Vector3 approach = Majelis + new Vector3(14f, 0, 0);
            director.Tick(approach, Frame);
            Assert.That(director.OfferSector, Is.EqualTo((int)CampaignSector.MajelisDaun));
            director.Tick(Majelis + new Vector3(5f, 0, 0), Frame);
            Assert.That(director.MajelisEngaged, Is.True);
            Assert.That(director.OfferSector, Is.EqualTo(-1));
            Assert.That(director.Run.GetPath(CampaignSector.MajelisDaun), Is.EqualTo(SectorPath.Dilawan));
            director.Tick(approach, Frame);
            Assert.That(director.OfferSector, Is.EqualTo(-1), "No offer once the fight started");
        }

        [Test]
        public void RangkulPaysAwardsSealAndCountsJatah()
        {
            var director = AtPlaza();
            int before = director.Run.Modal;
            director.Tick(Majelis + new Vector3(14f, 0, 0), Frame);
            Assert.That(director.Rangkul(CampaignSector.MajelisDaun, false), Is.EqualTo(RangkulResult.Paid));
            Assert.That(director.Run.HasSeal(CampaignSector.MajelisDaun), Is.True);
            Assert.That(director.Run.Modal, Is.EqualTo(before - CampaignTuning.Politik.RangkulCostMajelis));
            Assert.That(director.Run.Jatah, Is.EqualTo(1));
            Assert.That(director.Rangkul(CampaignSector.MajelisDaun, false), Is.EqualTo(RangkulResult.None), "Only once");
            Assert.That(director.CompleteMajelis(), Is.False, "A bought seal is not awarded twice");
        }

        [Test]
        public void ShortModalNeedsALoanThatMakesAPuppet()
        {
            var director = AtPlaza();
            Assert.That(director.Rangkul(CampaignSector.MajelisDaun, false), Is.EqualTo(RangkulResult.Paid));
            // Modal left (15 + 60 - 45 = 30) is below the Biro price (35).
            Assert.That(director.Rangkul(CampaignSector.BiroProsedur, false), Is.EqualTo(RangkulResult.TooPoor));
            Assert.That(director.Run.HasSeal(CampaignSector.BiroProsedur), Is.False);
            Assert.That(director.Rangkul(CampaignSector.BiroProsedur, true), Is.EqualTo(RangkulResult.Loan));
            Assert.That(director.Run.Modal, Is.EqualTo(0));
            Assert.That(director.Run.TookLoan, Is.True);
            Assert.That(director.Run.Jatah, Is.EqualTo(2 + CampaignTuning.Politik.LoanExtraJatah));
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.GerbangDalam), "Two seals open the inner gate");
            Assert.That(director.Run.Ending, Is.EqualTo(CampaignEnding.BonekaSistem));
        }

        [Test]
        public void EnoughModalBuysBothForACoalition()
        {
            var director = AtPlaza();
            director.Run.AddModal(10); // e.g. a few Kroni knocked down
            Assert.That(director.Rangkul(CampaignSector.MajelisDaun, false), Is.EqualTo(RangkulResult.Paid));
            Assert.That(director.Rangkul(CampaignSector.BiroProsedur, false), Is.EqualTo(RangkulResult.Paid));
            Assert.That(director.Run.Ending, Is.EqualTo(CampaignEnding.RajaKoalisi));
        }

        [Test]
        public void FightingEverythingIsTheIronThrone()
        {
            var director = AtPlaza();
            director.Tick(Majelis, Frame);
            Assert.That(director.CompleteMajelis(), Is.True);
            Assert.That(director.CompleteBiro(), Is.True);
            Assert.That(director.Run.LawanCount, Is.EqualTo(2));
            Assert.That(director.Run.Ending, Is.EqualTo(CampaignEnding.TakhtaBesi));
            Assert.That(director.Run.Restu, Is.EqualTo(Mathf.Min(100, CampaignTuning.Politik.RestuStart +
                CampaignTuning.Politik.RestuGate + 2 * CampaignTuning.Politik.RestuLawan)));
        }

        [Test]
        public void BlusukanGreetsThreeGroupsBeforeThePlaza()
        {
            Vector3[] groups = { new Vector3(-8.6f, 0, -26), new Vector3(8.6f, 0, -26), new Vector3(13, 0, -21) };
            var director = new CampaignObjectiveDirector(new CampaignObjectiveLayout(Plaza, Majelis, Biro,
                new Vector3(0, 0, 28), new Vector3(0, 0, 49), groups));
            director.MarkGateCleared();
            director.Tick(Plaza, Frame);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.GerbangRakyat), "SUARA first");
            Assert.That(director.BlusukanDone, Is.False);

            int modal = director.Run.Modal;
            for (int i = 0; i < groups.Length; i++)
            {
                director.Tick(groups[i], Frame);
                Assert.That(director.BlusukanCurrent, Is.EqualTo(i));
                for (float t = 0; t < CampaignTuning.Blusukan.GreetSeconds + 0.1f; t += Frame)
                    director.Tick(groups[i], Frame);
                Assert.That(director.SuaraCount, Is.EqualTo(i + 1));
            }
            Assert.That(director.BlusukanDone, Is.True);
            Assert.That(director.Run.Modal, Is.EqualTo(modal - CampaignTuning.Blusukan.ModalCost[1]));
            director.Tick(Plaza, Frame);
            Assert.That(director.Run.Phase, Is.EqualTo(CampaignPhase.PlazaAspirasi));
        }

        [Test]
        public void LeavingAGroupResetsTheGreeting()
        {
            Vector3[] groups = { new Vector3(-8.6f, 0, -26) };
            var director = new CampaignObjectiveDirector(new CampaignObjectiveLayout(Plaza, Majelis, Biro,
                new Vector3(0, 0, 28), new Vector3(0, 0, 49), groups));
            director.MarkGateCleared();
            for (float t = 0; t < 1f; t += Frame) director.Tick(groups[0], Frame);
            Assert.That(director.BlusukanProgress, Is.GreaterThan(0.4f));
            director.Tick(Vector3.zero, Frame);
            Assert.That(director.BlusukanProgress, Is.EqualTo(0f));
            Assert.That(director.SuaraCount, Is.EqualTo(0));
        }

        [Test]
        public void EndingRules()
        {
            Assert.That(CampaignRunState.EndingFor(0, 0), Is.EqualTo(CampaignEnding.TakhtaBesi));
            Assert.That(CampaignRunState.EndingFor(1, 1), Is.EqualTo(CampaignEnding.RajaKoalisi));
            Assert.That(CampaignRunState.EndingFor(2, 2), Is.EqualTo(CampaignEnding.RajaKoalisi));
            Assert.That(CampaignRunState.EndingFor(1, CampaignTuning.Politik.BonekaJatah), Is.EqualTo(CampaignEnding.BonekaSistem));
        }

        [Test]
        public void KoranHeadlineFollowsTheEndingAndStoriesFollowTheRun()
        {
            KoranEdition iron = KoranKonoha.Compose(CampaignEnding.TakhtaBesi, "Mega", SectorPath.Dilawan, SectorPath.Dilawan,
                40, 0, 80, false, 0, 321);
            Assert.That(iron.Headline, Does.StartWith("MEGA"));
            Assert.That(iron.Archetype, Is.EqualTo("TAKHTA BESI"));
            Assert.That(iron.Edition, Does.Contain("5:21"));
            Assert.That(iron.News, Has.Some.Contains("sudah sesuai prosedur"));

            KoranEdition puppet = KoranKonoha.Compose(CampaignEnding.BonekaSistem, "PAK WI", SectorPath.Dirangkul,
                SectorPath.Dirangkul, 0, 4, 20, true, 2, 400);
            Assert.That(puppet.Archetype, Is.EqualTo("BONEKA SISTEM"));
            Assert.That(puppet.Subhead, Does.Contain("Konsorsium"));
            Assert.That(puppet.News, Has.Some.Contains("02.00"));
            Assert.That(puppet.News, Has.Some.Contains("#KaburAjaDulu"));
            foreach (string hero in new[] { "MEGA", "GEMOY", "ABAH", "PAK WI" })
                Assert.That(KoranKonoha.HeroLine(hero), Is.Not.Empty);
        }
    }
}
