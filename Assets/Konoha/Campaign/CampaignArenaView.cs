using System.Collections.Generic;
using Konoha.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // A device-side inspection view; entering it pauses solo gameplay and clears the HUD.
    public sealed class CampaignArenaView : MonoBehaviour
    {
        public MobileCombatCamera follow;
        public CampaignPreviewController campaign;
        public Transform safeRoot;
        public Button viewButton;
        private readonly List<GameObject> hidden = new List<GameObject>();
        private bool showing;
        private bool followWasEnabled;
        private bool campaignWasEnabled;
        private float previousTimeScale;
        private float previousFov;
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private float angle;
        private Camera viewCamera;

        private void Start()
        {
            viewCamera = follow.GetComponent<Camera>();
            viewButton.onClick.AddListener(Toggle);
        }

        public void Toggle()
        {
            if (showing) { Restore(); return; }
            showing = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            campaignWasEnabled = campaign.enabled;
            campaign.enabled = false;
            followWasEnabled = follow.enabled;
            follow.enabled = false;
            previousPosition = follow.transform.position;
            previousRotation = follow.transform.rotation;
            previousFov = viewCamera.fieldOfView;
            viewCamera.fieldOfView = 48f;
            angle = 0;
            foreach (Transform child in safeRoot)
                if (child != viewButton.transform && child.gameObject.activeSelf)
                { hidden.Add(child.gameObject); child.gameObject.SetActive(false); }
            viewButton.GetComponentInChildren<Text>().text = "KEMBALI MAIN";
            PositionCamera();
        }

        private void LateUpdate()
        {
            if (!showing) return;
            angle += Time.unscaledDeltaTime * .12f;
            PositionCamera();
        }

        private void PositionCamera()
        {
            var focus = new Vector3(0, 1.8f, 3.5f);
            float orbit = 22f + Mathf.Sin(angle) * 16f;
            follow.transform.position = focus + Quaternion.Euler(0, orbit, 0) * new Vector3(0, 25, -38);
            follow.transform.LookAt(focus);
        }

        private void Restore()
        {
            if (!showing) return;
            showing = false;
            Time.timeScale = previousTimeScale;
            if (campaign != null) campaign.enabled = campaignWasEnabled;
            foreach (var child in hidden) if (child != null) child.SetActive(true);
            hidden.Clear();
            if (follow != null)
            {
                follow.transform.SetPositionAndRotation(previousPosition, previousRotation);
                follow.enabled = followWasEnabled;
            }
            if (viewCamera != null) viewCamera.fieldOfView = previousFov;
            if (viewButton != null) viewButton.GetComponentInChildren<Text>().text = "LIHAT ARENA";
        }

        private void OnDisable() => Restore();
        private void OnDestroy()
        {
            Restore();
            if (viewButton != null) viewButton.onClick.RemoveListener(Toggle);
        }
    }
}
