#if HAS_VUPLEX_WEBVIEW
#else

using System;
using UnityEngine;

namespace Vuplex.WebView
{
    public class CanvasWebViewPrefab : MonoBehaviour
    {
        public event EventHandler<EventArgs> Initialized;
        public bool HoveringEnabled { get; set; }
        public bool ClickingEnabled { get; set; }
        public bool ScrollingEnabled { get; set; }
        public bool KeyboardEnabled { get; set; }
        public bool CursorIconsEnabled { get; set; }
        public IWebView WebView { get; set; }
    }
}

#endif
