using Unity.Netcode;
using UnityEngine;

namespace Konoha.Networking
{
    public enum GreyboxMatchState
    {
        Waiting = 0,
        Countdown = 1,
        Playing = 2,
        Overtime = 3,
        SuddenPower = 4,
        Result = 5
    }

    public sealed class NetworkMatchManager : NetworkBehaviour
    {
        public const ulong NoClient = ulong.MaxValue;
        public const int PowerToWin = 100;
        public const int SuddenPowerToWin = 5;
        public const float MatchDurationSeconds = 420f;
        public const float CountdownSeconds = 5f;
        public const float CaptureSeconds = 3f;
        public const float CaptureRadius = 3.4f;
        public const float SitRadius = 1.8f;

        public static NetworkMatchManager Instance { get; private set; }

        public Vector3 chairPosition = Vector3.zero;

        private NetworkVariable<int> state = new NetworkVariable<int>(
            (int)GreyboxMatchState.Waiting,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<float> countdownRemaining = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<float> matchTimeRemaining = new NetworkVariable<float>(
            MatchDurationSeconds,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<int> cyanPower = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<int> orangePower = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<int> suddenCyanPower = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<int> suddenOrangePower = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<int> chairOwnerTeam = new NetworkVariable<int>(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<int> captureTeam = new NetworkVariable<int>(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<float> captureProgress = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<bool> contested = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<ulong> rulerClientId = new NetworkVariable<ulong>(
            NoClient,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<int> winnerTeam = new NetworkVariable<int>(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<int> playerCount = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private float powerAccumulator;

        public GreyboxMatchState State => (GreyboxMatchState)state.Value;
        public float CountdownRemaining => countdownRemaining.Value;
        public float MatchTimeRemaining => matchTimeRemaining.Value;
        public int CyanPower => cyanPower.Value;
        public int OrangePower => orangePower.Value;
        public int SuddenCyanPower => suddenCyanPower.Value;
        public int SuddenOrangePower => suddenOrangePower.Value;
        public int ChairOwnerTeam => chairOwnerTeam.Value;
        public int CaptureTeam => captureTeam.Value;
        public float CaptureProgress => captureProgress.Value;
        public bool IsContested => contested.Value;
        public ulong RulerClientId => rulerClientId.Value;
        public int WinnerTeam => winnerTeam.Value;
        public int PlayerCount => playerCount.Value;
        public Vector3 ChairPosition => chairPosition;
        public Vector3 ChairSeatPosition => chairPosition + new Vector3(0f, 0.1f, 0f);

        public bool AllowsGameplay =>
            State == GreyboxMatchState.Playing ||
            State == GreyboxMatchState.Overtime ||
            State == GreyboxMatchState.SuddenPower;

        public bool CanHostStart =>
            IsServer &&
            (State == GreyboxMatchState.Waiting || State == GreyboxMatchState.Result) &&
            PlayerCount >= 2;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Instance = this;

            if (IsServer)
                ResetWaitingState();

            Debug.Log("[KONOHA MATCH] Match manager spawned | server=" + IsServer);
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this)
                Instance = null;

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsServer || !IsSpawned)
                return;

            playerCount.Value = CountPlayerObjects();

            float deltaTime = Mathf.Min(Time.deltaTime, 0.05f);

            switch (State)
            {
                case GreyboxMatchState.Waiting:
                case GreyboxMatchState.Result:
                    return;

                case GreyboxMatchState.Countdown:
                    countdownRemaining.Value = Mathf.Max(0f, countdownRemaining.Value - deltaTime);
                    if (countdownRemaining.Value <= 0f)
                        BeginPlaying();
                    return;

                case GreyboxMatchState.Playing:
                    matchTimeRemaining.Value = Mathf.Max(0f, matchTimeRemaining.Value - deltaTime);
                    ProcessObjective(deltaTime);
                    CheckPowerVictory();

                    if (State == GreyboxMatchState.Playing && matchTimeRemaining.Value <= 0f)
                        ResolveExpiredTime();
                    return;

                case GreyboxMatchState.Overtime:
                    ProcessObjective(deltaTime);
                    CheckPowerVictory();

                    if (State == GreyboxMatchState.Overtime && !contested.Value)
                        ResolveExpiredTime();
                    return;

                case GreyboxMatchState.SuddenPower:
                    ProcessObjective(deltaTime);
                    CheckSuddenPowerVictory();
                    return;
            }
        }

        public void RequestStartOrRematchFromLocal()
        {
            if (!IsServer || !CanHostStart)
                return;

            StartCountdown();
        }

        public void RequestChairActionFromLocal()
        {
            if (!IsSpawned)
                return;

            ChairActionServerRpc();
        }

        public bool IsRuler(ulong clientId)
        {
            return rulerClientId.Value == clientId;
        }

        public bool CanLocalPlayerSit()
        {
            if (!AllowsGameplay || NetworkManager == null)
                return false;

            ulong localClientId = NetworkManager.LocalClientId;

            if (rulerClientId.Value == localClientId)
                return true;

            if (rulerClientId.Value != NoClient)
                return false;

            int team = NetworkTeamUtility.GetTeam(localClientId);
            if (chairOwnerTeam.Value != team)
                return false;

            NetworkPlayerCombat combat = FindPlayerCombat(localClientId);
            if (combat == null || combat.IsKnockedOut)
                return false;

            return HorizontalDistanceSqr(combat.transform.position, ChairSeatPosition) <= SitRadius * SitRadius;
        }

        public void HandlePlayerKnockedOut(ulong clientId)
        {
            if (!IsServer)
                return;

            if (rulerClientId.Value == clientId)
                ClearRuler();
        }

        [ServerRpc(RequireOwnership = false)]
        private void ChairActionServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!AllowsGameplay)
                return;

            ulong senderClientId = rpcParams.Receive.SenderClientId;

            if (rulerClientId.Value == senderClientId)
            {
                ClearRuler();
                Debug.Log("[KONOHA MATCH] Ruler left chair | client=" + senderClientId);
                return;
            }

            if (rulerClientId.Value != NoClient)
                return;

            NetworkPlayerCombat combat = FindPlayerCombat(senderClientId);
            if (combat == null || combat.IsKnockedOut)
                return;

            int team = NetworkTeamUtility.GetTeam(senderClientId);
            if (chairOwnerTeam.Value != team)
                return;

            if (HorizontalDistanceSqr(combat.transform.position, ChairSeatPosition) > SitRadius * SitRadius)
                return;

            rulerClientId.Value = senderClientId;
            powerAccumulator = 0f;
            captureTeam.Value = -1;
            captureProgress.Value = 0f;
            contested.Value = false;

            Debug.Log(
                "[KONOHA MATCH] PENGUASA seated | client=" + senderClientId +
                " | team=" + NetworkTeamUtility.GetTeamName(team));
        }

        private void StartCountdown()
        {
            cyanPower.Value = 0;
            orangePower.Value = 0;
            suddenCyanPower.Value = 0;
            suddenOrangePower.Value = 0;
            winnerTeam.Value = -1;
            chairOwnerTeam.Value = -1;
            captureTeam.Value = -1;
            captureProgress.Value = 0f;
            contested.Value = false;
            rulerClientId.Value = NoClient;
            powerAccumulator = 0f;
            matchTimeRemaining.Value = MatchDurationSeconds;
            countdownRemaining.Value = CountdownSeconds;

            ResetAllPlayersForMatch();

            state.Value = (int)GreyboxMatchState.Countdown;

            Debug.Log("[KONOHA MATCH] Countdown started | players=" + PlayerCount);
        }

        private void BeginPlaying()
        {
            countdownRemaining.Value = 0f;
            state.Value = (int)GreyboxMatchState.Playing;
            Debug.Log("[KONOHA MATCH] PLAYING");
        }

        private void ResolveExpiredTime()
        {
            if (contested.Value)
            {
                state.Value = (int)GreyboxMatchState.Overtime;
                Debug.Log("[KONOHA MATCH] MASA KRITIS / OVERTIME");
                return;
            }

            if (cyanPower.Value == orangePower.Value)
            {
                state.Value = (int)GreyboxMatchState.SuddenPower;
                suddenCyanPower.Value = 0;
                suddenOrangePower.Value = 0;
                powerAccumulator = 0f;
                Debug.Log("[KONOHA MATCH] SUDDEN POWER");
                return;
            }

            FinishMatch(cyanPower.Value > orangePower.Value
                ? NetworkTeamUtility.CyanTeam
                : NetworkTeamUtility.OrangeTeam);
        }

        private void ProcessObjective(float deltaTime)
        {
            if (rulerClientId.Value != NoClient)
            {
                NetworkPlayerCombat ruler = FindPlayerCombat(rulerClientId.Value);
                int rulerTeam = NetworkTeamUtility.GetTeam(rulerClientId.Value);

                bool invalidRuler =
                    ruler == null ||
                    ruler.IsKnockedOut ||
                    rulerTeam != chairOwnerTeam.Value ||
                    HorizontalDistanceSqr(ruler.transform.position, ChairSeatPosition) >
                    (SitRadius + 0.85f) * (SitRadius + 0.85f);

                if (invalidRuler)
                {
                    ClearRuler();
                }
                else
                {
                    contested.Value = false;
                    captureTeam.Value = -1;
                    captureProgress.Value = 0f;

                    powerAccumulator += deltaTime;
                    while (powerAccumulator >= 1f)
                    {
                        powerAccumulator -= 1f;
                        AwardPower(rulerTeam);
                    }

                    return;
                }
            }

            powerAccumulator = 0f;

            int cyanInside = 0;
            int orangeInside = 0;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject == null || !networkObject.IsPlayerObject)
                    continue;

                NetworkPlayerCombat combat = networkObject.GetComponent<NetworkPlayerCombat>();
                if (combat == null || combat.IsKnockedOut)
                    continue;

                if (HorizontalDistanceSqr(combat.transform.position, ChairPosition) > CaptureRadius * CaptureRadius)
                    continue;

                int team = NetworkTeamUtility.GetTeam(networkObject.OwnerClientId);
                if (team == NetworkTeamUtility.CyanTeam)
                    cyanInside++;
                else
                    orangeInside++;
            }

