using Konoha.Networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // Jalur Takhta HUD bridge (every peer). Reads the replicated CampaignDirector state and
    // the local hero; sends the contextual SAHKAN / DUDUK / ULANG action to the host.
    // Combat, hero abilities and enemies are the shared networked systems since 0.0.9;
    // the old prototype guard and single-button skills are gone.
    public sealed class CampaignPreviewController : MonoBehaviour
    {
        public CampaignStage stage;
        public GameObject chairBarrier;
        public Text objectiveText;
        public Text statusText;
        public Text waypointText;
        public Text heroText;
        public Text feedbackText;
        public Image objectiveProgress;
        public Button sitButton;

        private Text sitLabel;
        private float nextWaypointRefresh;

        private void Start()
        {
            if (sitButton != null)
            {
                sitButton.onClick.AddListener(Interact);
                sitLabel = sitButton.GetComponentInChildren<Text>(true);
                sitButton.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (sitButton != null)
                sitButton.onClick.RemoveListener(Interact);
        }

        private void Interact()
        {
            CampaignDirector director = CampaignDirector.Instance;
            if (director != null)
                director.RequestInteract();
        }

        private void Update()
        {
            CampaignDirector director = CampaignDirector.Instance;
            if (director == null || !director.IsSpawned || stage == null)
            {
                if (objectiveText != null) objectiveText.text = "MENYIAPKAN JALUR TAKHTA...";
                if (sitButton != null) sitButton.gameObject.SetActive(false);
                return;
            }

            if (chairBarrier != null)
                chairBarrier.SetActive(director.Phase < CampaignPhase.KursiTerbuka);

            NetworkObject hero = LocalHero();
            RefreshObjective(director);
            RefreshHero(hero);
            RefreshAction(director, hero);

            if (feedbackText != null)
                feedbackText.text = Time.time < director.LastMessageUntil ? director.LastMessage : string.Empty;

            if (Time.unscaledTime >= nextWaypointRefresh)
            {
                nextWaypointRefresh = Time.unscaledTime + 0.15f;
                RefreshWaypoint(director, hero);
            }
        }

        private static NetworkObject LocalHero()
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null || manager.LocalClient == null)
                return null;
            return manager.LocalClient.PlayerObject;
        }

        private void RefreshObjective(CampaignDirector director)
        {
            int gateTotal = CampaignTuning.Encounters.GateKroni;
            switch (director.Phase)
            {
                case CampaignPhase.GerbangRakyat:
                    objectiveText.text = director.GateCleared
                        ? "GERBANG RAKYAT  •  Menuju PLAZA ASPIRASI"
                        : "GERBANG RAKYAT  •  Kalahkan Kroni " + (gateTotal - director.GateRemaining) + "/" + gateTotal;
                    break;
                case CampaignPhase.PlazaAspirasi:
                    objectiveText.text = !director.HasSeal(CampaignSector.MajelisDaun)
                        ? "1/2  MAJELIS: tahan di lingkaran " + Mathf.CeilToInt(Mathf.Max(0f,
                            CampaignTuning.PreviewSlice.MajelisHoldSeconds - director.MajelisHold)) + " detik"
                        : "2/2  BIRO: masuk lingkaran, tekan SAHKAN " + (director.BiroSteps + 1) + "/" +
                            CampaignTuning.PreviewSlice.BiroSteps;
                    break;
                case CampaignPhase.GerbangDalam:
                    objectiveText.text = "GERBANG DALAM TERBUKA  >  Hadapi GARDA TAKHTA";
                    break;
                case CampaignPhase.GardaTakhta:
                    objectiveText.text = "GARDA TAKHTA  >  Kalahkan pengawal (" + director.GardaRemaining + " tersisa)";
                    break;
                case CampaignPhase.KursiTerbuka:
                    objectiveText.text = "KURSI TERBUKA  >  Dekati dan tekan DUDUK";
                    break;
                case CampaignPhase.Memerintah:
                    objectiveText.text = director.CounterRemaining > 0
                        ? "SERANGAN BALIK  >  Bertahan di dekat Kursi"
                        : "PERTAHANKAN TAKHTA  >  Tetap di dekat Kursi";
                    break;
                default:
                    objectiveText.text = "JALUR TAKHTA SELESAI!  Tekan ULANG untuk bermain lagi";
                    break;
            }

            statusText.text = "SEGEL " + director.SealCount + "/" + director.RequiredSeals +
                "  |  KUASA " + director.Power + "/" + director.TargetPower +
                "  |  RUNTUH " + director.RuntuhCount;

            if (objectiveProgress == null)
                return;

            float fill = 0f;
            switch (director.Phase)
            {
                case CampaignPhase.GerbangRakyat:
                    fill = director.GateCleared ? 1f : (gateTotal - director.GateRemaining) / (float)gateTotal;
                    break;
                case CampaignPhase.PlazaAspirasi:
                    fill = director.HasSeal(CampaignSector.MajelisDaun)
                        ? director.BiroSteps / (float)CampaignTuning.PreviewSlice.BiroSteps
                        : director.MajelisHold / CampaignTuning.PreviewSlice.MajelisHoldSeconds;
                    break;
                case CampaignPhase.GardaTakhta:
                    fill = 1f - director.GardaRemaining / (float)director.GardaTotal;
                    break;
                case CampaignPhase.Memerintah:
                case CampaignPhase.Menang:
                    fill = director.Power / (float)director.TargetPower;
                    break;
            }
            objectiveProgress.fillAmount = Mathf.Clamp01(fill);
        }

        private void RefreshHero(NetworkObject hero)
        {
            if (heroText == null)
                return;

            NetworkPlayerCombat combat = hero != null ? hero.GetComponent<NetworkPlayerCombat>() : null;
            NetworkHeroKit kit = hero != null ? hero.GetComponent<NetworkHeroKit>() : null;
            if (combat == null || kit == null)
            {
                heroText.text = string.Empty;
                return;
            }

            string state = combat.IsKnockedOut ? "RUNTUH - bangkit sebentar lagi"
                : kit.IsStunned ? "STUN"
                : "WIBAWA " + combat.Wibawa + "/" + combat.MaxWibawaValue;
            heroText.text = NetworkHeroKit.GetHeroName(kit.Hero) + "  •  " + state +
                "  •  PENGARUH " + kit.Pengaruh + "%";
        }

        private void RefreshAction(CampaignDirector director, NetworkObject hero)
        {
            if (sitButton == null)
                return;

            Vector3 position = hero != null ? hero.transform.position : Vector3.one * 999f;
            bool atBiro = director.Phase == CampaignPhase.PlazaAspirasi &&
                !director.HasSeal(CampaignSector.BiroProsedur) &&
                CampaignObjectiveDirector.Near(position, stage.biro.position, CampaignTuning.PreviewSlice.SectorRadius);
            bool canSit = director.Phase == CampaignPhase.KursiTerbuka &&
                CampaignObjectiveDirector.Near(position, stage.chair.position, CampaignTuning.PreviewSlice.ChairRadius);
            bool won = director.Phase == CampaignPhase.Menang;

            sitButton.gameObject.SetActive(atBiro || canSit || won);
            if (sitLabel != null)
                sitLabel.text = won ? "ULANG"
                    : atBiro ? "SAHKAN " + (director.BiroSteps + 1) + "/" + CampaignTuning.PreviewSlice.BiroSteps
                    : "DUDUK";
        }

        private void RefreshWaypoint(CampaignDirector director, NetworkObject hero)
        {
            if (waypointText == null)
                return;
            if (hero == null)
            {
                waypointText.text = string.Empty;
                return;
            }

            Vector3 destination;
            string label;
            float arrival = CampaignTuning.PreviewSlice.WaypointArrivalRadius;
            switch (director.Phase)
            {
                case CampaignPhase.GerbangRakyat:
                    if (!director.GateCleared && NearestEnemy(hero.transform.position, out destination))
                        label = "KRONI";
                    else
                    {
                        destination = stage.plaza.position;
                        label = "PLAZA";
                    }
                    break;
                case CampaignPhase.PlazaAspirasi:
                    bool majelisDone = director.HasSeal(CampaignSector.MajelisDaun);
                    destination = majelisDone ? stage.biro.position : stage.majelis.position;
                    label = majelisDone ? "BIRO" : "MAJELIS";
                    arrival = CampaignTuning.PreviewSlice.SectorRadius;
                    break;
                case CampaignPhase.GerbangDalam:
                case CampaignPhase.GardaTakhta:
                    if (!NearestEnemy(hero.transform.position, out destination))
                        destination = stage.garda.position;
                    label = "GARDA";
                    break;
                case CampaignPhase.KursiTerbuka:
                case CampaignPhase.Memerintah:
                    destination = stage.chair.position;
                    label = "KURSI";
                    break;
                default:
                    waypointText.text = string.Empty;
                    return;
            }

            Vector3 delta = destination - hero.transform.position;
            float distance = new Vector2(delta.x, delta.z).magnitude;
            if (distance < arrival)
            {
                waypointText.text = director.Phase == CampaignPhase.PlazaAspirasi
                    ? (label == "MAJELIS" ? "DI MAJELIS: tetap di sini hingga bar penuh" : "DI BIRO: tekan SAHKAN 3 kali")
                    : "DI LOKASI: " + label;
                return;
            }

            // Directions follow the current camera so rotating the view does not invert the guidance.
            Camera view = Camera.main;
            if (view != null)
            {
                Vector3 ahead = view.transform.forward;
                ahead.y = 0f;
                ahead.Normalize();
                Vector3 right = new Vector3(ahead.z, 0f, -ahead.x);
                delta = new Vector3(Vector3.Dot(delta, right), 0f, Vector3.Dot(delta, ahead));
            }

            string direction;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.z) * 1.8f)
                direction = delta.x < 0f ? "KIRI" : "KANAN";
            else if (Mathf.Abs(delta.z) > Mathf.Abs(delta.x) * 1.8f)
                direction = delta.z < 0f ? "BELAKANG" : "DEPAN";
            else
                direction = (delta.z < 0f ? "BELAKANG " : "DEPAN ") + (delta.x < 0f ? "KIRI" : "KANAN");
            waypointText.text = "ARAH " + label + ": " + direction + "  •  " + Mathf.CeilToInt(distance) + " m";
        }

        private static bool NearestEnemy(Vector3 from, out Vector3 position)
        {
            position = Vector3.zero;
            float best = float.MaxValue;
            foreach (CampaignEnemy enemy in FindObjectsByType<CampaignEnemy>(FindObjectsSortMode.None))
            {
                if (enemy == null || !enemy.IsSpawned || enemy.IsDown)
                    continue;
                float distance = (enemy.transform.position - from).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    position = enemy.transform.position;
                }
            }
            return best < float.MaxValue;
        }
    }
}
