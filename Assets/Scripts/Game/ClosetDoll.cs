using Ushimitsu.Interaction;
using Ushimitsu.UI;
using UnityEngine;

namespace Ushimitsu.Game
{
    public class ClosetDoll : MonoBehaviour, IInteractable
    {
        enum Stage { Unopened, DollRevealed, KeyTaken }
        Stage stage = Stage.Unopened;

        public string GetPrompt()
        {
            switch (stage)
            {
                case Stage.Unopened: return "[E] 押入れを開ける";
                case Stage.DollRevealed: return "[E] 帯をほどく";
                default: return "[E] 押入れ";
            }
        }

        public void Interact()
        {
            var gm = GameManager.Instance;
            switch (stage)
            {
                case Stage.Unopened:
                    stage = Stage.DollRevealed;
                    gm.hud?.Log("襖を開けると、暗い押入れの奥に何かが座っている。よく見ると、古い市松人形だった。人形の帯に、何か硬いものが縫い付けられている。", HUDController.LineStyle.Normal);
                    break;
                case Stage.DollRevealed:
                    stage = Stage.KeyTaken;
                    gm.closetDollPulled = true;
                    gm.ObtainKey();
                    break;
                default:
                    gm.hud?.Log("人形はもう何も持っていない。……人形と、目が合った気がした。", HUDController.LineStyle.Flavor);
                    break;
            }
        }
    }
}
