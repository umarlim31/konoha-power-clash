using System;
using UnityEngine;

namespace Konoha.Campaign
{
    // World positions of the preview slice objectives (read once from the generated scene).
    public readonly struct CampaignObjectiveLayout
    {
        public Vector3 Plaza { get; }
        public Vector3 Majelis { get; }
        public Vector3 Biro { get; }
        public Vector3 Garda { get; }
        public Vector3 Chair { get; }

        public CampaignObjectiveLayout(Vector3 plaza, Vector3 majelis, Vector3 biro, Vector3 garda, Vector3 chair)
        {
            Plaza = plaza;
            Majelis = majelis;
            Biro = biro;
            Garda = garda;
            Chair = chair;
        }
    }

    // Objective rules of the 0.0.8.x solo slice: Majelis hold, Biro three confirmations,
    // inner gate, guard, seat, Power and the single counterattack. Plain C# (no
    // MonoBehaviour); the caller supplies player position, input and clock. It is the
    // only writer of CampaignRunState in the preview. Values come from
    // CampaignTuning.PreviewSlice so the slice plays exactly like 0.0.8.2.
    public sealed class CampaignObjectiveDirector
    {
        private readonly CampaignObjectiveLayout layout;
        private float powerClock;
        private float counterattackClock;
        private bool enteredMajelis;

        public CampaignRunState Run { get; private set; }
        public float MajelisHold { get; private set; }
        public int BiroSteps { get; private set; }
        public float NextBiroStep { get; private set; }
        public bool CounterattackStarted { get; private set; }
        public bool GuardActive { get; private set; }
        public int GuardHealth { get; private set; }
        public int GuardMaxHealth => CampaignTuning.PreviewSlice.GuardHealth;

        // Raised with a player-facing message.
        public event Action<string> Notified;
        // Raised with the guard's starting health; the caller places and shows the guard at Layout.Garda.
        public event Action<int> GuardSpawned;
        // Raised when the guard leaves play (defeated or the run was won).
        public event Action GuardRemoved;

        public CampaignObjectiveLayout Layout => layout;

        public CampaignObjectiveDirector(CampaignObjectiveLayout layout)
        {
            this.layout = layout;
            Run = NewRun();
        }

        public void Restart()
        {
            Run = NewRun();
            BiroSteps = 0;
            MajelisHold = 0f;
            enteredMajelis = false;
            NextBiroStep = 0f;
            powerClock = 0f;
            counterattackClock = 0f;
            CounterattackStarted = false;
            GuardActive = false;
        }

        public void Tick(Vector3 player, float deltaTime)
        {
            if (Run.Phase == CampaignPhase.GerbangRakyat &&
                Near(player, layout.Plaza, CampaignTuning.PreviewSlice.PlazaRadius))
                Run.ReachPlaza();

            if (Run.Phase == CampaignPhase.PlazaAspirasi)
            {
                // Majelis: listen to the people; Biro: confirm three separate files.
                if (!Run.HasSeal(CampaignSector.MajelisDaun))
                {
                    bool insideMajelis = Near(player, layout.Majelis, CampaignTuning.PreviewSlice.SectorRadius);
                    if (insideMajelis && !enteredMajelis)
                    {
                        Run.BeginSector(CampaignSector.MajelisDaun);
                        Notify("Tetap di lingkaran Majelis sampai bar penuh");
                    }
                    enteredMajelis = insideMajelis;
                    MajelisHold = insideMajelis
                        ? Mathf.Min(CampaignTuning.PreviewSlice.MajelisHoldSeconds, MajelisHold + deltaTime)
                        : Mathf.Max(0f, MajelisHold - deltaTime * CampaignTuning.PreviewSlice.MajelisDecayRate);
                    if (MajelisHold >= CampaignTuning.PreviewSlice.MajelisHoldSeconds &&
                        Run.AwardSeal(CampaignSector.MajelisDaun))
                        Notify("Segel Majelis Daun diperoleh");
                }
                Run.TryOpenInnerGate();
            }

            if (Run.Phase == CampaignPhase.GerbangDalam &&
                Near(player, layout.Garda, CampaignTuning.PreviewSlice.GardaTriggerRadius))
            {
                if (Run.TryStartGuard()) SpawnGuard(CampaignTuning.PreviewSlice.GuardHealth);
            }

            if (Run.Phase == CampaignPhase.Memerintah)
            {
                if (!Near(player, layout.Chair, CampaignTuning.PreviewSlice.ChairRadius))
                {
                    Run.LoseSeat();
                    powerClock = 0f;
                }
                else
                {
                    powerClock += deltaTime;
                    while (powerClock >= CampaignTuning.PreviewSlice.PowerTickSeconds && Run.Phase == CampaignPhase.Memerintah)
                    {
                        powerClock -= CampaignTuning.PreviewSlice.PowerTickSeconds;
                        Run.GainPower(CampaignTuning.PreviewSlice.PowerPerTick);
                    }
                    if (!CounterattackStarted)
                    {
                        counterattackClock += deltaTime;
                        if (counterattackClock >= CampaignTuning.PreviewSlice.CounterattackDelaySeconds &&
                            Run.Phase != CampaignPhase.Menang)
                        {
                            CounterattackStarted = true;
                            SpawnGuard(CampaignTuning.PreviewSlice.CounterattackGuardHealth);
                        }
                    }
                }
            }

            if (Run.Phase == CampaignPhase.Menang && GuardActive)
                RemoveGuard();
        }

