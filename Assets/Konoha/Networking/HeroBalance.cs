namespace Konoha.Networking
{
    // 0.2.4: hero numbers in one place, with two profiles.
    //  - PvP (Rebut Kursi): the original prototype values, unchanged.
    //  - Solo (Jalur Takhta, ICombatRules.UsesSoloHeroKit): in solo there are no allies and
    //    the enemies never cast skills, so kit parts that only help teammates (road speed for
    //    allies, BARIS! shields behind, silence, PvP proyek walls) did nothing. The solo profile
    //    turns those into things that work alone, so all four heroes clear a sector with a
    //    similar amount of pressure (see EstimatedPressure and HeroBalanceTests).
    // Pure data and arithmetic; NetworkHeroKit reads it.
    public static class HeroBalance
    {
        // --- Basic attack -------------------------------------------------------------

        public static int BasicDamage(PrototypeHero hero, bool solo)
        {
            switch (hero)
            {
                case PrototypeHero.Prabowo: return 24;
                case PrototypeHero.Abah: return 17;
                // PAK WI was the weakest melee (15 every 0.68 s); solo 19.
                case PrototypeHero.Jokowi: return solo ? 19 : 15;
                default: return 18;
            }
        }

        public static float BasicRange(PrototypeHero hero)
        {
            switch (hero)
            {
                case PrototypeHero.Abah: return 6.8f;
                case PrototypeHero.Prabowo: return 2.8f;
                default: return 2.7f;
            }
        }

        public static float BasicCooldown(PrototypeHero hero, bool garuda)
        {
            switch (hero)
            {
                case PrototypeHero.Prabowo: return garuda ? 0.62f : 0.88f;
                case PrototypeHero.Abah: return 0.72f;
                case PrototypeHero.Jokowi: return 0.68f;
                default: return 0.64f;
            }
        }

        // --- Solo-only kit changes ----------------------------------------------------------

        // PAK WI S1 INFRASTRUKTUR: while PAK WI stands on his own road (radius 5 m).
        public const float SoloRoadDamageMultiplier = 1.25f;
        public const float SoloRoadDamageTakenMultiplier = 0.80f;
        // PAK WI S2 BLUSUKAN: hits enemies along the dash and grants a small shield.
        public const int SoloBlusukanDamage = 18;
        public const float SoloBlusukanWidth = 1.5f;
        public const int SoloBlusukanShield = 15;
        // PAK WI ULT PROYEK: in solo a ground-breaking blast around PAK WI instead of the
        // fixed PvP walls (which stood in the plaza and blocked the route).
        public const float SoloProyekRadius = 7f;
        public const int SoloProyekDamage = 30;
        public const float SoloProyekKnockback = 3f;
        public const int SoloProyekShield = 25;
        // MEGA S2 PERISAI RAKYAT: the Kader also shield MEGA herself (solo has no allies to cover).
        public const int SoloKaderShield = 15;
        // GEMOY S2 BARIS!: harder push and the shield goes to GEMOY himself (no allies).
        public const int SoloBarisDamage = 16;
        public const int SoloBarisSelfShield = 10;
        // ABAH ULT PIDATO: silence is useless against the Sistem; more damage and a push.
        public const int SoloPidatoDamage = 30;
        public const float SoloPidatoKnockback = 3f;

        public const int PvpBarisDamage = 10;
        public const int PvpPidatoDamage = 18;

        // --- Balance estimate ------------------------------------------------------------

        // Rough damage one hero puts out in 30 s of fighting a group of three Sistem
        // members: 70% basic uptime (melee) or 85% (ABAH's 6.8 m reach keeps him hitting
        // while he kites), plus each skill as often as its cooldown allows, times how many
        // of the three it usually catches, plus one ultimate per two minutes. Shields count
        // as damage prevented. Used by the tests to keep the solo heroes within ±15%.
        public static float EstimatedPressure(PrototypeHero hero, bool solo)
        {
            const float window = 30f;
            float uptime = hero == PrototypeHero.Abah ? .85f : .70f;
            float basic = BasicDamage(hero, solo) / BasicCooldown(hero, false) * window * uptime;
            float skills, ultimate;
            switch (hero)
            {
                case PrototypeHero.Mega:
                    // SERUAN IBU 20 on ~2 targets + stun; PERISAI RAKYAT blocks ~1 hit (20) per cast.
                    skills = window / 10f * 20f * 2f + window / 14f * (20f + (solo ? SoloKaderShield : 0f));
                    ultimate = 30f * 3f;
                    break;
                case PrototypeHero.Prabowo:
                    // CMD LEAP 22 on ~2.5 targets; BARIS! on ~2 targets (+ self shield in solo).
                    skills = window / 9f * 22f * 2.5f +
                             window / 12f * ((solo ? SoloBarisDamage : PvpBarisDamage) * 2f + (solo ? SoloBarisSelfShield : 0f));
                    // GARUDA: 8 s of +30% damage and faster basics.
                    ultimate = 8f * (24f * 1.3f / .62f - 24f / .88f) * uptime + 8f * 10f * .28f;
                    break;
                case PrototypeHero.Abah:
                    // NARASI: 6 ticks of 4 on ~2.5 targets; ELECTRIC DASH is an escape (no damage).
                    skills = window / 11f * 6f * 4f * 2.5f;
                    ultimate = (solo ? SoloPidatoDamage : PvpPidatoDamage) * 3f;
                    break;
                default:
                    // INFRASTRUKTUR (solo): +25% basic damage and -20% damage taken on the road,
                    // on the road about half of the fight (the Sistem hits ~10/s, landing ~30%).
                    // BLUSUKAN (solo) hits ~1.5 targets + shield.
                    skills = solo
                        ? basic * (SoloRoadDamageMultiplier - 1f) * .5f +
                          10f * (1f - SoloRoadDamageTakenMultiplier) * window * .5f * .3f +
                          window / 10f * (SoloBlusukanDamage * 1.5f + SoloBlusukanShield)
                        : 0f;
                    ultimate = solo ? SoloProyekDamage * 3f + SoloProyekShield : 0f;
                    break;
            }
            // One ultimate per two minutes -> a quarter of one per 30 s window.
            return basic + skills + ultimate * .25f;
        }
    }
}
