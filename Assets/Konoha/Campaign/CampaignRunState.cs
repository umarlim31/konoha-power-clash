using System;

namespace Konoha.Campaign
{
    public enum CampaignSector
    {
        MajelisDaun = 0,
        BiroProsedur = 1,
        KomisiSuara = 2,
        KonsorsiumModal = 3,
        MenaraNarasi = 4
    }

    public enum CampaignPhase
    {
        GerbangRakyat,
        PlazaAspirasi,
        GerbangDalam,
        GardaTakhta,
        KursiTerbuka,
        Memerintah,
        Menang
    }

    // Respawn points of §3. Values are ordered along the route; the run never moves back.
    // Majelis and Biro may be cleared in either order, so reaching a lower-numbered
    // sector after a higher one keeps the later checkpoint.
    public enum CampaignCheckpoint
    {
        GerbangRakyat = 1,
        PlazaAspirasi = 2,
        MajelisDaun = 3,
        BiroProsedur = 4,
        GardaTakhta = 5
    }

    public enum SectorState
    {
        Terkunci,
        Tersedia,
        Berlangsung,
        Selesai
    }

    // Local run state for the solo campaign prototype. Callers must award seals only
    // after a validated encounter; co-op will need one host-owned source of truth.
    public sealed class CampaignRunState
    {
        private readonly bool[] seals = new bool[CampaignTuning.Seals.SectorCount];
        private readonly SectorState[] sectors = new SectorState[CampaignTuning.Seals.SectorCount];
        public CampaignPhase Phase { get; private set; } = CampaignPhase.GerbangRakyat;
        public CampaignCheckpoint Checkpoint { get; private set; } = CampaignCheckpoint.GerbangRakyat;
        public int RuntuhCount { get; private set; }
        public int RequiredSeals { get; }
        public int SealCount { get; private set; }
        public int Power { get; private set; }
        public int TargetPower { get; }

        public CampaignRunState(int requiredSeals = CampaignTuning.Seals.Required,
            int targetPower = CampaignTuning.Memerintah.TargetPower)
        {
            if (requiredSeals < 1 || requiredSeals > seals.Length)
                throw new ArgumentOutOfRangeException(nameof(requiredSeals));
            if (targetPower <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetPower));
            RequiredSeals = requiredSeals;
            TargetPower = targetPower;
        }

        public void ReachPlaza()
        {
            if (Phase != CampaignPhase.GerbangRakyat) return;
            Phase = CampaignPhase.PlazaAspirasi;
            ReachCheckpoint(CampaignCheckpoint.PlazaAspirasi);
            for (int i = 0; i < sectors.Length; i++)
                if (sectors[i] == SectorState.Terkunci) sectors[i] = SectorState.Tersedia;
        }

        public SectorState GetSectorState(CampaignSector sector)
        {
            int index = (int)sector;
            return index >= 0 && index < sectors.Length ? sectors[index] : SectorState.Terkunci;
        }

        // Marks an available sector as in progress; its checkpoint (if any) is recorded.
        public bool BeginSector(CampaignSector sector)
        {
            int index = (int)sector;
            if (Phase != CampaignPhase.PlazaAspirasi || index < 0 || index >= sectors.Length ||
                sectors[index] != SectorState.Tersedia)
                return false;
            sectors[index] = SectorState.Berlangsung;
            RecordSectorCheckpoint(sector);
            return true;
        }

        // Moves the checkpoint forward only; returns false for the same or an earlier one.
        public bool ReachCheckpoint(CampaignCheckpoint checkpoint)
        {
            if (checkpoint <= Checkpoint) return false;
            Checkpoint = checkpoint;
            return true;
        }

        // Counts a hero collapse for the result screen. Position/seat handling stays with
        // the caller (LoseSeat), so the existing slice behaviour is unchanged.
        public void RecordRuntuh() => RuntuhCount++;

        public bool HasSeal(CampaignSector sector)
        {
            int index = (int)sector;
            return index >= 0 && index < seals.Length && seals[index];
        }

        public bool AwardSeal(CampaignSector sector)
        {
            int index = (int)sector;
            if (Phase != CampaignPhase.PlazaAspirasi || index < 0 || index >= seals.Length || seals[index])
                return false;
            seals[index] = true;
            sectors[index] = SectorState.Selesai;
            SealCount++;
            RecordSectorCheckpoint(sector);
            return true;
        }

        public bool TryOpenInnerGate()
        {
            if (Phase != CampaignPhase.PlazaAspirasi || SealCount < RequiredSeals)
                return false;
            Phase = CampaignPhase.GerbangDalam;
            return true;
        }

        public bool TryStartGuard()
        {
            if (Phase != CampaignPhase.GerbangDalam) return false;
            Phase = CampaignPhase.GardaTakhta;
            ReachCheckpoint(CampaignCheckpoint.GardaTakhta);
            return true;
        }

        public bool DefeatGuard()
        {
            if (Phase != CampaignPhase.GardaTakhta) return false;
            Phase = CampaignPhase.KursiTerbuka;
            return true;
        }

        public bool TrySit()
        {
            if (Phase != CampaignPhase.KursiTerbuka) return false;
            Phase = CampaignPhase.Memerintah;
            return true;
        }

        // §9 KUDETA: the reign failed; Power returns to zero and the seat is open again.
        public void ResetPower()
        {
            Power = 0;
            if (Phase == CampaignPhase.Memerintah)
                Phase = CampaignPhase.KursiTerbuka;
        }

        public void LoseSeat()
        {
            if (Phase == CampaignPhase.Memerintah)
                Phase = CampaignPhase.KursiTerbuka;
        }

        public bool GainPower(int amount)
        {
            if (Phase != CampaignPhase.Memerintah || amount <= 0) return false;
            Power += Math.Min(amount, TargetPower - Power);
            if (Power == TargetPower)
                Phase = CampaignPhase.Menang;
            return true;
        }

        private void RecordSectorCheckpoint(CampaignSector sector)
        {
            if (sector == CampaignSector.MajelisDaun) ReachCheckpoint(CampaignCheckpoint.MajelisDaun);
            else if (sector == CampaignSector.BiroProsedur) ReachCheckpoint(CampaignCheckpoint.BiroProsedur);
        }
    }
}
