using System.Collections.Generic;
using System.Linq;
using Konoha.Campaign;
using NUnit.Framework;

namespace Konoha.Tests
{
    public sealed class CampaignLogicTests
    {
        private static int Count(IReadOnlyList<UnitSpawn> spawns, UnitRole role, SpawnTrigger? trigger = null) =>
            spawns.Where(s => s.Role == role && (trigger == null || s.Trigger == trigger)).Sum(s => s.Count);

        [Test]
        public void SoloCompositionMatchesFactionSpec()
        {
            // §8.1: 1 Ketua, 2 Senior, 3 Kroni, 1 Guard.
            var majelis = EncounterComposer.Compose(FactionId.MajelisDaun, 1);
            Assert.That(Count(majelis, UnitRole.Pemimpin), Is.EqualTo(1));
            Assert.That(Count(majelis, UnitRole.Senior), Is.EqualTo(2));
            Assert.That(Count(majelis, UnitRole.Kroni), Is.EqualTo(3));
            Assert.That(Count(majelis, UnitRole.Guard), Is.EqualTo(1));
            Assert.That(EncounterComposer.TotalUnits(majelis), Is.EqualTo(7));

            // §8.2: 1 Kepala, 1 Pengawas, 2 respawning Arsip, 1 Security.
            var biro = EncounterComposer.Compose(FactionId.BiroProsedur, 1);
            Assert.That(Count(biro, UnitRole.Pemimpin), Is.EqualTo(1));
            Assert.That(Count(biro, UnitRole.Spesialis), Is.EqualTo(1));
            Assert.That(Count(biro, UnitRole.Kroni, SpawnTrigger.Respawn), Is.EqualTo(2));
            Assert.That(Count(biro, UnitRole.Guard), Is.EqualTo(1));
            Assert.That(EncounterComposer.LoketCount(1), Is.EqualTo(3));

            // §8.3: 1 Panglima, 2 Guard, 2 Kroni reinforcements.
            var garda = EncounterComposer.Compose(FactionId.GardaTakhta, 1);
            Assert.That(Count(garda, UnitRole.Pemimpin), Is.EqualTo(1));
            Assert.That(Count(garda, UnitRole.Guard), Is.EqualTo(2));
            Assert.That(Count(garda, UnitRole.Kroni, SpawnTrigger.Reinforcement), Is.EqualTo(2));
            Assert.That(Count(garda, UnitRole.Kroni, SpawnTrigger.Start), Is.Zero);
        }

        [Test]
        public void ScalingAddsUnitsNotOnlyHealth()
        {
            foreach (var faction in new[] { FactionId.MajelisDaun, FactionId.BiroProsedur, FactionId.GardaTakhta })
            {
                int solo = EncounterComposer.TotalUnits(EncounterComposer.Compose(faction, 1));
                int previous = solo;
                for (int players = 2; players <= 4; players++)
                {
                    int total = EncounterComposer.TotalUnits(EncounterComposer.Compose(faction, players));
                    Assert.That(total, Is.GreaterThan(solo), faction + " x" + players);
                    Assert.That(total, Is.GreaterThanOrEqualTo(previous), faction + " x" + players);
                    previous = total;
                }
            }
            // Specific §11 rows.
            Assert.That(Count(EncounterComposer.Compose(FactionId.MajelisDaun, 2), UnitRole.Senior), Is.EqualTo(3));
            Assert.That(Count(EncounterComposer.Compose(FactionId.BiroProsedur, 2), UnitRole.Kroni), Is.EqualTo(3));
            Assert.That(Count(EncounterComposer.Compose(FactionId.BiroProsedur, 3), UnitRole.Spesialis), Is.EqualTo(2));
            Assert.That(EncounterComposer.LoketCount(4), Is.EqualTo(4));
            var fullGarda = EncounterComposer.Compose(FactionId.GardaTakhta, 4);
            Assert.That(fullGarda.Where(s => s.Trigger == SpawnTrigger.Reinforcement).Select(s => s.Wave).Distinct().Count(),
                Is.EqualTo(2));
        }

        [Test]
        public void PlayerCountIsClampedToOneThroughFour()
        {
            Assert.That(EncounterComposer.ClampPlayers(0), Is.EqualTo(1));
            Assert.That(EncounterComposer.ClampPlayers(-3), Is.EqualTo(1));
            Assert.That(EncounterComposer.ClampPlayers(9), Is.EqualTo(4));
            foreach (var faction in new[] { FactionId.MajelisDaun, FactionId.BiroProsedur, FactionId.GardaTakhta })
            {
                Assert.That(EncounterComposer.TotalUnits(EncounterComposer.Compose(faction, 0)),
                    Is.EqualTo(EncounterComposer.TotalUnits(EncounterComposer.Compose(faction, 1))));
                Assert.That(EncounterComposer.TotalUnits(EncounterComposer.Compose(faction, 12)),
                    Is.EqualTo(EncounterComposer.TotalUnits(EncounterComposer.Compose(faction, 4))));
            }
        }

