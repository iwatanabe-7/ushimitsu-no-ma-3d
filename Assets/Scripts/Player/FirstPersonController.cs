using UnityEngine;

namespace Ushimitsu.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        public float moveSpeed = 3.2f;
        public float lookSensitivity = 2.2f;
        public float gravity = -18f;
        public Transform cameraPivot;

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

            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -70f, 70f);
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            Vector3 move = (transform.right * x + transform.forward * z).normalized * moveSpeed;

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
