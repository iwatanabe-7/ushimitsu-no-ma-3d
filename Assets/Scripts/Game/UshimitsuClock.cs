using UnityEngine;
using Ushimitsu.UI;

namespace Ushimitsu.Game
{
    public class UshimitsuClock : MonoBehaviour
    {
        public const int ClockStartSec = 1 * 3600 + 57 * 60; // 午前1時57分00秒
        public const int ClockEndSec = 2 * 3600;              // 午前2時00分00秒(丑三つ時)
        public const int CriticalRemaining = 30;

        public HUDController hud;

        int clockSec;
        float accumulator;
        bool running;
        bool criticalTriggered;

        void Start()
        {
            clockSec = ClockStartSec;
            hud?.SetClockText(FormatClock(clockSec), false);
        }

        void Update()
        {
            if (!running) return;

            accumulator += Time.deltaTime;
            if (accumulator < 1f) return;
            accumulator -= 1f;

            clockSec += 1;
            int remaining = ClockEndSec - clockSec;

            bool critical = remaining <= CriticalRemaining;
            hud?.SetClockText(FormatClock(clockSec), critical);

            if (critical && !criticalTriggered)
            {
                criticalTriggered = true;
                GameManager.Instance?.TriggerJumpscare();
            }

            if (remaining <= 0)
            {
                running = false;
                GameManager.Instance?.GameOver();
            }
        }

        public void StartClock()
        {
            accumulator = 0f;
            running = true;
        }

        public void StopClock()
        {
            running = false;
        }

        static string FormatClock(int totalSec)
        {
            int h = totalSec / 3600;
            int m = (totalSec % 3600) / 60;
            int s = totalSec % 60;
            int h12 = h % 12 == 0 ? 12 : h % 12;
            return "午前" + h12 + "時" + m.ToString("00") + "分" + s.ToString("00") + "秒";
        }
    }
}
