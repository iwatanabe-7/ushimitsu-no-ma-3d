using UnityEngine;
using Ushimitsu.Platform;

namespace Ushimitsu.UI
{
    // The left/right touch split assumes landscape. Unity's anchors already measure
    // against whatever the real screen width/height are at runtime, so the split
    // itself adapts automatically - but if the phone is still held upright there's
    // no "half" worth having, so block play with a clear prompt until it's rotated.
    public class OrientationGuard : MonoBehaviour
    {
        public GameObject prompt;

        bool touchDevice;

        void Awake()
        {
            // Input.touchSupported alone is not enough - see MobileBrowserDetect.
            touchDevice = MobileBrowserDetect.IsMobile;
            if (!touchDevice && prompt != null) prompt.SetActive(false);
        }

        void Update()
        {
            if (!touchDevice || prompt == null) return;
            bool portrait = Screen.height > Screen.width;
            if (prompt.activeSelf != portrait) prompt.SetActive(portrait);
        }
    }
}
