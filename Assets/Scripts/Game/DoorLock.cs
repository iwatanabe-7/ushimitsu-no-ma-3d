using Ushimitsu.Interaction;
using Ushimitsu.UI;
using UnityEngine;

namespace Ushimitsu.Game
{
    public class DoorLock : MonoBehaviour, IInteractable
    {
        public string GetPrompt()
        {
            return "[E] 扉のダイヤル錠を調べる";
        }

        public void Interact()
        {
            GameManager.Instance.hud?.OpenDialPanel(this);
        }

        public void OnDialSubmit(int a, int b, int c)
        {
            var gm = GameManager.Instance;
            if (!gm.HasAllDigits())
            {
                gm.hud?.dialPanel?.ShowHint("まだ、何か足りない気がする。");
                return;
            }

            if (gm.CheckCode(a, b, c))
            {
                gm.hud?.CloseDialPanel();
                gm.Escape();
            }
            else
            {
                gm.hud?.ShakeDial();
                gm.hud?.dialPanel?.ShowHint("かちり、と虚しい音が鳴った。番号が違うようだ。");
            }
        }
    }
}
