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

    // Objective rules of the solo slice: Gerbang Rakyat fight, Majelis Daun sidang, Biro
    // three confirmations, inner gate, Garda Takhta, seat, Power and the single counterattack.
    // Plain C# (no MonoBehaviour); the caller supplies player position, input, clock and
    // encounter outcomes. It is the only writer of CampaignRunState. The fights use real
    // enemies: this class only announces when an encounter must be prepared (events) and
    // is told when it was cleared (MarkGateCleared, CompleteMajelis, CompleteBiro,
    // CompleteGarda). Majelis and Biro may be cleared in either order (§3).
    public sealed class CampaignObjectiveDirector
    {
        private readonly CampaignObjectiveLayout layout;
        private float powerClock;
        private float counterattackClock;

        public CampaignRunState Run { get; private set; }
        public bool GateCleared { get; private set; }
        public bool GardaPrepared { get; private set; }
        public bool MajelisPrepared { get; private set; }
        public bool MajelisEngaged { get; private set; }
        public bool BiroPrepared { get; private set; }
        public bool BiroEngaged { get; private set; }
        // §9 reign: set on the first DUDUK, cleared by KUDETA or a restart.
        public bool ReignStarted { get; private set; }
        public int ReignRuntuh { get; private set; }
        public int CounterattackWaves { get; private set; }
        // True: DUDUK wins the run (0.1.1). False: the §9 Power phase (tests, later levels).
        public bool SeatWinsRun { get; set; } = CampaignTuning.Memerintah.SeatWinsRun;
        private bool wavesArmed;

        // Raised with a player-facing message.
        public event Action<string> Notified;
        // The plaza was reached: place the Majelis Daun sidang in its hall.
        public event Action MajelisRequested;
        // The plaza was reached: open the Biro Prosedur loket hall.
        public event Action BiroRequested;
        // The inner gate opened: place the Garda Takhta defenders.
        public event Action GardaRequested;
        // §9: every 15 s of the reign, send a counterattack wave.
        public event Action CounterattackRequested;
        // The hero sat down: the host places them on the seat.
        public event Action Seated;
        // §9 KUDETA: three Runtuh during the reign; Power is back to zero.
        public event Action Kudeta;
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
            MajelisPrepared = false;
            MajelisEngaged = false;
            BiroPrepared = false;
            BiroEngaged = false;
            powerClock = 0f;
            counterattackClock = 0f;
            ReignStarted = false;
            ReignRuntuh = 0;
            CounterattackWaves = 0;
            wavesArmed = false;
        }

        // The Gerbang Rakyat defenders are down; the plaza may now be reached.
        public void MarkGateCleared()
        {
            if (GateCleared) return;
            GateCleared = true;
            Notify("Gerbang Rakyat terbuka! Menuju PLAZA ASPIRASI");
        }

        // Ketua down and no officer standing (MajelisEncounter.Cleared). Awards the seal.
        public bool CompleteMajelis()
        {
            if (Run.Phase != CampaignPhase.PlazaAspirasi || Run.HasSeal(CampaignSector.MajelisDaun))
                return false;
            if (!MajelisEngaged)
            {
                MajelisEngaged = true;
                Run.BeginSector(CampaignSector.MajelisDaun);
            }
            if (!Run.AwardSeal(CampaignSector.MajelisDaun)) return false;
            Notify("SEGEL MAJELIS diperoleh!  Pengaruh +" + CampaignTuning.Majelis.SealPengaruh);
            if (Run.TryOpenInnerGate())
                Notify("Gerbang Dalam terbuka! Hadapi GARDA TAKHTA");
            return true;
        }

        // Every loket stamped and the Kepala Biro down (BiroEncounter.Cleared). Awards the seal.
        public bool CompleteBiro()
        {
            if (Run.Phase != CampaignPhase.PlazaAspirasi || Run.HasSeal(CampaignSector.BiroProsedur))
                return false;
            if (!BiroEngaged)
            {
                BiroEngaged = true;
                Run.BeginSector(CampaignSector.BiroProsedur);
            }
            if (!Run.AwardSeal(CampaignSector.BiroProsedur)) return false;
            Notify("SEGEL BIRO diperoleh!  Pengaruh +" + CampaignTuning.Biro.SealPengaruh);
            if (Run.TryOpenInnerGate())
                Notify("Gerbang Dalam terbuka! Hadapi GARDA TAKHTA");
            return true;
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
                (Near(player, layout.Plaza, CampaignTuning.PreviewSlice.PlazaRadius) ||
                 player.z > layout.Plaza.z - CampaignTuning.PreviewSlice.PlazaEntryDepth))
                Run.ReachPlaza();

            if (Run.Phase == CampaignPhase.PlazaAspirasi)
            {
                // Majelis: the sidang waits in its hall from the moment the plaza is reached.
                if (!MajelisPrepared)
                {
                    MajelisPrepared = true;
                    MajelisRequested?.Invoke();
                }
                if (!MajelisEngaged && !Run.HasSeal(CampaignSector.MajelisDaun) &&
                    Near(player, layout.Majelis, CampaignTuning.Majelis.SectorEngageRadius))
                {
                    MajelisEngaged = true;
                    Run.BeginSector(CampaignSector.MajelisDaun);
                    Notify("SIDANG MAJELIS!  Blok Majelis menahan serangan: incar ANGGOTA SENIOR dulu");
                }
                // Biro: the loket hall opens together with the plaza.
                if (!BiroPrepared)
                {
                    BiroPrepared = true;
                    BiroRequested?.Invoke();
                }
                if (!BiroEngaged && !Run.HasSeal(CampaignSector.BiroProsedur) &&
                    Near(player, layout.Biro, CampaignTuning.Biro.SectorEngageRadius))
                {
                    BiroEngaged = true;
                    Run.BeginSector(CampaignSector.BiroProsedur);
                    Notify("BIRO PROSEDUR!  Cap 3 LOKET, usir petugas dari antrian, awas PENGAWAS");
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

            // §9 Fase Memerintah: Power +2/s while seated; leaving the seat stops it (no loss).
            if (Run.Phase == CampaignPhase.Memerintah)
            {
                if (!Near(player, layout.Chair, CampaignTuning.Memerintah.ChairRadius))
                {
                    Run.LoseSeat();
                    powerClock = 0f;
                    Notify("Terdorong dari Kursi! Kuasa berhenti, DUDUK lagi");
                }
                else
                {
                    powerClock += deltaTime;
                    while (powerClock >= CampaignTuning.Memerintah.PowerTickSeconds && Run.Phase == CampaignPhase.Memerintah)
                    {
                        powerClock -= CampaignTuning.Memerintah.PowerTickSeconds;
                        Run.GainPower(1);
                        if (Run.Phase == CampaignPhase.Menang)
                        {
                            Notify("TAKHTA DIKUASAI!");
                            Won?.Invoke();
                        }
                    }
                }
            }

            // Counterattack waves. SeatWinsRun: from KURSI TERBUKA until the hero sits (first
            // wave after 6 s). Otherwise (§9): during the whole reign, seated or not.
            bool wavesActive = SeatWinsRun
                ? Run.Phase == CampaignPhase.KursiTerbuka
                : ReignStarted && (Run.Phase == CampaignPhase.Memerintah || Run.Phase == CampaignPhase.KursiTerbuka);
            if (wavesActive && SeatWinsRun && !wavesArmed)
            {
                wavesArmed = true;
                counterattackClock = CampaignTuning.Memerintah.CounterattackIntervalSeconds -
                    CampaignTuning.Memerintah.FirstWaveSeconds;
            }
            if (wavesActive)
            {
                counterattackClock += deltaTime;
                if (counterattackClock >= CampaignTuning.Memerintah.CounterattackIntervalSeconds)
                {
                    counterattackClock -= CampaignTuning.Memerintah.CounterattackIntervalSeconds;
                    CounterattackWaves++;
                    Notify(SeatWinsRun
                        ? "SERANGAN BALIK " + CounterattackWaves + "!  Cepat DUDUK di Kursi"
                        : "SERANGAN BALIK " + CounterattackWaves + "!  Sisa kekuatan lama menyerbu Kursi");
                    CounterattackRequested?.Invoke();
                }
            }
        }

        public bool CanSit(Vector3 player) =>
            Run.Phase == CampaignPhase.KursiTerbuka &&
            Near(player, layout.Chair, CampaignTuning.Memerintah.ChairRadius);

        public bool CanStand(Vector3 player) =>
            Run.Phase == CampaignPhase.Memerintah &&
            Near(player, layout.Chair, CampaignTuning.Memerintah.ChairRadius + 1f);

        // Contextual action button (DUDUK). Returns true when something happened. The Biro
        // lokets are zones since 0.0.9.3, so there is no SAHKAN any more.
        public bool Interact(Vector3 player, float now)
        {
            if (CanSit(player) && Run.TrySit())
            {
                if (SeatWinsRun)
                {
                    Run.GainPower(Run.TargetPower);
                    Seated?.Invoke();
                    Notify("TAKHTA DIKUASAI!  Kamu duduk di Kursi Kekuasaan");
                    Won?.Invoke();
                    return true;
                }
                powerClock = 0f;
                if (!ReignStarted)
                {
                    ReignStarted = true;
                    ReignRuntuh = 0;
                    counterattackClock = 0f;
                    Notify("MEMERINTAH!  Kuasa +2/detik. Bertahan sampai 100");
                }
                else
                {
                    Notify("Kembali ke Kursi. Kuasa berjalan lagi");
                }
                Seated?.Invoke();
                return true;
            }
            if (CanStand(player))
            {
                Run.LoseSeat();
                powerClock = 0f;
                Notify("Berdiri dari Kursi. Kuasa berhenti");
                return true;
            }
            return false;
        }

        // The hero collapsed. The caller revives it at the current checkpoint.
        public void HandleRuntuh()
        {
            Run.LoseSeat();
            Run.RecordRuntuh();
            if (!ReignStarted)
            {
                Notify("Tumbang! Bangkit di checkpoint terakhir");
                return;
            }

            ReignRuntuh++;
            if (ReignRuntuh < CampaignTuning.Memerintah.KudetaRuntuhLimit)
            {
                Notify("Tumbang! Kursi lepas (" + ReignRuntuh + "/" + CampaignTuning.Memerintah.KudetaRuntuhLimit +
                    " menuju KUDETA)");
                return;
            }

            // KUDETA: the reign failed. Back to checkpoint 5 with the counts reset.
            Run.ResetPower();
            ReignStarted = false;
            ReignRuntuh = 0;
            CounterattackWaves = 0;
            counterattackClock = 0f;
            powerClock = 0f;
            Notify("KUDETA!  Kekuasaan direbut. Kuasa kembali 0, rebut Kursi lagi");
            Kudeta?.Invoke();
        }

        private void Notify(string message) => Notified?.Invoke(message);

        private static CampaignRunState NewRun() =>
            new CampaignRunState(CampaignTuning.Seals.Required, CampaignTuning.Memerintah.TargetPower);

        public static bool Near(Vector3 a, Vector3 b, float radius)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z <= radius * radius;
        }
    }
}
