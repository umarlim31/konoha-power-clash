using System.Collections.Generic;
using Konoha.Networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // 0.6.0 KARIER Level 1 "Warga Biasa": the scene side of KarierLife. Feeds the hero
    // position and the fight state into the rules, shows the HUD (money, energy, restu,
    // catatan hitam, target Ketua RT), the KONOHA KERJA phone, the contextual action
    // button, the police choices and the Grup WA, and drives the job visuals (ojol motor,
    // cement sack), the crowd and the police (KarierCrowd) and the preman encounters
    // (CampaignDirector, host). Progress is saved on the tablet (PlayerPrefs).
    // Solo host only, like the rest of the campaign; co-op is not part of 0.6.0.
    public sealed class KarierController : MonoBehaviour
    {
        [Header("Shared HUD")]
        public Text objectiveText;
        public Text statusText;
        public Text waypointText;
        public Text heroText;
        public Text feedbackText;
        public Image objectiveProgress;
        public CampaignTraversal traversal;
        public CampaignBodies bodies;
        // S1, S2, ULT and GANTI HERO: a warga biasa only punches, dodges and jumps.
        public Button[] hiddenInKarier = new Button[0];

        [Header("KARIER HUD")]
        public GameObject hudRoot;
        public Text targetText;
        public GameObject messagePanel;
        public Text messageText;
        public Button phoneButton;
        public GameObject phonePanel;
        public Text phoneInfo;
        public Button ojolButton, kuliButton, buzzerButton, rebahanButton, closePhoneButton;
        public Button actionButton;
        public GameObject policePanel;
        public Text policeText;
        public Button kaburButton, damaiButton, polsekButton;
        public GameObject waPanel;
        public Text waText;
        public Button waCloseButton;
        public GameObject rebahanPanel;
        public Text rebahanText;
        public Image fade;

        [Header("World")]
        public string[] placeNames = new string[0];
        public Vector3[] placePoints = new Vector3[0];
        public Vector3 kuliPickup, kuliDrop, warkop, posRt;
        public string[] sapaNames = new string[0];
        public Vector3[] sapaPoints = new Vector3[0];
        public Vector3 premanCenter = new Vector3(1f, 0f, -29.5f);
        public Vector3[] premanPoints = new Vector3[0];
        public Vector3 premanLook = new Vector3(8.6f, 0f, -26f);
        public Vector3 homePoint = new Vector3(0f, 0.35f, -44f);
        public KarierCrowd crowd;
        public Transform ojolMotor;
        public GameObject ojolPassenger;
        public Transform sack;

        private KarierLife life;
        private bool started;
        private readonly Queue<string> messages = new Queue<string>();
        private float messageUntil;
        // By instance, not NetworkObjectId: Netcode recycles ids of despawned preman.
        private readonly HashSet<CampaignEnemy> beaten = new HashSet<CampaignEnemy>();
        private float premanClock;
        private float nextPremanAt;
        private float premanSince = -1f;
        private int premanEvents;
        private bool lastShouted, lastMelerai, lastPolice;
        private float calmSince = -1f;
        private string lastTarget;
        private string lastSave;
        private float nextSave;
        private float fadeUntil = -1f;
        private bool polsekPending;
        private int polsekStage;
        private KarierAction shownAction = KarierAction.None;
        private int shownSapa = -1;
        private float nextFeed;
        private int feedIndex;
        private System.Random random;
        private Text actionLabel;
        private bool runtuhPending;

        private static readonly string[] Feed =
        {
            "Video kucing joget. Lima menit hilang.",
            "IKLAN: \"PINJOL CAIR 5 MENIT, TANPA AGUNAN!\" (bunga 4% per hari)",
            "Grup WA keluarga: \"Bapak-bapak, besok kerja bakti ya\" ...dibaca, tidak dibalas.",
            "Trending: #KaburAjaDulu. Kamu masih rebahan.",
            "Konten motivasi: \"Kaya di usia 25 tanpa kerja!\" Kamu 30, masih rebahan.",
            "Tetangga update story: liburan ke luar negeri. Cicilan siapa?",
            "Live jualan: \"Kak, kak, checkout sekarang, stok tinggal 3!\""
        };

        public bool Started => started;
        public KarierLife Life => life;

        private void Start()
        {
            random = new System.Random(System.Environment.TickCount);
            Listen(phoneButton, TogglePhone);
            Listen(closePhoneButton, ClosePhone);
            Listen(ojolButton, () => Work(KarierJob.Ojol));
            Listen(kuliButton, () => Work(KarierJob.Kuli));
            Listen(buzzerButton, () => Work(KarierJob.Buzzer));
            Listen(rebahanButton, Rebahan);
            Listen(actionButton, DoAction);
            Listen(kaburButton, () => Police(KarierPoliceChoice.Kabur));
            Listen(damaiButton, () => Police(KarierPoliceChoice.Damai));
            Listen(polsekButton, () => Police(KarierPoliceChoice.Polsek));
            Listen(waCloseButton, () => SetActive(waPanel, false));
            actionLabel = actionButton != null ? actionButton.GetComponentInChildren<Text>(true) : null;
            CampaignKarier.HeroRuntuh += OnHeroRuntuh;
            ShowHud(false);
        }

        private void OnDestroy()
        {
            CampaignKarier.HeroRuntuh -= OnHeroRuntuh;
            foreach (Button button in new[] { phoneButton, closePhoneButton, ojolButton, kuliButton, buzzerButton,
                rebahanButton, actionButton, kaburButton, damaiButton, polsekButton, waCloseButton })
                if (button != null) button.onClick.RemoveAllListeners();
            if (life != null)
            {
                life.Notified -= Say;
                life.Cued -= OnCue;
            }
            SaveNow();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveNow();
        }

        private static void Listen(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }

        // MULAI HIDUP / LANJUTKAN HIDUP on the character creator.
        public void BeginLife(bool fresh)
        {
            if (started)
                return;
            life = new KarierLife(BuildLayout(), System.Environment.TickCount);
            if (fresh)
                PlayerPrefs.DeleteKey(CampaignKarier.SaveKey);
            else
                life.TryLoad(PlayerPrefs.GetString(CampaignKarier.SaveKey, string.Empty));
            life.Notified += Say;
            life.Cued += OnCue;
            started = true;
            nextPremanAt = CampaignTuning.Karier.PremanFirstSeconds;
            ShowHud(true);
            HideHeroControls();

            CampaignDirector director = CampaignDirector.Instance;
            if (director != null && director.IsServer)
                director.ServerKarierStart();
            string name = CampaignKarier.Avatar.Name;
            Say(fresh
                ? "Selamat datang di RT 03, " + name + ". Dompet " + KarierLife.Rupiah(life.Duit) + ". Buka HP > KONOHA KERJA untuk cari duit."
                : "Selamat datang kembali, " + name + ". Hidup berlanjut, cicilan juga.");
            Say("Target LEVEL 1: jadi KETUA RT. Syarat: " + KarierLife.Rupiah(CampaignTuning.Karier.SyukuranRT) +
                " untuk syukuran + RESTU " + CampaignTuning.Karier.RestuSyaratRT + ".");
            SaveNow();
        }

        private KarierLayout BuildLayout()
        {
            var layout = new KarierLayout
            {
                PlaceNames = (string[])placeNames.Clone(),
                Places = new KarierPoint[placePoints.Length],
                KuliPickup = Point(kuliPickup),
                KuliDrop = Point(kuliDrop),
                Warkop = Point(warkop),
                PosRt = Point(posRt),
                SapaNames = (string[])sapaNames.Clone(),
                SapaPoints = new KarierPoint[sapaPoints.Length]
            };
            for (int i = 0; i < placePoints.Length; i++)
                layout.Places[i] = Point(placePoints[i]);
            for (int i = 0; i < sapaPoints.Length; i++)
                layout.SapaPoints[i] = Point(sapaPoints[i]);
            return layout;
        }

        private static KarierPoint Point(Vector3 v) => new KarierPoint(v.x, v.z);
        private static Vector3 World(KarierPoint p) => new Vector3(p.X, 0f, p.Z);

        // --- Update ---------------------------------------------------------------------------

        private void Update()
        {
            if (!started || life == null)
                return;
            CampaignDirector director = CampaignDirector.Instance;
            NetworkObject hero = LocalHero();
            if (director == null || !director.IsSpawned || hero == null)
                return;
            // MULAI HIDUP may come before the director was ready: keep asking until the world runs.
            if (director.IsServer && !director.HeroLocked)
                director.ServerKarierStart();

            float dt = Mathf.Min(Time.deltaTime, CampaignTuning.PreviewSlice.MaxFrameSeconds);
            NetworkPlayerCombat combat = hero.GetComponent<NetworkPlayerCombat>();
            bool heroDown = combat != null && combat.IsKnockedOut;
            Vector3 position = hero.transform.position;
            bool premanNear = NearestPreman(position, CampaignTuning.Karier.FightRadius, out Vector3 premanAt, false);
            bool fighting = !heroDown && premanNear;

            if (runtuhPending)
            {
                runtuhPending = false;
                life.OnHeroRuntuh();
                if (crowd != null)
                {
                    crowd.Disperse();
                    if (!life.PoliceCalled) crowd.PoliceLeave();
                }
                ClosePhone();
            }

            life.Tick(Point(position), dt, fighting, heroDown);
            CountBeaten();
            UpdatePreman(director, dt, fighting);
            UpdateKeributan(director, position, premanAt);
            UpdatePolsek(director);
            UpdateJobVisuals();
            UpdateGuidance(position);
            UpdateAction(position);
            UpdateHud(combat);
            UpdateMessages();
            UpdateRebahanFeed();

            if (traversal != null)
                traversal.seatLocked = life.Rebahan || life.PoliceArrived || polsekPending;

            if (Time.unscaledTime >= nextSave)
            {
                nextSave = Time.unscaledTime + 4f;
                SaveNow();
            }
        }

        private static NetworkObject LocalHero()
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null || manager.LocalClient == null)
                return null;
            return manager.LocalClient.PlayerObject;
        }

        // Standing preman near the hero (calmed ones count unless excluded).
        private static bool NearestPreman(Vector3 from, float radius, out Vector3 position, bool includeCalmed)
        {
            position = from;
            float best = radius * radius;
            bool found = false;
            foreach (CampaignEnemy enemy in CampaignEnemy.Active)
            {
                if (enemy == null || !enemy.IsSpawned || !enemy.IsKarier || enemy.IsOutOfFight)
                    continue;
                if (!includeCalmed && enemy.Calmed)
                    continue;
                Vector3 d = enemy.transform.position - from;
                d.y = 0f;
                if (d.sqrMagnitude <= best)
                {
                    best = d.sqrMagnitude;
                    position = enemy.transform.position;
                    found = true;
                }
            }
            return found;
        }

        private static int StandingPreman()
        {
            int count = 0;
            foreach (CampaignEnemy enemy in CampaignEnemy.Active)
                if (enemy != null && enemy.IsSpawned && enemy.IsKarier && !enemy.IsOutOfFight)
                    count++;
            return count;
        }

        private void CountBeaten()
        {
            int fresh = 0;
            beaten.RemoveWhere(enemy => enemy == null || !enemy.IsSpawned);
            foreach (CampaignEnemy enemy in CampaignEnemy.Active)
                if (enemy != null && enemy.IsSpawned && enemy.IsKarier && enemy.IsDown && beaten.Add(enemy))
                    fresh++;
            if (fresh > 0)
                life.OnPremanBeaten(fresh);
        }

        // Preman memalak warga at the mouth of the gang: optional fights every minute or so.
        private void UpdatePreman(CampaignDirector director, float dt, bool fighting)
        {
            int standing = StandingPreman();
            if (standing > 0)
            {
                if (premanSince < 0f)
                    premanSince = Time.time;
                if (!fighting && !life.PoliceCalled && Time.time - premanSince > CampaignTuning.Karier.PremanLeaveSeconds &&
                    director.IsServer)
                {
                    director.ServerKarierClear(false);
                    Say("Preman pergi membawa uang palakan. Warga menggerutu: \"Nggak ada yang berani...\"");
                    premanSince = -1f;
                }
                return;
            }
            premanSince = -1f;
            if (life.PoliceCalled || life.Rebahan || !director.IsServer)
                return;
            premanClock += dt;
            if (premanClock < nextPremanAt)
                return;
            premanClock = 0f;
            nextPremanAt = CampaignTuning.Karier.PremanEverySeconds * (0.8f + 0.4f * (float)random.NextDouble());
            bool boss = premanEvents >= 1 && random.NextDouble() < 0.5;
            if (director.ServerKarierSpawnPreman(premanCenter, premanPoints, premanLook, boss) > 0)
            {
                premanEvents++;
                premanSince = Time.time;
                string[] victims = { "tukang sayur", "bapak-bapak pos ronda", "driver ojol", "anak sekolah" };
                Say((boss ? "BOS PREMAN dan anak buahnya" : "PREMAN") + " malak " + victims[random.Next(victims.Length)] +
                    " di mulut gang!  Usir dia, atau cuek saja.");
                Play(CampaignSound.Warning, 0.8f);
            }
        }

        // Shout, crowd, melerai and police follow KarierLife's keributan meter.
        private void UpdateKeributan(CampaignDirector director, Vector3 heroPosition, Vector3 premanAt)
        {
            bool shouted = life.Shouted;
            bool melerai = life.MeleraiActive;
            bool police = life.PoliceCalled;
            if (crowd != null)
            {
                if (shouted && !lastShouted)
                    crowd.Gather((heroPosition + premanAt) * 0.5f);
                if (melerai && !lastMelerai)
                {
                    if (director.IsServer)
                        director.ServerKarierCalm(CampaignTuning.Karier.MeleraiSeconds);
                    Vector3 other = premanAt;
                    if (!NearestPreman(heroPosition, CampaignTuning.Karier.FightRadius * 2f, out other, true))
                        other = heroPosition + Vector3.forward * 2f;
                    crowd.Melerai(heroPosition, other, CampaignTuning.Karier.MeleraiSeconds);
                }
                if (police && !lastPolice)
                    crowd.DispatchPolice(heroPosition);
                if (police && !life.PoliceArrived && (crowd.PoliceArrived || crowd.policeMotor == null))
                {
                    life.PoliceArrive();
                    if (director.IsServer)
                        director.ServerKarierClear(true);
                    OpenPolicePanel();
                }
                // The show is over: the crowd goes home a few seconds after things calm down.
                if (crowd.Gathered && !police && life.Keributan <= 0f && StandingPreman() == 0)
                {
                    if (calmSince < 0f) calmSince = Time.time;
                    else if (Time.time - calmSince > 3f)
                    {
                        crowd.Disperse();
                        calmSince = -1f;
                    }
                }
                else
                {
                    calmSince = -1f;
                }
            }
            else if (police && !life.PoliceArrived)
            {
                life.PoliceArrive();
                OpenPolicePanel();
            }
            lastShouted = shouted;
            lastMelerai = melerai;
            lastPolice = police;
        }

        private void OpenPolicePanel()
        {
            ClosePhone();
            SetActive(policePanel, true);
            bool hoaks = life.PoliceReason == KarierPoliceReason.Hoaks;
            if (policeText != null)
                policeText.text = hoaks
                    ? "\"Saudara " + CampaignKarier.Avatar.Name + ", postingan Saudara dilaporkan.\nPasal karet: penyebaran berita bohong.\"\n\nCATATAN HITAM: " + life.CatatanHitam
                    : "\"Ada keributan di sini! Siapa yang mulai?\"\nWarga menunjuk ke arahmu sambil merekam.\n\nCATATAN HITAM: " + life.CatatanHitam;
            Caption(kaburButton, "KABUR\nCATATAN HITAM +" + CampaignTuning.Karier.KaburCatatan);
            Caption(damaiButton, "DAMAI DI TEMPAT\n" + KarierLife.Rupiah(life.DamaiPrice) + (life.CanDamai ? string.Empty : " (duit kurang)"));
            if (damaiButton != null)
                damaiButton.interactable = life.CanDamai;
            Caption(polsekButton, "IKUT KE POLSEK\nsemalam, " + KarierLife.Rupiah(CampaignTuning.Karier.PolsekBiaya));
        }

        private void Police(KarierPoliceChoice choice)
        {
            if (life == null || !life.ResolvePolice(choice))
                return;
            SetActive(policePanel, false);
            if (crowd != null)
            {
                crowd.PoliceLeave();
                crowd.Disperse();
            }
            if (choice == KarierPoliceChoice.Polsek)
            {
                polsekPending = true;
                polsekStage = 0;
                fadeUntil = Time.unscaledTime + 1.6f;
                SetFade(0f);
            }
            SaveNow();
        }

        // IKUT KE POLSEK: fade to black, the night passes, back at home.
        private void UpdatePolsek(CampaignDirector director)
        {
            if (!polsekPending)
                return;
            float left = fadeUntil - Time.unscaledTime;
            if (polsekStage == 0)
            {
                SetFade(1f - Mathf.Clamp01(left / 1.6f));
                if (left <= 0f)
                {
                    polsekStage = 1;
                    fadeUntil = Time.unscaledTime + 1.6f;
                    if (director.IsServer)
                        director.ServerKarierTeleportHero(homePoint);
                }
            }
            else
            {
                SetFade(Mathf.Clamp01(left / 1.6f));
                if (left <= 0f)
                {
                    polsekPending = false;
                    SetFade(0f);
                }
            }
        }

        private void SetFade(float alpha)
        {
            if (fade == null)
                return;
            bool show = alpha > 0.01f;
            if (fade.gameObject.activeSelf != show)
                fade.gameObject.SetActive(show);
            fade.color = new Color(0f, 0f, 0f, alpha);
        }

        private void OnHeroRuntuh() => runtuhPending = true;

        // --- Jobs, phone, actions -------------------------------------------------------------

        private void TogglePhone()
        {
            if (life == null || phonePanel == null)
                return;
            bool open = !phonePanel.activeSelf;
            if (open && (life.PoliceArrived || polsekPending))
                return;
            phonePanel.SetActive(open);
            if (open)
            {
                RefreshPhone();
                Play(CampaignSound.Notif, 0.6f);
            }
        }

        private void ClosePhone() => SetActive(phonePanel, false);

        private void Work(KarierJob job)
        {
            if (life == null)
                return;
            if (life.Job == job)
            {
                life.CancelJob();
                RefreshPhone();
                return;
            }
            if (!life.TakeJob(job, out string reason))
            {
                Say(reason);
                Play(CampaignSound.Warning, 0.5f);
                RefreshPhone();
                return;
            }
            ClosePhone();
        }

        private void Rebahan()
        {
            if (life == null)
                return;
            if (life.StartRebahan(out string reason))
            {
                ClosePhone();
                feedIndex = random.Next(Feed.Length);
                nextFeed = 0f;
            }
            else
            {
                Say(reason);
                RefreshPhone();
            }
        }

        private void RefreshPhone()
        {
            if (life == null)
                return;
            bool busy = life.Busy;
            JobButton(ojolButton, KarierJob.Ojol, "OJOL", "Rp 25-60rb per antar  •  capek dikit");
            JobButton(kuliButton, KarierJob.Kuli, "KULI BANGUNAN",
                KarierLife.Rupiah(CampaignTuning.Karier.KuliWage - CampaignTuning.Karier.KuliMandorCut) + " per 5 sak  •  capek banget");
            string buzzer = life.BuzzerCooldown > 0f
                ? "tunggu order " + Mathf.CeilToInt(life.BuzzerCooldown) + " dtk"
                : KarierLife.Rupiah(CampaignTuning.Karier.BuzzerPay) + " cepat  •  CATATAN HITAM +" + CampaignTuning.Karier.BuzzerCatatan;
            JobButton(buzzerButton, KarierJob.Buzzer, "BUZZER HOAKS", buzzer);
            Caption(rebahanButton, "REBAHAN\nscroll sosmed, ENERGI +" + CampaignTuning.Karier.RebahanEnergi);
            if (rebahanButton != null)
                rebahanButton.interactable = !busy && !life.Rebahan;
            if (phoneInfo != null)
                phoneInfo.text = KarierLife.Rupiah(life.Duit) + "   ENERGI " + life.Energi +
                    (life.Lemas ? " (LEMAS)" : string.Empty) + "\n" +
                    (busy ? "Sedang kerja: " + JobName(life.Job) + ". Tekan lagi untuk berhenti." : "Pilih kerja. Duit bersih lambat, duit kotor cepat.");
        }

        private void JobButton(Button button, KarierJob job, string title, string detail)
        {
            if (button == null)
                return;
            bool current = life.Job == job;
            Caption(button, current ? "BERHENTI " + title + "\n(kerja sedang jalan)" : title + "\n" + detail);
            button.interactable = current || !life.Busy;
        }

        private static string JobName(KarierJob job)
        {
            switch (job)
            {
                case KarierJob.Ojol: return "OJOL";
                case KarierJob.Kuli: return "KULI";
                case KarierJob.Buzzer: return "BUZZER";
                default: return "-";
            }
        }

        private void DoAction()
        {
            if (life == null || shownAction == KarierAction.None)
                return;
            KarierAction action = shownAction;
            bool done = life.DoAction(action, shownSapa);
            if (done && action == KarierAction.DaftarRT)
                OpenWa();
            SaveNow();
        }

        private void OpenWa()
        {
            ClosePhone();
            SetActive(waPanel, true);
            if (waText == null)
                return;
            string name = CampaignKarier.Avatar.Name;
            waText.text =
                "<b>Pak RT 03:</b> Pendaftaran calon Ketua RT a.n. " + name + " DITERIMA. Terima kasih nasi kotaknya.\n\n" +
                "<b>Bu Tejo:</b> Ayamnya kecil ya. Tapi gpp, yang penting niatnya.\n\n" +
                "<b>Juragan Kos:</b> Saya juga daftar. Sembako sudah saya siapkan 200 paket.\n\n" +
                "<b>Pak Haji:</b> Semoga amanah. Jangan lupa iuran sampah tetap Rp 20.000.\n\n" +
                "<b>Admin:</b> <i>Pemilihan Ketua RT hadir di versi berikutnya. Progresmu tersimpan.</i>";
        }

        // --- Visuals -------------------------------------------------------------------------

        private void UpdateJobVisuals()
        {
            NetworkObject hero = LocalHero();
            bool riding = life.Job == KarierJob.Ojol;
            bool carrying = life.Carrying;
            if (ojolMotor != null && hero != null)
            {
                if (riding && ojolMotor.parent != hero.transform)
                {
                    ojolMotor.SetParent(hero.transform, false);
                    ojolMotor.localPosition = Vector3.zero;
                    ojolMotor.localRotation = Quaternion.identity;
                }
                if (ojolMotor.gameObject.activeSelf != riding)
                    ojolMotor.gameObject.SetActive(riding);
            }
            SetActive(ojolPassenger, riding && life.Step == KarierStep.OjolAntar);
            if (sack != null && hero != null)
            {
                if (carrying && sack.parent != hero.transform)
                {
                    sack.SetParent(hero.transform, false);
                    sack.localPosition = new Vector3(0.24f, 1.78f, -0.05f);
                    sack.localRotation = Quaternion.Euler(0f, 0f, -12f);
                }
                if (sack.gameObject.activeSelf != carrying)
                    sack.gameObject.SetActive(carrying);
            }
            CampaignHumanoid body = bodies != null ? bodies.LocalBody() : null;
            if (body != null)
            {
                body.riding = riding;
                body.carrying = carrying && !riding;
            }
            if (traversal != null)
            {
                float speed = riding ? CampaignTuning.Karier.OjolSpeed : carrying ? CampaignTuning.Karier.KuliCarrySpeed : 1f;
                if (life.Lemas && !riding)
                    speed *= CampaignTuning.Karier.LemasSpeed;
                traversal.speedBonus = speed;
            }
        }

        private void UpdateGuidance(Vector3 heroPosition)
        {
            string label = string.Empty;
            if (life.Target(out KarierPoint target, out label))
            {
                Vector3 world = World(target);
                CampaignGuideArrow.Show(world, CampaignTuning.Karier.ZoneRadius * 0.8f, label);
                if (waypointText != null)
                    waypointText.text = Direction(label, world, heroPosition);
            }
            else
            {
                CampaignGuideArrow.Hide();
                if (waypointText != null)
                    waypointText.text = string.Empty;
            }
            if (label != lastTarget)
            {
                // A small phone "ting" for every new destination (owner: sound instead of the pillar);
                // job steps already ting from KarierLife, so only the other targets ring here.
                if (!string.IsNullOrEmpty(label) && life.Step == KarierStep.None)
                    Play(CampaignSound.Notif, 0.55f);
                lastTarget = label;
            }
        }

        private static string Direction(string label, Vector3 destination, Vector3 from)
        {
            Vector3 delta = destination - from;
            float distance = new Vector2(delta.x, delta.z).magnitude;
            if (distance < CampaignTuning.Karier.ZoneRadius)
                return "DI LOKASI: " + label + "  •  tetap berdiri sebentar";
            Camera view = Camera.main;
            if (view != null)
            {
                Vector3 ahead = view.transform.forward;
                ahead.y = 0f;
                ahead.Normalize();
                var right = new Vector3(ahead.z, 0f, -ahead.x);
                delta = new Vector3(Vector3.Dot(delta, right), 0f, Vector3.Dot(delta, ahead));
            }
            string direction;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.z) * 1.8f)
                direction = delta.x < 0f ? "KIRI" : "KANAN";
            else if (Mathf.Abs(delta.z) > Mathf.Abs(delta.x) * 1.8f)
                direction = delta.z < 0f ? "BELAKANG" : "DEPAN";
            else
                direction = (delta.z < 0f ? "BELAKANG " : "DEPAN ") + (delta.x < 0f ? "KIRI" : "KANAN");
            return "ARAH " + label + ": " + direction + "  •  " + Mathf.CeilToInt(distance) + " m";
        }

        private void UpdateAction(Vector3 heroPosition)
        {
            KarierAction action = life.ActionAt(Point(heroPosition), out int sapa);
            if (action == shownAction && sapa == shownSapa)
                return;
            shownAction = action;
            shownSapa = sapa;
            if (actionButton == null)
                return;
            bool show = action != KarierAction.None;
            if (actionButton.gameObject.activeSelf != show)
                actionButton.gameObject.SetActive(show);
            if (actionLabel == null || !show)
                return;
            switch (action)
            {
                case KarierAction.Makan:
                    actionLabel.text = "MAKAN\n" + KarierLife.Rupiah(CampaignTuning.Karier.MakanPrice);
                    break;
                case KarierAction.Sapa:
                    actionLabel.text = "SAPA\n" + (sapa >= 0 && sapa < sapaNames.Length ? sapaNames[sapa] : "WARGA");
                    break;
                case KarierAction.DaftarRT:
                    actionLabel.text = "DAFTAR\nCALON RT";
                    break;
            }
        }

        // --- HUD -----------------------------------------------------------------------------

        private void UpdateHud(NetworkPlayerCombat combat)
        {
            if (objectiveText != null)
                objectiveText.text = Objective();
            if (statusText != null)
                statusText.text = KarierLife.Rupiah(life.Duit) + "  |  ENERGI " + life.Energi + (life.Lemas ? " LEMAS" : string.Empty) +
                    "  |  RESTU " + life.Restu + "  |  CATATAN HITAM " + life.CatatanHitam;
            if (objectiveProgress != null)
                objectiveProgress.fillAmount = Mathf.Clamp01(Progress());
            if (heroText != null)
            {
                string state = combat == null ? string.Empty
                    : combat.IsKnockedOut ? "PINGSAN - dibawa warga..."
                    : "WIBAWA " + combat.Wibawa + "/" + combat.MaxWibawaValue;
                heroText.text = CampaignKarier.Avatar.Name + "  •  WARGA BIASA  •  " + state;
            }
            if (feedbackText != null && feedbackText.text.Length > 0)
                feedbackText.text = string.Empty;
            if (targetText != null)
                targetText.text = TargetList();
            if (phonePanel != null && phonePanel.activeSelf && Time.frameCount % 15 == 0)
                RefreshPhone();
        }

        private string Objective()
        {
            if (life.PoliceArrived)
                return "POLISI • Pilih: kabur, damai, atau ikut ke polsek";
            if (life.PoliceCalled)
                return "SIRENE! • Polisi menuju ke sini";
            if (life.MeleraiActive)
                return "DILERAI WARGA • Tahan dulu...";
            if (life.Rebahan)
                return "REBAHAN • scroll sosmed... (" + Mathf.CeilToInt(life.RebahanLeft) + " dtk)";
            switch (life.Step)
            {
                case KarierStep.OjolJemput:
                    return "OJOL • Jemput penumpang di " + life.PlaceName(life.OjolFrom);
                case KarierStep.OjolAntar:
                    return "OJOL • Antar ke " + life.PlaceName(life.OjolTo) + " (" +
                        (life.OjolTimeLeft >= 0f ? Mathf.CeilToInt(life.OjolTimeLeft) + " dtk)" : "TELAT!)");
                case KarierStep.KuliAmbil:
                    return "KULI • Ambil semen (" + life.KuliDelivered + "/" + CampaignTuning.Karier.KuliSacks + ")";
                case KarierStep.KuliAntar:
                    return "KULI • Bawa sak ke PROYEK (" + life.KuliDelivered + "/" + CampaignTuning.Karier.KuliSacks + ")";
                case KarierStep.BuzzerKetik:
                    return "BUZZER • Duduk di WARKOP, sebar pesan";
            }
            if (life.Registered)
                return "CALON KETUA RT • Pemilihan menyusul, tetap cari duit";
            if (life.CanDaftar)
                return "SYARAT LENGKAP • Daftar CALON RT di POS RONDA";
            return "WARGA BIASA • Tekan HP > KONOHA KERJA";
        }

        private float Progress()
        {
            if (life.Rebahan)
                return 1f - life.RebahanLeft / CampaignTuning.Karier.RebahanSeconds;
            if (life.PoliceCalled || life.MeleraiActive || life.Keributan > 0f)
                return life.Keributan;
            switch (life.Step)
            {
                case KarierStep.OjolJemput:
                    return life.StepProgress;
                case KarierStep.OjolAntar:
                    return life.StepProgress > 0f ? life.StepProgress
                        : life.OjolTimeLimit > 0f ? Mathf.Clamp01(life.OjolTimeLeft / life.OjolTimeLimit) : 0f;
                case KarierStep.KuliAmbil:
                case KarierStep.KuliAntar:
                    return (life.KuliDelivered + life.StepProgress) / CampaignTuning.Karier.KuliSacks;
                case KarierStep.BuzzerKetik:
                    return life.StepProgress;
            }
            return Mathf.Min(life.Duit / (float)CampaignTuning.Karier.SyukuranRT, life.Restu / (float)CampaignTuning.Karier.RestuSyaratRT);
        }

        private string TargetList()
        {
            bool duit = life.Registered || life.Duit >= CampaignTuning.Karier.SyukuranRT;
            bool restu = life.Restu >= CampaignTuning.Karier.RestuSyaratRT;
            string done = "<color=#F2C35A>■ ", todo = "<color=#FFFFFF>□ ", end = "</color>\n";
            string text = "<b>LEVEL 1 • WARGA BIASA</b>\nTarget: KETUA RT 03\n" +
                (duit ? done : todo) + "Syukuran " + KarierLife.Rupiah(CampaignTuning.Karier.SyukuranRT) + end +
                (restu ? done : todo) + "Restu warga " + life.Restu + "/" + CampaignTuning.Karier.RestuSyaratRT + end +
                (life.Registered ? done : todo) + "Daftar di POS RONDA" + end +
                "<color=#BBBBBB>Sapa warga " + life.SapaCount + "/" + life.Layout.SapaPoints.Length + "</color>";
            if (life.CatatanHitam >= CampaignTuning.Karier.PasalKaretFrom)
                text += "\n<color=#FF7A6A>Catatan hitam tinggi: awas pasal karet</color>";
            return text;
        }

        private void ShowHud(bool show)
        {
            SetActive(hudRoot, show);
            SetActive(phonePanel, false);
            SetActive(policePanel, false);
            SetActive(waPanel, false);
            SetActive(rebahanPanel, false);
            if (actionButton != null) actionButton.gameObject.SetActive(false);
            SetFade(0f);
        }

        private void HideHeroControls()
        {
            foreach (Button button in hiddenInKarier)
            {
                if (button == null)
                    continue;
                CanvasGroup group = button.GetComponent<CanvasGroup>();
                if (group == null)
                    group = button.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }

        // --- Messages, sounds ----------------------------------------------------------------

        private void Say(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            if (messages.Count >= 4)
                messages.Dequeue();
            messages.Enqueue(message);
        }

        private void UpdateMessages()
        {
            if (Time.time < messageUntil)
                return;
            if (messages.Count == 0)
            {
                SetActive(messagePanel, false);
                return;
            }
            string message = messages.Dequeue();
            SetActive(messagePanel, true);
            if (messageText != null)
                messageText.text = message;
            // Longer lines stay longer; a queue behind it shortens the wait.
            float seconds = Mathf.Clamp(1.6f + message.Length * 0.035f, 2.6f, 5.5f);
            messageUntil = Time.time + (messages.Count > 1 ? seconds * 0.75f : seconds);
        }

        private void UpdateRebahanFeed()
        {
            bool show = life.Rebahan;
            SetActive(rebahanPanel, show);
            if (!show || rebahanText == null || Time.time < nextFeed)
                return;
            nextFeed = Time.time + 1.7f;
            feedIndex = (feedIndex + 1) % Feed.Length;
            rebahanText.text = "REBAHAN • scroll sosmed\n\n" + Feed[feedIndex];
            Play(CampaignSound.UiClick, 0.3f);
        }

        private void OnCue(KarierCue cue)
        {
            switch (cue)
            {
                case KarierCue.Notif: Play(CampaignSound.Notif, 0.7f); break;
                case KarierCue.Koin: Play(CampaignSound.Koin, 0.9f); break;
                case KarierCue.Gagal: Play(CampaignSound.Runtuh, 0.6f); break;
                case KarierCue.Teriak: Play(CampaignSound.Ribut, 0.9f); break;
                case KarierCue.Melerai: Play(CampaignSound.Ribut, 0.6f, 1.15f); break;
                case KarierCue.Sirene: Play(CampaignSound.Sirene, 0.8f); break;
                case KarierCue.Restu: Play(CampaignSound.Restu, 0.8f); break;
                case KarierCue.Daftar: Play(CampaignSound.Victory, 0.9f); break;
            }
        }

        private static void Play(CampaignSound sound, float volume, float pitch = 1f)
        {
            if (CampaignAudio.Instance != null)
                CampaignAudio.Instance.Play(sound, volume, pitch);
        }

        private void SaveNow()
        {
            if (life == null || !started)
                return;
            string data = life.Serialize();
            if (data == lastSave)
                return;
            lastSave = data;
            PlayerPrefs.SetString(CampaignKarier.SaveKey, data);
            PlayerPrefs.Save();
        }

        private static void Caption(Button button, string text)
        {
            Text label = button != null ? button.GetComponentInChildren<Text>(true) : null;
            if (label != null)
                label.text = text;
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target != null && target.activeSelf != value)
                target.SetActive(value);
        }
    }
}
