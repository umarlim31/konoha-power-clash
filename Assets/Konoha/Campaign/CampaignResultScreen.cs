using Konoha.Networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // §10 result screen. 0.4.0: a satirical front page, KORAN KONOHA, whose headline follows
    // the ending (Takhta Besi / Raja Koalisi / Boneka Sistem) and whose stories follow the run
    // (KoranKonoha.Compose). The stats line keeps time, Runtuh and Pengaruh. Buttons:
    // ULANG (same hero, straight back to the Gerbang Rakyat) and GANTI HERO (hero screen).
    public sealed class CampaignResultScreen : MonoBehaviour
    {
        public GameObject panel;
        public Text statsText;
        // 0.4.0 Koran Konoha (all optional; without them the stats text carries everything).
        public Text editionText;
        public Text headlineText;
        public Text subheadText;
        public Text newsText;
        public Text archetypeText;
        public Button retryButton;
        public Button changeHeroButton;
        // Seconds after TAKHTA DIKUASAI before the screen appears (let the fanfare land).
        public float showDelay = 1.6f;

        private double trackedRun = double.NaN;
        private int lastPengaruh;
        private int collected;
        private float wonAt = -1f;

        public int CollectedPengaruh => collected;

        private void Start()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(Retry);
            if (changeHeroButton != null)
                changeHeroButton.onClick.AddListener(ChangeHero);
            if (panel != null)
                panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (retryButton != null)
                retryButton.onClick.RemoveListener(Retry);
            if (changeHeroButton != null)
                changeHeroButton.onClick.RemoveListener(ChangeHero);
        }

        private void Retry() => CampaignDirector.Instance?.RequestRestart(false);

        private void ChangeHero() => CampaignDirector.Instance?.RequestRestart(true);

        private void Update()
        {
            CampaignDirector director = CampaignDirector.Instance;
            bool running = director != null && director.IsSpawned && director.HeroLocked;
            NetworkHeroKit kit = LocalKit();

            // A new run (MULAI or ULANG) resets the Pengaruh tally.
            if (running && director.RunStartedAt != trackedRun)
            {
                trackedRun = director.RunStartedAt;
                collected = 0;
                lastPengaruh = kit != null ? kit.Pengaruh : 0;
            }

            bool won = running && director.Phase == CampaignPhase.Menang;
            if (running && !won && kit != null)
            {
                // Sum of every gain; spending on an ultimate or the Runtuh penalty does not subtract.
                int now = kit.Pengaruh;
                if (now > lastPengaruh)
                    collected += now - lastPengaruh;
                lastPengaruh = now;
            }

            if (!won)
                wonAt = -1f;
            else if (wonAt < 0f)
                wonAt = Time.unscaledTime;

            bool show = won && Time.unscaledTime - wonAt >= showDelay;
            if (panel != null && panel.activeSelf != show)
                panel.SetActive(show);
            if (show)
                Fill(director, kit);
        }

        private void Fill(CampaignDirector director, NetworkHeroKit kit)
        {
            int seconds = Mathf.FloorToInt((float)director.RunSeconds);
            string hero = kit != null ? NetworkHeroKit.GetHeroName(kit.Hero) : "-";
            KoranEdition koran = KoranKonoha.Compose(director.Ending, hero, director.Path(CampaignSector.MajelisDaun),
                director.Path(CampaignSector.BiroProsedur), director.Modal, director.Jatah, director.Restu,
                director.TookLoan, director.RuntuhCount, seconds);
            string stats = "HERO " + hero + "  •  WAKTU " + KoranKonoha.Clock(seconds) + "  •  RUNTUH " +
                director.RuntuhCount + "  •  PENGARUH " + collected + "  •  MODAL " + director.Modal +
                "  •  JATAH " + director.Jatah + "  •  RESTU " + director.Restu;
            if (headlineText == null)
            {
                // Plain fallback layout.
                if (statsText != null)
                    statsText.text = koran.Masthead + "\n" + koran.Headline + "\n" + koran.Subhead + "\n\n" +
                        string.Join("\n", koran.News) + "\n\nARKETIPE: " + koran.Archetype + "\n" + stats;
                return;
            }
            if (editionText != null) editionText.text = koran.Edition;
            headlineText.text = koran.Headline;
            if (subheadText != null) subheadText.text = koran.Subhead;
            if (newsText != null)
            {
                var lines = new System.Text.StringBuilder();
                foreach (string line in koran.News)
                    lines.Append("• ").Append(line).Append('\n');
                newsText.text = lines.ToString();
            }
            if (archetypeText != null) archetypeText.text = "ARKETIPE: " + koran.Archetype;
            if (statsText != null) statsText.text = stats;
        }

        private static NetworkHeroKit LocalKit()
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null || manager.LocalClient == null || manager.LocalClient.PlayerObject == null)
                return null;
            return manager.LocalClient.PlayerObject.GetComponent<NetworkHeroKit>();
        }
    }
}
