using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using Vuplex.WebView;

public class WebViewManager : AbstractSingletonBehaviour, INeedInjection
{
    public static WebViewManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<WebViewManager>();

    [InjectedInInspector]
    public CanvasWebViewPrefab webViewPrefab;

    [InjectedInInspector]
    public string testUrl;
    
    private IWebView webView;

    private bool IsWebViewInitialized => webView != null;
    private readonly Subject<bool> webViewInitializedEventStream = new();

    private readonly Dictionary<string, string> hostToHtmlTemplatePath = new();
    private readonly Dictionary<string, CachedHtmlTemplate> hostToCachedHtmlTemplate = new();
    private readonly Dictionary<string, CachedHtmlTemplate> urlToCachedHtmlTemplate = new();

    public bool IsPlaying
    {
        get
        {
            if (!IsWebViewInitialized)
            {
                return false;
            }
            return GetBoolFromJavaScript("isPlaying()");
        }
        set
        {
            if (!IsWebViewInitialized)
            {
                return;
            }

            if (value)
            {
                ContinuePlayback();
            }
            else
            {
                PausePlayback();
            }
        }
    }
    
    public int DurationInMillis
    {
        get
        {
            if (!IsWebViewInitialized)
            {
                return 0;
            }
            return GetIntFromJavaScript("getPlaybackPositionInMillis()");
        }
    }
    
    public int PlaybackPositionInMillis
    {
        get
        {
            if (!IsWebViewInitialized)
            {
                return 0;
            }
            return GetIntFromJavaScript("getPlaybackPositionInMillis()");
        }
        set
        {
            webView.ExecuteJavaScript($"setPlaybackPositionInMillis({value})");
        }
    }
    
    public int VolumeInPercent
    {
        get
        {
            if (!IsWebViewInitialized)
            {
                return 0;
            }
            return GetIntFromJavaScript("getVolume()");
        }
        set
        {
            webView.ExecuteJavaScript($"setVolume({value})");
        }
    }

    public bool IsContentLoaded
    {
        get
        {
            if (!IsWebViewInitialized)
            {
                return false;
            }
            return GetBoolFromJavaScript("isContentLoaded()");
        }
    }

    private long lastPlaybackPositionUpdatedTimeInMillis;
    
    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        // By default browsers block web pages from autoplaying video or audio.
        // Explicitly allow playback of video or audio without user interaction.
        // This must be called early, e.g. in Awake.
        Web.SetAutoplayEnabled(true);
        
