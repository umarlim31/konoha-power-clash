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
        public Text phaseText;
        public Text timerText;
        public Text rosterText;
        public Text cyanScoreText;
        public Text orangeScoreText;
        public Image cyanPowerFill;
        public Image orangePowerFill;
        public Image captureFill;
        public GameObject captureBarRoot;
        public Button startButton;
        public Button chairButton;
        public Button debugTimerButton;
        public Button debugPowerButton;
        public Button debugPengaruhButton;
        public Button debugToolsToggleButton;
        public GameObject debugToolsRoot;

        private Text startLabel;
        private Text chairLabel;
        private Text debugTimerLabel;
        private Text debugPowerLabel;
        private Text debugPengaruhLabel;
        private Text debugToolsToggleLabel;
        private bool debugToolsOpen;
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

            if (debugPengaruhButton != null)
            {
                debugPengaruhLabel = debugPengaruhButton.GetComponentInChildren<Text>();
                debugPengaruhButton.onClick.AddListener(OnDebugPengaruhPressed);
            }

            if (debugToolsToggleButton != null)
            {
                debugToolsToggleLabel = debugToolsToggleButton.GetComponentInChildren<Text>();
                debugToolsToggleButton.onClick.AddListener(OnDebugToolsTogglePressed);
            }

            debugToolsOpen = false;
            if (debugToolsRoot != null)
                debugToolsRoot.SetActive(false);
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

            if (debugPengaruhButton != null)
                debugPengaruhButton.onClick.RemoveListener(OnDebugPengaruhPressed);

            if (debugToolsToggleButton != null)
                debugToolsToggleButton.onClick.RemoveListener(OnDebugToolsTogglePressed);
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
                SetText(phaseText, "WAITING");
                SetText(timerText, "--:--");
                SetText(rosterText, "4v4 --/8");
                SetText(cyanScoreText, "CYAN 000");
                SetText(orangeScoreText, "ORANGE 000");
                SetFill(cyanPowerFill, 0f);
                SetFill(orangePowerFill, 0f);
                if (captureBarRoot != null) captureBarRoot.SetActive(false);
                SetButton(startButton, startLabel, false, "START MATCH");
                SetButton(chairButton, chairLabel, false, "DUDUK");
                SetButton(debugTimerButton, debugTimerLabel, false, "TEST TIMER 10s");
                SetButton(debugPowerButton, debugPowerLabel, false, "TEST POWER 95");
                SetButton(debugPengaruhButton, debugPengaruhLabel, false, "TEST ULT 100");
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
                "REBUT KURSI | " + stateText + " " + timerText +
                " | " + manager.ActorCount + "/8" +
                " (" + manager.PlayerCount + "H+" + manager.BotCount + "B)");

            SetText(phaseText, stateText);
            SetText(this.timerText, timerText);
            SetText(
                rosterText,
                manager.ActorCount + "/8  •  " +
                manager.PlayerCount + "H / " + manager.BotCount + "B");

            SetText(cyanScoreText, "CYAN  " + manager.CyanPower.ToString("000"));
            SetText(orangeScoreText, "ORANGE  " + manager.OrangePower.ToString("000"));
            SetFill(cyanPowerFill, manager.CyanPower / (float)NetworkMatchManager.PowerToWin);
            SetFill(orangePowerFill, manager.OrangePower / (float)NetworkMatchManager.PowerToWin);
            RefreshCaptureBar();

            if (manager.State == GreyboxMatchState.SuddenPower)
            {
                SetText(
                    scoreText,
                    "POWER | CYAN " + manager.CyanPower + " • ORANGE " + manager.OrangePower +
                    " | SUDDEN " + manager.SuddenCyanPower + "-" + manager.SuddenOrangePower +
                    "/" + NetworkMatchManager.SuddenPowerToWin);
            }
            else
            {
                SetText(
                    scoreText,
                    "POWER | CYAN " + manager.CyanPower + "/" + NetworkMatchManager.PowerToWin +
                    " • ORANGE " + manager.OrangePower + "/" + NetworkMatchManager.PowerToWin);
            }

            SetText(objectiveText, GetObjectiveText());
        }

        private string GetObjectiveText()
        {
            if (manager.State == GreyboxMatchState.SuddenPower)
                return "KURSI EMAS | FIRST +5 POWER WINS";

            if (manager.HasRuler)
            {
                NetworkObject ruler = FindActor(manager.RulerNetworkObjectId);

                if (ruler != null)
                {
                    int team = NetworkTeamUtility.GetTeam(ruler);
                    NetworkBotController bot = ruler.GetComponent<NetworkBotController>();

                    string rulerName = bot != null
                        ? "BOT " + bot.Slot + " " + bot.HeroName
                        : manager.RulerClientId != NetworkMatchManager.NoClient
                            ? "P" + manager.RulerClientId
                            : "ACTOR";

                    return "PENGUASA | " + rulerName + " " +
                           NetworkTeamUtility.GetTeamName(team) + " | +1/s";
                }

                return "PENGUASA AKTIF | +1 POWER/DETIK";
            }

            if (manager.IsContested)
                return "KURSI DIPEREBUTKAN | CAPTURE PAUSE";

            if (manager.CaptureTeam >= 0)
            {
                int percent = Mathf.RoundToInt(manager.CaptureProgress * 100f);
                return "CAPTURE " + NetworkTeamUtility.GetTeamName(manager.CaptureTeam) +
                       " | " + percent + "%";
            }

            if (manager.ChairOwnerTeam >= 0)
                return "KURSI " +
                       NetworkTeamUtility.GetTeamName(manager.ChairOwnerTeam) +
                       " | DEKATI + DUDUK";

            return "KURSI: NETRAL | MASUK ZONA UNTUK CAPTURE";
        }

        private void RefreshButtons()
        {
            bool isHostUi = manager.IsServer;
            bool showStart = isHostUi &&
                             (manager.State == GreyboxMatchState.Waiting ||
                              manager.State == GreyboxMatchState.Result);
            SetHostOnlyVisible(startButton, showStart);
            SetHostOnlyVisible(debugToolsToggleButton, isHostUi);

            if (debugToolsRoot != null)
                debugToolsRoot.SetActive(isHostUi && debugToolsOpen);

            SetHostOnlyVisible(debugTimerButton, isHostUi && debugToolsOpen);
            SetHostOnlyVisible(debugPowerButton, isHostUi && debugToolsOpen);
            SetHostOnlyVisible(debugPengaruhButton, isHostUi && debugToolsOpen);

            if (debugToolsToggleLabel != null)
                debugToolsToggleLabel.text = debugToolsOpen ? "DEV ×" : "DEV";

            bool hostCanStart = manager.CanHostStart;
            string startCaption = manager.State == GreyboxMatchState.Result ? "REMATCH" : "START";
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
            SetButton(debugPengaruhButton, debugPengaruhLabel, manager.CanUseDebugTest, "TEST ULT 100");
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

        private void OnDebugPengaruhPressed()
        {
            manager?.RequestDebugPengaruhFromLocal();
        }

        private void OnDebugToolsTogglePressed()
        {
            debugToolsOpen = !debugToolsOpen;

            if (debugToolsRoot != null)
                debugToolsRoot.SetActive(debugToolsOpen);
        }

        private void RefreshCaptureBar()
        {
            if (captureBarRoot == null || captureFill == null || manager == null)
                return;

            if (manager.IsContested)
            {
                captureBarRoot.SetActive(true);
                captureFill.color = new Color(1f, 0.82f, 0.18f, 0.95f);
                captureFill.fillAmount = 1f;
                return;
            }

            if (manager.CaptureTeam >= 0)
            {
                captureBarRoot.SetActive(true);
                captureFill.color = NetworkTeamUtility.GetTeamColor(manager.CaptureTeam);
                captureFill.fillAmount = Mathf.Clamp01(manager.CaptureProgress);
                return;
            }

            captureBarRoot.SetActive(false);
        }

        private static NetworkObject FindActor(ulong networkObjectId)
        {
            if (networkObjectId == NetworkMatchManager.NoClient ||
                NetworkManager.Singleton == null ||
                NetworkManager.Singleton.SpawnManager == null)
                return null;

            foreach (NetworkObject networkObject in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject != null && networkObject.NetworkObjectId == networkObjectId)
                    return networkObject;
            }

            return null;
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

        private static void SetFill(Image image, float value)
        {
            if (image != null)
                image.fillAmount = Mathf.Clamp01(value);
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
                target.text = value;
        }

        private static void SetHostOnlyVisible(Button button, bool visible)
        {
            if (button != null && button.gameObject.activeSelf != visible)
                button.gameObject.SetActive(visible);
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
