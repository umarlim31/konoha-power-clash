using UnityEngine;
using UnityEngine.EventSystems;

namespace Konoha.Input
{
    public sealed class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform pad;
        public RectTransform handle;
        [Range(0f, 0.4f)] public float deadZone = 0.12f;
        private int? ownerPointer;
        public Vector2 Value { get; private set; }

        public void OnPointerDown(PointerEventData e)
        {
            if (ownerPointer.HasValue) return;
            ownerPointer = e.pointerId;
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (ownerPointer != e.pointerId) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(pad, e.position, e.pressEventCamera, out var local)) return;
            var radius = Mathf.Max(1f, (pad.rect.width - handle.rect.width) * 0.5f);
            var raw = Vector2.ClampMagnitude(local / radius, 1f);
            Value = RemapDeadZone(raw, deadZone);
            handle.anchoredPosition = raw * radius;
        }

        public static Vector2 RemapDeadZone(Vector2 raw, float deadZone)
        {
            float magnitude = Mathf.Min(raw.magnitude, 1f);
            deadZone = Mathf.Clamp(deadZone, 0f, 0.99f);
            return magnitude <= deadZone ? Vector2.zero : raw.normalized * ((magnitude - deadZone) / (1f - deadZone));
        }

        public void OnPointerUp(PointerEventData e) { if (ownerPointer == e.pointerId) ResetInput(); }
        public void ResetInput()
        {
            ownerPointer = null;
            Value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }
        private void OnDisable() => ResetInput();
        private void OnApplicationPause(bool paused) { if (paused) ResetInput(); }
        private void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
    }
}
