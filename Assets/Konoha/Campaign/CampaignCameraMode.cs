using Konoha.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // 0.2.8: two camera styles for Jalur Takhta, switched with the KAMERA button and
    // remembered on the tablet.
    //  - JAUH: the tested overview (pitch 22, 22 m). The Kursi is visible from the spawn (§13).
    //  - DEKAT: a low over-the-shoulder street camera (pitch 12, 9 m, wider lens) that shows
    //    buildings, trees and people at eye level, so the city is felt, not just seen from above.
    // Orbit, pinch zoom, KAMERA AWAL, the occluders and LIHAT ARENA work the same in both.
    public sealed class CampaignCameraMode : MonoBehaviour
    {
        public const string PrefKey = "konoha.camera.dekat";

        [System.Serializable]
        public struct Preset
        {
            public float pitch, minPitch, distance, minDistance, maxDistance, focusHeight, fieldOfView;
        }

        public MobileCombatCamera follow;
        public Camera view;
        public Button button;
        public Preset far = new Preset
        {
            pitch = 22f, minPitch = 16f, distance = 22f, minDistance = 13f, maxDistance = 28f, focusHeight = .8f, fieldOfView = 50f
        };
        public Preset close = new Preset
        {
            pitch = 12f, minPitch = 5f, distance = 9f, minDistance = 5.5f, maxDistance = 16f, focusHeight = 1.55f, fieldOfView = 60f
        };

        private Text label;
        private bool closeMode;

        public bool CloseMode => closeMode;

        private void Start()
        {
            if (button != null)
            {
                label = button.GetComponentInChildren<Text>(true);
                button.onClick.AddListener(Toggle);
            }
            closeMode = PlayerPrefs.GetInt(PrefKey, 0) == 1;
            Apply();
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(Toggle);
        }

        public void Toggle()
        {
            closeMode = !closeMode;
            PlayerPrefs.SetInt(PrefKey, closeMode ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        private void Apply()
        {
            Preset p = closeMode ? close : far;
            if (follow != null)
            {
                follow.resetPitch = p.pitch;
                follow.minPitch = p.minPitch;
                follow.resetDistance = p.distance;
                follow.minOrbitDistance = p.minDistance;
                follow.maxOrbitDistance = p.maxDistance;
                follow.orbitFocusHeight = p.focusHeight;
                follow.ResetOrbit();
            }
            if (view != null)
                view.fieldOfView = p.fieldOfView;
            if (label != null)
                label.text = closeMode ? "KAMERA: DEKAT" : "KAMERA: JAUH";
        }
    }
}
