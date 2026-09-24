using UnityEngine;
using UnityEngine.EventSystems;

namespace Ushimitsu.UI
{
    // Covers the screen area not claimed by the joystick/buttons. Dragging anywhere
    // on it accumulates a raw pixel delta for the camera to consume next frame.
    public class TouchLookArea : MonoBehaviour, IDragHandler
    {
        public Vector2 Delta { get; private set; }

        public void OnDrag(PointerEventData eventData)
        {
            Delta += eventData.delta;
        }

        // Called by FirstPersonController once it has read the value, so drags that
        // stop don't leave a stale delta being applied forever.
        public Vector2 ConsumeDelta()
        {
            Vector2 d = Delta;
            Delta = Vector2.zero;
            return d;
        }
    }
}
