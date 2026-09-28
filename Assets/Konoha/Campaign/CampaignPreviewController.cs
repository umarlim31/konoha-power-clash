using Konoha.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // Offline campaign slice: Unity bridge only (input, HUD, visuals, prototype guard
    // combat). Objective rules live in CampaignObjectiveDirector; numbers in
    // CampaignTuning.PreviewSlice. The abilities are lightweight prototype rules and do
    // not share the PvP hero kit or network authority.
    public sealed class CampaignPreviewController : MonoBehaviour
    {
        public CharacterMotor player;
        public CampaignMonument centralMonument;
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

        private CampaignObjectiveDirector director;
        private readonly float[] skillReadyAt = new float[4];
        private int heroIndex;
        private int health = CampaignTuning.PreviewSlice.PlayerHealth;
        private float nextGuardHit;
        private float nextBasic;
        private float guardStunnedUntil;
        private float shieldUntil;
        private float speedUntil;
        private string feedback;
        private float feedbackUntil;
        private float guardFlashUntil;
        private MaterialPropertyBlock ringBlock;
        private MaterialPropertyBlock guardBlock;

        private CampaignRunState Run => director.Run;
        private bool GuardActive => director.GuardActive;

        private void Awake()
        {
            director = new CampaignObjectiveDirector(new CampaignObjectiveLayout(
                plaza.position, majelis.position, biro.position, garda.position, chair.position));
            director.Notified += Notify;
            director.GuardSpawned += ShowGuard;
            director.GuardRemoved += HideGuard;
        }

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
            if (director != null)
            {
                director.Notified -= Notify;
                director.GuardSpawned -= ShowGuard;
                director.GuardRemoved -= HideGuard;
            }
        }

        private void Update()
        {
            if (player == null) return;
            float dt = Mathf.Min(Time.deltaTime, CampaignTuning.PreviewSlice.MaxFrameSeconds);
            player.speedMultiplier = Time.time < speedUntil ? CampaignTuning.PreviewSlice.PakWiSpeedMultiplier : 1f;

            director.Tick(player.transform.position, dt);
            if (GuardActive && Run.Phase != CampaignPhase.Menang)
                MoveAndAttackGuard(dt);
            chairBarrier.SetActive(Run.Phase < CampaignPhase.KursiTerbuka);
            RefreshWorldFeedback();
            RefreshHud();
        }

        private void ShowGuard(int health)
        {
            Vector3 position = director.Layout.Garda;
            guardVisual.transform.position = new Vector3(position.x, 0f, position.z);
            guardVisual.SetActive(true);
            guardStunnedUntil = 0f;
            nextGuardHit = Time.time + CampaignTuning.PreviewSlice.GuardFirstHitDelaySeconds;
        }

        private void HideGuard() => guardVisual.SetActive(false);

        private void MoveAndAttackGuard(float dt)
        {
            if (Time.time < guardStunnedUntil) return;
            Vector3 target = player.transform.position;
            target.y = guardVisual.transform.position.y;
            float distance = Vector3.Distance(guardVisual.transform.position, target);
            // The first guard holds the final approach; it does not cross the locked
            // chair enclosure to chase a player who has not reached this sector.
            if (Run.Phase == CampaignPhase.GardaTakhta && distance > CampaignTuning.PreviewSlice.GuardLeashRadius) return;
            if (distance > CampaignTuning.PreviewSlice.GuardReach)
            {
                Vector3 destination = centralMonument != null
                    ? CampaignMonument.GuardDestination(guardVisual.transform.position, target, centralMonument.solid.bounds)
                    : target;
                guardVisual.transform.position = Vector3.MoveTowards(guardVisual.transform.position,
                    destination, CampaignTuning.PreviewSlice.GuardSpeed * dt);
            }
            else if (Time.time >= nextGuardHit)
            {
                health = Mathf.Max(0, health - (Time.time < shieldUntil
                    ? CampaignTuning.PreviewSlice.GuardDamageShielded : CampaignTuning.PreviewSlice.GuardDamage));
                nextGuardHit = Time.time + CampaignTuning.PreviewSlice.GuardHitIntervalSeconds;
                if (health > 0)
                {
                    ActiveAnimator()?.PlayHit();
                    return;
                }
                player.Teleport(new Vector3(CampaignTuning.PreviewSlice.StartX, CampaignTuning.PreviewSlice.StartY,
                    CampaignTuning.PreviewSlice.RuntuhRespawnZ));
                health = CampaignTuning.PreviewSlice.PlayerHealth;
                director.HandleRuntuh();
            }
        }

        private void BasicAttack()
        {
            if (!GuardActive || Time.time < nextBasic || !Near(player.transform.position,
                    guardVisual.transform.position, CampaignTuning.PreviewSlice.BasicRange)) return;
            nextBasic = Time.time + CampaignTuning.PreviewSlice.BasicCooldownSeconds;
            ActiveAnimator()?.PlayAttack();
            HitGuard(CampaignTuning.PreviewSlice.BasicDamage);
        }

        private void HitGuard(int damage)
        {
            if (director.DamageGuard(damage))
                guardFlashUntil = Time.time + CampaignTuning.PreviewSlice.GuardFlashSeconds;
        }

        private void Sit()
        {
            if (Run.Phase == CampaignPhase.Menang)
            {
                Restart();
                return;
            }
            director.Interact(player.transform.position, Time.time);
        }

        private void UseSkill()
        {
            if (Run.Phase == CampaignPhase.Menang || Time.time < skillReadyAt[heroIndex]) return;
            switch (heroIndex)
            {
                case 0: // Mega: close range protection and area control.
                    shieldUntil = Time.time + CampaignTuning.PreviewSlice.MegaShieldSeconds;
                    if (GuardWithin(CampaignTuning.PreviewSlice.MegaSkillRange)) HitGuard(CampaignTuning.PreviewSlice.MegaSkillDamage);
                    Notify("Mega: perisai rakyat aktif 5 detik");
                    skillReadyAt[0] = Time.time + CampaignTuning.PreviewSlice.MegaCooldownSeconds;
                    break;
                case 1: // Gemoy: powerful command hit with longer reach.
                    if (!GuardWithin(CampaignTuning.PreviewSlice.GemoySkillRange)) return;
                    HitGuard(CampaignTuning.PreviewSlice.GemoySkillDamage);
                    Notify("Gemoy: komando maju!");
                    skillReadyAt[1] = Time.time + CampaignTuning.PreviewSlice.GemoyCooldownSeconds;
                    break;
                case 2: // Abah: mend wounds and halt the opponent briefly.
                    health = Mathf.Min(CampaignTuning.PreviewSlice.PlayerHealth, health + CampaignTuning.PreviewSlice.AbahHeal);
                    if (GuardWithin(CampaignTuning.PreviewSlice.AbahStunRange))
                        guardStunnedUntil = Time.time + CampaignTuning.PreviewSlice.AbahStunSeconds;
                    Notify("Abah: pulih 40 HP dan Garda tertahan");
                    skillReadyAt[2] = Time.time + CampaignTuning.PreviewSlice.AbahCooldownSeconds;
                    break;
                default: // Pak Wi: navigate the two routes and dodge pursuit.
                    speedUntil = Time.time + CampaignTuning.PreviewSlice.PakWiSpeedSeconds;
                    Notify("Pak Wi: jalan baru, bergerak lebih cepat");
                    skillReadyAt[3] = Time.time + CampaignTuning.PreviewSlice.PakWiCooldownSeconds;
                    break;
            }
            ActiveAnimator()?.PlaySkill();
        }

        private bool GuardWithin(float range) => GuardActive &&
            Near(player.transform.position, guardVisual.transform.position, range);

        // Null for primitive heroes, so animation calls are skipped (real null for "?.").
        private HeroAnimatorDriver ActiveAnimator()
        {
            if (heroVisuals == null || heroIndex >= heroVisuals.Length || heroVisuals[heroIndex] == null) return null;
            var driver = heroVisuals[heroIndex].GetComponent<HeroAnimatorDriver>();
            return driver != null ? driver : null;
        }

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
                    // A rigged 3D hero replaces the capsule; primitive heroes keep it.
                    renderer.enabled = !HeroAnimatorDriver.UsesModel(heroVisuals[heroIndex]);
                    Color[] colors = {
                        new Color(0.43f, 0.11f, 0.16f), new Color(0.83f, 0.78f, 0.68f),
                        new Color(0.11f, 0.34f, 0.26f), new Color(0.85f, 0.78f, 0.67f)
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
                float pulse = 1.32f + 0.04f * Mathf.Sin(Time.time * 3f);
                heroMarker.localScale = new Vector3(pulse, 0.012f, pulse);
            }
            if (GuardActive && guardRenderer != null && guardBlock != null)
            {
                guardBlock.SetColor("_BaseColor", Time.time < guardFlashUntil
                    ? new Color(1f, 0.82f, 0.38f) : new Color(0.17f, 0.22f, 0.32f));
                guardRenderer.SetPropertyBlock(guardBlock);
            }
        }

        private void Restart()
        {
            director.Restart();
            player.Teleport(new Vector3(CampaignTuning.PreviewSlice.StartX, CampaignTuning.PreviewSlice.StartY,
                CampaignTuning.PreviewSlice.StartZ));
            health = CampaignTuning.PreviewSlice.PlayerHealth;
            nextBasic = 0f;
            guardVisual.SetActive(false);
            Notify("Perjalanan baru dimulai");
            shieldUntil = speedUntil = 0f;
            for (int i = 0; i < skillReadyAt.Length; i++) skillReadyAt[i] = 0f;
        }

        private void Notify(string message)
        {
            feedback = message;
            feedbackUntil = Time.time + CampaignTuning.PreviewSlice.FeedbackSeconds;
        }

        private void RefreshHud()
        {
            string[] names = { "MEGA", "GEMOY", "ABAH", "PAK WI" };
            heroText.text = "HERO: " + names[Mathf.Clamp(heroIndex, 0, names.Length - 1)] + "  |  HP " + health;
            if (feedbackText != null)
                feedbackText.text = Time.time < feedbackUntil ? feedback : string.Empty;
            var run = Run;
            statusText.text = "SEGEL " + run.SealCount + "/" + run.RequiredSeals +
                "  |  KUASA " + run.Power + "/" + run.TargetPower +
                (GuardActive ? "  |  GARDA " + director.GuardHealth + " HP" : "");
            switch (run.Phase)
            {
                case CampaignPhase.GerbangRakyat:
                    objectiveText.text = "GERBANG RAKYAT  •  Menuju PLAZA ASPIRASI"; break;
                case CampaignPhase.PlazaAspirasi:
                    objectiveText.text = !run.HasSeal(CampaignSector.MajelisDaun)
                        ? "1/2  MAJELIS: tahan di lingkaran " + Mathf.CeilToInt(Mathf.Max(0f,
                            CampaignTuning.PreviewSlice.MajelisHoldSeconds - director.MajelisHold)) + " detik"
                        : "2/2  BIRO: masuk lingkaran, tekan SAHKAN " + (director.BiroSteps + 1) + "/" +
                            CampaignTuning.PreviewSlice.BiroSteps;
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
            Vector3 position = player.transform.position;
            bool atBiro = director.IsAtBiro(position);
            bool canConfirm = director.CanConfirmBiro(position, Time.time);
            bool canSit = director.CanSit(position);
            sitButton.interactable = canConfirm || canSit || run.Phase == CampaignPhase.Menang;
            sitButton.GetComponentInChildren<Text>(true).text = run.Phase == CampaignPhase.Menang ? "ULANG"
                : atBiro ? "SAHKAN " + (director.BiroSteps + 1) + "/" + CampaignTuning.PreviewSlice.BiroSteps : "DUDUK";
            sitButton.gameObject.SetActive(atBiro || canSit || run.Phase == CampaignPhase.Menang);
            attackButton.interactable = GuardWithin(CampaignTuning.PreviewSlice.BasicRange) && Time.time >= nextBasic;
            attackButton.gameObject.SetActive(GuardActive);
            string[] skills = { "PERISAI", "KOMANDO", "PULIHKAN", "JALAN BARU" };
            float remaining = Mathf.Max(0f, skillReadyAt[heroIndex] - Time.time);
            skillButton.interactable = run.Phase != CampaignPhase.Menang && remaining <= 0f &&
                (heroIndex != 1 || GuardWithin(CampaignTuning.PreviewSlice.GemoySkillRange));
            skillButton.GetComponentInChildren<Text>().text = remaining > 0f
                ? skills[heroIndex] + " " + Mathf.CeilToInt(remaining) : skills[heroIndex];
            if (objectiveProgress != null)
            {
                objectiveProgress.fillAmount = run.Phase == CampaignPhase.PlazaAspirasi
                    ? (run.HasSeal(CampaignSector.MajelisDaun)
                        ? director.BiroSteps / (float)CampaignTuning.PreviewSlice.BiroSteps
                        : director.MajelisHold / CampaignTuning.PreviewSlice.MajelisHoldSeconds)
                    : run.Phase == CampaignPhase.GardaTakhta ? 1f - director.GuardHealth / (float)director.GuardMaxHealth
                    : run.Phase == CampaignPhase.Memerintah ? (float)run.Power / run.TargetPower : 0f;
            }
            RefreshWaypoint();
        }

        private void RefreshWaypoint()
        {
            if (waypointText == null) return;
            var run = Run;
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
                    destination = GuardActive ? guardVisual.transform : garda;
                    label = "GARDA"; break;
                case CampaignPhase.KursiTerbuka:
                case CampaignPhase.Memerintah:
                    destination = GuardActive ? guardVisual.transform : chair;
                    label = GuardActive ? "PERTAHANKAN KURSI" : "KURSI"; break;
                default:
                    waypointText.text = string.Empty;
                    return;
            }
            Vector3 delta = destination.position - player.transform.position;
            float distance = new Vector2(delta.x, delta.z).magnitude;
            if (distance < (run.Phase == CampaignPhase.PlazaAspirasi
                    ? CampaignTuning.PreviewSlice.SectorRadius : CampaignTuning.PreviewSlice.WaypointArrivalRadius))
            {
                waypointText.text = run.Phase == CampaignPhase.PlazaAspirasi
                    ? (destination == majelis ? "DI MAJELIS: tetap di sini hingga bar penuh"
                        : "DI BIRO: tekan SAHKAN 3 kali")
                    : "DI LOKASI: " + label;
                return;
            }
            // Directions follow the current camera so rotating the view does not invert the guidance.
            if (Camera.main != null)
            {
                Vector3 ahead=Camera.main.transform.forward; ahead.y=0; ahead.Normalize();
                Vector3 right=new Vector3(ahead.z,0,-ahead.x);
                delta=new Vector3(Vector3.Dot(delta,right),0,Vector3.Dot(delta,ahead));
            }
            string direction;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.z) * 1.8f)
                direction = delta.x < 0f ? "KIRI" : "KANAN";
            else if (Mathf.Abs(delta.z) > Mathf.Abs(delta.x) * 1.8f)
                direction = delta.z < 0f ? "BELAKANG" : "DEPAN";
            else direction = (delta.z < 0f ? "BELAKANG " : "DEPAN ") +
                    (delta.x < 0f ? "KIRI" : "KANAN");
            waypointText.text = "ARAH " + label + ": " + direction + "  •  " + Mathf.CeilToInt(distance) + " m";
        }

        private static bool Near(Vector3 a, Vector3 b, float radius) =>
            CampaignObjectiveDirector.Near(a, b, radius);
    }
}
