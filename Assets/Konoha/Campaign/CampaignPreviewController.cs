using Konoha.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // Offline campaign slice. The abilities are lightweight prototype rules and do not
    // share the PvP hero kit or network authority.
    public sealed class CampaignPreviewController : MonoBehaviour
    {
        public CharacterMotor player;
        public Transform plaza;
        public Transform majelis;
        public Transform biro;
        public Transform garda;
        public Transform chair;
        public GameObject chairBarrier;
        public GameObject guardVisual;
        public Renderer guardRenderer;
        public Transform heroMarker;
        public GameObject[] heroVisuals;
        public Text objectiveText;
        public Text statusText;
        public Text waypointText;
        public Text heroText;
        public Text feedbackText;
        public Image objectiveProgress;
        public Button attackButton;
        public Button sitButton;
        public Button heroButton;
        public Button skillButton;

        private CampaignRunState run = new CampaignRunState(2, 35);
        private readonly float[] skillReadyAt = new float[4];
        private int heroIndex;
        private int health = 100;
        private int guardHealth;
        private int biroSteps;
        private float majelisHold;
        private float nextBiroStep;
        private float nextGuardHit;
        private float nextBasic;
        private float powerClock;
        private float counterattackClock;
        private float guardStunnedUntil;
        private float shieldUntil;
        private float speedUntil;
        private bool counterattackStarted;
        private bool guardActive;
        private string feedback;
        private float feedbackUntil;
        private float guardFlashUntil;
        private bool enteredMajelis;
        private MaterialPropertyBlock ringBlock;
        private MaterialPropertyBlock guardBlock;

        private void Start()
        {
            attackButton.onClick.AddListener(BasicAttack);
            sitButton.onClick.AddListener(Sit);
            heroButton.onClick.AddListener(NextHero);
            skillButton.onClick.AddListener(UseSkill);
            guardVisual.SetActive(false);
            ringBlock = new MaterialPropertyBlock();
            guardBlock = new MaterialPropertyBlock();
            ShowHero();
            RefreshHud();
        }

        private void OnDestroy()
        {
            if (attackButton != null) attackButton.onClick.RemoveListener(BasicAttack);
            if (sitButton != null) sitButton.onClick.RemoveListener(Sit);
            if (heroButton != null) heroButton.onClick.RemoveListener(NextHero);
            if (skillButton != null) skillButton.onClick.RemoveListener(UseSkill);
        }

        private void Update()
        {
            if (player == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            var position = player.transform.position;
            player.speedMultiplier = Time.time < speedUntil ? 1.55f : 1f;

            if (run.Phase == CampaignPhase.GerbangRakyat && Near(position, plaza.position, 2.4f))
                run.ReachPlaza();

            if (run.Phase == CampaignPhase.PlazaAspirasi)
            {
                // Majelis: listen to the people; Biro: confirm three separate files.
                if (!run.HasSeal(CampaignSector.MajelisDaun))
                {
                    bool insideMajelis = Near(position, majelis.position, 2.8f);
                    if (insideMajelis && !enteredMajelis)
                        Notify("Tetap di lingkaran Majelis sampai bar penuh");
                    enteredMajelis = insideMajelis;
                    majelisHold = insideMajelis
                        ? Mathf.Min(2.5f, majelisHold + dt)
                        : Mathf.Max(0f, majelisHold - dt * 0.25f);
                    if (majelisHold >= 2.5f && run.AwardSeal(CampaignSector.MajelisDaun))
                        Notify("Segel Majelis Daun diperoleh");
                }
                run.TryOpenInnerGate();
            }

            if (run.Phase == CampaignPhase.GerbangDalam && Near(position, garda.position, 2.6f))
            {
                if (run.TryStartGuard()) SpawnGuard(garda.position, 100);
            }

            if (run.Phase == CampaignPhase.Memerintah)
            {
                if (!Near(position, chair.position, 2.2f))
                {
                    run.LoseSeat();
                    powerClock = 0f;
                }
                else
                {
                    powerClock += dt;
                    while (powerClock >= 1f && run.Phase == CampaignPhase.Memerintah)
                    {
                        powerClock -= 1f;
                        run.GainPower(5);
                    }
                    if (!counterattackStarted)
                    {
                        counterattackClock += dt;
                        if (counterattackClock >= 2.5f && run.Phase != CampaignPhase.Menang)
                        {
                            counterattackStarted = true;
                            SpawnGuard(garda.position, 75);
                        }
                    }
                }
            }

            if (guardActive && run.Phase != CampaignPhase.Menang)
                MoveAndAttackGuard(dt);
            chairBarrier.SetActive(run.Phase < CampaignPhase.KursiTerbuka);
            if (run.Phase == CampaignPhase.Menang && guardActive)
            {
                guardActive = false;
                guardVisual.SetActive(false);
            }
            RefreshWorldFeedback();
            RefreshHud();
        }

        private void SpawnGuard(Vector3 position, int hp)
        {
            guardVisual.transform.position = new Vector3(position.x, 0f, position.z);
            guardVisual.SetActive(true);
            guardHealth = hp;
            guardActive = true;
            guardStunnedUntil = 0f;
            nextGuardHit = Time.time + 1.5f;
        }

        private void MoveAndAttackGuard(float dt)
        {
            if (Time.time < guardStunnedUntil) return;
            Vector3 target = player.transform.position;
            target.y = guardVisual.transform.position.y;
            float distance = Vector3.Distance(guardVisual.transform.position, target);
            // The first guard holds the final approach; it does not cross the locked
            // chair enclosure to chase a player who has not reached this sector.
            if (run.Phase == CampaignPhase.GardaTakhta && distance > 6f) return;
            if (distance > 1.5f)
                guardVisual.transform.position = Vector3.MoveTowards(guardVisual.transform.position,
                    target, 2.1f * dt);
            else if (Time.time >= nextGuardHit)
            {
                health = Mathf.Max(0, health - (Time.time < shieldUntil ? 8 : 18));
                nextGuardHit = Time.time + 1.3f;
                if (health == 0)
                {
                    run.LoseSeat();
                    player.transform.position = new Vector3(0f, 0.1f, -8.5f);
                    health = 100;
                    Notify("Tumbang! Kembali ke Gerbang Rakyat");
                    if (run.Phase == CampaignPhase.GardaTakhta)
                        SpawnGuard(garda.position, 100);
                }
            }
        }

        private void BasicAttack()
        {
            if (!guardActive || Time.time < nextBasic || !Near(player.transform.position,
                    guardVisual.transform.position, 3f)) return;
            nextBasic = Time.time + 0.55f;
            HitGuard(25);
        }

        private void HitGuard(int damage)
        {
            if (!guardActive) return;
            guardHealth -= damage;
            guardFlashUntil = Time.time + 0.22f;
            Notify("Garda terkena " + damage + "  •  sisa " + Mathf.Max(0, guardHealth) + " HP");
            if (guardHealth > 0) return;
            guardActive = false;
            guardVisual.SetActive(false);
            if (run.Phase == CampaignPhase.GardaTakhta)
            {
                run.DefeatGuard();
                Notify("Garda tumbang! Kursi terbuka");
            }
        }

        private void Sit()
        {
            if (run.Phase == CampaignPhase.Menang)
            {
                Restart();
                return;
            }
            if (run.Phase == CampaignPhase.PlazaAspirasi &&
                !run.HasSeal(CampaignSector.BiroProsedur) &&
                Near(player.transform.position, biro.position, 2.8f) && Time.time >= nextBiroStep)
            {
                nextBiroStep = Time.time + 0.65f;
                biroSteps++;
                if (biroSteps >= 3 && run.AwardSeal(CampaignSector.BiroProsedur))
                    Notify("Segel Biro Prosedur diperoleh");
                else Notify("Berkas disahkan: " + biroSteps + "/3");
                return;
            }
            if (run.Phase == CampaignPhase.KursiTerbuka &&
                Near(player.transform.position, chair.position, 2.2f))
                if (run.TrySit()) Notify("Bertahan di Kursi untuk mengumpulkan Kuasa");
        }

        private void UseSkill()
        {
            if (run.Phase == CampaignPhase.Menang || Time.time < skillReadyAt[heroIndex]) return;
            switch (heroIndex)
            {
                case 0: // Mega: close range protection and area control.
                    shieldUntil = Time.time + 5f;
                    if (GuardWithin(3.5f)) HitGuard(30);
                    Notify("Mega: perisai rakyat aktif 5 detik");
                    skillReadyAt[0] = Time.time + 11f;
                    break;
                case 1: // Gemoy: powerful command hit with longer reach.
                    if (!GuardWithin(4.5f)) return;
                    HitGuard(50);
                    Notify("Gemoy: komando maju!");
                    skillReadyAt[1] = Time.time + 9f;
                    break;
                case 2: // Abah: mend wounds and halt the opponent briefly.
                    health = Mathf.Min(100, health + 40);
                    if (GuardWithin(4f)) guardStunnedUntil = Time.time + 2.5f;
                    Notify("Abah: pulih 40 HP dan Garda tertahan");
                    skillReadyAt[2] = Time.time + 11f;
                    break;
                default: // Pak Wi: navigate the two routes and dodge pursuit.
                    speedUntil = Time.time + 5f;
                    Notify("Pak Wi: jalan baru, bergerak lebih cepat");
                    skillReadyAt[3] = Time.time + 9f;
                    break;
            }
        }

        private bool GuardWithin(float range) => guardActive &&
            Near(player.transform.position, guardVisual.transform.position, range);

        private void NextHero()
        {
            if (heroVisuals == null || heroVisuals.Length == 0) return;
            heroIndex = (heroIndex + 1) % heroVisuals.Length;
            speedUntil = 0f;
            shieldUntil = 0f;
            ShowHero();
        }

        private void ShowHero()
        {
            if (heroVisuals == null) return;
            for (int i = 0; i < heroVisuals.Length; i++)
                if (heroVisuals[i] != null) heroVisuals[i].SetActive(i == heroIndex);
            var body = player.transform.Find("PlaceholderSilhouette");
            if (body != null)
            {
                Renderer renderer = body.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color[] colors = {
                        new Color(0.19f, 0.08f, 0.10f), new Color(0.83f, 0.78f, 0.68f),
                        new Color(0.08f, 0.28f, 0.22f), new Color(0.85f, 0.78f, 0.67f)
                    };
                    var block = new MaterialPropertyBlock();
                    block.SetColor("_BaseColor", colors[heroIndex]);
                    renderer.SetPropertyBlock(block);
                    body.localScale = heroIndex == 1 ? new Vector3(1.12f, 1.06f, 1.12f)
                        : heroIndex == 2 ? new Vector3(0.94f, 1f, 0.94f) : Vector3.one;
                }
            }
        }

        private void RefreshWorldFeedback()
        {
            if (heroMarker != null && ringBlock != null)
            {
                Color[] colors = {
                    new Color(0.88f, 0.24f, 0.28f), new Color(0.94f, 0.68f, 0.30f),
                    new Color(0.27f, 0.78f, 0.52f), new Color(0.94f, 0.55f, 0.30f)
                };
                ringBlock.SetColor("_BaseColor", colors[heroIndex]);
                heroMarker.GetComponent<Renderer>().SetPropertyBlock(ringBlock);
                float pulse = 1.65f + 0.06f * Mathf.Sin(Time.time * 3f);
                heroMarker.localScale = new Vector3(pulse, 0.012f, pulse);
            }
            if (guardActive && guardRenderer != null && guardBlock != null)
            {
                guardBlock.SetColor("_BaseColor", Time.time < guardFlashUntil
                    ? new Color(1f, 0.82f, 0.38f) : new Color(0.17f, 0.22f, 0.32f));
                guardRenderer.SetPropertyBlock(guardBlock);
            }
        }

        private void Restart()
        {
            run = new CampaignRunState(2, 35);
            player.transform.position = new Vector3(0f, 0.1f, -9f);
            health = 100;
            biroSteps = 0;
            majelisHold = 0f;
            enteredMajelis = false;
            nextBiroStep = 0f;
            nextBasic = 0f;
            powerClock = 0f;
            counterattackClock = 0f;
            counterattackStarted = false;
            guardActive = false;
            guardVisual.SetActive(false);
            Notify("Perjalanan baru dimulai");
            shieldUntil = speedUntil = 0f;
            for (int i = 0; i < skillReadyAt.Length; i++) skillReadyAt[i] = 0f;
        }

        private void Notify(string message)
        {
            feedback = message;
            feedbackUntil = Time.time + 2.2f;
        }

        private void RefreshHud()
        {
            string[] names = { "MEGA", "GEMOY", "ABAH", "PAK WI" };
            heroText.text = "HERO: " + names[Mathf.Clamp(heroIndex, 0, names.Length - 1)] + "  |  HP " + health;
            if (feedbackText != null)
                feedbackText.text = Time.time < feedbackUntil ? feedback : string.Empty;
            statusText.text = "SEGEL " + run.SealCount + "/" + run.RequiredSeals +
                "  |  KUASA " + run.Power + "/" + run.TargetPower +
                (guardActive ? "  |  GARDA " + guardHealth + " HP" : "");
            switch (run.Phase)
            {
                case CampaignPhase.GerbangRakyat:
                    objectiveText.text = "GERBANG RAKYAT  •  Menuju PLAZA di depan"; break;
                case CampaignPhase.PlazaAspirasi:
                    objectiveText.text = !run.HasSeal(CampaignSector.MajelisDaun)
                        ? "1/2  MAJELIS: tahan di lingkaran " + Mathf.CeilToInt(Mathf.Max(0f, 2.5f - majelisHold)) + " detik"
                        : "2/2  BIRO: masuk lingkaran, tekan SAHKAN " + (biroSteps + 1) + "/3";
                    break;
                case CampaignPhase.GerbangDalam:
                    objectiveText.text = "GERBANG DALAM TERBUKA  >  Hadapi GARDA TAKHTA"; break;
                case CampaignPhase.GardaTakhta:
                    objectiveText.text = "GARDA TAKHTA  >  Dekati dan tekan BASIC"; break;
                case CampaignPhase.KursiTerbuka:
                    objectiveText.text = "KURSI TERBUKA  >  Dekati dan tekan DUDUK"; break;
                case CampaignPhase.Memerintah:
                    objectiveText.text = "PERTAHANKAN TAKHTA  >  Tetap di dekat Kursi"; break;
                default:
                    objectiveText.text = "JALUR TAKHTA SELESAI! Tekan ULANG untuk bermain lagi"; break;
            }
            bool atBiro = run.Phase == CampaignPhase.PlazaAspirasi &&
                !run.HasSeal(CampaignSector.BiroProsedur) &&
                Near(player.transform.position, biro.position, 2.8f);
            bool canConfirm = atBiro && Time.time >= nextBiroStep;
            bool canSit = run.Phase == CampaignPhase.KursiTerbuka &&
                Near(player.transform.position, chair.position, 2.2f);
            sitButton.interactable = canConfirm || canSit || run.Phase == CampaignPhase.Menang;
            sitButton.GetComponentInChildren<Text>(true).text = run.Phase == CampaignPhase.Menang ? "ULANG"
                : atBiro ? "SAHKAN " + (biroSteps + 1) + "/3" : "DUDUK";
            sitButton.gameObject.SetActive(atBiro || canSit || run.Phase == CampaignPhase.Menang);
            attackButton.interactable = GuardWithin(3f) && Time.time >= nextBasic;
            attackButton.gameObject.SetActive(guardActive);
            string[] skills = { "PERISAI", "KOMANDO", "PULIHKAN", "JALAN BARU" };
            float remaining = Mathf.Max(0f, skillReadyAt[heroIndex] - Time.time);
            skillButton.interactable = run.Phase != CampaignPhase.Menang && remaining <= 0f &&
                (heroIndex != 1 || GuardWithin(4.5f));
            skillButton.GetComponentInChildren<Text>().text = remaining > 0f
                ? skills[heroIndex] + " " + Mathf.CeilToInt(remaining) : skills[heroIndex];
            if (objectiveProgress != null)
            {
                objectiveProgress.fillAmount = run.Phase == CampaignPhase.PlazaAspirasi
                    ? (run.HasSeal(CampaignSector.MajelisDaun) ? biroSteps / 3f : majelisHold / 2.5f)
                    : run.Phase == CampaignPhase.GardaTakhta ? 1f - guardHealth / 100f
                    : run.Phase == CampaignPhase.Memerintah ? (float)run.Power / run.TargetPower : 0f;
            }
            RefreshWaypoint();
        }

        private void RefreshWaypoint()
        {
            if (waypointText == null) return;
            Transform destination;
            string label;
            switch (run.Phase)
            {
                case CampaignPhase.GerbangRakyat:
                    destination = plaza; label = "PLAZA"; break;
                case CampaignPhase.PlazaAspirasi:
                    destination = run.HasSeal(CampaignSector.MajelisDaun) ? biro : majelis;
                    label = run.HasSeal(CampaignSector.MajelisDaun) ? "BIRO" : "MAJELIS";
                    break;
                case CampaignPhase.GerbangDalam:
                case CampaignPhase.GardaTakhta:
                    destination = guardActive ? guardVisual.transform : garda;
                    label = "GARDA"; break;
                case CampaignPhase.KursiTerbuka:
                case CampaignPhase.Memerintah:
                    destination = guardActive ? guardVisual.transform : chair;
                    label = guardActive ? "PERTAHANKAN KURSI" : "KURSI"; break;
                default:
                    waypointText.text = string.Empty;
                    return;
            }
            Vector3 delta = destination.position - player.transform.position;
            float distance = new Vector2(delta.x, delta.z).magnitude;
            if (distance < (run.Phase == CampaignPhase.PlazaAspirasi ? 2.8f : 2.3f))
            {
                waypointText.text = run.Phase == CampaignPhase.PlazaAspirasi
                    ? (destination == majelis ? "DI MAJELIS: tetap di sini hingga bar penuh"
                        : "DI BIRO: tekan SAHKAN 3 kali")
                    : "DI LOKASI: " + label;
                return;
            }
            string direction;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.z) * 1.8f)
                direction = delta.x < 0f ? "BARAT" : "TIMUR";
            else if (Mathf.Abs(delta.z) > Mathf.Abs(delta.x) * 1.8f)
                direction = delta.z < 0f ? "SELATAN" : "UTARA";
            else direction = (delta.z < 0f ? "SELATAN " : "UTARA ") +
                    (delta.x < 0f ? "BARAT" : "TIMUR");
            waypointText.text = "ARAH " + label + ": " + direction + "  •  " + Mathf.CeilToInt(distance) + " m";
        }

        private static bool Near(Vector3 a, Vector3 b, float radius)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z <= radius * radius;
        }
    }
}
