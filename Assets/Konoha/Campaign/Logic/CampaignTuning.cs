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

            // 0.1.0 tuning assumptions (not in §8.3), to confirm on device.
            public const float LockdownRadius = 8.5f;            // Barrier ring around the Garda post (0, 28).
            public const float LockdownCloseRadius = 7f;         // Hero this far inside closes it.
            public const float CounterPushTriggerRange = 3.5f;   // Panglima pushes heroes this close.
            public const float CounterPushFirstDelaySeconds = 4f;
            public const int MaxCounterattackAlive = 6;          // §9 waves stop stacking beyond this.
        }

        // 0.1.0: Wibawa recovery out of combat (tuning assumption, not in §5–§9). Added after
        // tablet runs with 9–12 Runtuh: without any healing, every fight chipped the next one.
        public static class Recovery
        {
            public const float DelaySeconds = 4f;     // No damage taken for this long...
            public const float WibawaPerSecond = 10f; // ...then Wibawa refills at this rate.
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
            public const float ChairRadius = 2.2f;
            // 0.1.1 (owner decision): sitting on the seat wins the run at once; the §9 Power
            // phase stays in code for later levels. Waves harass the hero until they sit.
            public const bool SeatWinsRun = true;
            public const float FirstWaveSeconds = 6f;
            public const float PowerTickSeconds = 1f / PowerPerSecond; // +1 Power per tick.
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
            // 0.0.9.2.1: basic hits from different enemies on the same hero are spaced by at
            // least this much, so a crowd trades blows in turn instead of all at once.
            // Telegraphed specials (KETOK PALU) are exempt. Tuning assumption.
            public const float TargetHitSpacingSeconds = 0.5f;
        }

        // 0.4.0 Musim Pemilu (Docs/GAME_LOGIC_JALUR_TAKHTA_v2.md §3–§4): political resources
        // and the LAWAN / RANGKUL choice. Starting values, tuned on the tablet.
        public static class Politik
        {
            public const int ModalStart = 60;
            public const int ModalPerDefeat = 2;        // Every Sistem member knocked down.
            public const int ModalGateBonus = 15;       // "Sumbangan relawan" after the Gerbang Rakyat.
            public const int RangkulCostMajelis = 45;
            public const int RangkulCostBiro = 35;
            public const int LoanExtraJatah = 2;        // PINJAM KONSORSIUM: the coalition and the lender both own you.
            public const int RestuStart = 50;
            public const int RestuGate = 10;
            public const int RestuLawan = 15;
            public const int RestuRangkul = -5;
            public const int RestuLoan = -10;
            public const int BonekaJatah = 3;           // From here on the ruler is someone else's puppet.
            public const float OfferRadius = 19f;       // Around a hall: the LAWAN / RANGKUL panel appears.
            public const float PanglimaWeakenPerRangkul = 0.15f; // Each embraced institution "conditions" the Panglima.
        }

        // 0.5.0 Jalan Nyaleg: BLUSUKAN, greet three groups of warga after the gang.
        public static class Blusukan
        {
            public const float ZoneRadius = 2.8f;
            public const float GreetSeconds = 2f;       // Standing in the zone this long greets the group.
            public const int RestuPerGroup = 5;
            public static readonly string[] Groups = { "POS RONDA", "IBU-IBU", "OJOL" };
            public static readonly string[] Lines =
            {
                "BAPAK-BAPAK POS RONDA: \"Jalan kampung bolong 3 tahun!\"  Kamu janji aspal mulus \"setelah terpilih\". SUARA +1",
                "IBU-IBU: \"Harga cabai naik, minyak langka!\"  Kamu ikut ngerumpi dan bagi kaos (MODAL -5). SUARA +1",
                "DRIVER OJOL: \"Potongan aplikasi kegedean!\"  Kamu foto bareng pakai jaket hijau. SUARA +1"
            };
            public static readonly int[] ModalCost = { 0, 5, 0 };
        }

        // 0.6.0 KARIER Level 1 "Warga Biasa" (Docs/VISI_KONOHA_HIDUP_v3.md §3). Money is
        // earned before anything can be bought; dirty money is fast but leaves a record.
        // Starting values, tuned on the tablet.
        public static class Karier
        {
            // Resources.
            public const int DuitStart = 50000;
            public const int EnergiMax = 100;
            public const int RestuStart = 30;
            public const int RestuMax = 100;
            public const int CatatanMax = 100;
            public const int EnergiLemas = 15;          // Below this the hero walks slowly.
            public const float LemasSpeed = 0.75f;

            // Goal of Level 1: register for the Ketua RT election.
            public const int SyukuranRT = 1000000;      // Nasi kotak for the whole RT.
            public const int RestuSyaratRT = 50;
            public const float ZoneRadius = 2.6f;

            // OJOL: pick a passenger up, take them across the city.
            public const int OjolBaseFare = 10000;
            public const int OjolFarePerMeter = 900;
            public const float OjolAppCut = 0.20f;      // "Potongan aplikasi".
            public const float OjolSpeed = 1.6f;        // Riding the motor.
            public const float OjolMinTrip = 15f;
            public const float OjolSecondsPerMeter = 0.25f;
            public const float OjolGraceSeconds = 12f;
            public const float OjolPickupSeconds = 1f;
            public const int OjolEnergi = 6;
            public const int OjolRestuOnTime = 1;

            // KULI BANGUNAN: carry five sacks of cement from the pile to the project.
            public const int KuliSacks = 5;
            public const int KuliWage = 150000;
            public const int KuliMandorCut = 25000;     // "Uang rokok" for the mandor.
            public const float KuliPickSeconds = 1.2f;
            public const float KuliDropSeconds = 0.8f;
            public const float KuliCarrySpeed = 0.85f;
            public const int KuliEnergiPerSack = 5;
            public const int KuliRestu = 2;

            // BUZZER HOAKS: sit at the warkop and spread a hoax in the WA groups.
            public const int BuzzerPay = 250000;
            public const float BuzzerTypeSeconds = 3f;
            public const float BuzzerCooldownSeconds = 30f;
            public const int BuzzerCatatan = 20;
            public const int BuzzerRestu = -4;
            public const int BuzzerEnergi = 2;
            public const int PasalKaretFrom = 50;       // From this Catatan Hitam the police may come.

            // REBAHAN and MAKAN.
            public const float RebahanSeconds = 7f;
            public const int RebahanEnergi = 45;
            public const int MakanPrice = 15000;
            public const int MakanEnergi = 35;

            // SAPA WARGA (once per group per save).
            public const int SapaRestu = 5;

            // Preman memalak warga at the gang (optional fights).
            public const float PremanFirstSeconds = 40f;
            public const float PremanEverySeconds = 85f;
            public const float PremanLeaveSeconds = 70f;  // Ignored long enough, they leave.
            public const int PremanWibawa = 70;
            public const int BosPremanWibawa = 160;
            public const float PremanLeashRadius = 9f;
            public const int PremanRestu = 8;           // Per preman beaten.
            public const int PremanTip = 20000;         // Thanks from the warga.
            public const int PingsanBiaya = 50000;      // Puskesmas after collapsing.
            public const int PingsanEnergi = 30;

            // KERIBUTAN: a fight fills it; warga shout, someone melerai, then the police come.
            public const float FightRadius = 6f;
            public const float KeributanFillSeconds = 9f;
            public const float KeributanDecaySeconds = 6f;
            public const float TeriakAt = 0.12f;
            public const float MeleraiAt = 0.40f;
            public const float MeleraiSeconds = 3f;
            public const float FightEnergiPerSecond = 0.6f;

            // POLISI: choices when they arrive.
            public const int DamaiFight = 100000;
            public const int DamaiFightPerCatatan = 1000;
            public const int DamaiHoaks = 250000;
            public const int DamaiHoaksPerCatatan = 2000;
            public const int DamaiCatatan = 5;
            public const int KaburCatatan = 20;
            public const int KaburRestu = -4;
            public const int PolsekBiaya = 50000;
            public const int PolsekRestu = -6;
            public const int PolsekCatatan = -5;
            public const int PolsekEnergi = -20;
            public const int ArrestPolsekCatatan = 0;   // Hoaks: the record stays.
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

            public const float PlazaRadius = 2.4f;
            // 0.1.1: walking this far north of the plaza point also counts as reaching it
            // (a hero taking the garden path to Majelis never touched the 2.4 m ring).
            public const float PlazaEntryDepth = 15f;
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
