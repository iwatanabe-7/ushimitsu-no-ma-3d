using Ushimitsu.Interaction;
using Ushimitsu.UI;
using UnityEngine;

namespace Ushimitsu.Game
{
    public class AltarDrawer : MonoBehaviour, IInteractable
    {
        bool opened;

        public string GetPrompt()
        {
            if (opened) return "[E] 仏壇";
            return GameManager.Instance != null && GameManager.Instance.hasKey
                ? "[E] 鍵で引き出しを開ける"
                : "[E] 仏壇を調べる";
        }

        public void Interact()
        {
            var gm = GameManager.Instance;
            if (opened)
            {
                gm.hud?.Log("引き出しはもう空だ。", HUDController.LineStyle.Flavor);
                return;
            }

            if (!gm.hasKey)
            {
                gm.hud?.Log("仏壇には古い位牌と線香立て。下の引き出しに、小さな南京錠がかかっている。鍵が必要なようだ。", HUDController.LineStyle.Normal);
                return;
            }

            opened = true;
            gm.altarOpened = true;
            gm.RevealClue(3, 8, "鍵はぴったりと合い、引き出しが開いた。中には黄ばんだメモが一枚。");
            gm.hud?.AddInventoryItem("お守り");
        }
    }
}
