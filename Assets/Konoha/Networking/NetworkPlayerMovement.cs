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
        public float dodgeCooldown = 1.15f;

        private CharacterController controller;
        private NetworkPlayerCombat combat;
        private TouchJoystick joystick;
        private MobileCombatCamera cameraFollow;
        private Button dodgeButton;
        private Text dodgeButtonLabel;
        private float nextDodgeTime;

        private void Awake()
        {
            if (motor == null)
                motor = GetComponent<CharacterMotor>();

            controller = GetComponent<CharacterController>();
            combat = GetComponent<NetworkPlayerCombat>();
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

            bool locked = combat != null && combat.IsKnockedOut;
            UpdateDodgeButtonVisual(locked);

            if (locked)
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

            if (combat != null && combat.IsKnockedOut)
                return;

            if (Time.unscaledTime < nextDodgeTime)
                return;

            nextDodgeTime = Time.unscaledTime + dodgeCooldown;

            Vector3 direction = transform.forward;
            if (joystick != null && joystick.Value.sqrMagnitude > 0.01f)
                direction = new Vector3(joystick.Value.x, 0f, joystick.Value.y).normalized;

            controller.Move(direction * dodgeDistance);
            Debug.Log("[KONOHA MOVE] Dodge | client=" + OwnerClientId);
        }

        public void ResetLocalInput()
        {
            if (IsOwner)
                joystick?.ResetInput();
        }

        private void UpdateDodgeButtonVisual(bool locked)
        {
            if (dodgeButton == null)
                return;

            float remaining = Mathf.Max(0f, nextDodgeTime - Time.unscaledTime);

            if (locked)
            {
                dodgeButton.interactable = false;
                if (dodgeButtonLabel != null)
                    dodgeButtonLabel.text = "DODGE\nKO";
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
