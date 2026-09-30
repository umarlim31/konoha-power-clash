using Konoha.Networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // §10 result screen (0.1.0): run time, Runtuh, Pengaruh collected and the final archetype.
    // MVP only has the combat route, so the archetype is always TAKHTA BESI. Buttons:
    // ULANG (same hero, straight back to the Gerbang Rakyat) and GANTI HERO (hero screen).
    public sealed class CampaignResultScreen : MonoBehaviour
    {
        public GameObject panel;
        public Text statsText;
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
            if (show && statsText != null)
                statsText.text = Describe(director, kit);
        }

        private string Describe(CampaignDirector director, NetworkHeroKit kit)
        {
            int seconds = Mathf.FloorToInt((float)director.RunSeconds);
            string hero = kit != null ? NetworkHeroKit.GetHeroName(kit.Hero) : "-";
            return "HERO  " + hero + "\n" +
                "WAKTU  " + (seconds / 60) + ":" + (seconds % 60).ToString("00") + "\n" +
                "RUNTUH  " + director.RuntuhCount + "\n" +
                "PENGARUH TERKUMPUL  " + collected + "\n\n" +
                "ARKETIPE:  TAKHTA BESI\n" +
                "Kursi direbut lewat adu kuat, bukan lewat koalisi.";
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
