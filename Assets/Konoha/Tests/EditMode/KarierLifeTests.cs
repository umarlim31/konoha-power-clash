using Konoha.Campaign;
using NUnit.Framework;

namespace Konoha.Tests
{
    // 0.6.0 KARIER Level 1: money is earned before anything is bought, dirty money leaves a
    // record, fights draw the neighbourhood and then the police.
    public sealed class KarierLifeTests
    {
        private const float Frame = 1f / 30f;
        private static readonly KarierPoint Away = new KarierPoint(100f, 100f);

        private static KarierLayout Layout()
        {
            return new KarierLayout
            {
                PlaceNames = new[] { "A", "B", "C" },
                Places = new[] { new KarierPoint(0f, 0f), new KarierPoint(30f, 0f), new KarierPoint(0f, 30f) },
                KuliPickup = new KarierPoint(-10f, -10f),
                KuliDrop = new KarierPoint(10f, -10f),
                Warkop = new KarierPoint(-20f, 0f),
                PosRt = new KarierPoint(-8.6f, -26f),
                Siomay = new KarierPoint(-5f, -14f),
                Salome = new KarierPoint(5f, -14f),
                Bakso = new KarierPoint(9.4f, -40f),
                SewaSepeda = new KarierPoint(-11f, -47f),
                SewaMotor = new KarierPoint(15f, -24f),
                SapaNames = new[] { "POS RONDA", "IBU-IBU", "OJOL" },
                SapaPoints = new[] { new KarierPoint(-8.6f, -26f), new KarierPoint(8.6f, -26f), new KarierPoint(13f, -21f) }
            };
        }

        private static void Stand(KarierLife life, KarierPoint at, float seconds, bool fighting = false)
        {
            for (float t = 0f; t < seconds; t += Frame)
                life.Tick(at, Frame, fighting, false);
        }

        [Test]
        public void StartsPoorAndCannotRegisterWithoutEarning()
        {
            var life = new KarierLife(Layout());
            Assert.That(life.Duit, Is.EqualTo(CampaignTuning.Karier.DuitStart));
            Assert.That(life.Duit, Is.LessThan(CampaignTuning.Karier.SyukuranRT));
            Assert.That(life.CanDaftar, Is.False);
            Assert.That(life.ActionAt(Layout().PosRt, out int sapa), Is.EqualTo(KarierAction.Sapa), "Pos ronda is greeted, not registered at");
            Assert.That(sapa, Is.EqualTo(0));
            Assert.That(life.DoAction(KarierAction.DaftarRT, -1), Is.False);
        }

        [Test]
        public void KuliPaysAfterFiveSacksMinusTheMandorCut()
        {
            KarierLayout layout = Layout();
            var life = new KarierLife(layout);
            Assert.That(life.TakeJob(KarierJob.Kuli, out _), Is.True);
            int start = life.Duit;
            for (int i = 0; i < CampaignTuning.Karier.KuliSacks; i++)
            {
                Stand(life, layout.KuliPickup, CampaignTuning.Karier.KuliPickSeconds + 0.1f);
                Assert.That(life.Carrying, Is.True);
                Stand(life, layout.KuliDrop, CampaignTuning.Karier.KuliDropSeconds + 0.1f);
                Assert.That(life.Carrying, Is.False);
            }
            Assert.That(life.Job, Is.EqualTo(KarierJob.None));
            // The first job also finishes mission KERJA PERTAMA (0.6.1).
            Assert.That(life.Duit - start, Is.EqualTo(CampaignTuning.Karier.KuliWage - CampaignTuning.Karier.KuliMandorCut +
                CampaignTuning.Karier.MisiKerjaReward));
            Assert.That(life.Mission, Is.EqualTo(1));
            Assert.That(life.Energi, Is.EqualTo(CampaignTuning.Karier.EnergiMax - CampaignTuning.Karier.KuliSacks * CampaignTuning.Karier.KuliEnergiPerSack));
        }

        [Test]
        public void LeavingTheZoneResetsTheStep()
        {
            KarierLayout layout = Layout();
            var life = new KarierLife(layout);
            life.TakeJob(KarierJob.Kuli, out _);
            Stand(life, layout.KuliPickup, CampaignTuning.Karier.KuliPickSeconds * 0.6f);
            Assert.That(life.StepProgress, Is.GreaterThan(0.4f));
            life.Tick(Away, Frame, false, false);
            Assert.That(life.StepProgress, Is.EqualTo(0f));
            Assert.That(life.Carrying, Is.False);
        }

