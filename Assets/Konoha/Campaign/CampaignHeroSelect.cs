using Konoha.Networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // Jalur Takhta hero screen (0.0.9.2.1). Shown before a run starts and again after ULANG;
    // the chosen hero is locked for the whole run (CampaignDirector.CanSelectHero). The pick
    // itself goes through the shared hero kit (server-validated), so co-op clients use the
    // same path later.
    public sealed class CampaignHeroSelect : MonoBehaviour
    {
        public GameObject panel;
        // Order: MEGA, GEMOY, ABAH, PAK WI (PrototypeHero values 0..3).
        public Button[] heroButtons = new Button[4];
        public Text detailText;
        public Button startButton;

        private static readonly Color Idle = new Color(0.14f, 0.20f, 0.23f, 0.96f);
        private static readonly Color StartReady = new Color(0.62f, 0.44f, 0.16f, 0.98f);
        private static readonly Color StartWaiting = new Color(0.25f, 0.25f, 0.25f, 0.90f);

        private void Start()
        {
            for (int i = 0; i < heroButtons.Length; i++)
            {
                if (heroButtons[i] == null)
                    continue;
                PrototypeHero hero = (PrototypeHero)i;
                heroButtons[i].onClick.AddListener(() => Pick(hero));
            }

            if (startButton != null)
                startButton.onClick.AddListener(StartRun);
        }

        private void OnDestroy()
        {
            foreach (Button button in heroButtons)
                if (button != null) button.onClick.RemoveAllListeners();
            if (startButton != null)
                startButton.onClick.RemoveListener(StartRun);
        }

        private void Update()
        {
            CampaignDirector director = CampaignDirector.Instance;
            // 0.6.0: KARIER has its own character creator (KarierAvatarPanel).
            bool open = director != null && director.IsSpawned && !director.HeroLocked && !CampaignKarier.Active;
            if (panel != null && panel.activeSelf != open)
                panel.SetActive(open);
            if (!open)
                return;

            NetworkHeroKit kit = LocalKit();
            PrototypeHero current = kit != null ? kit.Hero : PrototypeHero.Mega;

            for (int i = 0; i < heroButtons.Length; i++)
            {
                Button button = heroButtons[i];
                if (button == null || button.targetGraphic == null)
                    continue;
                bool selected = kit != null && (int)current == i;
                button.targetGraphic.color = selected
                    ? Color.Lerp(NetworkHeroKit.GetHeroAccentColor((PrototypeHero)i), Color.white, 0.10f)
                    : Idle;
            }

            if (detailText != null)
                detailText.text = kit != null ? Describe(current) : "Menyiapkan hero...";

            if (startButton != null)
            {
                startButton.interactable = kit != null;
                if (startButton.targetGraphic != null)
                    startButton.targetGraphic.color = kit != null ? StartReady : StartWaiting;
            }
        }

        private void Pick(PrototypeHero hero)
        {
            NetworkHeroKit kit = LocalKit();
            if (kit != null)
                kit.TrySelectHero(hero);
        }

        private void StartRun()
        {
            CampaignDirector director = CampaignDirector.Instance;
            if (director != null && LocalKit() != null)
                director.RequestStartRun();
        }

        private static NetworkHeroKit LocalKit()
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null || manager.LocalClient == null || manager.LocalClient.PlayerObject == null)
                return null;
            return manager.LocalClient.PlayerObject.GetComponent<NetworkHeroKit>();
        }

        // Player-facing summaries of the current hero kits (NetworkHeroKit).
        public static string Describe(PrototypeHero hero)
        {
            switch (hero)
            {
                case PrototypeHero.Mega:
                    return "MEGA  •  Petarung depan\n" +
                        "S1 SERUAN IBU: terjang 6 m, dorong + stun sebentar\n" +
                        "S2 PERISAI RAKYAT: dua kader penahan musuh + perisai 15\n" +
                        "ULT MONCONG: serangan garis jauh 12 m";
                case PrototypeHero.Prabowo:
                    return "GEMOY  •  Komandan\n" +
                        "S1 CMD LEAP: lompat 4 m, damage area pendaratan\n" +
                        "S2 BARIS!: dorong + 16 damage di depan, perisai 10 untuk GEMOY\n" +
                        "ULT GARUDA: damage +30% dan lebih tahan selama 8 detik";
                case PrototypeHero.Abah:
                    return "ABAH  •  Pengendali narasi\n" +
                        "S1 NARASI: zona 6 detik, sekutu cepat, musuh lambat + damage\n" +
                        "S2 ELECTRIC DASH: melesat 5 m\n" +
                        "ULT PIDATO: 30 damage area 7 m + dorong musuh";
                case PrototypeHero.Jokowi:
                    return "PAK WI  •  Pembangun lincah\n" +
                        "S1 INFRASTRUKTUR: jalan 8 dtk; di atasnya cepat, damage +25%, tahan +20%\n" +
                        "S2 BLUSUKAN: melesat 6 m, 18 damage di lintasan + perisai 15\n" +
                        "ULT PROYEK: ledakan 30 damage area 7 m + perisai 25";
                default:
                    return NetworkHeroKit.GetHeroName(hero);
            }
        }
    }
}
