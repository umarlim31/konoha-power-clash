namespace Konoha.Campaign
{
    // Single source of Jalur Takhta numbers from Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md §5–§9.
    // All values are starting points for device tuning, not validated balance.
    // Unit role stats (§7) live in UnitRoleStats; player scaling (§11) in EncounterComposer.
    public static class CampaignTuning
    {
        // §5 Resources.
        public static class ResourceRules
        {
            public const int MaxWibawa = 100;
            public const int MaxPengaruh = 100;
            public const int UltimateCost = 100;
            public const int PengaruhBasicHit = 4;
            public const int PengaruhSkillHit = 6;
            public const int PengaruhKroniDown = 5;
            public const int PengaruhEliteDown = 15;
            public const int PengaruhSeal = 25;
        }

        // §3/§5 Access seals. The run state supports up to five sectors ("3 of 5" later).
        public static class Seals
        {
            public const int Required = 2;
            public const int SectorCount = 5;
        }

        // §6 Runtuh in the campaign.
        public static class Runtuh
        {
            public const float RespawnDelaySeconds = 4f;
            public const int RespawnWibawa = ResourceRules.MaxWibawa;
            public const float PengaruhPenaltyFraction = 0.30f; // Pengaruh −30%, rounded down.
            public const float WoundedEnemyRecoveryFraction = 0.50f;
        }

        // §7 Readability shared by all roles.
        public static class Enemy
        {
            public const float GuardLeashRadius = 7f;
            public const float TelegraphSeconds = 1f;
        }

        // §8.1 Majelis Daun — "Pecahkan Blok".
        public static class Majelis
        {
            public const int VotingBlockMinSeniors = 2;
            public const float VotingBlockDamageReduction = 0.40f;
            public const float KetokPaluRadius = 3f;
            public const float KetokPaluTelegraphSeconds = 1f;
            public const int KetokPaluDamage = 22;
            public const float KetokPaluCooldownSeconds = 9f;
            public const float SurrenderVanishSeconds = 3f;
            public const int SealPengaruh = ResourceRules.PengaruhSeal;

            // 0.0.9.2 tuning assumptions (not in §8.1), to confirm on device.
            public const float KetokPaluForwardOffset = 1.8f;   // Circle centre in front of the Ketua.
            public const float KetokPaluTriggerRange = 4f;      // Hero this close starts the telegraph.
            public const float KetokPaluFirstDelaySeconds = 3f; // No hammer in the first seconds of a fight.
            public const float SectorEngageRadius = 9f;         // Entering this ring starts the sidang.
            public const float LeashRadius = 11f;               // Members never chase beyond this around the hall.
        }

        // §8.2 Biro Prosedur — "Sahkan Berkas".
        public static class Biro
        {
            public const int LoketCount = 3;
            public const float LoketRadius = 2f;
            public const float LoketFillSeconds = 3f;
            public const float LoketDecayPerSecond = 0.20f; // Fraction of a full stamp lost per second outside.
            public const float StempelTundaRadius = 8f;
            public const float StempelTundaCooldownMultiplier = 1.30f;
            public const float ArsipRespawnSeconds = 12f;
            public const int ArsipMaxAlive = 2;
            public const float SalahLoketTelegraphSeconds = 1f;
            public const float SalahLoketCooldownSeconds = 12f;
            public const int SealPengaruh = ResourceRules.PengaruhSeal;

            // 0.0.9.3 tuning assumptions (not in §8.2), to confirm on device.
            public const float SalahLoketRadius = 2.5f;          // Warning circle on the hero.
            public const float SalahLoketTriggerRange = 7f;      // Kepala Biro casts within this range.
            public const float SalahLoketFirstDelaySeconds = 3f; // After the door opens.
            public const float SectorEngageRadius = 9f;          // Around the hall: the sector begins.
            public const float LeashRadius = 13f;                // Members stay in the loket hall.
            public const float LockedLeaderRadius = 1.8f;        // Kepala Biro ignores heroes outside his office.
        }

        // §8.3 Garda Takhta — "Gerbang Terakhir".
        public static class Garda
        {
            public const float CounterPushIntervalSeconds = 12f;
            public const float CounterPushRadius = 4f;
            public const float CounterPushTelegraphSeconds = 1.2f;
            public const float CounterPushDistance = 5f;
            public const int CounterPushDamage = 15;
            public const float ReinforcementHealthFraction = 0.50f;
            public const int ReinforcementKroni = 2;
        }

        // §9 Fase Memerintah.
        public static class Memerintah
        {
            public const int PowerPerSecond = 2;
            public const int TargetPower = 100;
            public const float CounterattackIntervalSeconds = 15f;
            public const int CounterattackKroni = 2;
            public const int CounterattackGuard = 1;
            public const float SeatedBasicRangeBonus = 1f;
            public const int KudetaRuntuhLimit = 3;
        }

        // 0.0.9 encounters that already use real hero combat (host-authoritative). Role
        // stats come from UnitRoleStats (§7); these are the shared behaviour numbers.
        public static class Encounters
        {
            public const int GateKroni = 3;                  // Gerbang Rakyat tutorial fight.
            public const float GardaEngageRadius = 9f;       // Entering this ring starts the Garda phase.
            public const float EnemyAggroRadius = 10f;
            public const float EnemyAttackReach = 1.8f;
            public const float EnemyAttackIntervalSeconds = 1.6f; // Tuning assumption, not in §7.
            public const float EnemyFirstAttackDelaySeconds = 0.8f;
            public const float EnemyDespawnSeconds = 1.4f;   // Collapsed enemies stay visible briefly.
            public const float CounterattackDelaySeconds = PreviewSlice.CounterattackDelaySeconds;
            // 0.0.9.2.1: basic hits from different enemies on the same hero are spaced by at
            // least this much, so a crowd trades blows in turn instead of all at once.
            // Telegraphed specials (KETOK PALU) are exempt. Tuning assumption.
            public const float TargetHitSpacingSeconds = 0.5f;
        }

        // 0.0.9.2.1 RESTU RAKYAT: reward for clearing the Gerbang Rakyat, lasts for the run
        // (kept after Runtuh, cleared by ULANG). Tuning assumption, not in §5–§9.
        public static class Restu
        {
            public const float HeroDamageMultiplier = 1.30f;   // Hero hits on the Sistem.
            public const float HeroDamageTakenMultiplier = 0.75f;
            public const int PengaruhBonus = 30;
        }

        // Values of the 0.0.8.x solo preview slice. They intentionally differ from the MVP
        // rules above so the current build keeps its exact feel until the real encounters
        // replace this slice (0.0.9+). Do not "correct" them to the MVP numbers here.
        public static class PreviewSlice
        {
            public const int RequiredSeals = Seals.Required;
            public const int TargetPower = 35;
            public const int PowerPerTick = 5;
            public const float PowerTickSeconds = 1f;
            public const float CounterattackDelaySeconds = 2.5f;

            public const float PlazaRadius = 2.4f;
            public const float SectorRadius = 2.8f;
            public const float GardaTriggerRadius = 2.6f;
            public const float ChairRadius = 2.2f;
            public const float WaypointArrivalRadius = 2.3f;


            public const int PlayerHealth = 100;
            public const float StartX = 0f, StartY = 0.1f, StartZ = -9f;
            public const float RuntuhRespawnZ = -8.5f;

            public const int GuardHealth = 100;
            public const int CounterattackGuardHealth = 75;
            public const float GuardSpeed = 2.1f;
            public const float GuardReach = 1.5f;
            public const float GuardLeashRadius = 6f;
            public const float GuardFirstHitDelaySeconds = 1.5f;
            public const float GuardHitIntervalSeconds = 1.3f;
            public const int GuardDamage = 18;
            public const int GuardDamageShielded = 8;
            public const float GuardFlashSeconds = 0.22f;

            public const float BasicRange = 3f;
            public const float BasicCooldownSeconds = 0.55f;
            public const int BasicDamage = 25;

            public const float MegaShieldSeconds = 5f;
            public const float MegaSkillRange = 3.5f;
            public const int MegaSkillDamage = 30;
            public const float MegaCooldownSeconds = 11f;
            public const float GemoySkillRange = 4.5f;
            public const int GemoySkillDamage = 50;
            public const float GemoyCooldownSeconds = 9f;
            public const int AbahHeal = 40;
            public const float AbahStunRange = 4f;
            public const float AbahStunSeconds = 2.5f;
            public const float AbahCooldownSeconds = 11f;
            public const float PakWiSpeedSeconds = 5f;
            public const float PakWiSpeedMultiplier = 1.55f;
            public const float PakWiCooldownSeconds = 9f;

            public const float FeedbackSeconds = 3.2f;
            public const float MaxFrameSeconds = 0.05f;
        }
    }
}
