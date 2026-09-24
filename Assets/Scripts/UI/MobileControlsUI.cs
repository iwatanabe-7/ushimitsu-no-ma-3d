using UnityEngine;
using UnityEngine.UI;
using Ushimitsu.Player;
using Ushimitsu.Interaction;

namespace Ushimitsu.UI
{
    // Landscape touch controls in the style most landscape 3D phone games use:
    //  - Left half: a floating stick. It appears wherever the thumb lands, has a
    //    dead zone so thumb wobble near the center doesn't twitch the player
    //    around, and its radius is a fraction of screen height so it's the same
    //    physical size on every phone (raw pixels differ ~3x between devices).
    //  - Right half: drag to look, measured relative to screen height (so the
    //    same thumb swipe turns the same amount on any phone), with a small
    //    "touch slop" so just resting a finger doesn't nudge the camera, and a
    //    light low-pass filter to even out uneven touch sampling.
    //
    // Both halves are read straight from Input.touches by fingerId rather than
    // through Unity UI drag events, which can mix two simultaneous fingers up on
    // WebGL. The desktop path is untouched: with no touch, nothing here runs.
    [DefaultExecutionOrder(-50)] // run before FirstPersonController so input is same-frame
    public class MobileControlsUI : MonoBehaviour
    {
        public GameObject root;
        public RectTransform joystickRing;
        public RectTransform joystickKnob;
        public RectTransform interactButtonRect;
        public Button interactButton;

        public FirstPersonController player;
        public InteractionController interaction;

        [Header("Movement stick")]
        [Tooltip("Stick radius as a fraction of screen height.")]
        public float stickRadiusOfScreen = 0.12f;
        [Tooltip("Fraction of the radius near the center that is ignored.")]
        [Range(0f, 0.5f)] public float stickDeadZone = 0.15f;

        [Header("Camera drag")]
        [Tooltip("Degrees of turn for a drag the height of the screen.")]
        public float yawDegreesPerScreen = 150f;
        public float pitchDegreesPerScreen = 100f;
        [Tooltip("Finger must move this far (fraction of screen height) before the camera starts turning.")]
        public float touchSlopOfScreen = 0.012f;
        [Tooltip("Smoothing time constant in seconds. 0 = off.")]
        public float lookSmoothing = 0.03f;

        bool touchDetected;
        RectTransform rootRect;
        Canvas canvas;
        Image ringImage;
        Color ringIdleColor;

        int moveFingerId = -1;
        Vector2 moveOrigin;

        int lookFingerId = -1;
        Vector2 lookDownPos;
        Vector2 lookLastPos;
        bool lookPastSlop;
        Vector2 smoothedLook;

        void Awake()
        {
            // Resolved in Awake (not Start) because the title screen can turn this
            // layer on from its own Start(), and only Awake is guaranteed to run first.
            touchDetected = Input.touchSupported;

            rootRect = root != null ? root.GetComponent<RectTransform>() : null;
            canvas = GetComponentInParent<Canvas>();
            if (joystickRing != null)
            {
                ringImage = joystickRing.GetComponent<Image>();
                if (ringImage != null) ringIdleColor = ringImage.color;
            }

            if (touchDetected)
            {
                // By default Unity also feeds every touch into the Mouse X/Y axes.
                // FirstPersonController reads those for desktop look, so without
                // this the walking thumb would ALSO turn the camera - the wobble.
                Input.simulateMouseWithTouches = false;
            }

            // Hidden until HUDController turns it on after the title screen; left on
            // there, it would sit above the title's own button and swallow the tap.
            if (root != null) root.SetActive(false);
            if (!touchDetected) enabled = false;

            if (interactButton != null)
            {
                interactButton.onClick.AddListener(() => interaction?.TriggerInteract());
            }
        }

        // Called by HUDController when gameplay starts/stops and around the dial.
        public void SetGameplayActive(bool active)
        {
            if (root != null) root.SetActive(active && touchDetected);
            if (!active)
            {
                EndMove();
                lookFingerId = -1;
                smoothedLook = Vector2.zero;
                if (player != null)
                {
                    player.externalMove = Vector2.zero;
                    player.externalLookDelta = Vector2.zero;
                }
            }
        }

        void Update()
        {
            if (player == null) return;

            float screenH = Mathf.Max(1f, Screen.height);
            float radiusPx = Mathf.Max(40f, screenH * stickRadiusOfScreen);
            float slopPx = Mathf.Max(8f, screenH * touchSlopOfScreen);
            float halfWidth = Screen.width * 0.5f;

            Vector2 move = Vector2.zero;
            Vector2 lookPx = Vector2.zero;
            bool moveSeen = false;
            bool lookSeen = false;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);