        [Test]
        public void OjolDeliveryPaysTheFareMinusTheAppCutAndKeepsNarik()
        {
            var life = new KarierLife(Layout(), 7);
            Assert.That(life.TakeJob(KarierJob.Ojol, out _), Is.True);
            Assert.That(life.OjolFrom, Is.Not.EqualTo(life.OjolTo));
            KarierPoint from = life.Place(life.OjolFrom), to = life.Place(life.OjolTo);
            int start = life.Duit;
            Stand(life, from, CampaignTuning.Karier.OjolPickupSeconds + 0.1f);
            Assert.That(life.Step, Is.EqualTo(KarierStep.OjolAntar));
            Stand(life, to, CampaignTuning.Karier.OjolPickupSeconds + 0.1f);
            int gross = KarierLife.OjolGrossFare(KarierPoint.Distance(from, to));
            int cut = KarierLife.Round500(gross * CampaignTuning.Karier.OjolAppCut);
            Assert.That(life.Duit - start, Is.EqualTo(gross - cut + CampaignTuning.Karier.MisiKerjaReward));
            Assert.That(life.TripsOjol, Is.EqualTo(1));
            Assert.That(life.Job, Is.EqualTo(KarierJob.Ojol), "A new order comes in automatically");
            Assert.That(life.Step, Is.EqualTo(KarierStep.OjolJemput));
        }

        [Test]
        public void BuzzerIsFastMoneyWithACriminalRecord()
        {
            KarierLayout layout = Layout();
            var life = new KarierLife(layout);
            Assert.That(life.TakeJob(KarierJob.Buzzer, out _), Is.True);
            Stand(life, layout.Warkop, CampaignTuning.Karier.BuzzerTypeSeconds + 0.1f);
            Assert.That(life.Duit, Is.EqualTo(CampaignTuning.Karier.DuitStart + CampaignTuning.Karier.BuzzerPay));
            Assert.That(life.CatatanHitam, Is.EqualTo(CampaignTuning.Karier.BuzzerCatatan));
            Assert.That(life.Restu, Is.EqualTo(CampaignTuning.Karier.RestuStart + CampaignTuning.Karier.BuzzerRestu));
            Assert.That(life.TakeJob(KarierJob.Buzzer, out string reason), Is.False, "Cooldown between orders");
            Assert.That(reason, Does.Contain("Tunggu"));
        }

        [Test]
        public void FightDrawsShoutThenMeleraiThenPolice()
        {
            var life = new KarierLife(Layout());
            Stand(life, Away, CampaignTuning.Karier.KeributanFillSeconds * CampaignTuning.Karier.TeriakAt + 0.1f, true);
            Assert.That(life.Shouted, Is.True);
            Assert.That(life.MeleraiDone, Is.False);
            Stand(life, Away, CampaignTuning.Karier.KeributanFillSeconds * (CampaignTuning.Karier.MeleraiAt - CampaignTuning.Karier.TeriakAt) + 0.1f, true);
            Assert.That(life.MeleraiActive, Is.True);
            float frozen = life.Keributan;
            Stand(life, Away, CampaignTuning.Karier.MeleraiSeconds * 0.5f, true);
            Assert.That(life.Keributan, Is.EqualTo(frozen), "The fight pauses while someone melerai");
            Stand(life, Away, CampaignTuning.Karier.KeributanFillSeconds + CampaignTuning.Karier.MeleraiSeconds, true);
            Assert.That(life.PoliceReason, Is.EqualTo(KarierPoliceReason.Keributan));
            Assert.That(life.ResolvePolice(KarierPoliceChoice.Polsek), Is.False, "Not before they arrive");
        }

        [Test]
        public void ShortFightStillBringsThePoliceLate()
        {
            var life = new KarierLife(Layout());
            Stand(life, Away, 2f, true);
            Assert.That(life.Shouted, Is.True);
            Assert.That(life.PoliceEta, Is.GreaterThan(0f), "Someone called the police");
            life.OnPremanBeaten(1);
            Assert.That(life.BeatenThisFight, Is.True);
            Stand(life, Away, CampaignTuning.Karier.PolisiDatangSeconds + 0.5f);
            Assert.That(life.PoliceReason, Is.EqualTo(KarierPoliceReason.Keributan), "0.6.1: every shouted fight gets the police");
            life.PoliceArrive();
            Assert.That(life.CanSaksi, Is.EqualTo(life.Restu >= CampaignTuning.Karier.SaksiRestu));
            life.AddRestu(100);
            Assert.That(life.ResolvePolice(KarierPoliceChoice.Saksi), Is.True, "Warga testify for their hero");
            Assert.That(life.PoliceCalled, Is.False);
            Assert.That(life.Shouted, Is.False);
        }

