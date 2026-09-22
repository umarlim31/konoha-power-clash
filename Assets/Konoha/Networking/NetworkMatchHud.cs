using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Networking
{
    public sealed class NetworkMatchHud : MonoBehaviour
    {
        public Text matchText;
        public Text scoreText;
        public Text objectiveText;
        public Button startButton;
        public Button chairButton;
        public Button debugTimerButton;
        public Button debugPowerButton;

        private Text startLabel;
        private Text chairLabel;
        private Text debugTimerLabel;
        private Text debugPowerLabel;
        private NetworkMatchManager manager;

        private void Awake()
        {
            if (startButton != null)
            {
                startLabel = startButton.GetComponentInChildren<Text>();
                startButton.onClick.AddListener(OnStartPressed);
            }

            if (chairButton != null)
            {
                chairLabel = chairButton.GetComponentInChildren<Text>();
                chairButton.onClick.AddListener(OnChairPressed);
            }

            if (debugTimerButton != null)
            {
                debugTimerLabel = debugTimerButton.GetComponentInChildren<Text>();
                debugTimerButton.onClick.AddListener(OnDebugTimerPressed);
            }

            if (debugPowerButton != null)
            {
                debugPowerLabel = debugPowerButton.GetComponentInChildren<Text>();
                debugPowerButton.onClick.AddListener(OnDebugPowerPressed);
            }
        }

        private void OnDestroy()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartPressed);

            if (chairButton != null)
                chairButton.onClick.RemoveListener(OnChairPressed);

            if (debugTimerButton != null)
                debugTimerButton.onClick.RemoveListener(OnDebugTimerPressed);

            if (debugPowerButton != null)
                debugPowerButton.onClick.RemoveListener(OnDebugPowerPressed);
        }

        private void Update()
        {
            if (manager == null || !manager.IsSpawned)
                manager = NetworkMatchManager.Instance;

            if (manager == null)
            {
                SetText(matchText, "REBUT KURSI | WAITING FOR MATCH STATE");
                SetText(scoreText, "CYAN 0 | ORANGE 0");
                SetText(objectiveText, "KURSI: OFFLINE");
                SetButton(startButton, startLabel, false, "START MATCH");
                SetButton(chairButton, chairLabel, false, "DUDUK");
                SetButton(debugTimerButton, debugTimerLabel, false, "TEST TIMER 10s");
                SetButton(debugPowerButton, debugPowerLabel, false, "TEST POWER 95");
                return;
            }

            RefreshMatch();
            RefreshButtons();
        }

        private void RefreshMatch()
        {
            string stateText = GetStateText(manager.State);
            string timerText = manager.State == GreyboxMatchState.Countdown
                ? "START " + Mathf.CeilToInt(manager.CountdownRemaining)
                : FormatTime(manager.MatchTimeRemaining);

            if (manager.State == GreyboxMatchState.Result)
                stateText = "RESULT | TEAM " + NetworkTeamUtility.GetTeamName(manager.WinnerTeam) + " MENANG";

            SetText(
                matchText,
                "REBUT KURSI | " + stateText + " | " + timerText + " | PLAYERS " + manager.PlayerCount);

            if (manager.State == GreyboxMatchState.SuddenPower)
            {
                SetText(
                    scoreText,
                    "POWER CYAN " + manager.CyanPower + " | ORANGE " + manager.OrangePower +
                    "   ||   SUDDEN " + manager.SuddenCyanPower + "-" + manager.SuddenOrangePower +
                    " / " + NetworkMatchManager.SuddenPowerToWin);
            }
            else
            {
                SetText(
                    scoreText,
                    "POWER   CYAN " + manager.CyanPower + "/" + NetworkMatchManager.PowerToWin +
                    "   |   ORANGE " + manager.OrangePower + "/" + NetworkMatchManager.PowerToWin);
            }

            SetText(objectiveText, GetObjectiveText());
        }

        private string GetObjectiveText()
        {
            if (manager.State == GreyboxMatchState.SuddenPower)
                return "KURSI EMAS | FIRST +5 POWER WINS";

            if (manager.RulerClientId != NetworkMatchManager.NoClient)
            {
                int team = NetworkTeamUtility.GetTeam(manager.RulerClientId);
                return "PENGUASA: P" + manager.RulerClientId + " TEAM " +
                       NetworkTeamUtility.GetTeamName(team) + " | +1 POWER/DETIK";
            }

            if (manager.IsContested)
                return "KURSI: DIPEREBUTKAN | CAPTURE BERHENTI";

            if (manager.CaptureTeam >= 0)
            {
                int percent = Mathf.RoundToInt(manager.CaptureProgress * 100f);
                return "CAPTURE TEAM " + NetworkTeamUtility.GetTeamName(manager.CaptureTeam) +
                       " | " + percent + "%";
            }

            if (manager.ChairOwnerTeam >= 0)
                return "KURSI DIKUASAI TEAM " +
                       NetworkTeamUtility.GetTeamName(manager.ChairOwnerTeam) +
                       " | DEKATI KURSI LALU DUDUK";

            return "KURSI: NETRAL | MASUK ZONA UNTUK CAPTURE";
        }

        private void RefreshButtons()
        {
            bool hostCanStart = manager.CanHostStart;
            string startCaption = manager.State == GreyboxMatchState.Result ? "REMATCH" : "START MATCH";
            SetButton(startButton, startLabel, hostCanStart, startCaption);

            ulong localClientId = NetworkManager.Singleton != null
                ? NetworkManager.Singleton.LocalClientId
                : NetworkMatchManager.NoClient;

            bool localIsRuler = localClientId != NetworkMatchManager.NoClient &&
                                manager.IsRuler(localClientId);

            bool chairInteractable = manager.CanLocalPlayerSit();
            SetButton(
                chairButton,
                chairLabel,
                chairInteractable,
                localIsRuler ? "TURUN" : "DUDUK");

            SetButton(debugTimerButton, debugTimerLabel, manager.CanUseDebugTest, "TEST TIMER 10s");
            SetButton(debugPowerButton, debugPowerLabel, manager.CanUseDebugTest, "TEST POWER 95");
        }

        private void OnStartPressed()
        {
            manager?.RequestStartOrRematchFromLocal();
        }

        private void OnChairPressed()
        {
            manager?.RequestChairActionFromLocal();
        }

        private void OnDebugTimerPressed()
        {
            manager?.RequestDebugTimerFromLocal();
        }

        private void OnDebugPowerPressed()
        {
            manager?.RequestDebugPowerFromLocal();
        }

        private static string GetStateText(GreyboxMatchState state)
        {
            switch (state)
            {
                case GreyboxMatchState.Waiting: return "WAITING";
                case GreyboxMatchState.Countdown: return "COUNTDOWN";
                case GreyboxMatchState.Playing: return "PLAYING";
                case GreyboxMatchState.Overtime: return "MASA KRITIS";
                case GreyboxMatchState.SuddenPower: return "SUDDEN POWER";
                case GreyboxMatchState.Result: return "RESULT";
                default: return state.ToString().ToUpperInvariant();
            }
        }

        private static string FormatTime(float seconds)
        {
            int value = Mathf.Max(0, Mathf.CeilToInt(seconds));
            int minutes = value / 60;
            int remainingSeconds = value % 60;
            return minutes.ToString("00") + ":" + remainingSeconds.ToString("00");
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
                target.text = value;
        }

        private static void SetButton(Button button, Text label, bool interactable, string caption)
        {
            if (button != null)
                button.interactable = interactable;

            if (label != null)
                label.text = caption;
        }
    }
}