        [Test]
        public void DeferredFactionsHaveNoRosterYet()
        {
            Assert.That(EncounterComposer.Compose(FactionId.KomisiSuara, 1), Is.Empty);
            Assert.That(FactionDefinition.Get(FactionId.KonsorsiumModal).IsMvp, Is.False);
            Assert.That(FactionDefinition.Get(FactionId.MajelisDaun).IsMvp, Is.True);
            Assert.That(FactionDefinition.Get(FactionId.BiroProsedur).BaseRoles, Contains.Item(UnitRole.Spesialis));
        }

        [Test]
        public void RoleStatsFollowOrganisationTable()
        {
            Assert.That(UnitRoleStats.For(UnitRole.Kroni).Wibawa, Is.EqualTo(40));
            Assert.That(UnitRoleStats.For(UnitRole.Guard).GuardRadius, Is.EqualTo(7f));
            Assert.That(UnitRoleStats.For(UnitRole.Guard).HasGuardRadius, Is.True);
            Assert.That(UnitRoleStats.For(UnitRole.Spesialis).Chases, Is.False);
            Assert.That(UnitRoleStats.For(UnitRole.Senior).IsElite, Is.True);
            Assert.That(UnitRoleStats.For(UnitRole.Pemimpin).Wibawa, Is.EqualTo(320));
            Assert.That(UnitRoleStats.For(UnitRole.Pemimpin).HasTelegraphedSpecial, Is.True);
        }

        [Test]
        public void CheckpointOnlyMovesForward()
        {
            var run = new CampaignRunState();
            Assert.That(run.Checkpoint, Is.EqualTo(CampaignCheckpoint.GerbangRakyat));
            run.ReachPlaza();
            Assert.That(run.Checkpoint, Is.EqualTo(CampaignCheckpoint.PlazaAspirasi));
            // Biro first, then Majelis: the later checkpoint is kept.
            Assert.That(run.BeginSector(CampaignSector.BiroProsedur), Is.True);
            Assert.That(run.Checkpoint, Is.EqualTo(CampaignCheckpoint.BiroProsedur));
            run.AwardSeal(CampaignSector.MajelisDaun);
            Assert.That(run.Checkpoint, Is.EqualTo(CampaignCheckpoint.BiroProsedur));
            Assert.That(run.ReachCheckpoint(CampaignCheckpoint.PlazaAspirasi), Is.False);
            run.AwardSeal(CampaignSector.BiroProsedur);
            run.TryOpenInnerGate();
            run.TryStartGuard();
            Assert.That(run.Checkpoint, Is.EqualTo(CampaignCheckpoint.GardaTakhta));
            Assert.That(run.ReachCheckpoint(CampaignCheckpoint.GerbangRakyat), Is.False);
            Assert.That(run.Checkpoint, Is.EqualTo(CampaignCheckpoint.GardaTakhta));
        }

        [Test]
        public void SectorStatesProgressWithRoute()
        {
            var run = new CampaignRunState();
            Assert.That(run.GetSectorState(CampaignSector.MajelisDaun), Is.EqualTo(SectorState.Terkunci));
            Assert.That(run.BeginSector(CampaignSector.MajelisDaun), Is.False);
            run.ReachPlaza();
            Assert.That(run.GetSectorState(CampaignSector.MajelisDaun), Is.EqualTo(SectorState.Tersedia));
            Assert.That(run.BeginSector(CampaignSector.MajelisDaun), Is.True);
            Assert.That(run.BeginSector(CampaignSector.MajelisDaun), Is.False);
            Assert.That(run.GetSectorState(CampaignSector.MajelisDaun), Is.EqualTo(SectorState.Berlangsung));
            run.AwardSeal(CampaignSector.MajelisDaun);
            Assert.That(run.GetSectorState(CampaignSector.MajelisDaun), Is.EqualTo(SectorState.Selesai));
            Assert.That(run.GetSectorState(CampaignSector.BiroProsedur), Is.EqualTo(SectorState.Tersedia));
        }

        [Test]
        public void RuntuhCountAccumulatesWithoutChangingPhase()
        {
            var run = new CampaignRunState();
            Assert.That(run.RuntuhCount, Is.Zero);
            run.RecordRuntuh();
            run.RecordRuntuh();
            Assert.That(run.RuntuhCount, Is.EqualTo(2));
            Assert.That(run.Phase, Is.EqualTo(CampaignPhase.GerbangRakyat));
        }

        [Test]
        public void DefaultRunUsesMvpTuning()
        {
            var run = new CampaignRunState();
            Assert.That(run.RequiredSeals, Is.EqualTo(CampaignTuning.Seals.Required));
            Assert.That(run.TargetPower, Is.EqualTo(CampaignTuning.Memerintah.TargetPower));
        }
    }
}