            if (cyanInside > 0 && orangeInside > 0)
            {
                contested.Value = true;
                return;
            }

            contested.Value = false;

            int activeTeam = cyanInside > 0
                ? NetworkTeamUtility.CyanTeam
                : orangeInside > 0
                    ? NetworkTeamUtility.OrangeTeam
                    : -1;

            if (activeTeam < 0)
            {
                captureTeam.Value = -1;
                captureProgress.Value = 0f;
                return;
            }

            if (chairOwnerTeam.Value == activeTeam)
            {
                captureTeam.Value = -1;
                captureProgress.Value = 0f;
                return;
            }

            if (captureTeam.Value != activeTeam)
            {
                captureTeam.Value = activeTeam;
                captureProgress.Value = 0f;
            }

            captureProgress.Value = Mathf.Clamp01(captureProgress.Value + deltaTime / CaptureSeconds);

            if (captureProgress.Value >= 1f)
            {
                chairOwnerTeam.Value = activeTeam;
                captureTeam.Value = -1;
                captureProgress.Value = 0f;

                Debug.Log(
                    "[KONOHA MATCH] Chair captured | team=" +
                    NetworkTeamUtility.GetTeamName(activeTeam));
            }
        }

        private void AwardPower(int team)
        {
            if (State == GreyboxMatchState.SuddenPower)
            {
                if (team == NetworkTeamUtility.CyanTeam)
                    suddenCyanPower.Value++;
                else
                    suddenOrangePower.Value++;

                CheckSuddenPowerVictory();
                return;
            }

            if (team == NetworkTeamUtility.CyanTeam)
                cyanPower.Value = Mathf.Min(PowerToWin, cyanPower.Value + 1);
            else
                orangePower.Value = Mathf.Min(PowerToWin, orangePower.Value + 1);

            CheckPowerVictory();
        }

