#if HAS_VUPLEX_WEBVIEW
#else

using System;

namespace Vuplex.WebView
{
    public class ProgressChangedEventArgs : EventArgs
    {
        public object Type { get; set; }
    }
}

#endif