                if (t.phase == TouchPhase.Began)
                {
                    if (t.position.x < halfWidth)
                    {
                        if (moveFingerId < 0) BeginMove(t);
                    }
                    else if (lookFingerId < 0 && !IsOverInteractButton(t.position))
                    {
                        lookFingerId = t.fingerId;
                        lookDownPos = t.position;
                        lookLastPos = t.position;
                        lookPastSlop = false;
                    }
                }

                bool ended = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;

                if (t.fingerId == moveFingerId)
                {
                    moveSeen = true;
                    if (ended)
                    {
                        EndMove();
                    }
                    else
                    {
                        Vector2 delta = Vector2.ClampMagnitude(t.position - moveOrigin, radiusPx);
                        move = ApplyDeadZone(delta / radiusPx);
                        SetKnob(delta);
                    }
                }
                else if (t.fingerId == lookFingerId)
                {
                    lookSeen = true;
                    if (ended)
                    {
                        lookFingerId = -1;
                    }
                    else if (!lookPastSlop)
                    {
                        if ((t.position - lookDownPos).magnitude >= slopPx)
                        {
                            // Start measuring from here so the slop distance itself
                            // doesn't get applied as one sudden jump.
                            lookPastSlop = true;
                            lookLastPos = t.position;
                        }
                    }
                    else
                    {
                        lookPx += t.position - lookLastPos;
                        lookLastPos = t.position;
                    }
                }
            }

            // A finger that disappeared without a clean Ended phase (happens on
            // WebGL) would otherwise stay "held" forever.
            if (!moveSeen && moveFingerId >= 0) EndMove();
            if (!lookSeen) lookFingerId = -1;

            Vector2 lookDeg = new Vector2(
                lookPx.x / screenH * yawDegreesPerScreen,
                lookPx.y / screenH * pitchDegreesPerScreen);

            if (lookSmoothing > 0f)
            {
                // First-order low-pass on the per-frame turn. It has unity gain, so
                // the total turn still matches the total drag - it just evens out
                // frames that received two touch samples vs. none.
                float a = 1f - Mathf.Exp(-Time.unscaledDeltaTime / lookSmoothing);
                smoothedLook = Vector2.Lerp(smoothedLook, lookDeg, a);
            }
            else
            {
                smoothedLook = lookDeg;
            }

            player.externalMove = move;
            player.externalLookDelta = smoothedLook;

            if (ringImage != null)
            {
                float radiusCanvas = radiusPx / CanvasScale();
                joystickRing.sizeDelta = Vector2.one * radiusCanvas * 2f;
                if (joystickKnob != null) joystickKnob.sizeDelta = Vector2.one * radiusCanvas * 0.8f;
                if (moveFingerId < 0) joystickRing.anchoredPosition = RestPosition(radiusCanvas);
            }
        }

        void BeginMove(Touch t)
        {
            moveFingerId = t.fingerId;
            moveOrigin = t.position;

            // Floating stick: jump the ring to wherever the thumb landed.
            if (joystickRing != null && rootRect != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, t.position, null, out Vector2 local))
            {
                joystickRing.anchoredPosition = local - rootRect.rect.min;
            }
            if (ringImage != null)
            {
                Color c = ringIdleColor;
                c.a = Mathf.Min(1f, ringIdleColor.a * 2f);
                ringImage.color = c;
            }
            SetKnob(Vector2.zero);
        }

        void EndMove()
        {
            moveFingerId = -1;
            SetKnob(Vector2.zero);
            if (ringImage != null) ringImage.color = ringIdleColor;
        }

        void SetKnob(Vector2 deltaPx)
        {
            if (joystickKnob != null) joystickKnob.anchoredPosition = deltaPx / CanvasScale();
        }

        Vector2 ApplyDeadZone(Vector2 v)
        {
            float m = v.magnitude;
            if (m <= stickDeadZone) return Vector2.zero;
            float scaled = (m - stickDeadZone) / (1f - stickDeadZone);
            return v / m * Mathf.Min(1f, scaled);
        }

        Vector2 RestPosition(float radiusCanvas)
        {
            float margin = radiusCanvas + 40f;
            return new Vector2(margin, margin);
        }

        float CanvasScale()
        {
            return canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        }

        bool IsOverInteractButton(Vector2 screenPos)
        {
            if (interactButtonRect == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(interactButtonRect, screenPos, null);
        }
    }
}