        private void CheckPowerVictory()
        {
            if (cyanPower.Value >= PowerToWin)
                FinishMatch(NetworkTeamUtility.CyanTeam);
            else if (orangePower.Value >= PowerToWin)
                FinishMatch(NetworkTeamUtility.OrangeTeam);
        }

        private void CheckSuddenPowerVictory()
        {
            if (suddenCyanPower.Value >= SuddenPowerToWin)
                FinishMatch(NetworkTeamUtility.CyanTeam);
            else if (suddenOrangePower.Value >= SuddenPowerToWin)
                FinishMatch(NetworkTeamUtility.OrangeTeam);
        }

        private void FinishMatch(int team)
        {
            winnerTeam.Value = team;
            state.Value = (int)GreyboxMatchState.Result;
            ClearRuler();
            contested.Value = false;
            captureTeam.Value = -1;
            captureProgress.Value = 0f;

            Debug.Log(
                "[KONOHA MATCH] RESULT | winner=" +
                NetworkTeamUtility.GetTeamName(team));
        }

        private void ClearRuler()
        {
            rulerClientId.Value = NoClient;
            powerAccumulator = 0f;
        }

        private void ResetWaitingState()
        {
            state.Value = (int)GreyboxMatchState.Waiting;
            countdownRemaining.Value = 0f;
            matchTimeRemaining.Value = MatchDurationSeconds;
            cyanPower.Value = 0;
            orangePower.Value = 0;
            suddenCyanPower.Value = 0;
            suddenOrangePower.Value = 0;
            chairOwnerTeam.Value = -1;
            captureTeam.Value = -1;
            captureProgress.Value = 0f;
            contested.Value = false;
            rulerClientId.Value = NoClient;
            winnerTeam.Value = -1;
            powerAccumulator = 0f;
        }

        private void ResetAllPlayersForMatch()
        {
            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject == null || !networkObject.IsPlayerObject)
                    continue;

                NetworkPlayerCombat combat = networkObject.GetComponent<NetworkPlayerCombat>();
                if (combat != null)
                    combat.ServerResetForMatch();
            }
        }

        private int CountPlayerObjects()
        {
            int count = 0;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject != null && networkObject.IsPlayerObject)
                    count++;
            }

            return count;
        }

        private NetworkPlayerCombat FindPlayerCombat(ulong clientId)
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
                return null;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject == null ||
                    !networkObject.IsPlayerObject ||
                    networkObject.OwnerClientId != clientId)
                    continue;

                return networkObject.GetComponent<NetworkPlayerCombat>();
            }

            return null;
        }

        private static float HorizontalDistanceSqr(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z;
        }
    }
}
