using Konoha.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    [RequireComponent(typeof(Button))]
    public sealed class CampaignCameraReset : MonoBehaviour
    {
        public MobileCombatCamera follow;
        private void Start() => GetComponent<Button>().onClick.AddListener(ResetView);
        private void ResetView() => follow.ResetOrbit();
        private void OnDestroy()
        {
            var button = GetComponent<Button>();
            if (button != null) button.onClick.RemoveListener(ResetView);
        }
    }
}
