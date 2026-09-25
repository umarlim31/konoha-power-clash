using Konoha.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // First solo gameplay preview. This uses the existing touch motor and generated hero
    // silhouettes; faction AI and hero abilities remain future campaign work.
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
        public GameObject[] heroVisuals;
        public Text objectiveText;
        public Text statusText;
        public Text heroText;
        public Button attackButton;
        public Button sitButton;
        public Button heroButton;

        private readonly CampaignRunState run = new CampaignRunState(2, 35);
        private readonly float[] hold = new float[2];
        private int heroIndex;
        private int health = 100;
        private int guardHealth;
        private float nextGuardHit;
        private float nextBasic;
        private float powerClock;
        private float counterattackClock;
        private bool counterattackStarted;
        private bool guardActive;

        private void Start()
        {
            attackButton.onClick.AddListener(BasicAttack);
            sitButton.onClick.AddListener(Sit);
            heroButton.onClick.AddListener(NextHero);
            guardVisual.SetActive(false);
            ShowHero();
            RefreshHud();
        }

        private void OnDestroy()
        {
            if (attackButton != null) attackButton.onClick.RemoveListener(BasicAttack);
            if (sitButton != null) sitButton.onClick.RemoveListener(Sit);
            if (heroButton != null) heroButton.onClick.RemoveListener(NextHero);
        }

        private void Update()
        {
            if (player == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            var position = player.transform.position;

            if (run.Phase == CampaignPhase.GerbangRakyat && Near(position, plaza.position, 2.4f))
                run.ReachPlaza();

            if (run.Phase == CampaignPhase.PlazaAspirasi)
            {
                Capture(CampaignSector.MajelisDaun, majelis, 0, position, dt);
                Capture(CampaignSector.BiroProsedur, biro, 1, position, dt);
                run.TryOpenInnerGate();
            }

            if (run.Phase == CampaignPhase.GerbangDalam && Near(position, garda.position, 2.6f))
            {
                run.TryStartGuard();
                SpawnGuard(garda.position, 75);
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
            RefreshHud();
        }

        private void Capture(CampaignSector sector, Transform location, int index, Vector3 position, float dt)
        {
            if (run.HasSeal(sector)) return;
            hold[index] = Near(position, location.position, 2.3f) ? hold[index] + dt : 0f;
            if (hold[index] >= 2.5f)
                run.AwardSeal(sector);
        }

        private void SpawnGuard(Vector3 position, int hp)
        {
            guardVisual.transform.position = new Vector3(position.x, 0f, position.z);
            guardVisual.SetActive(true);
            guardHealth = hp;
            guardActive = true;
            nextGuardHit = Time.time + 1.5f;
        }

        private void MoveAndAttackGuard(float dt)
        {
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
                health = Mathf.Max(0, health - 18);
                nextGuardHit = Time.time + 1.3f;
                if (health == 0)
                {
                    run.LoseSeat();
                    player.transform.position = new Vector3(0f, 0.1f, -8.5f);
                    health = 100;
                    if (run.Phase == CampaignPhase.GardaTakhta)
                        SpawnGuard(garda.position, 75);
                }
            }
        }

        private void BasicAttack()
        {
            if (!guardActive || Time.time < nextBasic || !Near(player.transform.position,
                    guardVisual.transform.position, 3f)) return;
            nextBasic = Time.time + 0.55f;
            guardHealth -= 25;
            if (guardHealth > 0) return;
            guardActive = false;
            guardVisual.SetActive(false);
            if (run.Phase == CampaignPhase.GardaTakhta)
                run.DefeatGuard();
        }

        private void Sit()
        {
            if (run.Phase == CampaignPhase.KursiTerbuka &&
                Near(player.transform.position, chair.position, 2.2f))
                run.TrySit();
        }

        private void NextHero()
        {
            if (heroVisuals == null || heroVisuals.Length == 0) return;
            heroIndex = (heroIndex + 1) % heroVisuals.Length;
            ShowHero();
        }

        private void ShowHero()
        {
            if (heroVisuals == null) return;
            for (int i = 0; i < heroVisuals.Length; i++)
                if (heroVisuals[i] != null) heroVisuals[i].SetActive(i == heroIndex);
        }

        private void RefreshHud()
        {
            string[] names = { "MEGA", "GEMOY", "ABAH", "PAK WI" };
            heroText.text = "HERO: " + names[Mathf.Clamp(heroIndex, 0, names.Length - 1)] + "  |  HP " + health;
            statusText.text = "SEGEL " + run.SealCount + "/" + run.RequiredSeals +
                "  |  KUASA " + run.Power + "/" + run.TargetPower +
                (guardActive ? "  |  GARDA " + guardHealth + " HP" : "");
            switch (run.Phase)
            {
                case CampaignPhase.GerbangRakyat:
                    objectiveText.text = "GERBANG RAKYAT  >  Bergerak maju ke PLAZA"; break;
                case CampaignPhase.PlazaAspirasi:
                    objectiveText.text = "KIRI MAJELIS " + (run.HasSeal(CampaignSector.MajelisDaun) ? "OK" : (hold[0] / 2.5f).ToString("P0")) +
                        "  |  KANAN BIRO " + (run.HasSeal(CampaignSector.BiroProsedur) ? "OK" : (hold[1] / 2.5f).ToString("P0")); break;
                case CampaignPhase.GerbangDalam:
                    objectiveText.text = "GERBANG DALAM TERBUKA  >  Hadapi GARDA TAKHTA"; break;
                case CampaignPhase.GardaTakhta:
                    objectiveText.text = "GARDA TAKHTA  >  Dekati dan tekan BASIC"; break;
                case CampaignPhase.KursiTerbuka:
                    objectiveText.text = "KURSI TERBUKA  >  Dekati dan tekan DUDUK"; break;
                case CampaignPhase.Memerintah:
                    objectiveText.text = "PERTAHANKAN TAKHTA  >  Tetap di dekat Kursi"; break;
                default:
                    objectiveText.text = "JALUR TAKHTA SELESAI  |  Kemenangan prototype"; break;
            }
            sitButton.interactable = run.Phase == CampaignPhase.KursiTerbuka &&
                Near(player.transform.position, chair.position, 2.2f);
            attackButton.interactable = guardActive;
        }

        private static bool Near(Vector3 a, Vector3 b, float radius)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z <= radius * radius;
        }
    }
}
