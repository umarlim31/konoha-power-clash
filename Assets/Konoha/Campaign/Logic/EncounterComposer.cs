using System;
using System.Collections.Generic;

namespace Konoha.Campaign
{
    public enum SpawnTrigger
    {
        // Present when the encounter starts.
        Start,
        // Returns on a timer while the objective is open; Count is the maximum alive.
        Respawn,
        // Arrives when the leader drops below the reinforcement threshold.
        Reinforcement
    }

    public readonly struct UnitSpawn
    {
        public UnitRole Role { get; }
        public string Title { get; }
        public int Count { get; }
        public SpawnTrigger Trigger { get; }
        // 1-based reinforcement wave; 0 for Start/Respawn groups.
        public int Wave { get; }

        public UnitSpawn(UnitRole role, string title, int count, SpawnTrigger trigger = SpawnTrigger.Start, int wave = 0)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            Role = role;
            Title = title;
            Count = count;
            Trigger = trigger;
            Wave = wave;
        }
    }

    // Pure encounter composition for §8 (solo) and §11 (2–4 players). Scaling adds units,
    // never only health. Rows of the §11 table are applied cumulatively (row 3 includes
    // the additions of row 2). This interpretation is a tuning assumption to confirm.
    public static class EncounterComposer
    {
        public const int MinPlayers = 1;
        public const int MaxPlayers = 4;

        public static int ClampPlayers(int playerCount) =>
            Math.Max(MinPlayers, Math.Min(MaxPlayers, playerCount));

        public static IReadOnlyList<UnitSpawn> Compose(FactionId faction, int playerCount)
        {
            int players = ClampPlayers(playerCount);
            var result = new List<UnitSpawn>();
            switch (faction)
            {
                case FactionId.MajelisDaun:
                    result.Add(new UnitSpawn(UnitRole.Pemimpin, "Ketua Majelis", 1));
                    result.Add(new UnitSpawn(UnitRole.Senior, "Anggota Senior",
                        2 + (players >= 2 ? 1 : 0) + (players >= 4 ? 1 : 0)));
                    result.Add(new UnitSpawn(UnitRole.Kroni, "Staf Fraksi",
                        3 + (players >= 2 ? 2 : 0) + (players >= 3 ? 2 : 0) + (players >= 4 ? 3 : 0)));
                    result.Add(new UnitSpawn(UnitRole.Guard, "Pengawal Sidang", 1 + (players >= 3 ? 1 : 0)));
                    break;
                case FactionId.BiroProsedur:
                    result.Add(new UnitSpawn(UnitRole.Pemimpin, "Kepala Biro", 1));
                    result.Add(new UnitSpawn(UnitRole.Spesialis, "Pengawas", 1 + (players >= 3 ? 1 : 0)));
                    result.Add(new UnitSpawn(UnitRole.Kroni, "Petugas Arsip", ArsipMaxAlive(players), SpawnTrigger.Respawn));
                    result.Add(new UnitSpawn(UnitRole.Guard, "Security", 1 + (players >= 2 ? 1 : 0)));
                    break;
                case FactionId.GardaTakhta:
                    result.Add(new UnitSpawn(UnitRole.Pemimpin, "Panglima Takhta", 1));
                    result.Add(new UnitSpawn(UnitRole.Guard, "Pengawal Takhta",
                        2 + (players >= 2 ? 1 : 0) + (players >= 3 ? 1 : 0) + (players >= 4 ? 2 : 0)));
                    int kroniPerWave = CampaignTuning.Garda.ReinforcementKroni + (players >= 3 ? 2 : 0);
                    for (int wave = 1; wave <= ReinforcementWaves(FactionId.GardaTakhta, players); wave++)
                        result.Add(new UnitSpawn(UnitRole.Kroni, "Kroni Bantuan", kroniPerWave, SpawnTrigger.Reinforcement, wave));
                    break;
                default:
                    // Deferred factions (§15) have no roster yet.
                    break;
            }
            return result.AsReadOnly();
        }

        public static int TotalUnits(IReadOnlyList<UnitSpawn> spawns)
        {
            int total = 0;
            foreach (var spawn in spawns) total += spawn.Count;
            return total;
        }

        // Biro objective stations: three, four at full co-op (§11).
        public static int LoketCount(int playerCount) =>
            ClampPlayers(playerCount) >= 4 ? CampaignTuning.Biro.LoketCount + 1 : CampaignTuning.Biro.LoketCount;

        public static int ArsipMaxAlive(int playerCount) =>
            CampaignTuning.Biro.ArsipMaxAlive + (ClampPlayers(playerCount) >= 2 ? 1 : 0);

        public static int ReinforcementWaves(FactionId faction, int playerCount) =>
            faction == FactionId.GardaTakhta ? (ClampPlayers(playerCount) >= 4 ? 2 : 1) : 0;
    }
}
