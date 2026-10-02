using Konoha.Data;
using Konoha.Input;
using UnityEngine;

namespace Konoha.Character
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMotor : MonoBehaviour
    {
        public LocomotionDefinition definition;
        private CharacterController controller;
        private float verticalSpeed;
        // Opt-in for Jalur Takhta. Existing PvP prefabs retain allowJump=false.
        public bool allowJump;
        [Min(.1f)] public float jumpHeight = 1.25f;
        private bool jumpPending;
        public float speedMultiplier = 1f;
        public bool Grounded => controller != null && controller.isGrounded;
        public Vector3 Velocity => controller != null ? controller.velocity : Vector3.zero;
        private void Awake() => controller = GetComponent<CharacterController>();

        public bool TryJump()
        {
            if (!allowJump || definition == null || !Grounded || jumpPending || verticalSpeed > 0f || definition.gravity >= 0f) return false;
            verticalSpeed = Mathf.Sqrt(-2f * definition.gravity * jumpHeight);
            jumpPending = true;
            return true;
        }

        public void Teleport(Vector3 position, bool resetVerticalSpeed = true)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.position = position;
            controller.enabled = wasEnabled;
            if (resetVerticalSpeed) { verticalSpeed = 0f; jumpPending = false; }
        }

        // Caller supplies both intent and timestep. This is not a network authority layer.
        public void Step(MoveIntent intent, float deltaTime)
        {
            if (definition == null || deltaTime <= 0f) return;
            if (controller == null) controller = GetComponent<CharacterController>();
            if (Grounded && verticalSpeed < 0f) verticalSpeed = definition.groundStickSpeed;
            verticalSpeed += definition.gravity * deltaTime;
            Vector3 direction = intent.WorldDirection;
            float effectiveSpeed = definition.speed * Mathf.Max(0.1f, speedMultiplier);
            var collision = controller.Move((direction * effectiveSpeed + Vector3.up * verticalSpeed) * deltaTime);
            if (!Grounded || verticalSpeed <= 0f) jumpPending = false;
            if ((collision & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(direction), definition.turnDegreesPerSecond * deltaTime);
        }
    }
}
