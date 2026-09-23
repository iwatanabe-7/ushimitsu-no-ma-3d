using Ushimitsu.Interaction;
using Ushimitsu.UI;
using UnityEngine;

namespace Ushimitsu.Game
{
    public class TatamiFloor : MonoBehaviour, IInteractable
    {
        bool checkedOnce;

        public string GetPrompt()
        {
            return checkedOnce ? "[E] 畳" : "[E] 畳をめくる";
        }

        public void Interact()
        {
            var gm = GameManager.Instance;
            if (!checkedOnce)
            {
                checkedOnce = true;
                gm.tatamiChecked = true;
                gm.RevealClue(2, 4, "畳が一枚だけ、わずかに浮いている。めくると、床板に古い刻み跡があった。");
            }
            else
            {
                gm.hud?.Log("床板には、もう何もなさそうだ。", HUDController.LineStyle.Flavor);
            }
        }
    }
}
