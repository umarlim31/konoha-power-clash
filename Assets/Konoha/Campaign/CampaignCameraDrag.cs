using Konoha.Character;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Konoha.Campaign
{
    // A UI surface behind action buttons. Joystick fingers never belong to this gesture.
    public sealed class CampaignCameraDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public MobileCombatCamera follow;
        private int? first, second;
        private Vector2 a, b;
        // Pinch change not yet applied. Small jitter of a resting second finger (for example
        // a thumb brushing the surface next to a skill button) must not zoom the view.
        private float pendingPinch;
        private const float PinchDeadZone = 0.035f; // Fraction of screen height.

        public void OnPointerDown(PointerEventData e)
        {
            if (Time.timeScale <= 0f) return;
            if (!first.HasValue) { first=e.pointerId; a=e.position; }
            else if (!second.HasValue && first != e.pointerId) { second=e.pointerId; b=e.position; pendingPinch=0f; }
        }

        public void OnDrag(PointerEventData e)
        {
            if (Time.timeScale <= 0f || !follow.enabled || (first != e.pointerId && second != e.pointerId)) return;
            float oldDistance=Vector2.Distance(a,b);
            Vector2 previous = first == e.pointerId ? a : b;
            if (first == e.pointerId) a=e.position; else b=e.position;
            if (second.HasValue)
            {
                pendingPinch += (Vector2.Distance(a,b)-oldDistance)/Mathf.Max(1,Screen.height);
                if (Mathf.Abs(pendingPinch) >= PinchDeadZone)
                {
                    follow.ZoomOrbit(pendingPinch - Mathf.Sign(pendingPinch) * PinchDeadZone);
                    pendingPinch = Mathf.Sign(pendingPinch) * PinchDeadZone;
                }
            }
            else
                follow.RotateOrbit(new Vector2((e.position.x-previous.x)/Mathf.Max(1,Screen.width),
                    (e.position.y-previous.y)/Mathf.Max(1,Screen.height)));
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == first)
            {
                first=second; a=b; second=null;
            }
            else if (e.pointerId == second) second=null;
        }
        private void OnDisable() { first=null; second=null; }
        private void OnApplicationFocus(bool focus) { if (!focus) { first=null; second=null; } }
        private void OnApplicationPause(bool pause) { if (pause) { first=null; second=null; } }
    }
}
