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
        public float speedMultiplier = 1f;
        public bool Grounded => controller != null && controller.isGrounded;
        public Vector3 Velocity => controller != null ? controller.velocity : Vector3.zero;
        private void Awake() => controller = GetComponent<CharacterController>();

        // Caller supplies both intent and timestep. This is not a network authority layer.
        public void Step(MoveIntent intent, float deltaTime)
        {
            if (definition == null || deltaTime <= 0f) return;
            if (Grounded && verticalSpeed < 0f) verticalSpeed = definition.groundStickSpeed;
            verticalSpeed += definition.gravity * deltaTime;
            Vector3 direction = intent.WorldDirection;
            float effectiveSpeed = definition.speed * Mathf.Max(0.1f, speedMultiplier);
            var collision = controller.Move((direction * effectiveSpeed + Vector3.up * verticalSpeed) * deltaTime);
            if ((collision & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(direction), definition.turnDegreesPerSecond * deltaTime);
        }
    }
}
