#if HAS_VUPLEX_WEBVIEW
#else

using System;

namespace Vuplex.WebView
{
    public class EventArgs<T> : EventArgs
    {
        public T Value { get; set; }
    }
}

#endif
