using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Vuplex.WebView;

public class WebViewManager : AbstractSingletonBehaviour, INeedInjection
{
    public static WebViewManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<WebViewManager>();

    [InjectedInInspector]
    public CanvasWebViewPrefab webViewPrefab;
    
    [InjectedInInspector]
    public Canvas webViewCanvas;
    
    [InjectedInInspector]
    public Camera webViewCamera;
    
    [InjectedInInspector]
    public TextAsset defaultWebViewHtml;
    
    [Inject]
    private UIDocument uiDocument;
    
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private UiManager uiManager;
    
    [Inject]
    private Settings settings;
    
    private IWebView webView;

    private bool IsWebViewInitialized => webView != null;
    private readonly Subject<bool> webViewInitializedEventStream = new();

    private bool hasScannedJavaScriptFiles;
    private readonly Dictionary<string, string> hostToWebViewScript = new();
    private readonly Dictionary<string, CachedWebViewScript> hostToCachedWebViewScript = new();
    private readonly Dictionary<string, CachedWebViewScript> urlToCachedWebViewScript = new();

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
            // The embedded browser does not consider AudioListener.volume. Thus, this must be considered here explicitly.
            float jsVolume = AudioListener.volume * NumberUtils.PercentToFactor(volumeInPercent) * 100;
            webView.ExecuteJavaScript($"setVolume({jsVolume})");
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

    private bool javaScriptCanLoadUrl;
    
    public bool IsWebViewCanvasControlEnabled => webViewCanvas.renderMode is RenderMode.ScreenSpaceOverlay;

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

    protected override void StartSingleton()
    {
        sceneNavigator.BeforeSceneChangeEventStream.Subscribe(_ => OnBeforeSceneChanged());
        sceneNavigator.SceneChangedEventStream.Subscribe(_ => OnSceneChanged());
        settings.ObserveEveryValueChanged(it => it.VolumePercent)
            .Subscribe(_ => UpdateVolume());
        RegisterInputActions();
    }

    private void OnBeforeSceneChanged()
    {
        PausePlayback();
    }

    private void OnSceneChanged()
    {
        RegisterInputActions();
    }

    private void RegisterInputActions()
    {
        InputManager.GetInputAction(R.InputActions.usplay_toggleWebViewControl).PerformedAsObservable()
            .Subscribe(_ => ToggleWebViewControl())
            .AddTo(gameObject);
        
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(200)
            .Subscribe(_ =>
            {
                if (IsWebViewCanvasControlEnabled)
                {
                    ToggleWebViewControl();
                    InputManager.GetInputAction(R.InputActions.usplay_back).CancelNotifyForThisFrame();
                }
            });
    }

    private void ToggleWebViewControl()
    {
        if (IsWebViewCanvasControlEnabled)
        {
            webViewCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            SetWebViewInputEnabled(false);
            SetUiToolkitInputEnabled(true);
        }
        else
        {
            webViewCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            SetWebViewInputEnabled(true);
            SetUiToolkitInputEnabled(false);
        }
    }

    private void SetWebViewInputEnabled(bool newValue)
    {
        webViewPrefab.HoveringEnabled = newValue;
        webViewPrefab.ClickingEnabled = newValue;
        webViewPrefab.ScrollingEnabled = newValue;
        webViewPrefab.KeyboardEnabled = newValue;
        webViewPrefab.CursorIconsEnabled = newValue;
    }
    
    private void SetUiToolkitInputEnabled(bool newValue)
    {
        uiDocument.rootVisualElement.SetVisibleByDisplay(newValue);
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
            || estimatedPlaybackPositionUpdatedFrameCount == Time.frameCount
            || receivedPlaybackPositionInMillis <= 0)
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
        
