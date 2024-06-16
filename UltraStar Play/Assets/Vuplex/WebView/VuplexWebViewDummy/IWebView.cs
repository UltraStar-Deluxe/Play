#if HAS_VUPLEX_WEBVIEW
#else

using System;
using System.Collections.Generic;

namespace Vuplex.WebView
{
    public interface IWebView
    {
        void ExecuteJavaScript(string s);
        event EventHandler<EventArgs<string>> MessageEmitted;
        event EventHandler<ProgressChangedEventArgs> LoadProgressChanged;
        void LoadHtml(string text);
        string Url { get; set; }
        List<string> PageLoadScripts { get; set; }
        void LoadUrl(string url);
    }
}

#endif
