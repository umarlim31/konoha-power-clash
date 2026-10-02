using Konoha.Character;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // 0.6.0 KARIER character creator: "jadi dirimu sendiri" as a citizen of Konoha (name,
    // laki-laki/perempuan, skin, hair/peci/jilbab/topi, build, shirt colour). The local hero
    // turns to face the camera and shows every change live (CampaignBodies + CampaignAvatarLook).
    // With a save on the tablet the player can continue or start from zero.
    public sealed class KarierAvatarPanel : MonoBehaviour
    {
        public GameObject panel;
        public InputField nameField;
        public Button genderButton, skinButton, hairButton, bodyButton, shirtButton;
        public Button startButton;
        public Button resetButton;
        public Text infoText;
        public KarierController controller;
        public MobileCombatCamera follow;

        private KarierAvatar draft = KarierAvatar.Default;
        private bool open;
        private bool hasSave;
        private bool cameraSaved;
        private float savedFocusHeight, savedMinPitch, savedMinDistance;

        public bool IsOpen => open;

        private void Start()
        {
            Listen(genderButton, () => { draft.Female = !draft.Female; Changed(); });
            Listen(skinButton, () => { draft.Skin = KarierAvatar.Wrap(draft.Skin + 1, KarierAvatar.SkinNames.Length); Changed(); });
            Listen(hairButton, () => { draft.Hair = KarierAvatar.Wrap(draft.Hair + 1, KarierAvatar.HairNames.Length); Changed(); });
            Listen(bodyButton, () => { draft.Body = KarierAvatar.Wrap(draft.Body + 1, KarierAvatar.BodyNames.Length); Changed(); });
            Listen(shirtButton, () => { draft.Shirt = KarierAvatar.Wrap(draft.Shirt + 1, KarierAvatar.ShirtNames.Length); Changed(); });
            Listen(startButton, () => Begin(false));
            Listen(resetButton, () => Begin(true));
            if (nameField != null)
                nameField.onEndEdit.AddListener(OnName);
            if (panel != null && !open)
                panel.SetActive(false);
        }

        private void OnDestroy()
        {
            foreach (Button button in new[] { genderButton, skinButton, hairButton, bodyButton, shirtButton, startButton, resetButton })
                if (button != null) button.onClick.RemoveAllListeners();
            if (nameField != null)
                nameField.onEndEdit.RemoveListener(OnName);
        }

        private static void Listen(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }

        // KARIER chosen in the mode menu (the local host is already running).
        public void Open()
        {
            hasSave = PlayerPrefs.HasKey(CampaignKarier.SaveKey);
            if (controller != null)
                controller.EnterCreator();
            draft = KarierAvatar.TryParse(PlayerPrefs.GetString(CampaignKarier.AvatarKey, string.Empty), out KarierAvatar saved)
                ? saved : KarierAvatar.Default;
            CampaignKarier.SetAvatar(draft);
            if (nameField != null)
                nameField.text = draft.Name == "WARGA" ? string.Empty : draft.Name;
            open = true;
            if (panel != null)
                panel.SetActive(true);
            if (resetButton != null)
                resetButton.gameObject.SetActive(hasSave);
            if (startButton != null)
            {
                Text label = startButton.GetComponentInChildren<Text>(true);
                if (label != null)
                    label.text = hasSave ? "LANJUTKAN HIDUP" : "MULAI HIDUP";
            }
            if (infoText != null)
                infoText.text = hasSave ? SavedSummary() : "Kamu warga biasa di RT 03.\nCari duit, cari restu, jadi KETUA RT.";
            if (follow != null && !cameraSaved)
            {
                // Close portrait camera in front of the hero while creating.
                savedFocusHeight = follow.orbitFocusHeight;
                savedMinPitch = follow.minPitch;
                savedMinDistance = follow.minOrbitDistance;
                cameraSaved = true;
                // Dragging or pinching the portrait camera must not snap it back to the far limits.
                follow.minPitch = 4f;
                follow.minOrbitDistance = 3.5f;
                follow.orbitYaw = 0f;
                follow.orbitPitch = 8f;
                follow.orbitDistance = 4.6f;
                follow.orbitFocusHeight = 1.15f;
            }
            Refresh();
        }

        private static string SavedSummary()
        {
            var life = new KarierLife(new KarierLayout());
            if (!life.TryLoad(PlayerPrefs.GetString(CampaignKarier.SaveKey, string.Empty)))
                return "Data lama tidak terbaca. MULAI DARI NOL disarankan.";
            return "Tersimpan: " + KarierLife.Rupiah(life.Duit) + "  •  RESTU " + life.Restu +
                "  •  CATATAN HITAM " + life.CatatanHitam + (life.Registered ? "\nSudah terdaftar calon Ketua RT." : string.Empty);
        }

        private void OnName(string text)
        {
            draft.Name = KarierAvatar.CleanName(text);
            if (nameField != null && nameField.text != draft.Name && draft.Name != "WARGA")
                nameField.text = draft.Name;
            Changed();
        }

        private void Changed()
        {
            CampaignKarier.SetAvatar(draft);
            Refresh();
        }

        private void Refresh()
        {
            KarierAvatar a = draft.Clamped();
            Caption(genderButton, "JENIS: " + (a.Female ? "PEREMPUAN" : "LAKI-LAKI"));
            Caption(skinButton, "KULIT: " + KarierAvatar.SkinNames[a.Skin]);
            Caption(hairButton, "KEPALA: " + KarierAvatar.HairNames[a.Hair]);
            Caption(bodyButton, "BADAN: " + KarierAvatar.BodyNames[a.Body]);
            Caption(shirtButton, "BAJU: " + KarierAvatar.ShirtNames[a.Shirt]);
        }

        private static void Caption(Button button, string text)
        {
            Text label = button != null ? button.GetComponentInChildren<Text>(true) : null;
            if (label != null)
                label.text = text + "  >";
        }

        private void LateUpdate()
        {
            if (!open)
                return;
            // The hero turns to the camera (the world is still frozen while creating).
            NetworkManager manager = NetworkManager.Singleton;
            if (manager != null && manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                manager.LocalClient.PlayerObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private void Begin(bool fresh)
        {
            if (!open)
                return;
            if (nameField != null)
                draft.Name = KarierAvatar.CleanName(nameField.text);
            draft = draft.Clamped();
            CampaignKarier.SetAvatar(draft);
            PlayerPrefs.SetString(CampaignKarier.AvatarKey, draft.Serialize());
            PlayerPrefs.Save();
            open = false;
            if (panel != null)
                panel.SetActive(false);
            if (follow != null && cameraSaved)
            {
                follow.orbitFocusHeight = savedFocusHeight;
                follow.minPitch = savedMinPitch;
                follow.minOrbitDistance = savedMinDistance;
                follow.ResetOrbit();
                cameraSaved = false;
            }
            NetworkManager manager = NetworkManager.Singleton;
            if (manager != null && manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                manager.LocalClient.PlayerObject.transform.rotation = Quaternion.identity;
            if (controller != null)
                controller.BeginLife(fresh || !hasSave);
        }
    }
}