        [Test]
        public void PolsekMeansJailUntilTimeOrTebus()
        {
            var life = new KarierLife(Layout());
            life.CallPolice(KarierPoliceReason.Keributan);
            life.PoliceArrive();
            Assert.That(life.ResolvePolice(KarierPoliceChoice.Polsek), Is.True);
            life.EnterJail();
            Assert.That(life.InJail, Is.True);
            Assert.That(life.TakeJob(KarierJob.Kuli, out _), Is.False);
            Assert.That(life.Tebus(), Is.False, "Rp 50.000 is not enough bail");
            Stand(life, Away, CampaignTuning.Karier.JailSeconds + 0.1f);
            Assert.That(life.InJail, Is.False);
            life.EnterJail();
            life.AddDuit(life.TebusPrice);
            Assert.That(life.Tebus(), Is.True);
            Assert.That(life.InJail, Is.False);
        }

        [Test]
        public void JajanAndRentalsCostMoney()
        {
            KarierLayout layout = Layout();
            var life = new KarierLife(layout);
            life.AddEnergi(-50);
            Assert.That(life.ActionAt(layout.Siomay, out _), Is.EqualTo(KarierAction.BeliSiomay));
            Assert.That(life.DoAction(KarierAction.BeliSiomay, -1), Is.True);
            Assert.That(life.Eating, Is.True);
            Assert.That(life.Duit, Is.EqualTo(CampaignTuning.Karier.DuitStart - CampaignTuning.Karier.SiomayPrice));
            Assert.That(life.ActionAt(layout.SewaSepeda, out _), Is.EqualTo(KarierAction.SewaSepeda));
            Assert.That(life.DoAction(KarierAction.SewaSepeda, -1), Is.True);
            Assert.That(life.Ride, Is.EqualTo(KarierRide.Sepeda));
            Assert.That(life.ActionAt(Away, out _), Is.EqualTo(KarierAction.Turun));
            Assert.That(life.TakeJob(KarierJob.Kuli, out _), Is.True);
            Assert.That(life.Ride, Is.EqualTo(KarierRide.None), "Jobs return the rented bike first");
        }

        [Test]
        public void MissionChainAdvancesAndWaitsForTheOffer()
        {
            KarierLayout layout = Layout();
            var life = new KarierLife(layout);
            Assert.That(life.MissionTitle, Is.EqualTo("KERJA PERTAMA"));
            life.TakeJob(KarierJob.Kuli, out _);
            for (int i = 0; i < CampaignTuning.Karier.KuliSacks; i++)
            {
                Stand(life, layout.KuliPickup, CampaignTuning.Karier.KuliPickSeconds + 0.1f);
                Stand(life, layout.KuliDrop, CampaignTuning.Karier.KuliDropSeconds + 0.1f);
            }
            for (int i = 0; i < 3; i++) life.DoAction(KarierAction.Sapa, i);
            Stand(life, Away, 0.2f);
            Assert.That(life.Mission, Is.EqualTo(2));
            life.DoAction(KarierAction.BeliBakso, -1);
            life.DoAction(KarierAction.SewaMotor, -1);
            life.OnPremanBeaten(1);
            Stand(life, Away, 0.5f);
            Assert.That(life.Mission, Is.EqualTo(KarierLife.MisiTawaran));
            Assert.That(life.OfferPending, Is.True);
            Stand(life, Away, 0.5f);
            Assert.That(life.Mission, Is.EqualTo(KarierLife.MisiTawaran), "The offer waits for an answer");
            int restu = life.Restu;
            Assert.That(life.AnswerOffer(false), Is.True);
            Assert.That(life.Restu, Is.EqualTo(restu + CampaignTuning.Karier.TolakRestu));
            Assert.That(life.Mission, Is.EqualTo(KarierLife.MisiTawaran + 1));
        }

        [Test]
        public void PoliceChoicesCostMoneyRecordOrReputation()
        {
            var life = new KarierLife(Layout());
            life.CallPolice(KarierPoliceReason.Keributan);
            life.PoliceArrive();
            Assert.That(life.CanDamai, Is.False, "Rp 50.000 is not enough to \"damai\"");
            Assert.That(life.ResolvePolice(KarierPoliceChoice.Damai), Is.False);
            Assert.That(life.ResolvePolice(KarierPoliceChoice.Kabur), Is.True);
            Assert.That(life.CatatanHitam, Is.EqualTo(CampaignTuning.Karier.KaburCatatan));
            Assert.That(life.PoliceCalled, Is.False);

            life.AddDuit(500000);
            life.CallPolice(KarierPoliceReason.Keributan);
            life.PoliceArrive();
            int price = life.DamaiPrice;
            int before = life.Duit;
            Assert.That(life.ResolvePolice(KarierPoliceChoice.Damai), Is.True);
            Assert.That(before - life.Duit, Is.EqualTo(price));
        }

