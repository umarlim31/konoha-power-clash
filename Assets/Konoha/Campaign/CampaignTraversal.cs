using Konoha.Character;
using Konoha.Input;
using Konoha.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // Solo input driver: camera-relative joystick, jump, invisible oval boundary and fall
    // recovery. Since 0.0.9 it drives the networked hero (CampaignSession.Bind); the hero's
    // NetworkPlayerMovement then only supplies locks (Runtuh, stun), speed and dodge.
    // PvP network movement keeps its original behaviour.
    public sealed class CampaignTraversal : MonoBehaviour
    {
        public CharacterMotor motor;
        // Optional lock source; null while driving the offline character.
        public NetworkPlayerMovement gate;
        public TouchJoystick joystick;
        public Transform movementCamera;
        public Button jumpButton;
        // §9: seated on the Kursi, the joystick and jump do nothing (BERDIRI stands up).
        public bool seatLocked;
        public Vector2 boundaryCenter = new Vector2(0, 4);
        public Vector2 boundaryRadii = new Vector2(26, 29);
        private Vector3 lastSafe = new Vector3(0, .1f, -9);
        private bool focused = true;
        private bool paused;

        private void Start() => jumpButton.onClick.AddListener(Jump);

        public void Bind(CharacterMotor newMotor, NetworkPlayerMovement newGate)
        {
            motor = newMotor;
            gate = newGate;
            if (motor != null)
                lastSafe = motor.transform.position + Vector3.up * .08f;
            joystick?.ResetInput();
        }

        private bool Locked => gate != null && gate.LocomotionLocked;

        private void Update()
        {
            if (!focused || paused || Time.timeScale <= 0f || motor == null || Locked) return;
            float dt = Mathf.Min(Time.deltaTime, .05f);
            Vector2 axis = seatLocked ? Vector2.zero : CameraRelative(joystick.Value, movementCamera.forward);
            // Clip intended horizontal movement first, preserving grounding and jump at the boundary.
            float travel = motor.definition.speed * Mathf.Max(.1f, motor.speedMultiplier) * dt;
            if (travel > .00001f)
            {
                Vector3 start = motor.transform.position;
                Vector3 desired = start + new Vector3(axis.x, 0, axis.y) * travel;
                Vector3 permitted = ClampToCampus(desired, boundaryCenter, boundaryRadii) - start;
                axis = new Vector2(permitted.x, permitted.z) / travel;
            }
            motor.Step(new MoveIntent(axis), dt);
            Vector3 position = motor.transform.position;
            if (position.y < -3f)
            {
                motor.Teleport(lastSafe);
                joystick.ResetInput();
                return;
            }
            Vector3 clamped = ClampToCampus(position, boundaryCenter, boundaryRadii);
            if ((clamped - position).sqrMagnitude > .000001f)
                motor.Teleport(clamped, false);
            // Save only stable dry locations; falling under geometry never overwrites the recovery point.
            if (motor.Grounded && position.y >= -.05f && !InCanal(position))
                lastSafe = new Vector3(clamped.x, clamped.y + .08f, clamped.z);
        }

        public void Jump()
        {
            if (focused && !paused && Time.timeScale > 0f && motor != null && !Locked && !seatLocked) motor.TryJump();
        }

        public static Vector2 CameraRelative(Vector2 axis, Vector3 forward)
        {
            forward.y = 0;
            if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0, -forward.x);
            Vector3 world = right * axis.x + forward * axis.y;
            return Vector2.ClampMagnitude(new Vector2(world.x, world.z), 1f);
        }

        public static Vector3 ClampToCampus(Vector3 position, Vector2 center, Vector2 radii)
        {
            float rx = Mathf.Max(1, radii.x), rz = Mathf.Max(1, radii.y);
            var unit = new Vector2((position.x-center.x)/rx, (position.z-center.y)/rz);
            if (unit.sqrMagnitude <= 1f) return position;
            unit.Normalize();
            return new Vector3(center.x + unit.x*rx, position.y, center.y + unit.y*rz);
        }

        private static bool InCanal(Vector3 p) => Mathf.Abs(Mathf.Abs(p.x)-10f)<3.2f && Mathf.Abs(p.z-5.8f)<3f;
        private void OnApplicationFocus(bool value) { focused=value; joystick?.ResetInput(); }
        private void OnApplicationPause(bool value) { paused=value; joystick?.ResetInput(); }
        private void OnDisable() => joystick?.ResetInput();
        private void OnDestroy()
        {
            if (jumpButton != null) jumpButton.onClick.RemoveListener(Jump);
        }
    }
}