        // Load html templates.
        LoadHtmlTemplates();
    }

    protected override void StartSingleton()
    {
        RunWhenWebViewInitialized(() => LoadTestUrl());
    }

    // private void Update()
    // {
    //     UpdatePlaybackPositionInMillisEstimate();
    // }
    //
    // private void UpdatePlaybackPositionInMillisEstimate()
    // {
    //     if (!isPlaying)
    //     {
    //         return;
    //     }
    //
    //     long currentTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
    //     long deltaTimeInMillis = currentTimeInMillis - lastPlaybackPositionUpdatedTimeInMillis;
    //     playbackPositionInMillis += (int)deltaTimeInMillis;
    //     lastPlaybackPositionUpdatedTimeInMillis = currentTimeInMillis;
    // }

    private void LoadTestUrl()
    {
        if (testUrl.IsNullOrEmpty())
        {
            return;
        }

        if (!CanHandleUrl(testUrl))
        {
            Debug.LogWarning("Cannot handle URL: " + testUrl);
            return;
        }

        LoadUrl(testUrl);
    }

    private void OnEnable()
    {
        webViewPrefab.Initialized += OnWebViewPrefabInitialized;
    }

    private void OnDisable()
    {
        webViewPrefab.Initialized -= OnWebViewPrefabInitialized;
    }
    
    private void OnWebViewPrefabInitialized(object sender, EventArgs e)
    {
        webView = webViewPrefab.WebView;
        webViewPrefab.WebView.MessageEmitted += OnWebViewMessageReceived;
        
        webViewInitializedEventStream.OnNext(true);
    }
    
    private void OnWebViewMessageReceived(object sender, EventArgs<string> e)
    {
        Debug.Log($"Received message from WebView: {e.Value}");
        string json = e.Value.Trim();
        if (!json.StartsWith("{")
            || !json.EndsWith("}"))
        {
            return;
        }
        HandleWebViewMessage(json);
    }

    private void HandleWebViewMessage(string json)
    {
        try
        {
            WebViewMessageDto webViewMessageDto = JsonConverter.FromJson<WebViewMessageDto>(json);
            if (webViewMessageDto == null)
            {
                Debug.LogError($"Failed to parse WebViewMessageDto: {json}");
            }
    
            // if (WebViewMessageDto.TryParseType(webViewMessageDto.type, out WebViewMessageType webViewMessageType)
            //     && int.TryParse(webViewMessageDto.value, out int valueAsInt))
            // {
            //     switch (webViewMessageType)
            //     {
            //         case WebViewMessageType.PlaybackPositionInMillis:
            //             playbackPositionInMillis = valueAsInt;
            //             lastPlaybackPositionUpdatedTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
            //             break;
            //         case WebViewMessageType.DurationInMillis:
            //             durationInMillis = valueAsInt;
            //             break;
            //         case WebViewMessageType.Started:
            //             isPlaying = true;
            //             break;
            //         case WebViewMessageType.Stopped:
            //             isPlaying = false;
            //             break;
            //         case WebViewMessageType.Volume:
            //             volumeInPercent = valueAsInt;
            //             break;
            //     }
            // }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Failed to parse WebViewMessageDto: " + json);
        }
    }

    public bool CanHandleUrl(string url)
    {
        string htmlTemplate = GetHtmlTemplate(url);
        return !htmlTemplate.IsNullOrEmpty();
    }

    public void LoadUrl(string url)
    {
        if (!CanHandleUrl(url))
        {
            throw new Exception($"Cannot handle URL: {url}");
        }

        string finalHtmlCode = GetFinalHtmlCode(url);
        if (finalHtmlCode.IsNullOrEmpty())
        {
            Debug.LogError($"Failed to load HTML code for url: {url}");
            return;
        }

        webView.LoadHtml(finalHtmlCode);
    }

    public void ContinuePlayback()
    {
        webView.ExecuteJavaScript("continuePlayback()");
    }

    public void PausePlayback()
    {
        webView.ExecuteJavaScript("pausePlayback()");
    }

    private string GetFinalHtmlCode(string url)
    {
        string htmlTemplate = GetHtmlTemplate(url);
        if (htmlTemplate.IsNullOrEmpty())
        {
            return "";
        }
        return htmlTemplate
            .Replace("{{URL}}", url);
    }
    
    private void LoadHtmlTemplates()
    {
        string webViewTemplatesFolder = ApplicationUtils.GetStreamingAssetsPath("WebViewTemplates");
        DirectoryUtils.CreateDirectory(webViewTemplatesFolder);
        
        string[] htmlTemplateFilePaths = Directory.GetFiles(webViewTemplatesFolder, "*.html");
        foreach (string htmlTemplateFilePath in htmlTemplateFilePaths)
        {
            string host = Path.GetFileNameWithoutExtension(htmlTemplateFilePath);
            hostToHtmlTemplatePath[host] = htmlTemplateFilePath;
        }
    }

    private string GetHtmlTemplate(string url)
    {
        // Try get cached template for the specific URL.
        if (urlToCachedHtmlTemplate.TryGetValue(url, out CachedHtmlTemplate cachedHtmlTemplate))
        {
            return cachedHtmlTemplate.HtmlTemplate;
        }
        
        // Try get cached template for the host.
        string urlHost = new Uri(url).Host;
        if (!hostToCachedHtmlTemplate.TryGetValue(urlHost, out cachedHtmlTemplate))
        {
            // Load and remember the template for the host.
            cachedHtmlTemplate = LoadAndCacheHtmlTemplateForHost(urlHost);
        }

        if (cachedHtmlTemplate == null)
        {
            return "";
        }
        
        // Remember the template for the specific URL.
        urlToCachedHtmlTemplate[url] = cachedHtmlTemplate;
        
        return cachedHtmlTemplate.HtmlTemplate;
    }

    private CachedHtmlTemplate LoadAndCacheHtmlTemplateForHost(string urlHost)
    {
        List<KeyValuePair<string, string>> matchingTemplatePaths = hostToHtmlTemplatePath
            .Where(entry => HostsMatch(entry.Key, urlHost))
            .ToList();
        if (matchingTemplatePaths.IsNullOrEmpty())
        {
            return null;
        }

        string htmlTemplatePath = matchingTemplatePaths.FirstOrDefault().Value;
        string htmlTemplate = File.ReadAllText(htmlTemplatePath);
        CachedHtmlTemplate cachedHtmlTemplate = new CachedHtmlTemplate(htmlTemplate);
        hostToCachedHtmlTemplate[urlHost] = cachedHtmlTemplate;
        return cachedHtmlTemplate;
    }

    private void RunWhenWebViewInitialized(Action action)
    {
        if (IsWebViewInitialized)
        {
            action?.Invoke();
        }
        else
        {
            IDisposable iDisposable = null;
            iDisposable = webViewInitializedEventStream.Subscribe(_ =>
            {
                action?.Invoke();
                iDisposable?.Dispose();
            });
        }
    }

    private string GetStringFromJavaScript(string code, string fallbackValue = "")
    {
        string jsResult = webView.ExecuteJavaScript(code).Result;
        if (jsResult.IsNullOrEmpty())
        {
            Debug.LogError($"Failed to get result from JavaScript via {code}");
            return fallbackValue;
        }
        
        return fallbackValue;
    }
    
    private int GetIntFromJavaScript(string code, int fallbackValue = 0)
    {
        string jsResult = GetStringFromJavaScript(code);
        if (jsResult.IsNullOrEmpty())
        {
            return fallbackValue;
        }

        if (int.TryParse(jsResult, out int valueAsInt))
        {
            return valueAsInt;
        }
        
        Debug.LogError($"Failed to parse JavaScript result of {code}. Result: " + jsResult);
        return fallbackValue;
    }
    
    private bool GetBoolFromJavaScript(string code, bool fallbackValue = false)
    {
        string jsResult = GetStringFromJavaScript(code);
        if (jsResult.IsNullOrEmpty())
        {
            return fallbackValue;
        }

        if (bool.TryParse(jsResult, out bool valueAsBool))
        {
            return valueAsBool;
        }
        
        Debug.LogError($"Failed to parse JavaScript result of {code}. Result: " + jsResult);
        return fallbackValue;
    }
    
    private static bool HostsMatch(string a, string b)
    {
        string aWithoutWww = a.Replace("www.", "");
        string bWithoutWww = b.Replace("www.", "");
        return aWithoutWww.ToLowerInvariant() == bWithoutWww.ToLowerInvariant();
    }

    private class CachedHtmlTemplate
    {
        public string HtmlTemplate { get; private set; }

        public CachedHtmlTemplate(string htmlTemplate)
        {
            this.HtmlTemplate = htmlTemplate;
        }
    } 
}
