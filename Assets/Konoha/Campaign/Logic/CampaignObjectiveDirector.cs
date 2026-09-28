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

    // Objective rules of the solo slice: Gerbang Rakyat fight, Majelis hold, Biro three
    // confirmations, inner gate, Garda Takhta, seat, Power and the single counterattack.
    // Plain C# (no MonoBehaviour); the caller supplies player position, input, clock and
    // encounter outcomes. It is the only writer of CampaignRunState. Since 0.0.9 the
    // fights use real enemies: this class only announces when an encounter must be
    // prepared (events) and is told when it was cleared (MarkGateCleared, CompleteGarda).
    // Majelis and Biro keep their 0.0.8.x placeholder mechanics until 0.0.9.2/0.0.9.3.
    public sealed class CampaignObjectiveDirector
    {
        private readonly CampaignObjectiveLayout layout;
        private float powerClock;
        private float counterattackClock;
        private bool enteredMajelis;

        public CampaignRunState Run { get; private set; }
        public bool GateCleared { get; private set; }
        public bool GardaPrepared { get; private set; }
        public float MajelisHold { get; private set; }
        public int BiroSteps { get; private set; }
        public float NextBiroStep { get; private set; }
        public bool CounterattackLaunched { get; private set; }

        // Raised with a player-facing message.
        public event Action<string> Notified;
        // The inner gate opened: place the Garda Takhta defenders.
        public event Action GardaRequested;
        // The ruler has held the seat long enough: send the counterattack.
        public event Action CounterattackRequested;
        // Power reached its target.
        public event Action Won;

        public CampaignObjectiveLayout Layout => layout;

        public CampaignObjectiveDirector(CampaignObjectiveLayout layout)
        {
            this.layout = layout;
            Run = NewRun();
        }

        public void Restart()
        {
            Run = NewRun();
            GateCleared = false;
            GardaPrepared = false;
            BiroSteps = 0;
            MajelisHold = 0f;
            enteredMajelis = false;
            NextBiroStep = 0f;
            powerClock = 0f;
            counterattackClock = 0f;
            CounterattackLaunched = false;
        }

        // The Gerbang Rakyat defenders are down; the plaza may now be reached.
        public void MarkGateCleared()
        {
            if (GateCleared) return;
            GateCleared = true;
            Notify("Gerbang Rakyat terbuka! Menuju PLAZA ASPIRASI");
        }

        // Every Garda Takhta defender is down.
        public bool CompleteGarda()
        {
            if (Run.Phase == CampaignPhase.GerbangDalam)
                Run.TryStartGuard();
            if (!Run.DefeatGuard()) return false;
            Notify("Garda tumbang! Kursi terbuka");
            return true;
        }

        public void Tick(Vector3 player, float deltaTime)
        {
            if (Run.Phase == CampaignPhase.GerbangRakyat && GateCleared &&
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
                if (Run.TryOpenInnerGate())
                    Notify("Gerbang Dalam terbuka! Hadapi GARDA TAKHTA");
            }

            if (Run.Phase == CampaignPhase.GerbangDalam && !GardaPrepared)
            {
                GardaPrepared = true;
                GardaRequested?.Invoke();
            }

            if (Run.Phase == CampaignPhase.GerbangDalam &&
                Near(player, layout.Garda, CampaignTuning.Encounters.GardaEngageRadius))
                Run.TryStartGuard();

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
                        if (Run.Phase == CampaignPhase.Menang)
                        {
                            Notify("TAKHTA DIKUASAI!");
                            Won?.Invoke();
                        }
                    }
                    if (!CounterattackLaunched && Run.Phase == CampaignPhase.Memerintah)
                    {
                        counterattackClock += deltaTime;
                        if (counterattackClock >= CampaignTuning.Encounters.CounterattackDelaySeconds)
                        {
                            CounterattackLaunched = true;
                            Notify("Serangan balik! Pertahankan Kursi");
                            CounterattackRequested?.Invoke();
                        }
                    }
                }
            }
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

        // The hero collapsed. The caller revives it at the current checkpoint.
        public void HandleRuntuh()
        {
            Run.LoseSeat();
            Run.RecordRuntuh();
            Notify("Tumbang! Bangkit di checkpoint terakhir");
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
