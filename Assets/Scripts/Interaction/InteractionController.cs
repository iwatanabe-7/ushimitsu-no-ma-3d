using UnityEngine;
using Ushimitsu.UI;

namespace Ushimitsu.Interaction
{
    public class InteractionController : MonoBehaviour
    {
        public float range = 2.6f;
        // A thin ray is hard to land on small/low objects (e.g. a floor-level
        // tatami mat) once the camera sits at a proper eye height, so we sweep a
        // small sphere instead - much more forgiving without changing any hitboxes.
        public float castRadius = 0.18f;
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
            if (Physics.SphereCast(ray, castRadius, out RaycastHit hit, range, interactMask, QueryTriggerInteraction.Collide))
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
                TriggerInteract();
            }
        }

        // Shared by the E key and the mobile "調べる" button.
        public void TriggerInteract()
        {
            if (inputLocked) return;

            // If a message is on screen, dismiss it first instead of immediately
            // firing a new interaction underneath it.
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
