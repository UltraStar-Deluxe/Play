using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
    public Camera webViewCamera;
    
    private IWebView webView;

    private bool IsWebViewInitialized => webView != null;
    private readonly Subject<bool> webViewInitializedEventStream = new();

    private bool hasScannedTemplates;
    private readonly Dictionary<string, string> hostToHtmlTemplatePath = new();
    private readonly Dictionary<string, CachedHtmlTemplate> hostToCachedHtmlTemplate = new();
    private readonly Dictionary<string, CachedHtmlTemplate> urlToCachedHtmlTemplate = new();

    private bool isPlaying;
    public bool IsPlaying
    {
        get
        {
            if (!IsWebViewInitialized)
            {
                return false;
            }
            return isPlaying;
        }
    }
    
    private double durationInMillis;
    public double DurationInMillis
    {
        get
        {
            if (!IsWebViewInitialized
                || !isContentLoaded)
            {
                return 0;
            }
            return durationInMillis;
        }
    }


    private long receivedPlaybackPositionUpdatedTimeInMillis;
    private double receivedPlaybackPositionInMillis;
    
    private int estimatedPlaybackPositionUpdatedFrameCount;
    private long estimatedPlaybackPositionUpdatedTimeInMillis;
    private double estimatedPlaybackPositionInMillis;
    public double EstimatedPlaybackPositionInMillis
    {
        get
        {
            if (!IsWebViewInitialized)
            {
                return 0;
            }

            return estimatedPlaybackPositionInMillis;
        }
    }
    
    public int volumeInPercent;
    public int VolumeInPercent
    {
        get
        {
            if (!IsWebViewInitialized)
            {
                return 0;
            }
            return volumeInPercent;
        }
        set
        {
            webView.ExecuteJavaScript($"setVolume({value})");
            volumeInPercent = value;
        }
    }

    public bool isContentLoaded;
    public bool IsContentLoaded
    {
        get
        {
            if (!IsWebViewInitialized)
            {
                return false;
            }
            return isContentLoaded;
        }
    }
    
    private string loadedUrl;

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
    }

    private void Update()
    {
        if (!IsWebViewInitialized)
        {
            return;
        }

        UpdatePlaybackPositionInMillisEstimate();
        SendPlaybackPositionInMillisIfNeeded();
    }

    private void SendPlaybackPositionInMillisIfNeeded()
    {
        long currentTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        long timeInMillisSinceLastUpdate = currentTimeInMillis - receivedPlaybackPositionUpdatedTimeInMillis;
        if (timeInMillisSinceLastUpdate > 100)
        {
            webView.ExecuteJavaScript("sendPlaybackPositionInMillis()");
        }
    }

    private void UpdatePlaybackPositionInMillisEstimate()
    {
        if (!isPlaying
            || !isContentLoaded
            || estimatedPlaybackPositionUpdatedFrameCount == Time.frameCount)
        {
            return;
        }

        long currentTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        long deltaTimeInMillis = currentTimeInMillis - estimatedPlaybackPositionUpdatedTimeInMillis;
        estimatedPlaybackPositionInMillis += (int)deltaTimeInMillis;
        estimatedPlaybackPositionUpdatedTimeInMillis = currentTimeInMillis;
        estimatedPlaybackPositionUpdatedFrameCount = Time.frameCount;
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
        // Debug.Log($"Received message from WebView: {e.Value}");
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
    
            if (WebViewMessageDto.TryParseType(webViewMessageDto.type, out WebViewMessageType webViewMessageType))
            {
                switch (webViewMessageType)
                {
                    case WebViewMessageType.PlaybackPositionInMillis:
                    {
                        NumberWebViewMessageDto numberWebViewMessageDto = JsonConverter.FromJson<NumberWebViewMessageDto>(json);

                        long currentTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
                        
                        // Log how far away from the actual time the estimate has become. 
                        // double oldEstimatedPlaybackPositionInMillis = EstimatedPlaybackPositionInMillis;
                        // double oldEstimatedPlaybackPositionInMillisOffset = numberWebViewMessageDto.value -
                        //                                                     oldEstimatedPlaybackPositionInMillis;
                        // Debug.Log($"Received new playback position. Old estimate offset: {oldEstimatedPlaybackPositionInMillisOffset}");
                        
                        receivedPlaybackPositionUpdatedTimeInMillis = currentTimeInMillis;
                        receivedPlaybackPositionInMillis = numberWebViewMessageDto.value;
                        
                        estimatedPlaybackPositionUpdatedFrameCount = Time.frameCount;
                        estimatedPlaybackPositionUpdatedTimeInMillis = currentTimeInMillis;
                        estimatedPlaybackPositionInMillis = receivedPlaybackPositionInMillis;
                        break;
                    }
                    case WebViewMessageType.DurationInMillis:
                    {
                        NumberWebViewMessageDto numberWebViewMessageDto = JsonConverter.FromJson<NumberWebViewMessageDto>(json);
                        durationInMillis = numberWebViewMessageDto.value;
                        break;
                    }
                    case WebViewMessageType.Ready:
                    {
                        isContentLoaded = true;
                        break;
                    }
                    case WebViewMessageType.StartedOrResumed:
                    {
                        isPlaying = true;
                        break;
                    }
                    case WebViewMessageType.StoppedOrPaused:
                    {
                        isPlaying = false;
                        break;
                    }
                    case WebViewMessageType.Volume:
                    {
                        NumberWebViewMessageDto numberWebViewMessageDto = JsonConverter.FromJson<NumberWebViewMessageDto>(json);
                        volumeInPercent = (int)numberWebViewMessageDto.value;
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Failed to parse WebViewMessageDto: " + json);
        }
    }

    public bool CanHandleUrl(string url)
    {
        try
        {
            // Try to parse the URL to make sure it's valid.
            string host = new Uri(url).Host;
        }
        catch (Exception e)
        {
            return false;
        }
        
        if (!hasScannedTemplates)
        {
            ScanHtmlTemplates();
        }
        
        string htmlTemplate = GetHtmlTemplate(url);
        return !htmlTemplate.IsNullOrEmpty();
    }

    public bool LoadUrl(string url)
    {
        if (!CanHandleUrl(url))
        {
            Debug.Log($"Cannot handle URL: {url}");
            return false;
        }

        string finalHtmlCode = GetFinalHtmlCode(url);
        if (finalHtmlCode.IsNullOrEmpty())
        {
            Debug.LogError($"Failed to load HTML code for url: {url}");
            return false;
        }

        if (isPlaying)
        {
            PausePlayback();
        }
        SetPlaybackPositionInMillis(0);

        if (loadedUrl == url)
        {
            // Already loaded.
            Debug.Log($"Reusing already loaded HTML for URL {url}");
            return true;
        }

        isContentLoaded = false;
        loadedUrl = url;
        RunWhenWebViewInitialized(() => webView.LoadHtml(finalHtmlCode));
        return true;
    }

    public void ResumePlayback()
    {
        if (!IsWebViewInitialized)
        {
            return;
        }

        isPlaying = true;
        webView.ExecuteJavaScript("resumePlayback()");
    }

    public void SetPlaybackPositionInMillis(double value)
    {
        webView.ExecuteJavaScript($"setPlaybackPositionInMillis({value})");
        receivedPlaybackPositionInMillis = value;
        estimatedPlaybackPositionInMillis = value;
        estimatedPlaybackPositionUpdatedTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        estimatedPlaybackPositionUpdatedFrameCount = Time.frameCount;
    }

    public void PausePlayback()
    {
        if (!IsWebViewInitialized)
        {
            return;
        }
        
        isPlaying = false;
        webView.ExecuteJavaScript("pausePlayback()");
    }

    public void StopPlayback()
    {
        if (!IsWebViewInitialized)
        {
            return;
        }
        
        isPlaying = false;
        webView.ExecuteJavaScript("stopPlayback()");
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
    
    private void ScanHtmlTemplates()
    {
        if (hasScannedTemplates)
        {
            return;
        }
        hasScannedTemplates = true;
        
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

    // private async Task<string> GetStringFromJavaScript(string code, string fallbackValue = "")
    // {
    //     string jsResult = await webView.ExecuteJavaScript(code);
    //     if (jsResult.IsNullOrEmpty())
    //     {
    //         Debug.LogError($"Failed to get result from JavaScript via {code}");
    //         return fallbackValue;
    //     }
    //     
    //     return fallbackValue;
    // }
    //
    // private async Task<double> GetNumberFromJavaScript(string code, int fallbackValue = 0)
    // {
    //     string jsResult = await GetStringFromJavaScript(code);
    //     if (jsResult.IsNullOrEmpty())
    //     {
    //         return fallbackValue;
    //     }
    //
    //     if (double.TryParse(jsResult, out double valueAsNumber))
    //     {
    //         return valueAsNumber;
    //     }
    //     
    //     Debug.LogError($"Failed to parse JavaScript result of {code} to double. Result: " + jsResult);
    //     return fallbackValue;
    // }
    
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
