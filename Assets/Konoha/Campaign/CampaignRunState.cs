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

    // Local run state for the solo campaign prototype. Callers must award seals only
    // after a validated encounter; co-op will need one host-owned source of truth.
    public sealed class CampaignRunState
    {
        private readonly bool[] seals = new bool[5];
        public CampaignPhase Phase { get; private set; } = CampaignPhase.GerbangRakyat;
        public int RequiredSeals { get; }
        public int SealCount { get; private set; }
        public int Power { get; private set; }
        public int TargetPower { get; }

        public CampaignRunState(int requiredSeals = 2, int targetPower = 100)
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
            if (Phase == CampaignPhase.GerbangRakyat)
                Phase = CampaignPhase.PlazaAspirasi;
        }

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
            SealCount++;
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
    }
}
