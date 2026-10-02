using System;

namespace Konoha.Campaign
{
    [Flags]
    public enum BiroChange
    {
        None = 0,
        LoketStamped = 1,
        AllStamped = 2,
        LeaderDown = 4,
        Cleared = 8
    }

    // §8.2 Biro Prosedur — "Sahkan Berkas". Pure rules of the encounter; the host feeds it
    // which loket a hero stands in, whether a Biro member queues inside that loket, how
    // many Petugas Arsip stand, and whether the Kepala Biro stands. Plain C#, EditMode tests.
    //
    // - LOKET: standing inside fills it in 3 s; an enemy inside pauses it (ANTRIAN); a loket
    //   nobody stands in loses 20% of a full stamp per second. A stamped loket stays stamped.
    // - All lokets stamped: the Kepala Biro's door opens.
    // - Petugas Arsip return every 12 s while fewer than the maximum stand, until all
    //   lokets are stamped.
    // - Cleared: every loket stamped and the Kepala Biro down.
    public sealed class BiroEncounter
    {
        private readonly float[] progress;
        private readonly bool[] stamped;
        private float arsipClock;
        // Frame-sized float steps must not leave a loket at 99.99%.
        private const float FillTolerance = 0.0001f;

        public int LoketCount => progress.Length;
        public int StampedCount { get; private set; }
        public bool AllStamped => StampedCount == progress.Length;
        public bool LeaderFallen { get; private set; }
        public bool Cleared { get; private set; }
        // Index of the loket stamped by the latest TickLokets, -1 when none.
        public int LastStamped { get; private set; } = -1;

        public BiroEncounter(int loketCount = CampaignTuning.Biro.LoketCount)
        {
            if (loketCount < 1) throw new ArgumentOutOfRangeException(nameof(loketCount));
            progress = new float[loketCount];
            stamped = new bool[loketCount];
        }

        public float Progress(int loket) => progress[loket];
        public bool IsStamped(int loket) => stamped[loket];

        // STEMPEL TUNDA (§8.2): skill cooldowns +30% near a living Pengawas.
        public static float CooldownMultiplier(bool insideStempelTunda) =>
            insideStempelTunda ? CampaignTuning.Biro.StempelTundaCooldownMultiplier : 1f;

        // heroLoket: index of the loket a hero stands in, -1 for none.
        // contested: a Biro member stands inside that same loket (ANTRIAN).
        public BiroChange TickLokets(int heroLoket, bool contested, float deltaTime)
        {
            LastStamped = -1;
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (heroLoket >= progress.Length) throw new ArgumentOutOfRangeException(nameof(heroLoket));

            BiroChange change = BiroChange.None;
            if (AllStamped)
                return change;

            for (int i = 0; i < progress.Length; i++)
            {
                if (stamped[i])
                    continue;

                if (i == heroLoket)
                {
                    if (contested)
                        continue; // ANTRIAN: paused, not lost.
                    progress[i] = Math.Min(1f, progress[i] + deltaTime / CampaignTuning.Biro.LoketFillSeconds);
                    if (progress[i] >= 1f - FillTolerance)
                    {
                        progress[i] = 1f;
                        stamped[i] = true;
                        StampedCount++;
                        LastStamped = i;
                        change |= BiroChange.LoketStamped;
                        if (AllStamped)
                            change |= BiroChange.AllStamped;
                    }
                }
                else
                {
                    progress[i] = Math.Max(0f, progress[i] - deltaTime * CampaignTuning.Biro.LoketDecayPerSecond);
                }
            }

            return change;
        }

        // Returns how many Petugas Arsip to place now (0 or 1).
        public int TickArsip(int arsipAlive, int maxAlive, float deltaTime)
        {
            if (AllStamped || arsipAlive >= maxAlive)
            {
                arsipClock = 0f;
                return 0;
            }

            arsipClock += deltaTime;
            if (arsipClock < CampaignTuning.Biro.ArsipRespawnSeconds)
                return 0;

            arsipClock = 0f;
            return 1;
        }

        public BiroChange Evaluate(bool leaderAlive)
        {
            BiroChange change = BiroChange.None;
            if (Cleared)
                return change;

            if (!LeaderFallen && !leaderAlive)
            {
                LeaderFallen = true;
                change |= BiroChange.LeaderDown;
            }

            if (LeaderFallen && AllStamped)
            {
                Cleared = true;
                change |= BiroChange.Cleared;
            }

            return change;
        }

        public void Reset()
        {
            Array.Clear(progress, 0, progress.Length);
            Array.Clear(stamped, 0, stamped.Length);
            StampedCount = 0;
            LeaderFallen = false;
            Cleared = false;
            LastStamped = -1;
            arsipClock = 0f;
        }
    }
}