        public bool IsAtBiro(Vector3 player) =>
            Run.Phase == CampaignPhase.PlazaAspirasi &&
            !Run.HasSeal(CampaignSector.BiroProsedur) &&
            Near(player, layout.Biro, CampaignTuning.PreviewSlice.SectorRadius);

        public bool CanConfirmBiro(Vector3 player, float now) => IsAtBiro(player) && now >= NextBiroStep;

        public bool CanSit(Vector3 player) =>
            Run.Phase == CampaignPhase.KursiTerbuka &&
            Near(player, layout.Chair, CampaignTuning.PreviewSlice.ChairRadius);

        // Contextual action button (SAHKAN / DUDUK). Returns true when something happened.
        public bool Interact(Vector3 player, float now)
        {
            if (CanConfirmBiro(player, now))
            {
                if (BiroSteps == 0) Run.BeginSector(CampaignSector.BiroProsedur);
                NextBiroStep = now + CampaignTuning.PreviewSlice.BiroStepIntervalSeconds;
                BiroSteps++;
                if (BiroSteps >= CampaignTuning.PreviewSlice.BiroSteps && Run.AwardSeal(CampaignSector.BiroProsedur))
                    Notify("Segel Biro Prosedur diperoleh");
                else Notify("Berkas disahkan: " + BiroSteps + "/" + CampaignTuning.PreviewSlice.BiroSteps);
                return true;
            }
            if (CanSit(player) && Run.TrySit())
            {
                Notify("Bertahan di Kursi untuk mengumpulkan Kuasa");
                return true;
            }
            return false;
        }

        // Returns true when the hit landed on an active guard.
        public bool DamageGuard(int damage)
        {
            if (!GuardActive) return false;
            GuardHealth -= damage;
            Notify("Garda terkena " + damage + "  •  sisa " + Mathf.Max(0, GuardHealth) + " HP");
            if (GuardHealth > 0) return true;
            RemoveGuard();
            if (Run.Phase == CampaignPhase.GardaTakhta)
            {
                Run.DefeatGuard();
                Notify("Garda tumbang! Kursi terbuka");
            }
            return true;
        }

        // The hero collapsed. The caller restores health and position (preview: Gerbang Rakyat).
        public void HandleRuntuh()
        {
            Run.LoseSeat();
            Run.RecordRuntuh();
            Notify("Tumbang! Kembali ke Gerbang Rakyat");
            if (Run.Phase == CampaignPhase.GardaTakhta)
                SpawnGuard(CampaignTuning.PreviewSlice.GuardHealth);
        }

        private void SpawnGuard(int health)
        {
            GuardHealth = health;
            GuardActive = true;
            GuardSpawned?.Invoke(health);
        }

        private void RemoveGuard()
        {
            GuardActive = false;
            GuardRemoved?.Invoke();
        }

        private void Notify(string message) => Notified?.Invoke(message);

        private static CampaignRunState NewRun() =>
            new CampaignRunState(CampaignTuning.PreviewSlice.RequiredSeals, CampaignTuning.PreviewSlice.TargetPower);

        public static bool Near(Vector3 a, Vector3 b, float radius)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z <= radius * radius;
        }
    }
}
