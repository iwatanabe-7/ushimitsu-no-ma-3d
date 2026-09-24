// Input.touchSupported reports true on plenty of desktop browsers/OSes that
// merely expose a touch API (a touchscreen laptop, some trackpad drivers,
// certain Chrome/Edge builds) even with no phone in sight. The user agent is
// the reliable signal browsers actually use for "is this a phone" - iPadOS
// is the one exception, reporting as a Mac, so it's caught separately via
// having multiple touch points on an otherwise Mac-like platform.
mergeInto(LibraryManager.library, {
  Ushimitsu_IsMobileBrowser: function () {
    var ua = navigator.userAgent || navigator.vendor || '';
    var isKnownMobileUA = /android|iphone|ipad|ipod|iemobile|blackberry|bada|mobile/i.test(ua);
    var isIpadOS = navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1;
    return (isKnownMobileUA || isIpadOS) ? 1 : 0;
  }
});
