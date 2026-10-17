// A short vibration of the phone (navigator.vibrate). Android browsers answer it once the page has been
// touched; iOS browsers and computers have no vibration and ignore it.
mergeInto(LibraryManager.library, {
  ChogVibrate: function (milliseconds) {
    try {
      if (typeof navigator !== 'undefined' && typeof navigator.vibrate === 'function') {
        navigator.vibrate(milliseconds);
      }
    } catch (e) {
    }
  }
});
