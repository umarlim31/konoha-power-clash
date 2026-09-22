using Konoha.Character;
using Konoha.Input;
using Unity.Netcode;
using UnityEngine;

namespace Konoha.Networking
{
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class NetworkPlayerMovement : NetworkBehaviour
    {
        public CharacterMotor motor;

        private readonly NetworkVariable<Vector3> syncedPosition = new NetworkVariable<Vector3>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private readonly NetworkVariable<Vector3> syncedEuler = new NetworkVariable<Vector3>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private TouchJoystick joystick;
        private MobileCombatCamera cameraFollow;
        private Vector3 remoteTargetPosition;
        private Quaternion remoteTargetRotation;
        private bool hasRemoteState;
        private float nextPublishTime;

        private void Awake()
        {
            if (motor == null)
                motor = GetComponent<CharacterMotor>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            syncedPosition.OnValueChanged += OnPositionChanged;
            syncedEuler.OnValueChanged += OnEulerChanged;

            remoteTargetPosition = transform.position;
            remoteTargetRotation = transform.rotation;

            if (IsOwner)
            {
                BindLocalControls();
                PublishState(true);
                Debug.Log("[KONOHA MOVE] Local movement bound | owner=" + OwnerClientId);
            }
            else
            {
                Debug.Log("[KONOHA MOVE] Remote movement observer ready | owner=" + OwnerClientId);
            }
        }

        public override void OnNetworkDespawn()
        {
            syncedPosition.OnValueChanged -= OnPositionChanged;
            syncedEuler.OnValueChanged -= OnEulerChanged;

            if (IsOwner && joystick != null)
                joystick.ResetInput();

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsSpawned)
                return;

            if (IsOwner)
            {
                if (joystick == null || motor == null)
                    BindLocalControls();

                if (joystick != null && motor != null)
                {
                    motor.Step(new MoveIntent(joystick.Value), Mathf.Min(Time.deltaTime, 0.05f));
                    PublishState(false);
                }
            }
            else if (hasRemoteState)
            {
                float positionBlend = 1f - Mathf.Exp(-14f * Time.deltaTime);
                float rotationBlend = 1f - Mathf.Exp(-18f * Time.deltaTime);

                transform.position = Vector3.Lerp(transform.position, remoteTargetPosition, positionBlend);
                transform.rotation = Quaternion.Slerp(transform.rotation, remoteTargetRotation, rotationBlend);
            }
        }

        private void BindLocalControls()
        {
            if (!IsOwner)
                return;

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
        }

        private void PublishState(bool force)
        {
            if (!IsOwner)
                return;

            if (!force && Time.unscaledTime < nextPublishTime)
                return;

            nextPublishTime = Time.unscaledTime + (1f / 30f);

            Vector3 position = transform.position;
            Vector3 euler = transform.eulerAngles;

            if (force || (syncedPosition.Value - position).sqrMagnitude > 0.000001f)
                syncedPosition.Value = position;

            if (force || DeltaEulerSqrMagnitude(syncedEuler.Value, euler) > 0.0001f)
                syncedEuler.Value = euler;
        }

        private void OnPositionChanged(Vector3 previous, Vector3 current)
        {
            if (IsOwner)
                return;

            remoteTargetPosition = current;
            hasRemoteState = true;
        }

        private void OnEulerChanged(Vector3 previous, Vector3 current)
        {
            if (IsOwner)
                return;

            remoteTargetRotation = Quaternion.Euler(current);
            hasRemoteState = true;
        }

        private static float DeltaEulerSqrMagnitude(Vector3 a, Vector3 b)
        {
            float x = Mathf.DeltaAngle(a.x, b.x);
            float y = Mathf.DeltaAngle(a.y, b.y);
            float z = Mathf.DeltaAngle(a.z, b.z);
            return x * x + y * y + z * z;
        }
    }
}
