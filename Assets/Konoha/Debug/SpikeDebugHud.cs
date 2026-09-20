using Konoha.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Diagnostics
{
    public sealed class SpikeDebugHud : MonoBehaviour
    {
        public Text display;
        public Button toggle;
        public CharacterMotor motor;
        private BuildIdentity identity;
        private float elapsed;
        private int frames;
        private bool visible = true;
        private void Start()
        {
            if (!UnityEngine.Debug.isDebugBuild && !Application.isEditor)
            {
                display.gameObject.SetActive(false);
                toggle.gameObject.SetActive(false);
                enabled = false;
                return;
            }
            identity = BuildIdentity.Load();
            toggle.onClick.AddListener(Toggle);
        }
        private void Toggle() { visible = !visible; display.gameObject.SetActive(visible); }
        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            frames++;
            if (elapsed < 0.5f) return;
            if (visible)
            {
                display.text = $"KONOHA {identity.build} | OFFLINE SPIKE\nCOMMIT {identity.commit}\nBALANCE {identity.balance}\nSERVER {identity.server}\n" +
                    $"FPS {frames / elapsed:F1} | Frame interval {elapsed * 1000f / frames:F1} ms\n" +
                    $"Position {motor.transform.position:F2} | {(motor.Grounded ? "GROUNDED" : "NOT GROUNDED")}\n" +
                    "Ping / jitter / loss / server tick / reconciliation: N/A\nWibawa / Pengaruh / abilities / CC / DR: NOT IMPLEMENTED";
            }
            frames = 0;
            elapsed = 0f;
        }
        private void OnDestroy() { if (toggle != null) toggle.onClick.RemoveListener(Toggle); }
    }
}
