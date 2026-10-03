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
        // 0.6.3: PARKIR, RONDA MALAM and TIDUR in the HP.
        public Button parkirButton, rondaButton, tidurButton;
        // 0.6.4 PEMILIHAN KETUA RT: SERANGAN FAJAR in the HP and the tally at the pos ronda.
        public Button fajarButton;
        public GameObject countPanel;
        public Text countTitle;
        public Text[] countLabels = new Text[0];
        public RectTransform[] countBars = new RectTransform[0];
        public float countBarWidth = 380f;
        public Button countSkipButton;
        public Button actionButton;
        public GameObject policePanel;
        public Text policeText;
        // 0.6.3: the same panel also asks after a TABRAK (title changes).
        public Text policeTitle;
        public Button kaburButton, damaiButton, polsekButton;
        // 0.6.1: "SAKSI WARGA" (shown when warga defend a hero they trust).
        public Button saksiButton;
        public GameObject waPanel;
        public Text waText;
        public Button waCloseButton;
        // 0.6.1: TERIMA / TOLAK for the TAWARAN GELAP mission.
        public Button waAcceptButton, waRejectButton;
        // 0.6.1: POLSEK cell countdown with TEBUS.
        public GameObject jailPanel;
        public Text jailText;
        public Button tebusButton;
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
        // 0.6.1: jajan stalls, rentals, the POLSEK, and street chatter.
        public Vector3 siomayPoint, salomePoint, baksoPoint, sepedaPoint, motorRentPoint;
        public Transform rentMotor;
        public Transform bike;
        public Transform plate;
        public Vector3 polsekDoor = new Vector3(16.4f, 0f, -31f);
        public Vector3 polsekCell = new Vector3(22.9f, 0f, -31f);
        public GameObject cellBars;
        public CampaignCityLife city;
        public TextMesh chatterBubble;
        // 0.6.3: solid props (KARIER only), day and night, PARKIR and RONDA places.
        public GameObject solidRoot;
        public KarierDayNight dayNight;
        // 0.6.4: roofs, trees and models are never hidden in KARIER.
        public CampaignOccluders occluders;
        public Vector3 parkirPoint = new Vector3(14f, 0f, -5f);
        public Vector3[] rondaPoints = new Vector3[0];
        public string[] rondaNames = new string[0];

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
        private enum Arrest { None, Approach, Escort, Ride, WalkIn, Cell }
        private Arrest arrest = Arrest.None;
        private float arrestTimer;
        private bool officerLeaving;
        private bool offerShown;
        private float nextChatter;
        private float chatterUntil;
        private Transform chatterTarget;
        private KarierAction shownAction = KarierAction.None;
        private int shownSapa = -1;
        private float nextFeed;
        private int feedIndex;
        private System.Random random;
        private Text actionLabel;
        private bool runtuhPending;
        // 0.6.3 TABRAK and the chase.
        private enum PolicePanelMode { Police, Tabrak }
        private PolicePanelMode panelMode = PolicePanelMode.Police;
        private bool chaseRunning;
        private float chaseReference;
        private bool celebrationShown;
        private float tidurFade;

        // 0.6.1 street chatter of the warga near the hero (owner: "lingkungannya betul-betul hidup").
        private static readonly string[] Chatter =
        {
            "Panas banget hari ini...", "Cabe naik lagi, Bu!", "Udah makan belum?", "Katanya mau ada pemilihan RT",
            "Anakku minta HP baru...", "Listrik naik, gaji tetap", "Mas, ojolnya bintang 5 ya!", "Hati-hati, preman lagi keliling",
            "Pinjol nelpon terus...", "Nanti malam ronda, jangan lupa", "Jalanan bolong belum diaspal", "Viral lagi tuh di grup"
        };

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
            Listen(parkirButton, () => Work(KarierJob.Parkir));
            Listen(rondaButton, () => Work(KarierJob.Ronda));
            Listen(tidurButton, Tidur);
            Listen(fajarButton, Fajar);
            Listen(countSkipButton, FinishCount);
            Listen(rebahanButton, Rebahan);
            Listen(actionButton, DoAction);
            Listen(kaburButton, () => Police(KarierPoliceChoice.Kabur));
            Listen(damaiButton, () => Police(KarierPoliceChoice.Damai));
            Listen(polsekButton, () => Police(KarierPoliceChoice.Polsek));
            Listen(saksiButton, () => Police(KarierPoliceChoice.Saksi));
            Listen(waCloseButton, () => SetActive(waPanel, false));
            Listen(waAcceptButton, () => AnswerOffer(true));
            Listen(waRejectButton, () => AnswerOffer(false));
            Listen(tebusButton, Tebus);
            actionLabel = actionButton != null ? actionButton.GetComponentInChildren<Text>(true) : null;
            CampaignKarier.HeroRuntuh += OnHeroRuntuh;
            ShowHud(false);
        }

        private void OnDestroy()
        {
            CampaignKarier.HeroRuntuh -= OnHeroRuntuh;
            CampaignTraversal.KarierWide = false;
            foreach (Button button in new[] { phoneButton, closePhoneButton, ojolButton, kuliButton, buzzerButton,
                rebahanButton, actionButton, kaburButton, damaiButton, polsekButton, waCloseButton, saksiButton,
                waAcceptButton, waRejectButton, tebusButton, parkirButton, rondaButton, tidurButton, fajarButton, countSkipButton })
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

        // 0.6.1: the character creator opened (KARIER chosen). Hero-only controls and the
        // Jalur Takhta texts must not show behind it (owner screenshot).
        public void EnterCreator()
        {
            HideHeroControls();
            if (objectiveText != null) objectiveText.text = "BUAT WARGAMU, lalu tekan MULAI HIDUP";
            if (statusText != null) statusText.text = string.Empty;
            if (waypointText != null) waypointText.text = string.Empty;
            if (heroText != null) heroText.text = string.Empty;
            if (feedbackText != null) feedbackText.text = string.Empty;
            if (objectiveProgress != null) objectiveProgress.fillAmount = 0f;
            CampaignGuideArrow.Hide();
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
            countTimer = -1f;
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
            if (!life.AllMissionsDone)
                Say("MISI: " + life.MissionTitle + ". " + life.MissionIntroText);
            if (cellBars != null)
                cellBars.SetActive(false);
            // 0.6.3: houses, carts, trees and poles are solid in KARIER (owner: "nggak bisa menembus apapun").
            if (solidRoot != null)
            {
                solidRoot.SetActive(true);
                Physics.SyncTransforms();
            }
            if (occluders != null)
                occluders.keepScenery = true;
            // 0.6.5: walk out to the ring road and the jalan raya.
            CampaignTraversal.KarierWide = true;
            if (dayNight != null)
                dayNight.SetClock(life.Clock);
            Say("HARI KE-" + life.Day + ", jam " + life.ClockText + ". Satu hari Konoha = 10 menit. Malam hari: TIDUR lewat HP.");
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
                SapaPoints = new KarierPoint[sapaPoints.Length],
                Siomay = Point(siomayPoint),
                Salome = Point(salomePoint),
                Bakso = Point(baksoPoint),
                SewaSepeda = Point(sepedaPoint),
                SewaMotor = Point(motorRentPoint),
                PremanSpot = Point(premanCenter),
                Plaza = Point(PlazaPoint()),
                Parkir = Point(parkirPoint),
                RondaPoints = new KarierPoint[rondaPoints.Length],
                RondaNames = (string[])rondaNames.Clone()
            };
            for (int i = 0; i < rondaPoints.Length; i++)
                layout.RondaPoints[i] = Point(rondaPoints[i]);
            for (int i = 0; i < placePoints.Length; i++)
                layout.Places[i] = Point(placePoints[i]);
            for (int i = 0; i < sapaPoints.Length; i++)
                layout.SapaPoints[i] = Point(sapaPoints[i]);
            return layout;
        }

        private static KarierPoint Point(Vector3 v) => new KarierPoint(v.x, v.z);

        private Vector3 PlazaPoint()
        {
            for (int i = 0; i < placeNames.Length && i < placePoints.Length; i++)
                if (placeNames[i] == "PLAZA")
                    return placePoints[i];
            return new Vector3(0f, 0f, -6.5f);
        }
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
            UpdateTabrak(hero, position);
            UpdateKeributan(director, position, premanAt);
            UpdateChase(position);
            UpdateWorldTime();
            UpdateCelebration();
            UpdateElection();
            UpdateArrest(hero, dt);
            UpdateOffer();
            UpdateChatter(position);
            UpdateJobVisuals();
            UpdateGuidance(position);
            UpdateAction(position);
            UpdateHud(combat);
            UpdatePhoneHint();
            UpdateMessages();
            UpdateRebahanFeed();

            if (traversal != null)
                traversal.seatLocked = life.Rebahan || life.PoliceArrived || life.Tidur || life.TabrakPending || countTimer >= 0f ||
                    arrest == Arrest.Approach || (waPanel != null && waPanel.activeSelf && offerShown);

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
                if (!fighting && !life.PoliceCalled && life.Mission != KarierLife.MisiPreman &&
                    Time.time - premanSince > CampaignTuning.Karier.PremanLeaveSeconds && director.IsServer)
                {
                    director.ServerKarierClear(false);
                    Say("Preman pergi membawa uang palakan. Warga menggerutu: \"Nggak ada yang berani...\"");
                    premanSince = -1f;
                }
                return;
            }
            premanSince = -1f;
            if (life.PoliceCalled || life.PoliceEta >= 0f || life.Rebahan || life.InJail || arrest != Arrest.None || !director.IsServer ||
                life.Tidur || life.TabrakPending)
                return;
            // 0.6.2: preman belong to the story. None before mission PAHLAWAN GANG, right away
            // during it, and only now and then afterwards.
            bool missionPreman = life.Mission == KarierLife.MisiPreman;
            if (life.Mission < KarierLife.MisiPreman)
                return;
            premanClock += dt;
            if (missionPreman ? premanClock < 3f : premanClock < nextPremanAt)
                return;
            premanClock = 0f;
            nextPremanAt = CampaignTuning.Karier.PremanEverySeconds * (0.8f + 0.4f * (float)random.NextDouble()) *
                (life.Night ? CampaignTuning.Karier.PremanNightFactor : 1f); // 0.6.3: the gang is bolder at night.
            bool boss = !missionPreman && premanEvents >= 1 && random.NextDouble() < 0.5;
            if (director.ServerKarierSpawnPreman(premanCenter, premanPoints, premanLook, boss) > 0)
            {
                premanEvents++;
                premanSince = Time.time;
                string[] victims = { "tukang sayur", "bapak-bapak pos ronda", "driver ojol", "anak sekolah" };
                if (life.Campaigning)
                    Say("TIM SUKSES JURAGAN KOS (preman bayaran) intimidasi warga di mulut gang. Usir mereka: suaramu naik!");
                else
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
                    crowd.Gather((heroPosition + premanAt) * 0.5f, "WOI! ADA YANG BERANTEM!");
                if (melerai && !lastMelerai)
                {
                    if (director.IsServer)
                        director.ServerKarierCalm(CampaignTuning.Karier.MeleraiSeconds);
                    Vector3 other = premanAt;
                    if (!NearestPreman(heroPosition, CampaignTuning.Karier.FightRadius * 2f, out other, true))
                        other = heroPosition + Vector3.forward * 2f;
                    crowd.Melerai(heroPosition, other, CampaignTuning.Karier.MeleraiSeconds);
                }
                bool tabrakLari = life.PoliceReason == KarierPoliceReason.TabrakLari;
                if (police && !lastPolice)
                {
                    if (tabrakLari)
                    {
                        // 0.6.3: a patrol nearby turns around and chases the fleeing hero.
                        crowd.StartChase(heroPosition);
                        chaseRunning = true;
                        chaseReference = 0f;
                    }
                    else
                    {
                        crowd.DispatchPolice(heroPosition);
                    }
                }
                if (police && !tabrakLari && !life.PoliceArrived && (crowd.PoliceArrived || crowd.policeMotor == null))
                {
                    // PAHLAWAN GANG: a preman the police take away after your fight counts as driven off.
                    int standing = StandingPreman();
                    if (life.Mission == KarierLife.MisiPreman && standing > 0 && life.PoliceReason == KarierPoliceReason.Keributan)
                        life.OnPremanBeaten(standing);
                    life.PoliceArrive();
                    if (director.IsServer)
                        director.ServerKarierClear(true);
                    OpenPolicePanel();
                }
                // The show is over: the crowd goes home a few seconds after things calm down.
                if (crowd.Gathered && !police && life.PoliceEta < 0f && arrest == Arrest.None && life.Keributan <= 0f &&
                    !life.TabrakPending && StandingPreman() == 0)
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
            panelMode = PolicePanelMode.Police;
            bool hoaks = life.PoliceReason == KarierPoliceReason.Hoaks;
            bool tabrak = life.PoliceReason == KarierPoliceReason.TabrakLari;
            if (policeTitle != null)
                policeTitle.text = tabrak ? "TERTANGKAP!" : "POLISI DATANG";
            SetActive(kaburButton != null ? kaburButton.gameObject : null, life.CanKabur);
            SetActive(polsekButton != null ? polsekButton.gameObject : null, true);
            if (policeText != null && tabrak)
                policeText.text = "\"Saudara " + CampaignKarier.Avatar.Name + ", Saudara menabrak warga lalu kabur.\nTABRAK LARI itu pidana. Mau damai, atau ikut ke POLSEK?\"\n\nCATATAN HITAM: " + life.CatatanHitam;
            else if (policeText != null)
                policeText.text = hoaks
                    ? "\"Saudara " + CampaignKarier.Avatar.Name + ", postingan Saudara dilaporkan.\nPasal karet: penyebaran berita bohong.\"\n\nCATATAN HITAM: " + life.CatatanHitam
                    : "\"Ada keributan di sini! Siapa yang mulai?\"\nWarga menunjuk ke arahmu sambil merekam.\n\nCATATAN HITAM: " + life.CatatanHitam;
            Caption(kaburButton, "KABUR\nCATATAN HITAM +" + CampaignTuning.Karier.KaburCatatan);
            Caption(damaiButton, "DAMAI DI TEMPAT\n" + KarierLife.Rupiah(life.DamaiPrice) + (life.CanDamai ? string.Empty : " (duit kurang)"));
            if (damaiButton != null)
                damaiButton.interactable = life.CanDamai;
            Caption(polsekButton, "IKUT KE POLSEK\ndiborgol, masuk sel");
            if (saksiButton != null)
            {
                bool saksi = life.CanSaksi;
                saksiButton.gameObject.SetActive(saksi);
                Caption(saksiButton, "SAKSI WARGA\n\"dia yang nolong!\"");
            }
            if (policeText != null && life.BeatenThisFight && !hoaks && !tabrak)
                policeText.text = "\"Siapa yang menghajar preman ini?\"\nWarga menunjuk ke arahmu sambil merekam.\n" +
                    (life.CanSaksi ? "Warga siap jadi SAKSI untukmu (RESTU tinggi)." : "RESTU kurang: warga diam saja.") +
                    "\n\nCATATAN HITAM: " + life.CatatanHitam;
        }

        private void Police(KarierPoliceChoice choice)
        {
            if (life == null)
                return;
            if (panelMode == PolicePanelMode.Tabrak)
            {
                // KABUR = drive away (the chase starts), DAMAI = tanggung jawab.
                bool done = choice == KarierPoliceChoice.Kabur ? life.KaburTabrak()
                    : choice == KarierPoliceChoice.Damai && life.TanggungJawab();
                if (done)
                {
                    CloseTabrakPanel();
                    SaveNow();
                }
                return;
            }
            if (!life.ResolvePolice(choice))
                return;
            SetActive(policePanel, false);
            chaseRunning = false;
            if (choice == KarierPoliceChoice.Polsek)
            {
                // 0.6.1 (owner): really cuffed and taken away, not "the next day...".
                arrest = Arrest.Approach;
                arrestTimer = 0f;
                officerLeaving = false;
                ClosePhone();
                Play(CampaignSound.Door, 0.6f, 1.4f);
            }
            else if (crowd != null)
            {
                crowd.PoliceLeave();
            }
            if (crowd != null)
                crowd.Disperse();
            SaveNow();
        }

        // Cuffs -> escort to the patrol motor -> ride to the POLSEK -> walk into the cell -> wait or TEBUS.
        private void UpdateArrest(NetworkObject hero, float dt)
        {
            if (arrest == Arrest.None)
                return;
            arrestTimer += dt;
            var heroMotor = hero.GetComponent<Konoha.Character.CharacterMotor>();
            Vector3 position = hero.transform.position;
            switch (arrest)
            {
                case Arrest.Approach:
                    if (crowd != null)
                        crowd.OfficerWalkTo(position);
                    if (crowd == null || crowd.OfficerArrived || arrestTimer > 6f)
                    {
                        arrest = Arrest.Escort;
                        arrestTimer = 0f;
                        Say("KLIK! Borgol terpasang. Polisi menggiringmu ke motor patroli.");
                        Play(CampaignSound.Stamp, 0.8f, 1.6f);
                    }
                    break;
                case Arrest.Escort:
                    if (crowd == null || !crowd.PoliceBusy)
                    {
                        StartWalkIn(heroMotor);
                        break;
                    }
                    Vector3 to = crowd.MotorPosition - position;
                    to.y = 0f;
                    crowd.OfficerWalkTo(position);
                    if (to.magnitude < 1.5f || arrestTimer > 9f)
                    {
                        if (traversal != null)
                        {
                            traversal.scripted = false;
                            traversal.frozen = true;
                        }
                        Vector3 motor = crowd.MotorPosition;
                        crowd.BeginRide(new[] { new Vector3(0f, 0f, motor.z), new Vector3(0f, 0f, polsekDoor.z), polsekDoor }, 7f);
                        arrest = Arrest.Ride;
                        arrestTimer = 0f;
                        Say("Dibonceng motor patroli ke POLSEK KONOHA. Warga melambai sambil live streaming.");
                    }
                    else if (traversal != null)
                    {
                        Vector3 direction = to.normalized * 0.5f;
                        traversal.scripted = true;
                        traversal.scriptedAxis = new Vector2(direction.x, direction.z);
                    }
                    break;
                case Arrest.Ride:
                    if (heroMotor != null && crowd != null)
                    {
                        heroMotor.Teleport(crowd.PassengerSeat + Vector3.up * 0.05f);
                        hero.transform.rotation = crowd.MotorRotation;
                    }
                    if (crowd == null || crowd.RideDone || arrestTimer > 25f)
                        StartWalkIn(heroMotor);
                    break;
                case Arrest.WalkIn:
                    if (crowd != null && !officerLeaving)
                        crowd.OfficerWalkTo(position);
                    if (position.x >= polsekCell.x - 0.35f || arrestTimer > 7f)
                    {
                        if (position.x < polsekCell.x - 0.35f && heroMotor != null)
                            heroMotor.Teleport(polsekCell + Vector3.up * 0.1f);
                        if (traversal != null)
                            traversal.scripted = false;
                        if (cellBars != null)
                            cellBars.SetActive(true);
                        life.EnterJail();
                        arrest = Arrest.Cell;
                        arrestTimer = 0f;
                        SetActive(jailPanel, true);
                        Play(CampaignSound.Door, 0.8f, 0.8f);
                        if (crowd != null)
                        {
                            crowd.OfficerWalkTo(polsekDoor + Vector3.left * 1.5f);
                            officerLeaving = true;
                        }
                    }
                    else if (traversal != null)
                    {
                        traversal.scripted = true;
                        traversal.scriptedAxis = new Vector2(0.5f, (polsekCell.z - position.z) * 0.5f);
                    }
                    break;
                case Arrest.Cell:
                    if (officerLeaving && crowd != null && (crowd.OfficerArrived || arrestTimer > 5f))
                    {
                        crowd.PoliceLeave();
                        officerLeaving = false;
                    }
                    if (jailText != null)
                        jailText.text = "SEL POLSEK KONOHA\nBebas dalam " + Mathf.CeilToInt(life.JailLeft) + " detik";
                    Caption(tebusButton, "TEBUS " + KarierLife.Rupiah(life.TebusPrice) + "\n(tanpa kuitansi)");
                    if (tebusButton != null)
                        tebusButton.interactable = life.Duit >= life.TebusPrice;
                    if (!life.InJail)
                    {
                        if (cellBars != null)
                            cellBars.SetActive(false);
                        SetActive(jailPanel, false);
                        if (officerLeaving && crowd != null)
                            crowd.PoliceLeave();
                        officerLeaving = false;
                        arrest = Arrest.None;
                        Say("Pintu sel terbuka. Keluar lewat pintu depan POLSEK, hidup berlanjut.");
                    }
                    break;
            }
            CampaignHumanoid body = bodies != null ? bodies.LocalBody() : null;
            if (body != null)
                body.cuffed = arrest == Arrest.Escort || arrest == Arrest.Ride || arrest == Arrest.WalkIn;
        }

        // From the POLSEK door the hero walks (escorted) into the cell; the bars close behind him.
        private void StartWalkIn(Konoha.Character.CharacterMotor heroMotor)
        {
            if (heroMotor != null)
                heroMotor.Teleport(polsekDoor + Vector3.up * 0.1f);
            NetworkObject hero = LocalHero();
            if (hero != null)
                hero.transform.rotation = Quaternion.LookRotation(polsekCell - polsekDoor);
            if (traversal != null)
            {
                traversal.frozen = false;
                traversal.scripted = true;
                traversal.scriptedAxis = new Vector2(0.5f, 0f);
            }
            if (cellBars != null)
                cellBars.SetActive(false);
            arrest = Arrest.WalkIn;
            arrestTimer = 0f;
        }

        private void Tebus()
        {
            if (life != null && life.Tebus())
                SaveNow();
        }

        // TAWARAN GELAP: a WA from an unknown number with TERIMA / TOLAK.
        private void UpdateOffer()
        {
            bool busy = life.PoliceCalled || arrest != Arrest.None || life.Rebahan || life.Tidur || life.TabrakPending ||
                (policePanel != null && policePanel.activeSelf) || (phonePanel != null && phonePanel.activeSelf);
            if (!life.OfferPending || busy)
            {
                if (offerShown && !life.OfferPending)
                    CloseOffer();
                return;
            }
            if (offerShown)
                return;
            offerShown = true;
            SetActive(waPanel, true);
            SetActive(waAcceptButton != null ? waAcceptButton.gameObject : null, true);
            SetActive(waRejectButton != null ? waRejectButton.gameObject : null, true);
            SetActive(waCloseButton != null ? waCloseButton.gameObject : null, false);
            if (waText != null)
                waText.text = "<b>+62 812-XXXX-XXXX:</b> Halo kak, mau duit cepat? Sebar pesan di bawah ke semua grup WA. " +
                    KarierLife.Rupiah(CampaignTuning.Karier.TawaranPay) + " langsung cair, tanpa ribet.\n\n" +
                    "<i>\"WASPADA! Air galon bisa bikin lupa ingatan, sebarkan sebelum dihapus!!!\"</i>\n\n" +
                    "<b>Pak RT 03 (grup):</b> Warga, hati-hati ada nomor asing nawarin duit buat sebar pesan aneh.\n\n" +
                    "TERIMA = duit cepat, CATATAN HITAM naik.  TOLAK = RESTU warga naik.";
            Play(CampaignSound.Notif, 0.8f);
        }

        private void AnswerOffer(bool accept)
        {
            if (life != null && life.AnswerOffer(accept))
                SaveNow();
            CloseOffer();
        }

        private void CloseOffer()
        {
            offerShown = false;
            SetActive(waPanel, false);
            SetActive(waAcceptButton != null ? waAcceptButton.gameObject : null, false);
            SetActive(waRejectButton != null ? waRejectButton.gameObject : null, false);
            SetActive(waCloseButton != null ? waCloseButton.gameObject : null, true);
        }

        // Now and then a warga near the hero says something (bubble above the head).
        private void UpdateChatter(Vector3 heroPosition)
        {
            if (chatterBubble == null)
                return;
            float now = Time.time;
            if (chatterTarget != null && now < chatterUntil && chatterTarget.gameObject.activeInHierarchy)
            {
                chatterBubble.transform.position = chatterTarget.position + Vector3.up * 2.2f;
                Camera view = Camera.main;
                if (view != null)
                {
                    Vector3 look = chatterBubble.transform.position - view.transform.position;
                    if (look.sqrMagnitude > 0.01f)
                        chatterBubble.transform.rotation = Quaternion.LookRotation(look);
                }
                return;
            }
            if (chatterBubble.gameObject.activeSelf)
                chatterBubble.gameObject.SetActive(false);
            chatterTarget = null;
            if (now < nextChatter || city == null || city.walkers == null || city.walkers.Length == 0)
                return;
            nextChatter = now + 5f + (float)random.NextDouble() * 6f;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                CampaignCityLife.Walker walker = city.walkers[random.Next(city.walkers.Length)];
                if (walker == null || walker.root == null || !walker.root.gameObject.activeInHierarchy)
                    continue;
                Vector3 d = walker.root.position - heroPosition;
                d.y = 0f;
                if (d.sqrMagnitude > 144f)
                    continue;
                chatterTarget = walker.root;
                chatterUntil = now + 3f;
                chatterBubble.text = Chatter[random.Next(Chatter.Length)];
                chatterBubble.gameObject.SetActive(true);
                return;
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

        // --- 0.6.3 TABRAK, chase, day and night, congratulations ---------------------------

        // Riding fast into a warga knocks them down: the kampung shouts, the hero chooses.
        private void UpdateTabrak(NetworkObject hero, Vector3 position)
        {
            if (city != null)
            {
                // Warga jump aside earlier and further from a motor or bicycle.
                bool riding = life.Riding;
                city.personalSpace = riding ? 4.6f : 2.4f;
                city.dodgeWidth = riding ? 2.3f : 1.6f;
                city.dodgeRate = riding ? 6f : 3f;
            }
            if (life.TabrakPending)
            {
                if (panelMode != PolicePanelMode.Tabrak && arrest == Arrest.None && (policePanel == null || !policePanel.activeSelf))
                    OpenTabrakPanel();
                return;
            }
            if (panelMode == PolicePanelMode.Tabrak)
                CloseTabrakPanel();
            if (city == null || !life.CanTabrak || arrest != Arrest.None)
                return;
            var controller = hero.GetComponent<CharacterController>();
            Vector3 velocity = controller != null ? controller.velocity : Vector3.zero;
            velocity.y = 0f;
            if (velocity.magnitude < CampaignTuning.Karier.TabrakSpeed)
                return;
            // A little ahead of the hero: the front wheel hits first.
            Vector3 front = position + velocity.normalized * 0.45f;
            CampaignCityLife.Walker victim = city.StandingNear(front, CampaignTuning.Karier.TabrakRadius);
            if (victim == null)
                return;
            city.KnockDown(victim, 9f);
            Vector3 at = victim.root.position;
            life.OnTabrak(Point(at));
            if (crowd != null)
                crowd.Gather(at, "TABRAK! TABRAK!\nJANGAN KABUR!");
            CampaignCameraShake.Shake(0.35f, 0.25f);
        }

        private void OpenTabrakPanel()
        {
            ClosePhone();
            panelMode = PolicePanelMode.Tabrak;
            SetActive(policePanel, true);
            if (policeTitle != null)
                policeTitle.text = "KAMU MENABRAK WARGA!";
            if (policeText != null)
                policeText.text = "Korban terkapar memegangi kaki. Warga berdatangan sambil merekam.\n\n" +
                    "TANGGUNG JAWAB: antar ke puskesmas, ganti rugi " + KarierLife.Rupiah(life.GantiRugiPrice) + ".\n" +
                    "KABUR: gas pol! Polisi mengejar, CATATAN HITAM +" + CampaignTuning.Karier.TabrakKaburCatatan + ".";
            SetActive(kaburButton != null ? kaburButton.gameObject : null, true);
            Caption(kaburButton, "KABUR\ndikejar polisi");
            Caption(damaiButton, "TANGGUNG JAWAB\n" + KarierLife.Rupiah(life.GantiRugiPrice));
            if (damaiButton != null)
                damaiButton.interactable = true;
            SetActive(polsekButton != null ? polsekButton.gameObject : null, false);
            SetActive(saksiButton != null ? saksiButton.gameObject : null, false);
        }

        private void CloseTabrakPanel()
        {
            panelMode = PolicePanelMode.Police;
            SetActive(policePanel, false);
            SetActive(polsekButton != null ? polsekButton.gameObject : null, true);
            if (policeTitle != null)
                policeTitle.text = "POLISI DATANG";
        }

        // The patrol motor follows the hero; close enough = caught, time up = escaped.
        private void UpdateChase(Vector3 position)
        {
            if (!chaseRunning)
                return;
            if (crowd == null)
            {
                chaseRunning = false;
                return;
            }
            if (life.Chasing && !life.PoliceArrived)
            {
                // A patrol motor just a little slower than the hero's ride: keep riding to get
                // away, stop or get stuck and you are caught.
                NetworkObject hero = LocalHero();
                var controller = hero != null ? hero.GetComponent<CharacterController>() : null;
                float speed = controller != null ? new Vector2(controller.velocity.x, controller.velocity.z).magnitude : 0f;
                chaseReference = Mathf.Max(chaseReference * 0.995f, speed);
                crowd.chaseSpeed = Mathf.Max(7.4f, chaseReference * 0.92f);
                crowd.ChaseTarget(position);
                if (crowd.ChaseDistance <= CampaignTuning.Karier.ChaseCatchDistance)
                {
                    crowd.ChaseCaught();
                    life.CaughtInChase();
                    chaseRunning = false;
                    OpenPolicePanel();
                }
                return;
            }
            if (!life.PoliceCalled)
            {
                // Escaped (or the hero collapsed): the patrol gives up.
                chaseRunning = false;
                crowd.PoliceLeave();
                crowd.Disperse();
            }
        }

        private void UpdateWorldTime()
        {
            if (dayNight != null)
                dayNight.SetClock(life.Clock);
            if (city != null)
                city.night = life.Night;
            // TIDUR: the screen goes dark, then light again in the morning.
            float target = life.Tidur ? 1f : 0f;
            tidurFade = Mathf.MoveTowards(tidurFade, target, Time.deltaTime * (life.Tidur ? 0.9f : 0.6f));
            if (arrest == Arrest.None)
                SetFade(tidurFade * 0.92f);
        }

        // After the last mission: a WA from Pak RT says what comes next (shown once).
        private void UpdateCelebration()
        {
            if (!life.CelebrationPending || celebrationShown)
                return;
            bool busy = arrest != Arrest.None || life.PoliceCalled || life.TabrakPending || life.Rebahan || life.Tidur ||
                offerShown || (waPanel != null && waPanel.activeSelf) || (policePanel != null && policePanel.activeSelf);
            if (busy)
                return;
            celebrationShown = true;
            ClosePhone();
            SetActive(waPanel, true);
            if (waText != null)
            {
                string name = CampaignKarier.Avatar.Name;
                waText.text =
                    "<b>Pak RT 03:</b> SELAMAT, " + name + "! Semua misi LEVEL 1 sudah selesai. Kamu resmi CALON KETUA RT.\n\n" +
                    "<b>Yang bisa kamu lakukan sekarang:</b>\n" +
                    "• <b>TUGAS HARIAN</b>: tiap hari ada 3 tugas kecil (lihat panel kiri), ada upah + bonus.\n" +
                    "• <b>Kerja lagi</b> kumpulkan tabungan: OJOL, KULI, PARKIR, RONDA MALAM.\n" +
                    "• <b>Istirahat</b>: REBAHAN kapan saja, TIDUR mulai jam 19.00 (lewat HP).\n\n" +
                    "<b>Pak RT 03:</b> PEMILIHAN KETUA RT: HARI KE-" + life.ElectionDay + ", jam 09.00-17.00 di POS RONDA. " +
                    "Lawanmu JURAGAN KOS dan PAK HAJI. Kampanye di titik warga (panah KAMPANYE).\n\n" +
                    "<b>Bu Tejo:</b> Jaga RESTU, jangan sampai viral yang jelek-jelek.";
            }
            life.AckCelebration();
            Play(CampaignSound.Victory, 0.8f);
            SaveNow();
        }

        // --- 0.6.4 PEMILIHAN KETUA RT: tally panel and the Grup WA ending ---------------------

        private float countTimer = -1f;
        private float nextCountTick;
        private bool countWa;

        private void UpdateElection()
        {
            if (countTimer < 0f)
            {
                // Wait until nothing else is on screen (the WA of a previous step, the police...).
                bool busy = arrest != Arrest.None || life.PoliceCalled || life.TabrakPending || offerShown ||
                    (waPanel != null && waPanel.activeSelf) || (policePanel != null && policePanel.activeSelf);
                if (!life.ResultPending || busy || countWa)
                    return;
                countTimer = 0f;
                ClosePhone();
                SetActive(countPanel, true);
                if (countTitle != null)
                    countTitle.text = "HITUNG SUARA • TPS POS RONDA RT 03";
                Play(CampaignSound.Seal, 0.8f);
                return;
            }
            countTimer += Time.deltaTime;
            float t = Mathf.Clamp01(countTimer / 6f);
            string[] names = { CampaignKarier.Avatar.Name + " (KAMU)", "JURAGAN KOS", "PAK HAJI" };
            int[] votes = { life.VotesKamu, life.VotesJuragan, life.VotesHaji };
            for (int i = 0; i < 3; i++)
            {
                int shown = Mathf.RoundToInt(votes[i] * t);
                if (i < countLabels.Length && countLabels[i] != null)
                    countLabels[i].text = names[i] + "   " + shown + " suara";
                if (i < countBars.Length && countBars[i] != null)
                    countBars[i].sizeDelta = new Vector2(countBarWidth * shown / Mathf.Max(1f, CampaignTuning.Karier.PemilihKK), countBars[i].sizeDelta.y);
            }
            if (t < 1f && Time.time >= nextCountTick)
            {
                nextCountTick = Time.time + 0.22f;
                Play(CampaignSound.UiClick, 0.5f, 1.3f);
            }
            if (countTimer >= 6f && countTitle != null)
                countTitle.text = life.Winner == "KAMU" ? "MENANG! " + CampaignKarier.Avatar.Name + " KETUA RT 03" : "PEMENANG: " + life.Winner;
            if (countTimer >= 8.5f)
                FinishCount();
        }

        private void FinishCount()
        {
            if (countTimer < 0f || life == null)
                return;
            countTimer = -1f;
            SetActive(countPanel, false);
            OpenElectionWa();
            life.AckResult();
            SaveNow();
        }

        private void OpenElectionWa()
        {
            SetActive(waPanel, true);
            Play(life.KetuaRT ? CampaignSound.Victory : CampaignSound.Runtuh, 0.8f);
            if (waText == null)
                return;
            string name = CampaignKarier.Avatar.Name;
            string tally = name + " " + life.VotesKamu + ", Juragan Kos " + life.VotesJuragan + ", Pak Haji " + life.VotesHaji +
                " (dari " + CampaignTuning.Karier.PemilihKK + " KK).";
            string fajar = life.FajarKetahuan
                ? "<b>Panwas RT:</b> Video amplop subuh tadi sudah kami terima. \"Akan dikaji.\" (tidak pernah dikaji)\n\n"
                : life.Fajar ? "<b>Bu Tejo:</b> Ada yang dapat amplop dua kali pagi ini, katanya. Siapa ya?\n\n" : string.Empty;
            if (life.KetuaRT)
                waText.text =
                    "<b>Pak RT 03 (lama):</b> Hasil hitung: " + tally + " Selamat kepada Bpk/Ibu " + name + ", KETUA RT 03 yang baru.\n\n" +
                    "<b>Pak Haji:</b> Selamat, semoga amanah. Iuran sampah tetap Rp 20.000 ya.\n\n" +
                    "<b>Juragan Kos:</b> Selamat... sembako sisa 150 paket, ada yang mau beli?\n\n" + fajar +
                    "<b>Admin:</b> <i>LEVEL 1 TAMAT. Level 2 (Kepala Desa) hadir di versi berikutnya. Kamu tetap bisa kerja dan tugas harian. Progresmu tersimpan.</i>";
            else
                waText.text =
                    "<b>Pak RT 03:</b> Hasil hitung: " + tally + " Selamat kepada " + life.Winner + ".\n\n" +
                    "<b>Warga:</b> Sembakonya enak sih...\n\n" + fajar +
                    "<b>Pak RT 03:</b> Ada protes kotak suara tertukar dengan kotak nasi. PEMILIHAN ULANG " +
                    CampaignTuning.Karier.PemiluUlangHari + " hari lagi. Kampanye lagi, jaga RESTU, kurangi CATATAN HITAM.";
            countWa = false;
        }

        private void Fajar()
        {
            if (life == null)
                return;
            if (life.SeranganFajar(out string reason))
            {
                ClosePhone();
                SaveNow();
            }
            else
            {
                Say(reason);
                RefreshPhone();
            }
        }

        private void Tidur()
        {
            if (life == null)
                return;
            if (life.StartTidur(out string reason))
            {
                ClosePhone();
                SaveNow();
            }
            else
            {
                Say(reason);
                RefreshPhone();
            }
        }

        // --- Jobs, phone, actions -------------------------------------------------------------

        private void TogglePhone()
        {
            if (life == null || phonePanel == null)
                return;
            bool open = !phonePanel.activeSelf;
            if (open && (life.PoliceArrived || arrest != Arrest.None || offerShown || life.Tidur || life.TabrakPending || life.Chasing))
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
            // 0.6.3: short captions (two columns) and opening hours.
            JobButton(ojolButton, KarierJob.Ojol, "OJOL", life.Night ? "Rp 25-75rb • tarif malam" : "Rp 25-60rb per antar");
            JobButton(kuliButton, KarierJob.Kuli, "KULI BANGUNAN", life.JobOpen(KarierJob.Kuli)
                ? KarierLife.Rupiah(CampaignTuning.Karier.KuliWage - CampaignTuning.Karier.KuliMandorCut) + " • capek banget"
                : "tutup • buka 07.00-17.00");
            JobButton(parkirButton, KarierJob.Parkir, "PARKIR", life.JobOpen(KarierJob.Parkir)
                ? "Rp 2rb per motor • liar" : "tutup • buka 08.00-21.00");
            JobButton(rondaButton, KarierJob.Ronda, "RONDA MALAM", life.JobOpen(KarierJob.Ronda)
                ? KarierLife.Rupiah(CampaignTuning.Karier.RondaPay) + " • RESTU +" + CampaignTuning.Karier.RondaRestu
                : "mulai jam 21.00-03.00");
            string buzzer = life.BuzzerCooldown > 0f
                ? "tunggu order " + Mathf.CeilToInt(life.BuzzerCooldown) + " dtk"
                : KarierLife.Rupiah(CampaignTuning.Karier.BuzzerPay) + " • HITAM +" + CampaignTuning.Karier.BuzzerCatatan;
            JobButton(buzzerButton, KarierJob.Buzzer, "BUZZER HOAKS", buzzer);
            Caption(rebahanButton, "REBAHAN\nENERGI +" + CampaignTuning.Karier.RebahanEnergi + " • 2 jam");
            if (rebahanButton != null)
                rebahanButton.interactable = !busy && !life.Rebahan;
            Caption(tidurButton, life.CanTidurNow ? "TIDUR SAMPAI PAGI\nENERGI penuh • hari baru" : "TIDUR\nbisa jam 19.00-04.00");
            if (tidurButton != null)
                tidurButton.interactable = !busy && life.CanTidurNow;
            // 0.6.4: envelopes before dawn on election day (dirty, optional).
            Caption(fajarButton, life.CanFajarNow
                ? "SERANGAN FAJAR\n" + (CampaignTuning.Karier.FajarBiaya / 1000) + "rb • HITAM +" + CampaignTuning.Karier.FajarCatatan
                : "SERANGAN FAJAR\nhari H 04.00-09.00");
            if (fajarButton != null)
                fajarButton.interactable = !busy && life.CanFajarNow && life.Duit >= CampaignTuning.Karier.FajarBiaya;
            if (phoneInfo != null)
                phoneInfo.text = "HARI " + life.Day + " • " + life.ClockText + " " + life.DayPart + "   " + KarierLife.Rupiah(life.Duit) +
                    "   ENERGI " + life.Energi + (life.Lemas ? " (LEMAS)" : string.Empty) + "\n" +
                    (busy ? "Sedang kerja: " + JobName(life.Job) + ". Tekan lagi untuk berhenti." : "Pilih kerja. Duit bersih lambat, duit kotor cepat.");
        }

        private void JobButton(Button button, KarierJob job, string title, string detail)
        {
            if (button == null)
                return;
            bool current = life.Job == job;
            Caption(button, current ? "BERHENTI " + title + "\n(sedang jalan)" : title + "\n" + detail);
            button.interactable = current || (!life.Busy && life.JobOpen(job));
        }

        private static string JobName(KarierJob job)
        {
            switch (job)
            {
                case KarierJob.Ojol: return "OJOL";
                case KarierJob.Kuli: return "KULI";
                case KarierJob.Buzzer: return "BUZZER";
                case KarierJob.Parkir: return "PARKIR";
                case KarierJob.Ronda: return "RONDA";
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
                "<b>Pak RT 03:</b> Pemilihan Ketua RT dua hari lagi di POS RONDA. Silakan kampanye, asal jangan bagi amplop.";
        }

        // --- Visuals -------------------------------------------------------------------------

        private void UpdateJobVisuals()
        {
            NetworkObject hero = LocalHero();
            bool ojol = life.Job == KarierJob.Ojol;
            bool motorRent = !ojol && life.Ride == KarierRide.Motor;
            bool sepeda = !ojol && life.Ride == KarierRide.Sepeda;
            bool carrying = life.Carrying;
            bool onPoliceMotor = arrest == Arrest.Ride;
            Attach(ojolMotor, hero, ojol, Vector3.zero, Quaternion.identity);
            Attach(rentMotor, hero, motorRent, Vector3.zero, Quaternion.identity);
            Attach(bike, hero, sepeda, Vector3.zero, Quaternion.identity);
            SetActive(ojolPassenger, ojol && life.Step == KarierStep.OjolAntar);
            Attach(sack, hero, carrying, new Vector3(0.24f, 1.78f, -0.05f), Quaternion.Euler(0f, 0f, -12f));

            CampaignHumanoid body = bodies != null ? bodies.LocalBody() : null;
            if (body != null)
            {
                body.riding = ojol || motorRent || onPoliceMotor;
                body.pedaling = sepeda && !onPoliceMotor;
                body.carrying = carrying && !ojol;
                body.eating = life.Eating;
                if (plate != null)
                {
                    bool eat = life.Eating && body.elbowRight != null && !body.riding && !body.pedaling;
                    if (eat && plate.parent != body.elbowRight)
                    {
                        plate.SetParent(body.elbowRight, false);
                        plate.localPosition = new Vector3(0f, -0.34f, 0.06f);
                        plate.localRotation = Quaternion.identity;
                    }
                    if (plate.gameObject.activeSelf != eat)
                        plate.gameObject.SetActive(eat);
                }
            }
            if (traversal != null)
            {
                float speed = ojol ? CampaignTuning.Karier.OjolSpeed
                    : motorRent ? CampaignTuning.Karier.MotorSpeed
                    : sepeda ? CampaignTuning.Karier.SepedaSpeed
                    : carrying ? CampaignTuning.Karier.KuliCarrySpeed : 1f;
                if (life.Lemas && !ojol && !motorRent)
                    speed *= CampaignTuning.Karier.LemasSpeed;
                traversal.speedBonus = speed;
            }
        }

        // Shows a prop on the hero (parented once) or hides it.
        private static void Attach(Transform prop, NetworkObject hero, bool show, Vector3 localPosition, Quaternion localRotation)
        {
            if (prop == null)
                return;
            if (show && hero != null && prop.parent != hero.transform)
            {
                prop.SetParent(hero.transform, false);
                prop.localPosition = localPosition;
                prop.localRotation = localRotation;
            }
            if (prop.gameObject.activeSelf != show)
                prop.gameObject.SetActive(show);
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
            if (arrest != Arrest.None)
                action = KarierAction.None;
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
                case KarierAction.BeliSiomay:
                    actionLabel.text = "SIOMAY\n" + KarierLife.Rupiah(CampaignTuning.Karier.SiomayPrice);
                    break;
                case KarierAction.BeliSalome:
                    actionLabel.text = "SALOME\n" + KarierLife.Rupiah(CampaignTuning.Karier.SalomePrice);
                    break;
                case KarierAction.BeliBakso:
                    actionLabel.text = "BAKSO\n" + KarierLife.Rupiah(CampaignTuning.Karier.BaksoPrice);
                    break;
                case KarierAction.SewaSepeda:
                    actionLabel.text = "SEWA\nSEPEDA " + (CampaignTuning.Karier.SewaSepedaPrice / 1000) + "rb";
                    break;
                case KarierAction.SewaMotor:
                    actionLabel.text = "SEWA\nMOTOR " + (CampaignTuning.Karier.SewaMotorPrice / 1000) + "rb";
                    break;
                case KarierAction.Turun:
                    actionLabel.text = "TURUN";
                    break;
                case KarierAction.Sapa:
                    actionLabel.text = "SAPA\n" + (sapa >= 0 && sapa < sapaNames.Length ? sapaNames[sapa] : "WARGA");
                    break;
                case KarierAction.DaftarRT:
                    actionLabel.text = "DAFTAR\nCALON RT";
                    break;
                case KarierAction.Kampanye:
                    actionLabel.text = "KAMPANYE\n" + (CampaignTuning.Karier.KampanyeBiaya / 1000) + "rb";
                    break;
                case KarierAction.Coblos:
                    actionLabel.text = "COBLOS";
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
                heroText.text = CampaignKarier.Avatar.Name + (life.KetuaRT ? "  •  KETUA RT 03" : string.Empty) + "  •  HARI " + life.Day + ", " +
                    life.ClockText + " " + life.DayPart + "  •  " + state;
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
            switch (arrest)
            {
                case Arrest.Approach:
                case Arrest.Escort:
                    return "DITANGKAP • Diborgol, digiring ke motor patroli";
                case Arrest.Ride:
                    return "DIBAWA KE POLSEK • Warga sibuk merekam";
                case Arrest.WalkIn:
                    return "POLSEK • Masuk sel";
                case Arrest.Cell:
                    return "DI SEL • Tunggu " + Mathf.CeilToInt(life.JailLeft) + " dtk atau TEBUS";
            }
            if (life.TabrakPending)
                return "MENABRAK WARGA • Tanggung jawab, atau kabur?";
            if (life.Chasing)
                return "DIKEJAR POLISI! • Lolos " + Mathf.CeilToInt(life.ChaseLeft) + " dtk lagi";
            if (life.Tidur)
                return "TIDUR • Zzz... sampai jam 06.00";
            if (life.PoliceArrived)
                return life.PoliceReason == KarierPoliceReason.TabrakLari
                    ? "TERTANGKAP • Pilih: damai atau ikut ke polsek"
                    : "POLISI • Pilih: kabur, damai, atau ikut ke polsek";
            if (life.PoliceCalled)
                return "SIRENE! • Polisi menuju ke sini";
            if (life.PoliceEta > 0f)
                return "ADA YANG LAPOR POLISI • datang " + Mathf.CeilToInt(life.PoliceEta) + " dtk lagi";
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
                case KarierStep.ParkirJaga:
                    return "PARKIR • Jaga depan KANTOR KELURAHAN (" + life.ParkirMotorsDone + "/" + CampaignTuning.Karier.ParkirMotors + " motor)";
                case KarierStep.RondaKeliling:
                    return "RONDA • Keliling titik " + (life.RondaIndex + 1) + "/" + rondaPoints.Length;
                case KarierStep.RondaPulang:
                    return "RONDA • Kembali lapor ke POS RONDA";
            }
            if (life.CanDaftar)
                return "SYARAT LENGKAP • Daftar CALON RT di POS RONDA";
            if (life.Ride != KarierRide.None)
                return (life.Ride == KarierRide.Motor ? "NAIK MOTOR" : "NAIK SEPEDA") + " • tekan TURUN untuk mengembalikan";
            if (!life.AllMissionsDone)
                return "MISI " + (life.Mission + 1) + " • " + life.MissionProgress;
            // 0.6.3: after the missions, the daily tasks say what to do next.
            return "TUGAS HARI " + life.Day + " • " + life.MissionProgress;
        }

        private float Progress()
        {
            if (life.Chasing)
                return Mathf.Clamp01(life.ChaseLeft / CampaignTuning.Karier.ChaseSeconds);
            if (life.Tidur)
                return 1f - life.TidurLeft / CampaignTuning.Karier.TidurSeconds;
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
                case KarierStep.ParkirJaga:
                    return life.Step == KarierStep.ParkirJaga
                        ? (life.ParkirMotorsDone + life.StepProgress) / CampaignTuning.Karier.ParkirMotors
                        : life.StepProgress;
                case KarierStep.RondaKeliling:
                case KarierStep.RondaPulang:
                    return rondaPoints.Length == 0 ? 0f : (life.RondaIndex + life.StepProgress) / (rondaPoints.Length + 1f);
            }
            return Mathf.Min(life.Duit / (float)CampaignTuning.Karier.SyukuranRT, life.Restu / (float)CampaignTuning.Karier.RestuSyaratRT);
        }

        private string TargetList()
        {
            bool duit = life.Registered || life.Duit >= CampaignTuning.Karier.SyukuranRT;
            bool restu = life.Restu >= CampaignTuning.Karier.RestuSyaratRT;
            string done = "<color=#F2C35A>■ ", todo = "<color=#FFFFFF>□ ", end = "</color>\n";
            if (life.AllMissionsDone)
            {
                // 0.6.3: the daily tasks take over the panel after the last mission.
                string tugas = "<b>" + (life.KetuaRT ? "KETUA RT 03 • HARI " : life.Campaigning ? "PEMILIHAN RT • HARI " : "TUGAS HARIAN • HARI ") +
                    life.Day + "</b>\n<color=#BFE8C8>" + life.MissionProgress + "</color>\n";
                if (life.Campaigning)
                {
                    // 0.6.4: the survey of the Grup WA (an estimate, not the result).
                    life.Survey(out int kamu, out int juragan, out int haji);
                    tugas += "<color=#F2C35A>Survei WA: KAMU " + kamu + "% • JURAGAN " + juragan + "% • HAJI " + haji + "%</color>\n" +
                        "<color=#FFFFFF>Pemilihan HARI " + life.ElectionDay + " 09.00 • kampanye hari ini " + life.KampanyeToday + "/" +
                        life.Layout.SapaPoints.Length + "</color>\n";
                }
                for (int i = 0; i < KarierLife.TugasCount; i++)
                    tugas += (life.TugasDone(i) ? done : todo) + life.TugasName(i) + " " + life.TugasProgress(i) + "/" + life.TugasNeed(i) + end;
                tugas += "<color=#BBBBBB>" + (CampaignTuning.Karier.TugasReward / 1000) + "rb per tugas • bonus " +
                    (CampaignTuning.Karier.TugasBonus / 1000) + "rb semua beres" + (life.KetuaRT ? " • LEVEL 1 TAMAT" : string.Empty) + "</color>";
                if (life.CatatanHitam >= CampaignTuning.Karier.PasalKaretFrom)
                    tugas += "\n<color=#FF7A6A>Catatan hitam tinggi: awas pasal karet</color>";
                return tugas;
            }
            string text = "<b>MISI " + Mathf.Min(life.Mission + 1, KarierLife.MissionTitles.Length) + "/" + KarierLife.MissionTitles.Length +
                ": " + life.MissionTitle + "</b>\n<color=#BFE8C8>" + life.MissionProgress + "</color>\n" +
                "<b>LEVEL 1 • target KETUA RT 03</b>\n" +
                (duit ? done : todo) + "Syukuran " + KarierLife.Rupiah(CampaignTuning.Karier.SyukuranRT) + end +
                (restu ? done : todo) + "Restu warga " + life.Restu + "/" + CampaignTuning.Karier.RestuSyaratRT + end +
                (life.Registered ? done : todo) + "Daftar di POS RONDA" + end +
                "<color=#BBBBBB>Sapa warga " + life.SapaCount + "/" + life.Layout.SapaPoints.Length + "</color>";
            if (life.CatatanHitam >= CampaignTuning.Karier.PasalKaretFrom)
                text += "\n<color=#FF7A6A>Catatan hitam tinggi: awas pasal karet</color>";
            return text;
        }

        // 0.6.2: the HP button blinks when the current mission step is done in the HP.
        private Color phoneBase;
        private Text phoneLabel;
        private bool phoneBaseKnown;

        private void UpdatePhoneHint()
        {
            if (phoneButton == null || phoneButton.targetGraphic == null)
                return;
            if (!phoneBaseKnown)
            {
                phoneBase = phoneButton.targetGraphic.color;
                phoneBaseKnown = true;
            }
            bool hint = (phonePanel == null || !phonePanel.activeSelf) && arrest == Arrest.None && !life.InJail &&
                !life.Rebahan && !life.PoliceCalled && !life.Tidur && !life.TabrakPending && life.MissionNeedsPhone;
            phoneButton.targetGraphic.color = hint
                ? Color.Lerp(phoneBase, new Color(1f, 0.82f, 0.30f, 1f), 0.5f + 0.5f * Mathf.Sin(Time.time * 6f))
                : phoneBase;
            if (phoneLabel == null)
                phoneLabel = phoneButton.GetComponentInChildren<Text>(true);
            Text label = phoneLabel;
            if (label != null)
            {
                string text = hint ? "HP • KERJA  <" : "HP • KERJA";
                if (label.text != text)
                    label.text = text;
            }
        }

        private void ShowHud(bool show)
        {
            SetActive(hudRoot, show);
            SetActive(phonePanel, false);
            SetActive(policePanel, false);
            SetActive(waPanel, false);
            SetActive(rebahanPanel, false);
            SetActive(jailPanel, false);
            SetActive(waAcceptButton != null ? waAcceptButton.gameObject : null, false);
            SetActive(waRejectButton != null ? waRejectButton.gameObject : null, false);
            SetActive(saksiButton != null ? saksiButton.gameObject : null, false);
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
                case KarierCue.Misi: Play(CampaignSound.Seal, 0.8f); break;
                case KarierCue.Tabrak:
                    Play(CampaignSound.Runtuh, 0.9f, 0.8f);
                    Play(CampaignSound.Ribut, 0.9f);
                    break;
                case KarierCue.Peluit: Play(CampaignSound.Notif, 0.8f, 1.9f); break;
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