        [Test]
        public void RegisteringNeedsMoneyAndRestuAtThePosRonda()
        {
            KarierLayout layout = Layout();
            var life = new KarierLife(layout);
            life.AddDuit(CampaignTuning.Karier.SyukuranRT);
            Assert.That(life.CanDaftar, Is.False, "Restu still too low");
            life.AddRestu(CampaignTuning.Karier.RestuSyaratRT);
            Assert.That(life.CanDaftar, Is.True);
            Assert.That(life.Target(out KarierPoint target, out _), Is.True);
            Assert.That(KarierPoint.Distance(target, layout.PosRt), Is.LessThan(0.01f));
            Assert.That(life.ActionAt(layout.PosRt, out _), Is.EqualTo(KarierAction.DaftarRT));
            Assert.That(life.DoAction(KarierAction.DaftarRT, -1), Is.True);
            Assert.That(life.Registered, Is.True);
            Assert.That(life.Duit, Is.EqualTo(CampaignTuning.Karier.DuitStart));
        }

        [Test]
        public void SapaOncePerGroupAndMakanCostsMoney()
        {
            KarierLayout layout = Layout();
            var life = new KarierLife(layout);
            Assert.That(life.DoAction(KarierAction.Sapa, 1), Is.True);
            Assert.That(life.DoAction(KarierAction.Sapa, 1), Is.False);
            Assert.That(life.Restu, Is.EqualTo(CampaignTuning.Karier.RestuStart + CampaignTuning.Karier.SapaRestu));
            // 0.6.1: stalls always offer food; eating with full energy just says "masih kenyang".
            Assert.That(life.ActionAt(layout.Warkop, out _), Is.EqualTo(KarierAction.Makan));
            Assert.That(life.DoAction(KarierAction.Makan, -1), Is.False, "Full energy: still full");
            life.AddEnergi(-50);
            Assert.That(life.ActionAt(layout.Warkop, out _), Is.EqualTo(KarierAction.Makan));
            Assert.That(life.DoAction(KarierAction.Makan, -1), Is.True);
            Assert.That(life.Duit, Is.EqualTo(CampaignTuning.Karier.DuitStart - CampaignTuning.Karier.MakanPrice));
        }

        [Test]
        public void RebahanRestoresEnergyOverTime()
        {
            var life = new KarierLife(Layout());
            Assert.That(life.StartRebahan(out _), Is.False, "Full energy");
            life.AddEnergi(-60);
            Assert.That(life.StartRebahan(out _), Is.True);
            Assert.That(life.TakeJob(KarierJob.Kuli, out _), Is.False);
            Stand(life, Away, CampaignTuning.Karier.RebahanSeconds + 0.1f);
            Assert.That(life.Rebahan, Is.False);
            Assert.That(life.Energi, Is.EqualTo(40 + CampaignTuning.Karier.RebahanEnergi));
        }

        [Test]
        public void SaveAndAvatarRoundTrip()
        {
            var life = new KarierLife(Layout());
            life.AddDuit(123500);
            life.AddCatatan(35);
            life.DoAction(KarierAction.Sapa, 2);
            var copy = new KarierLife(Layout());
            Assert.That(copy.TryLoad(life.Serialize()), Is.True);
            Assert.That(copy.Duit, Is.EqualTo(life.Duit));
            Assert.That(copy.CatatanHitam, Is.EqualTo(35));
            Assert.That(copy.Sapa(2), Is.True);
            Assert.That(copy.TryLoad("rusak"), Is.False);
            Assert.That(copy.TryLoad("K1;250000;80;40;10;3;0;2;1;0;1"), Is.True, "0.6.0 saves still load");
            Assert.That(copy.Duit, Is.EqualTo(250000));
            Assert.That(copy.Mission, Is.EqualTo(0));

            var avatar = new KarierAvatar { Name = "budi santoso!!", Female = true, Skin = 2, Hair = 3, Body = 2, Shirt = 9 };
            Assert.That(KarierAvatar.TryParse(avatar.Serialize(), out KarierAvatar parsed), Is.True);
            Assert.That(parsed.Name, Is.EqualTo("BUDI SANTOSO"));
            Assert.That(parsed.Female, Is.True);
            Assert.That(parsed.Shirt, Is.EqualTo(9 % KarierAvatar.ShirtNames.Length));
            Assert.That(KarierAvatar.CleanName("   "), Is.EqualTo("WARGA"));
        }

        [Test]
        public void RupiahUsesDots()
        {
            Assert.That(KarierLife.Rupiah(1250000), Is.EqualTo("Rp 1.250.000"));
            Assert.That(KarierLife.Rupiah(500), Is.EqualTo("Rp 500"));
            Assert.That(KarierLife.Round500(12340f), Is.EqualTo(12500));
        }
    }
}
