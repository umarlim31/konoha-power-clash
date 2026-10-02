using Konoha.Networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // Jalur Takhta HUD bridge (every peer). Reads the replicated CampaignDirector state and
    // the local hero; sends the contextual DUDUK / ULANG action to the host.
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
        // Biro Prosedur (0.0.9.3): office door, loket zone discs and labels, debuff line.
        public GameObject biroDoorClosed;
        public GameObject biroDoorOpen;
        public Renderer[] loketZones = new Renderer[0];
        public TextMesh[] loketLabels = new TextMesh[0];
        public Text debuffText;
        // 0.1.0: Garda LOCKDOWN ring (colliders + posts) and the seat lock on the joystick.
        public GameObject gardaLockdown;
        public CampaignTraversal traversal;

        private static readonly Color LoketIdle = new Color(0.22f, 0.34f, 0.52f);
        private static readonly Color LoketFull = new Color(0.95f, 0.74f, 0.26f);
        private static readonly Color LoketQueue = new Color(0.78f, 0.16f, 0.14f);
        private static readonly Color LoketDone = new Color(0.30f, 0.66f, 0.36f);
        private MaterialPropertyBlock loketBlock;
        private int[] loketLabelState;

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

            bool officeLocked = !director.BiroDoorOpen;
            if (biroDoorClosed != null && biroDoorClosed.activeSelf != officeLocked)
                biroDoorClosed.SetActive(officeLocked);
            if (biroDoorOpen != null && biroDoorOpen.activeSelf == officeLocked)
                biroDoorOpen.SetActive(!officeLocked);
            RefreshLokets(director);

            if (gardaLockdown != null && gardaLockdown.activeSelf != director.GardaLockdown)
                gardaLockdown.SetActive(director.GardaLockdown);

            NetworkObject hero = LocalHero();
            if (traversal != null)
                traversal.seatLocked = hero != null && director.SeatedObjectId == hero.NetworkObjectId;
            RefreshAura(director, hero);
            RefreshDebuff(hero);
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
                            objectiveText.text = BiroObjective(director);
                            break;
                        default:
                            objectiveText.text = "PLAZA • Lawan atau rangkul MAJELIS & BIRO";
                            break;
                    }
                    break;
                case CampaignPhase.GerbangDalam:
                    objectiveText.text = "GERBANG DALAM • Masuk lapangan GARDA TAKHTA";
                    break;
                case CampaignPhase.GardaTakhta:
                    objectiveText.text = director.GardaLockdown
                        ? "LOCKDOWN • Tumbangkan PANGLIMA TAKHTA"
                        : "GARDA • Tumbangkan PANGLIMA (awas DORONGAN)";
                    break;
                case CampaignPhase.KursiTerbuka:
                    objectiveText.text = director.ReignStarted
                        ? "KURSI LEPAS • DUDUK lagi (Runtuh " + director.ReignRuntuh + "/" +
                            CampaignTuning.Memerintah.KudetaRuntuhLimit + ")"
                        : CampaignTuning.Memerintah.SeatWinsRun
                            ? "KURSI TERBUKA • Naik ramp, DUDUK = MENANG"
                            : "KURSI TERBUKA • Naik ramp dan tekan DUDUK";
                    break;
                case CampaignPhase.Memerintah:
                    objectiveText.text = director.CounterRemaining > 0
                        ? "SERANGAN BALIK • Pertahankan Kursi (" + director.CounterRemaining + ")"
                        : "MEMERINTAH • Kuasa " + director.Power + "/" + director.TargetPower;
                    break;
                default:
                    objectiveText.text = "TAKHTA DIKUASAI!";
                    break;
            }

            // 0.4.0 Musim Pemilu: political resources replace KUASA (DUDUK wins the run).
            statusText.text = "SEGEL " + director.SealCount + "/" + director.RequiredSeals +
                "  |  MODAL " + director.Modal + "  |  JATAH " + director.Jatah + "  |  RESTU " + director.Restu;

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
                            ? BiroFill(director)
                            : director.SealCount / (float)director.RequiredSeals;
                    break;
                case CampaignPhase.GardaTakhta:
                    fill = 1f - director.PanglimaFraction;
                    break;
                case CampaignPhase.KursiTerbuka:
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
            if (!biroDone && (majelisDone || director.BiroStarted))
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

        // §8.2 "Sahkan Berkas": lokets first, then the Kepala Biro. Kept under ~46 characters.
        private static string BiroObjective(CampaignDirector director)
        {
            const string head = "BIRO • ";
            int stamped = director.LoketStampedCount;
            if (!director.BiroStarted)
                return head + "Datangi loket di sayap kanan";
            if (!director.BiroDoorOpen)
            {
                for (int i = 0; i < director.LoketCount; i++)
                    if (director.LoketContested(i) && !director.LoketStamped(i))
                        return head + "Cap LOKET " + stamped + "/3 • usir ANTRIAN";
                return head + "Berdiri di LOKET untuk cap (" + stamped + "/3)";
            }
            return director.BiroLeaderDown
                ? head + "Selesaikan loket (" + stamped + "/3)"
                : head + "Pintu terbuka! Tumbangkan KEPALA BIRO";
        }

        private static float BiroFill(CampaignDirector director)
        {
            if (director.BiroDoorOpen)
                return 1f;
            float total = 0f;
            for (int i = 0; i < director.LoketCount; i++)
                total += director.LoketStamped(i) ? 1f : director.LoketProgress(i);
            return total / director.LoketCount;
        }

        // Loket discs: blue idle, gold while filling, red with a queue, green once stamped.
        private void RefreshLokets(CampaignDirector director)
        {
            loketBlock ??= new MaterialPropertyBlock();
            for (int i = 0; i < loketZones.Length && i < director.LoketCount; i++)
            {
                bool stamped = director.LoketStamped(i);
                bool queue = !stamped && director.LoketContested(i);
                float progress = director.LoketProgress(i);
                Color color = stamped ? LoketDone : queue ? LoketQueue : Color.Lerp(LoketIdle, LoketFull, progress);

                if (loketZones[i] != null)
                {
                    loketZones[i].GetPropertyBlock(loketBlock);
                    loketBlock.SetColor("_BaseColor", color);
                    loketZones[i].SetPropertyBlock(loketBlock);
                }

                if (i < loketLabels.Length && loketLabels[i] != null)
                {
                    // Rebuilt only when the shown state changes (no per-frame string garbage).
                    int state = stamped ? 1000 : queue ? 1001 : Mathf.FloorToInt(progress * 100f);
                    if (loketLabelState == null || loketLabelState.Length != loketLabels.Length)
                        loketLabelState = new int[loketLabels.Length];
                    if (loketLabelState[i] != state + 1)
                    {
                        loketLabelState[i] = state + 1;
                        loketLabels[i].text = "LOKET " + (i + 1) + "\n" + (stamped ? "TERCAP"
                            : queue ? "ANTRIAN"
                            : state > 0 ? state + "%"
                            : "BERDIRI DI SINI");
                    }
                    loketLabels[i].color = stamped ? new Color(0.62f, 1f, 0.66f) : queue ? new Color(1f, 0.45f, 0.40f) : Color.white;
                }
            }
        }

        private void RefreshDebuff(NetworkObject hero)
        {
            if (debuffText == null)
                return;
            bool slowed = hero != null && CampaignEnemy.InsideStempelTunda(hero.transform.position);
            debuffText.text = slowed ? "STEMPEL TUNDA: cooldown skill +30%" : string.Empty;
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
                "  •  PENGARUH " + kit.Pengaruh + "%";
        }

        private void RefreshAction(CampaignDirector director, NetworkObject hero)
        {
            if (sitButton == null)
                return;

            Vector3 position = hero != null ? hero.transform.position : Vector3.one * 999f;
            bool canSit = director.Phase == CampaignPhase.KursiTerbuka &&
                CampaignObjectiveDirector.Near(position, stage.chair.position, CampaignTuning.Memerintah.ChairRadius);
            bool seated = hero != null && director.SeatedObjectId == hero.NetworkObjectId;
            bool won = director.Phase == CampaignPhase.Menang;

            // The result screen owns ULANG / GANTI HERO after a victory.
            sitButton.gameObject.SetActive((canSit || seated) && !won);
            if (sitLabel != null)
                sitLabel.text = seated ? "BERDIRI" : "DUDUK";
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
                        if (!BiroTarget(director, hero.transform.position, out destination, out label, out arrival))
                        {
                            destination = stage.biro.position;
                            label = "BIRO";
                            arrival = CampaignTuning.PreviewSlice.SectorRadius;
                        }
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
                    if (NearestEnemy(hero.transform.position, out destination, FactionId.GardaTakhta, UnitRole.Pemimpin))
                        label = "PANGLIMA";
                    else
                    {
                        destination = stage.garda.position;
                        label = "GARDA";
                    }
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
                waypointText.text = label.StartsWith("LOKET")
                    ? "DI " + label + ": tetap berdiri sampai tercap"
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

        // Nearest unstamped loket, then the Kepala Biro once his door is open.
        private bool BiroTarget(CampaignDirector director, Vector3 from, out Vector3 position, out string label,
            out float arrival)
        {
            arrival = CampaignTuning.Biro.LoketRadius * 0.8f;
            if (!director.BiroStarted)
            {
                position = stage.biro.position;
                label = "BIRO";
                arrival = CampaignTuning.PreviewSlice.SectorRadius;
                return true;
            }

            if (director.BiroDoorOpen && !director.BiroLeaderDown &&
                NearestEnemy(from, out position, FactionId.BiroProsedur, UnitRole.Pemimpin))
            {
                label = "KEPALA BIRO";
                arrival = CampaignTuning.PreviewSlice.WaypointArrivalRadius;
                return true;
            }

            position = Vector3.zero;
            label = "BIRO";
            float best = float.MaxValue;
            for (int i = 0; i < stage.loketPoints.Length && i < director.LoketCount; i++)
            {
                if (director.LoketStamped(i))
                    continue;
                float distance = (stage.loketPoints[i] - from).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    position = stage.loketPoints[i];
                    label = "LOKET " + (i + 1);
                }
            }
            return best < float.MaxValue;
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
            foreach (CampaignEnemy enemy in CampaignEnemy.Active)
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
