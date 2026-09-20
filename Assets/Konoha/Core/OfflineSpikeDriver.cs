using Konoha.Character;
using Konoha.Input;
using UnityEngine;

namespace Konoha.Core
{
    public sealed class OfflineSpikeDriver : MonoBehaviour
    {
        public TouchJoystick joystick;
        public CharacterMotor motor;
        private bool focused = true;
        private bool paused;
        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            UnityEngine.Input.multiTouchEnabled = true;
        }
        private void Update()
        {
            if (focused && !paused && joystick != null && motor != null)
                motor.Step(new MoveIntent(joystick.Value), Mathf.Min(Time.deltaTime, 0.05f));
        }
        private void OnApplicationFocus(bool value) { focused = value; joystick?.ResetInput(); }
        private void OnApplicationPause(bool value) { paused = value; joystick?.ResetInput(); }
    }
}
