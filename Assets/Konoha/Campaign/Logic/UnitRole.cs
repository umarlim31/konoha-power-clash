using System;

namespace Konoha.Campaign
{
    // Enemy organisation roles, Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md §7.
    public enum UnitRole
    {
        Kroni,
        Guard,
        Spesialis,
        Senior,
        Pemimpin
    }

    public readonly struct UnitRoleStats
    {
        // No leash: the unit chases (or holds position) without a guard radius.
        public const float NoGuardRadius = 0f;

        public UnitRole Role { get; }
        public int Wibawa { get; }
        public int DamagePerHit { get; }
        public float Speed { get; }
        public float GuardRadius { get; }
        public bool Chases { get; }

        public bool HasGuardRadius => GuardRadius > NoGuardRadius;
        // Elite and boss units show a Wibawa bar (§7).
        public bool IsElite => Role == UnitRole.Senior || Role == UnitRole.Pemimpin;
        // Only leaders own a telegraphed special attack in the MVP (§7).
        public bool HasTelegraphedSpecial => Role == UnitRole.Pemimpin;

        private UnitRoleStats(UnitRole role, int wibawa, int damage, float speed, float guardRadius, bool chases)
        {
            Role = role;
            Wibawa = wibawa;
            DamagePerHit = damage;
            Speed = speed;
            GuardRadius = guardRadius;
            Chases = chases;
        }

        public static UnitRoleStats For(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Kroni: return new UnitRoleStats(role, 40, 6, 3.2f, NoGuardRadius, true);
                case UnitRole.Guard: return new UnitRoleStats(role, 90, 10, 2.8f, CampaignTuning.Enemy.GuardLeashRadius, true);
                case UnitRole.Spesialis: return new UnitRoleStats(role, 70, 5, 3.0f, NoGuardRadius, false);
                case UnitRole.Senior: return new UnitRoleStats(role, 160, 12, 2.8f, NoGuardRadius, true);
                case UnitRole.Pemimpin: return new UnitRoleStats(role, 320, 16, 2.6f, NoGuardRadius, true);
                default: throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }
    }
}
