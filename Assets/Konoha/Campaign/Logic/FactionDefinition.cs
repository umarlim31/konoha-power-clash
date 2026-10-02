using System;
using System.Collections.Generic;

namespace Konoha.Campaign
{
    // Fictional Negara Konoha institutions. Only the first three are in the MVP (§8, §15).
    public enum FactionId
    {
        MajelisDaun,
        BiroProsedur,
        GardaTakhta,
        KomisiSuara,
        KonsorsiumModal,
        MenaraNarasi
    }

    // Engine-independent colour so the logic layer stays plain C#.
    public readonly struct FactionColor
    {
        public float R { get; }
        public float G { get; }
        public float B { get; }

        public FactionColor(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
        }
    }

    public sealed class FactionDefinition
    {
        private static readonly UnitRole[] NoRoles = new UnitRole[0];
        private static readonly FactionDefinition[] All =
        {
            new FactionDefinition(FactionId.MajelisDaun, "MARKAS KOALISI", true,
                new FactionColor(0.45f, 0.09f, 0.16f), new FactionColor(0.86f, 0.66f, 0.28f),
                new[] { UnitRole.Pemimpin, UnitRole.Senior, UnitRole.Kroni, UnitRole.Guard }),
            new FactionDefinition(FactionId.BiroProsedur, "KANTOR KELURAHAN", true,
                new FactionColor(0.72f, 0.68f, 0.60f), new FactionColor(0.20f, 0.36f, 0.58f),
                new[] { UnitRole.Pemimpin, UnitRole.Spesialis, UnitRole.Kroni, UnitRole.Guard }),
            new FactionDefinition(FactionId.GardaTakhta, "GARDA ISTANA", true,
                new FactionColor(0.09f, 0.13f, 0.24f), new FactionColor(0.86f, 0.66f, 0.28f),
                new[] { UnitRole.Pemimpin, UnitRole.Guard, UnitRole.Kroni }),
            // Deferred factions (§15): identity only, no roster until they are designed.
            new FactionDefinition(FactionId.KomisiSuara, "KOMISI SUARA KONOHA", false,
                new FactionColor(0.55f, 0.55f, 0.58f), new FactionColor(0.90f, 0.90f, 0.90f), NoRoles),
            new FactionDefinition(FactionId.KonsorsiumModal, "KONSORSIUM MODAL", false,
                new FactionColor(0.12f, 0.35f, 0.25f), new FactionColor(0.86f, 0.66f, 0.28f), NoRoles),
            new FactionDefinition(FactionId.MenaraNarasi, "MENARA NARASI", false,
                new FactionColor(0.30f, 0.18f, 0.42f), new FactionColor(0.80f, 0.80f, 0.86f), NoRoles),
        };

        public FactionId Id { get; }
        public string DisplayName { get; }
        public bool IsMvp { get; }
        public FactionColor PrimaryColor { get; }
        public FactionColor AccentColor { get; }
        public IReadOnlyList<UnitRole> BaseRoles { get; }

        private FactionDefinition(FactionId id, string displayName, bool isMvp,
            FactionColor primary, FactionColor accent, UnitRole[] roles)
        {
            Id = id;
            DisplayName = displayName;
            IsMvp = isMvp;
            PrimaryColor = primary;
            AccentColor = accent;
            BaseRoles = Array.AsReadOnly(roles);
        }

        public static FactionDefinition Get(FactionId id)
        {
            foreach (var definition in All)
                if (definition.Id == id) return definition;
            throw new ArgumentOutOfRangeException(nameof(id), id, null);
        }
    }
}
