using UnityEngine;

namespace Ushimitsu.Platform
{
    // Input.touchSupported is not a reliable "is this a phone" check - see
    // MobileBrowserDetect.jslib for why. This reads the browser's own user agent
    // instead, which is what real mobile-detection code always ends up doing.
    public static class MobileBrowserDetect
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern int Ushimitsu_IsMobileBrowser();

        public static bool IsMobile => Ushimitsu_IsMobileBrowser() == 1;
#else
        // Standalone/editor builds have no browser to ask; fall back to Unity's
        // own flag so a touchscreen Windows/Mac dev build still works for testing.
        public static bool IsMobile => Input.touchSupported;
#endif
    }
}
