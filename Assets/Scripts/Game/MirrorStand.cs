using Ushimitsu.Interaction;
using Ushimitsu.UI;
using UnityEngine;

namespace Ushimitsu.Game
{
    public class MirrorStand : MonoBehaviour, IInteractable
    {
        bool seen;

        public string GetPrompt()
        {
            return seen ? "[E] 鏡台" : "[E] 鏡を覗き込む";
        }

        public void Interact()
        {
            var gm = GameManager.Instance;
            if (!seen)
            {
                seen = true;
                gm.mirrorSeen = true;
                gm.hud?.Log("古い鏡台。鏡は斜めにひびが入り、埃をかぶっている。覗き込むと、一瞬だけ――自分ではない誰かが、こちらを見ていた気がした。", HUDController.LineStyle.Normal);
                gm.TriggerJumpscare();
            }
            else
            {
                gm.hud?.Log("もう、あの鏡は覗きたくない。", HUDController.LineStyle.Flavor);
            }
        }
    }
}
