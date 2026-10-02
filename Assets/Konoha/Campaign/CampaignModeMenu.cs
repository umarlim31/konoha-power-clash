using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // 0.1.0 mode menu, the first screen of the APK: JALUR TAKHTA (solo, offline local host)
    // or REBUT KURSI (the untouched PvP 4v4 scene). 0.6.0: KARIER first (Level 1 Warga
    // Biasa, same local host, own character creator); JALUR TAKHTA becomes MODE PRESIDEN. Solo never creates a PvP room; the
    // campaign network object is removed before the PvP scene loads, so exactly one
    // NetworkManager exists at a time.
    public sealed class CampaignModeMenu : MonoBehaviour
    {
        public GameObject panel;
        public Button soloButton;
        public Button pvpButton;
        public Button karierButton;
        public KarierAvatarPanel avatarPanel;
        public Text versionText;
        public CampaignSession session;
        // Build path of the PvP scene (SpikeProject.ScenePath) and its build index (loaded by index).
        public string pvpScene;
        public int pvpBuildIndex = 1;

        private bool leaving;

        private void Start()
        {
            if (soloButton != null)
                soloButton.onClick.AddListener(PlaySolo);
            if (karierButton != null)
                karierButton.onClick.AddListener(PlayKarier);
            if (pvpButton != null)
            {
                pvpButton.onClick.AddListener(PlayPvp);
                pvpButton.interactable = !string.IsNullOrEmpty(pvpScene);
            }
            if (versionText != null)
                versionText.text = "Versi " + Application.version;
            if (panel != null)
                panel.SetActive(true);
        }

        private void OnDestroy()
        {
            if (soloButton != null)
                soloButton.onClick.RemoveListener(PlaySolo);
            if (karierButton != null)
                karierButton.onClick.RemoveListener(PlayKarier);
            if (pvpButton != null)
                pvpButton.onClick.RemoveListener(PlayPvp);
        }

        private void PlaySolo()
        {
            if (leaving)
                return;
            leaving = true;
            CampaignKarier.Active = false;
            if (panel != null)
                panel.SetActive(false);
            if (session != null)
                session.BeginSolo();
        }

        private void PlayKarier()
        {
            if (leaving)
                return;
            leaving = true;
            CampaignKarier.Active = true;
            if (panel != null)
                panel.SetActive(false);
            if (session != null)
                session.BeginSolo(true);
            if (avatarPanel != null)
                avatarPanel.Open();
        }

        private void PlayPvp()
        {
            if (leaving || string.IsNullOrEmpty(pvpScene))
                return;
            leaving = true;
            StartCoroutine(LoadPvp());
        }

        private IEnumerator LoadPvp()
        {
            // The campaign NetworkManager never started; remove it (it may be marked
            // DontDestroyOnLoad) and wait a frame so the PvP scene owns the singleton.
            if (session != null)
            {
                session.gameObject.SetActive(false);
                Destroy(session.gameObject);
            }
            yield return null;
            SceneManager.LoadScene(pvpBuildIndex, LoadSceneMode.Single);
        }
    }
}