        UnityEngine.InputSystem.Keyboard keyboard = InputSystem.GetDevice<UnityEngine.InputSystem.Keyboard>();
        if (keyboard != null)
        {
            keyboard.onTextInput += OnKeyboardTextInput;
        }
    }

    private void OnDisable()
    {
        webViewPrefab.Initialized -= OnWebViewPrefabInitialized;
        
        UnityEngine.InputSystem.Keyboard keyboard = InputSystem.GetDevice<UnityEngine.InputSystem.Keyboard>();
        if (keyboard != null)
        {
            keyboard.onTextInput -= OnKeyboardTextInput;
        }
    }
    
    private void OnWebViewPrefabInitialized(object sender, EventArgs e)
    {
        webView = webViewPrefab.WebView;
        webViewPrefab.WebView.MessageEmitted += OnWebViewMessageReceived;
        webView.LoadProgressChanged += OnWebViewLoadProgressChanged;
        
        webView.LoadHtml(defaultWebViewHtml.text);
        
        webViewInitializedEventStream.OnNext(true);
    }

    private void OnWebViewLoadProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        if (e.Type is ProgressChangeType.Finished)
        {
            OnWebViewFinishedLoading();
        }
        else if (e.Type is ProgressChangeType.Failed)
        {
            OnWebViewFailedLoading();
        }
    }

    private void OnWebViewFinishedLoading()
    {
        Debug.Log("Finished loading of URL: " + loadedUrl);
        isContentLoaded = true;
        UpdateVolume();
    }

    private void UpdateVolume()
    {
        VolumeInPercent = VolumeInPercent;
    }

    private void OnWebViewFailedLoading()
    {
        Debug.Log("Failed loading of URL: " + loadedUrl);
        UiManager.CreateNotification($"Failed to load {loadedUrl}");
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
                    case WebViewMessageType.CanLoadUrl:
                    {
                        BoolWebViewMessageDto boolWebViewMessageDto = JsonConverter.FromJson<BoolWebViewMessageDto>(json);
                        javaScriptCanLoadUrl = boolWebViewMessageDto.value;
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
        
        if (!hasScannedJavaScriptFiles)
        {
            ScanJavaScriptFiles();
        }
        
        string webViewScript = GetWebViewScript(url);
        return !webViewScript.IsNullOrEmpty();
    }

    public bool LoadUrl(string url)
    {
        if (!CanHandleUrl(url))
        {
            Debug.Log($"Cannot handle URL: {url}");
            return false;
        }

        string host = new Uri(url).Host;
        if (settings.AcceptedWebViewHosts.Contains(host))
        {
            return DoLoadUrl(url);
        }
        
        MessageDialogControl messageDialogControl = uiManager.CreateDialogControl("Open in Embedded Browser");
        messageDialogControl.Message = $"The song file references an external website.\n"
                                       + $"Do you want to open {host} in the embedded browser?";

        VisualElement infoContainer = new();
        infoContainer.name = "row";
        infoContainer.AddToClassList("ml-auto");
        infoContainer.AddToClassList("mr-auto");
        infoContainer.AddToClassList("my-3");
        messageDialogControl.AddVisualElement(infoContainer);
        
        FontIcon infoIcon = new MaterialIcon();
        infoIcon.Icon = "info_outline";
        infoIcon.style.fontSize = 14;
        infoIcon.AddToClassList("mr-1");
        infoContainer.Add(infoIcon);
        
        Label infoLabel = new Label($"You can open the embedded browser anytime by pressing F8 or Ctrl+B.");
        infoLabel.AddToClassList("smallFont");
        infoContainer.Add(infoLabel);
        
        messageDialogControl.AddButton("Yes, do not ask again", _ =>
        {
            messageDialogControl.CloseDialog();
            settings.AcceptedWebViewHosts.Add(host);
            DoLoadUrl(url);
        });
        messageDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.cancel),
            _ => messageDialogControl.CloseDialog());

        return false;
    }

    private bool DoLoadUrl(string url)
    {
        string webViewScript = GetWebViewScript(url);
        if (webViewScript.IsNullOrEmpty())
        {
            Debug.LogError($"Failed to load WebView script code for url: {url}");
            return false;
        }
        
        if (isPlaying)
        {
            PausePlayback();
        }

        if (loadedUrl == url)
        {
            // Already loaded.
            Debug.Log($"Reusing already loaded web page for URL {url}");
            SetPlaybackPositionInMillis(0);
            return true;
        }

        if (!javaScriptCanLoadUrl)
        {
            isContentLoaded = false;
        }
        
        loadedUrl = url;
        RunWhenWebViewInitialized(() =>
        {
            if (isContentLoaded && javaScriptCanLoadUrl)
            {
                Debug.Log("Loading new URL via JavaScript");
                webView.ExecuteJavaScript($"setVolume(0)");
                webView.ExecuteJavaScript($"loadUrl('{url}')");
            }
            else
            {
                Debug.Log("Loading new URL into WebView");
                webView.PageLoadScripts.Clear();
                webView.PageLoadScripts.Add(webViewScript);
                webView.LoadUrl(url);
            }
        });
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
        UpdateVolume();
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
        webView.ExecuteJavaScript("setVolume(0)");
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

    private void ScanJavaScriptFiles()
    {
        if (hasScannedJavaScriptFiles)
        {
            return;
        }
        hasScannedJavaScriptFiles = true;
        
        string webViewScriptsFolder = ApplicationUtils.GetWebViewScriptsAbsolutePath();
        DirectoryUtils.CreateDirectory(webViewScriptsFolder);
        
        string[] webViewScriptPaths = Directory.GetFiles(webViewScriptsFolder, "*.js");
        foreach (string webViewScriptPath in webViewScriptPaths)
        {
            string host = Path.GetFileNameWithoutExtension(webViewScriptPath);
            hostToWebViewScript[host] = webViewScriptPath;
        }
    }

    private string GetWebViewScript(string url)
    {
        // Try get cached template for the specific URL.
        if (urlToCachedWebViewScript.TryGetValue(url, out CachedWebViewScript cachedWebViewScript))
        {
            return cachedWebViewScript.Content;
        }
        
        // Try get cached template for the host.
        string urlHost = new Uri(url).Host;
        if (!hostToCachedWebViewScript.TryGetValue(urlHost, out cachedWebViewScript))
        {
            // Load and remember the template for the host.
            cachedWebViewScript = LoadAndCacheWebViewScriptForHost(urlHost);
        }

        if (cachedWebViewScript == null)
        {
            return "";
        }
        
        // Remember the template for the specific URL.
        urlToCachedWebViewScript[url] = cachedWebViewScript;
        
        return cachedWebViewScript.Content;
    }

    private CachedWebViewScript LoadAndCacheWebViewScriptForHost(string urlHost)
    {
        List<KeyValuePair<string, string>> matches = hostToWebViewScript
            .Where(entry => HostsMatch(entry.Key, urlHost))
            .ToList();
        if (matches.IsNullOrEmpty())
        {
            return null;
        }

        string filePath = matches.FirstOrDefault().Value;
        string fileContent = File.ReadAllText(filePath);
        
        Debug.Log($"Found WebView script for host '{urlHost}' in file '{filePath}'");
        
        CachedWebViewScript cachedWebViewScript = new CachedWebViewScript(fileContent);
        hostToCachedWebViewScript[urlHost] = cachedWebViewScript;
        return cachedWebViewScript;
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

    private void OnKeyboardTextInput(char newChar)
    {
        // if (IsWebViewInitialized
        //     && IsWebViewCanvasControlEnabled)
        // {
        //     webView.SendKey(newChar.ToString());
        // }
    }

    private static bool HostsMatch(string a, string b)
    {
        string aWithoutWww = a.Replace("www.", "");
        string bWithoutWww = b.Replace("www.", "");
        return aWithoutWww.ToLowerInvariant() == bWithoutWww.ToLowerInvariant();
    }
    
    public void ReloadScripts()
    {
        hostToWebViewScript.Clear();
        hostToCachedWebViewScript.Clear();
        urlToCachedWebViewScript.Clear();
        loadedUrl = null;
        hasScannedJavaScriptFiles = false;
        javaScriptCanLoadUrl = false;
        Debug.Log("Reloaded WebView scripts by clearing cache.");
    }

    private class CachedWebViewScript
    {
        public string Content { get; private set; }

        public CachedWebViewScript(string content)
        {
            this.Content = content;
        }
    }
}
