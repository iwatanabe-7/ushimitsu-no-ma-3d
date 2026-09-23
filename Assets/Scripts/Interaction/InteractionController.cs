using UnityEngine;
using Ushimitsu.UI;

namespace Ushimitsu.Interaction
{
    public class InteractionController : MonoBehaviour
    {
        public float range = 2.6f;
        public LayerMask interactMask = ~0;
        public HUDController hud;

        IInteractable current;
        bool inputLocked;

        public void SetInputLocked(bool locked)
        {
            inputLocked = locked;
            if (locked && hud != null)
            {
                hud.SetPrompt(string.Empty);
            }
        }

        void Update()
        {
            if (inputLocked)
            {
                current = null;
                return;
            }

            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, range, interactMask, QueryTriggerInteraction.Collide))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    current = interactable;
                    if (hud != null) hud.SetPrompt(interactable.GetPrompt());
                }
                else
                {
                    current = null;
                    if (hud != null) hud.SetPrompt(string.Empty);
                }
            }
            else
            {
                current = null;
                if (hud != null) hud.SetPrompt(string.Empty);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                // If a message is on screen, E dismisses it first instead of
                // immediately firing a new interaction underneath it.
                if (hud != null && hud.IsMessageShowing())
                {
                    hud.DismissMessage();
                }
                else if (current != null)
                {
                    current.Interact();
                }
            }
        }
    }
}
