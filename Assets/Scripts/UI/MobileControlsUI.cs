using UnityEngine;
using UnityEngine.UI;
using Ushimitsu.Player;
using Ushimitsu.Interaction;

namespace Ushimitsu.UI
{
    // Shows the on-screen joystick / look area / interact button only on touch
    // devices, and feeds them into the same player the keyboard and mouse drive.
    // The desktop path is untouched: on a device with no touch, every value here
    // simply stays zero and this layer does nothing.
    public class MobileControlsUI : MonoBehaviour
    {
        public GameObject root;
        public VirtualJoystick joystick;
        public TouchLookArea lookArea;
        public Button interactButton;

        public FirstPersonController player;
        public InteractionController interaction;

        void Start()
        {
            bool isTouch = Input.touchSupported;
            if (root != null) root.SetActive(isTouch);
            if (!isTouch) enabled = false;
        }

        void Awake()
        {
            if (interactButton != null)
            {
                interactButton.onClick.AddListener(() => interaction?.TriggerInteract());
            }
        }

        void Update()
        {
            if (player == null) return;

            if (joystick != null)
            {
                player.externalMove = joystick.Value;
            }
            if (lookArea != null)
            {
                player.externalLookDelta = lookArea.ConsumeDelta();
            }
        }
    }
}
