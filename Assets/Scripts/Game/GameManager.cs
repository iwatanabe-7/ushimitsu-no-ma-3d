using UnityEngine;
using Ushimitsu.Player;
using Ushimitsu.Interaction;
using Ushimitsu.UI;
using Ushimitsu.Audio;

namespace Ushimitsu.Game
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        public FirstPersonController playerController;
        public InteractionController interactionController;
        public HUDController hud;
        public JumpscareController jumpscare;
        public UshimitsuClock clock;

        [Header("State (read-only at runtime)")]
        public bool hasKey;
        public bool altarOpened;
        public bool closetDollPulled;
        public bool tatamiChecked;
        public bool scrollChecked;
        public bool mirrorSeen;

        public int? digit1;
        public int? digit2;
        public int? digit3;

        bool gameEnded;

        void Awake()
        {
            Instance = this;
        }

        // Called by the title screen once the player chooses to start.
        public void BeginGame()
        {
            hud?.Log(
                "目を開けると、そこは知らない和室だった。畳の匂いと、線香の残り香がする。\n" +
                "出口は、恐らく家のどこかにあるはずだ。\n" +
                "時計は、午前一時五十七分を指していた。丑三つ時になる前に、ここを出なければ。",
                HUDController.LineStyle.Normal);
            clock?.StartClock();
        }

        public void ObtainKey()
        {
            if (hasKey) return;
            hasKey = true;
            hud?.Log("中から、小さな鍵が出てきた。", HUDController.LineStyle.Flavor);
            hud?.AddInventoryItem("古びた鍵");
        }

        // Combines the discovery flavor text with the revealed digit into a single
        // message so the player has time to read both before the panel auto-hides.
        public void RevealClue(int index, int value, string message)
        {
            switch (index)
            {
                case 1: digit1 = value; break;
                case 2: digit2 = value; break;
                case 3: digit3 = value; break;
            }
            hud?.Log(message + "\n暗証番号　" + IndexLabel(index) + "桁目：" + value, HUDController.LineStyle.Clue);
        }

        string IndexLabel(int index)
        {
            switch (index)
            {
                case 1: return "一";
                case 2: return "二";
                case 3: return "三";
                default: return index.ToString();
            }
        }

        public bool HasAllDigits()
        {
            return digit1.HasValue && digit2.HasValue && digit3.HasValue;
        }

        public bool CheckCode(int a, int b, int c)
        {
            if (!HasAllDigits()) return false;
            return digit1.Value == a && digit2.Value == b && digit3.Value == c;
        }

        public void TriggerJumpscare()
        {
            jumpscare?.Play();
        }

        public void SetPlayerInputLocked(bool locked)
        {
            playerController?.SetInputLocked(locked);
            interactionController?.SetInputLocked(locked);
        }

        public void Escape()
        {
            if (gameEnded) return;
            gameEnded = true;
            clock?.StopClock();
            playerController?.SetInputLocked(true);
            interactionController?.SetInputLocked(true);
            hud?.Log("ダイヤルが小さく鳴り、扉がすっと開いた。", HUDController.LineStyle.Normal);
            string extra = mirrorSeen
                ? "\n\n――ただ、鏡の中で笑っていたあの顔だけは、まだ脳裏から離れない。"
                : "";
            hud?.SetGameplayHudVisible(false);
            hud?.ShowEnding("脱出成功", "外は静まり返った夜道だった。振り返らずに、あなたはその家を後にした。" + extra);
        }

        public void GameOver()
        {
            if (gameEnded) return;
            gameEnded = true;
            clock?.StopClock();
            playerController?.SetInputLocked(true);
            interactionController?.SetInputLocked(true);
            hud?.Log("時計が、午前二時を告げた。丑三つ時――扉はまだ開かない。", HUDController.LineStyle.Flavor);
            jumpscare?.Play();
            hud?.SetGameplayHudVisible(false);
            hud?.ShowEnding("ゲームオーバー", "丑三つ時になった。\n\n家のどこかで、何かが静かに笑っていた気配がした。");
        }
    }
}
