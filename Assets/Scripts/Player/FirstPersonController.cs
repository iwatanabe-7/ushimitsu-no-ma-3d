using UnityEngine;

namespace Ushimitsu.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        public float moveSpeed = 3.2f;
        public float lookSensitivity = 1.4f;
        // Touch drag deltas arrive in raw screen pixels, a completely different scale
        // from the mouse axis, so they get their own multiplier instead of sharing
        // lookSensitivity.
        public float touchLookSensitivity = 0.18f;
        public float gravity = -18f;
        public Transform cameraPivot;

        // Fed once per frame by the mobile joystick / touch-look UI when present.
        // Left untouched (zero) on desktop, so they simply add nothing below - no
        // platform branching needed in the movement code itself.
        [System.NonSerialized] public Vector2 externalMove;
        [System.NonSerialized] public Vector2 externalLookDelta;

        CharacterController controller;
        float pitch;
        float verticalVelocity;
        bool inputLocked;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        // Cursor state is driven entirely by SetInputLocked so the title screen and
        // the dial panel keep a usable mouse pointer.

        public void SetInputLocked(bool locked)
        {
            inputLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        void Update()
        {
            if (inputLocked) return;

            // GetAxis (not Raw) runs the mouse delta through Unity's built-in
            // smoothing filter, which is exactly the laggy/floaty feel being reported.
            // Raw input tracks the mouse 1:1, the way a first-person look should.
            float mouseX = Input.GetAxisRaw("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * lookSensitivity;
            float lookX = mouseX + externalLookDelta.x * touchLookSensitivity;
            float lookY = mouseY + externalLookDelta.y * touchLookSensitivity;
            externalLookDelta = Vector2.zero; // consumed for this frame

            transform.Rotate(Vector3.up * lookX);

            pitch -= lookY;
            pitch = Mathf.Clamp(pitch, -70f, 70f);
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            float x = Mathf.Clamp(Input.GetAxisRaw("Horizontal") + externalMove.x, -1f, 1f);
            float z = Mathf.Clamp(Input.GetAxisRaw("Vertical") + externalMove.y, -1f, 1f);
            // Clamp the magnitude rather than normalizing outright, so a lightly
            // tilted touch joystick still walks slower than a full push - normalizing
            // would snap every non-zero input to full speed.
            Vector3 moveDir = transform.right * x + transform.forward * z;
            if (moveDir.magnitude > 1f) moveDir.Normalize();
            Vector3 move = moveDir * moveSpeed;

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }
            verticalVelocity += gravity * dt;

            Vector3 velocity = move + Vector3.up * verticalVelocity;
            controller.Move(velocity * dt);
        }
    }
}
