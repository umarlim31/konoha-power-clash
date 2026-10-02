using System;

namespace Konoha.Campaign
{
    [Flags]
    public enum GardaChange
    {
        None = 0,
        Reinforce = 1,
        LeaderDown = 2
    }

    // §8.3 Garda Takhta — "Gerbang Terakhir". Pure rules; the host feeds the Panglima's
    // Wibawa fraction and whether a hero stands deep inside the parade ring.
    //
    // - Bala bantuan: once, when the Panglima drops below 50% Wibawa.
    // - LOCKDOWN: the barrier ring closes when a hero is well inside it and opens again
    //   when that hero collapses (they revive outside, at checkpoint 5) or the Panglima falls.
    // - The Panglima falling ends the encounter: KURSI TERBUKA.
    public sealed class GardaEncounter
    {
        public bool ReinforcementSent { get; private set; }
        public bool LeaderFallen { get; private set; }
        public bool LockdownClosed { get; private set; }

        public GardaChange Evaluate(float leaderWibawaFraction, bool leaderAlive)
        {
            GardaChange change = GardaChange.None;
            if (LeaderFallen)
                return change;

            if (!leaderAlive)
            {
                LeaderFallen = true;
                LockdownClosed = false;
                return GardaChange.LeaderDown;
            }

            if (!ReinforcementSent && leaderWibawaFraction < CampaignTuning.Garda.ReinforcementHealthFraction)
            {
                ReinforcementSent = true;
                change |= GardaChange.Reinforce;
            }
            return change;
        }

        // Returns true when the ring state changed.
        public bool UpdateLockdown(bool heroInsideRing, bool heroDown)
        {
            bool closed = !LeaderFallen && !heroDown && (LockdownClosed || heroInsideRing);
            if (closed == LockdownClosed)
                return false;
            LockdownClosed = closed;
            return true;
        }

        public void Reset()
        {
            ReinforcementSent = false;
            LeaderFallen = false;
            LockdownClosed = false;
        }
    }
}
