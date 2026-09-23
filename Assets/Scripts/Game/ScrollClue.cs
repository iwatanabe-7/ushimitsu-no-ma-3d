using Ushimitsu.Interaction;
using Ushimitsu.UI;
using UnityEngine;

namespace Ushimitsu.Game
{
    public class ScrollClue : MonoBehaviour, IInteractable
    {
        bool checkedOnce;

        public string GetPrompt()
        {
            return checkedOnce ? "[E] 掛け軸" : "[E] 掛け軸を調べる";
        }

        public void Interact()
        {
            var gm = GameManager.Instance;
            if (!checkedOnce)
            {
                checkedOnce = true;
                gm.scrollChecked = true;
                gm.RevealClue(1, 3, "床の間に掛け軸が下がっている。裏側を覗くと、小さな走り書きがあった。");
            }
            else
            {
                gm.hud?.Log("掛け軸に、他の仕掛けはなさそうだ。", HUDController.LineStyle.Flavor);
            }
        }
    }
}
