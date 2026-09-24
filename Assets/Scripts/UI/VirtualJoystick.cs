using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ushimitsu.UI
{
    // A classic floating movement stick: press anywhere in its zone, drag, and the
    // knob follows (clamped to the ring) while Value reports the normalized offset.
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform ring;
        public RectTransform knob;
        public float ringRadius = 90f;

        public Vector2 Value { get; private set; }

        int activePointerId = -1;

        public void OnPointerDown(PointerEventData eventData)
        {
            activePointerId = eventData.pointerId;
            UpdateKnob(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            UpdateKnob(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            activePointerId = -1;
            Value = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }

        void UpdateKnob(PointerEventData eventData)
        {
            if (ring == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                ring, eventData.position, eventData.pressEventCamera, out Vector2 local);

            Vector2 clamped = Vector2.ClampMagnitude(local, ringRadius);
            if (knob != null) knob.anchoredPosition = clamped;
            Value = clamped / ringRadius;
        }
    }
}
