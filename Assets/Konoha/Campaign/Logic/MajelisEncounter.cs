using System;

namespace Konoha.Campaign
{
    [Flags]
    public enum MajelisChange
    {
        None = 0,
        BlockBroken = 1,
        LeaderDown = 2,
        Cleared = 4
    }

    // §8.1 Majelis Daun — "Pecahkan Blok". Pure rules of the fight; the host feeds it the
    // number of members still standing each frame and applies the reported changes
    // (announcements, surrender, seal). Plain C#, covered by EditMode tests.
    //
    // - VOTING BLOCK: while at least two Anggota Senior stand, every Majelis unit takes 40%
    //   less damage. Once broken it stays broken for this sidang (recovered Wibawa after a
    //   hero Runtuh does not rebuild it; fallen Seniors do not return).
    // - When the Ketua falls the Staf Fraksi surrender.
    // - The sector is cleared when the Ketua is down and no officer (Senior, Pengawal) stands.
    public sealed class MajelisEncounter
    {
        public bool BlockBroken { get; private set; }
        public bool LeaderFallen { get; private set; }
        public bool Cleared { get; private set; }

        // The block protects the Majelis right now.
        public bool BlockHolds => !BlockBroken;

        public float IncomingDamageMultiplier => IncomingMultiplier(BlockHolds);

        public static bool BlockActive(int seniorsAlive) =>
            seniorsAlive >= CampaignTuning.Majelis.VotingBlockMinSeniors;

        public static float IncomingMultiplier(bool blockHolds) =>
            blockHolds ? 1f - CampaignTuning.Majelis.VotingBlockDamageReduction : 1f;

        // officersAlive counts Seniors and Pengawal Sidang (not the Ketua, not Staf Fraksi).
        public MajelisChange Evaluate(int seniorsAlive, bool leaderAlive, int officersAlive)
        {
            if (seniorsAlive < 0 || officersAlive < 0)
                throw new ArgumentOutOfRangeException(seniorsAlive < 0 ? nameof(seniorsAlive) : nameof(officersAlive));

            MajelisChange change = MajelisChange.None;
            if (Cleared)
                return change;

            if (!BlockBroken && !BlockActive(seniorsAlive))
            {
                BlockBroken = true;
                change |= MajelisChange.BlockBroken;
            }

            if (!LeaderFallen && !leaderAlive)
            {
                LeaderFallen = true;
                change |= MajelisChange.LeaderDown;
            }

            if (LeaderFallen && officersAlive == 0)
            {
                Cleared = true;
                change |= MajelisChange.Cleared;
            }

            return change;
        }

        public void Reset()
        {
            BlockBroken = false;
            LeaderFallen = false;
            Cleared = false;
        }
    }
}
