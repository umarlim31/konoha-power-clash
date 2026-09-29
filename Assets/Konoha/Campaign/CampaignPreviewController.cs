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
        // Gerbang Dalam: closed leaves (solid) until the inner gate opens, then open leaves.
        public GameObject innerGateClosed;
        public GameObject innerGateOpen;
        public Text objectiveText;
        public Text statusText;
        public Text waypointText;
        public Text heroText;
        public Text feedbackText;
        public Image objectiveProgress;
        public Button sitButton;
        // Gold ring under the local hero while RESTU RAKYAT is active (no collider).
        public Transform restuAura;

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
            bool innerLocked = director.Phase < CampaignPhase.GerbangDalam;
            if (innerGateClosed != null && innerGateClosed.activeSelf != innerLocked)
                innerGateClosed.SetActive(innerLocked);
            if (innerGateOpen != null && innerGateOpen.activeSelf == innerLocked)
                innerGateOpen.SetActive(!innerLocked);

            NetworkObject hero = LocalHero();
            RefreshAura(director, hero);
            if (!director.HeroLocked)
            {
                // Hero screen (CampaignHeroSelect) is open; the run has not started yet.
                objectiveText.text = "PILIH HERO, lalu tekan MULAI";
                if (objectiveProgress != null) objectiveProgress.fillAmount = 0f;
                if (waypointText != null) waypointText.text = string.Empty;
                if (sitButton != null) sitButton.gameObject.SetActive(false);
                RefreshHero(director, hero);
                if (feedbackText != null)
                    feedbackText.text = Time.time < director.LastMessageUntil ? director.LastMessage : string.Empty;
                return;
            }

            RefreshObjective(director);
            RefreshHero(director, hero);
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
                        ? "GERBANG RAKYAT • Menuju PLAZA ASPIRASI"
                        : "GERBANG RAKYAT • Kroni " + (gateTotal - director.GateRemaining) + "/" + gateTotal +
                            " • hadiah RESTU";
                    break;
                case CampaignPhase.PlazaAspirasi:
                    switch (PlazaFocus(director))
                    {
                        case PlazaTask.Majelis:
                            objectiveText.text = MajelisObjective(director);
                            break;
                        case PlazaTask.Biro:
                            objectiveText.text = "BIRO • Masuk lingkaran, tekan SAHKAN " +
                                (director.BiroSteps + 1) + "/" + CampaignTuning.PreviewSlice.BiroSteps;
                            break;
                        default:
                            objectiveText.text = "PLAZA • Rebut segel MAJELIS (kiri) & BIRO (kanan)";
                            break;
                    }
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
                    PlazaTask task = PlazaFocus(director);
                    fill = task == PlazaTask.Majelis
                        ? (director.MajelisTotal > 0 ? 1f - director.MajelisRemaining / (float)director.MajelisTotal : 0f)
                        : task == PlazaTask.Biro
                            ? director.BiroSteps / (float)CampaignTuning.PreviewSlice.BiroSteps
                            : director.SealCount / (float)director.RequiredSeals;
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

        private enum PlazaTask
        {
            Choose,
            Majelis,
            Biro
        }

        // Which seal the HUD guides to: the sector in progress, else the one still missing.
        private static PlazaTask PlazaFocus(CampaignDirector director)
        {
            bool majelisDone = director.HasSeal(CampaignSector.MajelisDaun);
            bool biroDone = director.HasSeal(CampaignSector.BiroProsedur);
            if (!majelisDone && (director.MajelisStarted || biroDone))
                return PlazaTask.Majelis;
            if (!biroDone && (majelisDone || director.BiroSteps > 0))
                return PlazaTask.Biro;
            return majelisDone && biroDone ? PlazaTask.Biro : PlazaTask.Choose;
        }

        // §8.1 "Pecahkan Blok": Senior first, then the Ketua, then the remaining officers.
        // Kept under ~46 characters: longer lines were cut off on the tablet (0.0.9.2).
        private static string MajelisObjective(CampaignDirector director)
        {
            const string head = "MAJELIS • ";
            if (!director.MajelisStarted)
                return head + "Datangi sidang di sayap kiri";
            if (director.MajelisBlock)
                return head + "BLOK -40%! Kalahkan SENIOR (" + director.SeniorsAlive + " lagi)";
            if (!director.MajelisLeaderDown)
                return head + "Blok pecah! Tumbangkan KETUA";
            return head + "Kalahkan sisa pejabat (" + director.MajelisRemaining + ")";
        }

        private void RefreshAura(CampaignDirector director, NetworkObject hero)
        {
            if (restuAura == null)
                return;

            NetworkPlayerCombat combat = hero != null ? hero.GetComponent<NetworkPlayerCombat>() : null;
            bool show = director.RestuActive && combat != null && !combat.IsKnockedOut;
            if (restuAura.gameObject.activeSelf != show)
                restuAura.gameObject.SetActive(show);
            if (show)
                restuAura.position = hero.transform.position + Vector3.up * 0.1f;
        }

        private void RefreshHero(CampaignDirector director, NetworkObject hero)
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
                "  •  PENGARUH " + kit.Pengaruh + "%" + (director.RestuActive ? "  •  RESTU" : string.Empty);
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
                    PlazaTask task = PlazaFocus(director);
                    if (task == PlazaTask.Biro)
                    {
                        destination = stage.biro.position;
                        label = "BIRO";
                        arrival = CampaignTuning.PreviewSlice.SectorRadius;
                    }
                    else if (director.MajelisStarted && MajelisTarget(director, hero.transform.position,
                        out destination, out label))
                    {
                        // Point at the member to hit next: Senior while the block holds, then the Ketua.
                    }
                    else
                    {
                        destination = stage.majelis.position;
                        label = "MAJELIS";
                        arrival = CampaignTuning.PreviewSlice.SectorRadius;
                    }
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
                waypointText.text = label == "BIRO" ? "DI BIRO: tekan SAHKAN 3 kali" : "DI LOKASI: " + label;
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

        private static bool MajelisTarget(CampaignDirector director, Vector3 from, out Vector3 position, out string label)
        {
            if (director.MajelisBlock && NearestEnemy(from, out position, FactionId.MajelisDaun, UnitRole.Senior))
            {
                label = "SENIOR";
                return true;
            }
            if (!director.MajelisLeaderDown && NearestEnemy(from, out position, FactionId.MajelisDaun, UnitRole.Pemimpin))
            {
                label = "KETUA";
                return true;
            }
            label = "MAJELIS";
            return NearestEnemy(from, out position, FactionId.MajelisDaun, null);
        }

        private static bool NearestEnemy(Vector3 from, out Vector3 position)
        {
            return NearestEnemy(from, out position, null, null);
        }

        private static bool NearestEnemy(Vector3 from, out Vector3 position, FactionId? faction, UnitRole? role)
        {
            position = Vector3.zero;
            float best = float.MaxValue;
            foreach (CampaignEnemy enemy in FindObjectsByType<CampaignEnemy>(FindObjectsSortMode.None))
            {
                if (enemy == null || !enemy.IsSpawned || enemy.IsOutOfFight)
                    continue;
                if ((faction.HasValue && enemy.Faction != faction.Value) || (role.HasValue && enemy.Role != role.Value))
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
