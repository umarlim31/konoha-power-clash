using Konoha.Character;
using Konoha.Input;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Networking
{
    [RequireComponent(typeof(CharacterMotor))]
    [RequireComponent(typeof(CharacterController))]
    public sealed class NetworkPlayerMovement : NetworkBehaviour
    {
        public CharacterMotor motor;
        public float dodgeDistance = 2.6f;
        public float dodgeCooldown = 4f;

        private CharacterController controller;
        private NetworkPlayerCombat combat;
        private NetworkHeroKit heroKit;
        private TouchJoystick joystick;
        private MobileCombatCamera cameraFollow;
        private Button dodgeButton;
        private Text dodgeButtonLabel;
        private float nextDodgeTime;
        private float ignoreChairPinUntil;

        private void Awake()
        {
            if (motor == null)
                motor = GetComponent<CharacterMotor>();

            controller = GetComponent<CharacterController>();
            combat = GetComponent<NetworkPlayerCombat>();
            heroKit = GetComponent<NetworkHeroKit>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                BindLocalControls();
                Debug.Log("[KONOHA MOVE] Owner movement ready | client=" + OwnerClientId);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (dodgeButton != null)
                dodgeButton.onClick.RemoveListener(TryDodge);

            if (IsOwner && joystick != null)
                joystick.ResetInput();

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner)
                return;

            if (joystick == null || motor == null || cameraFollow == null || dodgeButton == null)
                BindLocalControls();

            NetworkMatchManager match = NetworkMatchManager.Instance;
            if (heroKit == null)
                heroKit = GetComponent<NetworkHeroKit>();

            bool knockedOut = combat != null && combat.IsKnockedOut;
            bool stunned = heroKit != null && heroKit.IsStunned;
            bool matchLocked = match == null || !match.AllowsGameplay;
            bool ruler = match != null &&
                         match.IsRuler(OwnerClientId) &&
                         Time.unscaledTime >= ignoreChairPinUntil;
            bool movementLocked = knockedOut || stunned || matchLocked || ruler;

            UpdateDodgeButtonVisual(knockedOut, stunned, matchLocked);

            if (motor != null)
                motor.speedMultiplier = heroKit != null ? heroKit.GetMovementSpeedMultiplier() : 1f;

            if (ruler)
            {
                joystick?.ResetInput();
                PinOwnerToChair(match);
                return;
            }

            if (movementLocked)
            {
                joystick?.ResetInput();
                return;
            }

            if (joystick != null && motor != null)
                motor.Step(new MoveIntent(joystick.Value), Mathf.Min(Time.deltaTime, 0.05f));
        }

        private void BindLocalControls()
        {
            if (!IsOwner)
                return;

            if (combat == null)
                combat = GetComponent<NetworkPlayerCombat>();

            if (heroKit == null)
                heroKit = GetComponent<NetworkHeroKit>();

            if (joystick == null)
                joystick = Object.FindFirstObjectByType<TouchJoystick>();

            if (cameraFollow == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                    cameraFollow = mainCamera.GetComponent<MobileCombatCamera>();
            }

            if (cameraFollow != null)
                cameraFollow.target = transform;

            if (dodgeButton == null)
            {
                GameObject dodgeObject = GameObject.Find("DodgeButton");
                if (dodgeObject != null)
                    dodgeButton = dodgeObject.GetComponent<Button>();

                if (dodgeButton != null)
                {
                    dodgeButtonLabel = dodgeButton.GetComponentInChildren<Text>();
                    dodgeButton.onClick.RemoveListener(TryDodge);
                    dodgeButton.onClick.AddListener(TryDodge);
                }
            }
        }

        public void TryDodge()
        {
            if (!IsOwner || !IsSpawned || controller == null)
                return;

            NetworkMatchManager match = NetworkMatchManager.Instance;

            if (combat != null && combat.IsKnockedOut)
                return;

            if (heroKit != null && heroKit.IsStunned)
                return;

            if (match == null || !match.AllowsGameplay)
                return;

            if (Time.unscaledTime < nextDodgeTime)
                return;

            bool leavingChair = match.IsRuler(OwnerClientId);
            if (leavingChair)
            {
                ignoreChairPinUntil = Time.unscaledTime + 0.45f;
                match.RequestChairActionFromLocal();
            }

            nextDodgeTime = Time.unscaledTime + dodgeCooldown;

            Vector3 direction = transform.forward;
            if (joystick != null && joystick.Value.sqrMagnitude > 0.01f)
                direction = new Vector3(joystick.Value.x, 0f, joystick.Value.y).normalized;

            if (leavingChair && direction.sqrMagnitude < 0.01f)
                direction = -Vector3.forward;

            controller.Move(direction.normalized * dodgeDistance);
            Debug.Log("[KONOHA MOVE] Dodge | client=" + OwnerClientId + " | leaveChair=" + leavingChair);
        }

        public void ResetLocalInput()
        {
            if (IsOwner)
                joystick?.ResetInput();
        }

        private void PinOwnerToChair(NetworkMatchManager match)
        {
            if (match == null || controller == null)
                return;

            Vector3 destination = match.ChairSeatPosition;
            Vector3 delta = destination - transform.position;

            if (delta.sqrMagnitude <= 0.0004f)
                return;

            controller.Move(delta);
        }

        private void UpdateDodgeButtonVisual(bool knockedOut, bool stunned, bool matchLocked)
        {
            if (dodgeButton == null)
                return;

            float remaining = Mathf.Max(0f, nextDodgeTime - Time.unscaledTime);

            if (knockedOut || stunned || matchLocked)
            {
                dodgeButton.interactable = false;

                if (dodgeButtonLabel != null)
                    dodgeButtonLabel.text = knockedOut
                        ? "DODGE\nRUNTUH"
                        : stunned
                            ? "DODGE\nSTUN"
                            : "DODGE";

                return;
            }

            dodgeButton.interactable = remaining <= 0f;

            if (dodgeButtonLabel != null)
                dodgeButtonLabel.text = remaining > 0f
                    ? "DODGE\n" + remaining.ToString("0.0")
                    : "DODGE";
        }
    }
}
