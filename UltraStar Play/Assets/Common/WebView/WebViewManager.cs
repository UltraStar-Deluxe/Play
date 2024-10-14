using System;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using Vuplex.WebView;

public class WebViewManager : AbstractSingletonBehaviour, INeedInjection
{
    public static WebViewManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<WebViewManager>();

    private static bool isWebConfigInitialized;

    [InjectedInInspector]
    public CanvasWebViewPrefab webViewPrefabPrefab;

    [InjectedInInspector]
    public Canvas webViewCanvas;

    [InjectedInInspector]
    public Camera webViewCamera;

    [InjectedInInspector]
    public TextAsset defaultWebViewHtml;

    [InjectedInInspector]
    public RenderTexture defaultRenderTexture;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private Settings settings;

    private IWebView webView;
    public IWebView WebView => webView; // Public getter to allow modding

    private bool IsWebViewInitialized => webView != null;
    private readonly Subject<VoidEvent> webViewInitializedEventStream = new();

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


    private long receivedPositionUpdatedTimeInMillis;
    private double receivedPositionInMillis;

    private int estimatedPositionUpdatedFrameCount;
    private long estimatedPositionUpdatedTimeInMillis;
    private double estimatedPositionInMillis;
    public double EstimatedPositionInMillis
    {
        get
        {
            if (!IsWebViewInitialized)
            {
                return 0;
            }

            return estimatedPositionInMillis;
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
            volumeInPercent = value;

            if (!IsWebViewInitialized)
            {
                return;
            }

            // The embedded browser does not consider AudioListener.volume. Thus, this must be considered here explicitly.
            float jsVolume = AudioListener.volume * NumberUtils.PercentToFactor(volumeInPercent) * 100;
            webView.ExecuteJavaScript($"setVolume({jsVolume})");
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
    public string LoadedUrl => loadedUrl; // Public getter to allow modding

    private bool javaScriptCanLoadUrl;

    public bool IsWebViewCanvasControlEnabled => webViewCanvas.renderMode is RenderMode.ScreenSpaceOverlay;

    private CanvasWebViewPrefab webViewPrefabInstance;

    private bool hasShownControlsNotification;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        // Disable camera until WebView texture requested
        webViewCamera.gameObject.SetActive(false);
    }

    protected override void StartSingleton()
    {
        DirectoryUtils.CreateDirectory(WebViewUtils.GetDefaultWebViewScriptsAbsolutePath());

        sceneNavigator.BeforeSceneChangeEventStream.Subscribe(_ => OnBeforeSceneChanged());
        sceneNavigator.SceneChangedEventStream.Subscribe(_ => OnSceneChanged());
        settings.ObserveEveryValueChanged(it => it.VolumePercent)
            .Subscribe(_ => UpdateVolume())
            .AddTo(gameObject);
        RegisterInputActions();

        if (settings.EnableWebView)
        {
            InitializeWebViewConfig();
            InstantiateWebViewPrefab();
        }
    }

    protected override void OnDestroySingleton()
    {
        base.OnDestroySingleton();
        if (webViewPrefabInstance != null)
        {
            webViewPrefabInstance.Initialized -= OnWebViewPrefabInstanceInitialized;
        }
        WebViewUtils.ClearCache();
    }

    private void InitializeWebViewConfig()
    {
        // Cannot change certain configuration after Chromium has been started
        if (isWebConfigInitialized)
        {
            return;
        }
        isWebConfigInitialized = true;

        try
        {
            // By default browsers block web pages from autoplaying video or audio.
            // Explicitly allow playback of video or audio without user interaction.
            // This must be called early, e.g. in Awake.
            Web.SetAutoplayEnabled(true);

            // Google only allows sign-in from selected browser.
            // Thus, set the User-Agent header to a browser that is allowed by Google.
            if (!settings.CustomUserAgent.IsNullOrEmpty())
            {
                Web.SetUserAgent(settings.CustomUserAgent);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"Failed to instantiate WebView config: {e.Message}");
        }
    }

    private void InstantiateWebViewPrefab()
    {
        if (webViewPrefabInstance != null)
        {
            Debug.LogWarning("Cannot instantiate WebView. WebView already instantiated.");
            return;
        }

        foreach (Transform child in webViewCanvas.transform)
        {
            Destroy(child.gameObject);
        }
        webViewPrefabInstance = Instantiate(webViewPrefabPrefab, webViewCanvas.transform);
        webViewPrefabInstance.Initialized += OnWebViewPrefabInstanceInitialized;
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
        UpdateWebViewCameraActive();
    }

    private void SetWebViewInputEnabled(bool newValue)
    {
        if (webViewPrefabInstance == null)
        {
            return;
        }
        webViewPrefabInstance.HoveringEnabled = newValue;
        webViewPrefabInstance.ClickingEnabled = newValue;
        webViewPrefabInstance.ScrollingEnabled = newValue;
        webViewPrefabInstance.KeyboardEnabled = newValue;
        webViewPrefabInstance.CursorIconsEnabled = newValue;
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

        UpdatePositionInMillisEstimate();
        SendPositionInMillisIfNeeded();
    }

    private void SendPositionInMillisIfNeeded()
    {
        long currentTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        long timeInMillisSinceLastUpdate = currentTimeInMillis - receivedPositionUpdatedTimeInMillis;
        if (timeInMillisSinceLastUpdate > 100)
        {
            webView.ExecuteJavaScript("sendPlaybackPositionInMillis()");
        }
    }

    private void UpdatePositionInMillisEstimate()
    {
        if (!isPlaying
            || !isContentLoaded
            || estimatedPositionUpdatedFrameCount == Time.frameCount
            || receivedPositionInMillis <= 0)
        {
            return;
        }

        long currentTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        long deltaTimeInMillis = currentTimeInMillis - estimatedPositionUpdatedTimeInMillis;
        estimatedPositionInMillis += (int)deltaTimeInMillis;
        estimatedPositionUpdatedTimeInMillis = currentTimeInMillis;
        estimatedPositionUpdatedFrameCount = Time.frameCount;
    }

    private void OnWebViewPrefabInstanceInitialized(object sender, EventArgs e)
    {
        webView = webViewPrefabInstance.WebView;
        webViewPrefabInstance.WebView.MessageEmitted += OnWebViewMessageReceived;
        webView.LoadProgressChanged += OnWebViewLoadProgressChanged;

        webView.LoadHtml(defaultWebViewHtml.text);

        webViewInitializedEventStream.OnNext(VoidEvent.instance);
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
        NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error_failedToLoadWithName,
            "name", loadedUrl));
    }

    private void OnWebViewMessageReceived(object sender, EventArgs<string> e)
    {
        Log.Verbose(() => $"Received message from WebView: {e.Value}");
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
                        // double oldEstimatedPlaybackPositionInMillis = EstimatedPositionInMillis;
                        // double oldEstimatedPlaybackPositionInMillisOffset = numberWebViewMessageDto.value -
                        //                                                     oldEstimatedPlaybackPositionInMillis;
                        // Log.Verbose(() => $"Received new playback position. Old estimate offset: {oldEstimatedPlaybackPositionInMillisOffset}");

                        receivedPositionUpdatedTimeInMillis = currentTimeInMillis;
                        receivedPositionInMillis = numberWebViewMessageDto.value;

                        estimatedPositionUpdatedFrameCount = Time.frameCount;
                        estimatedPositionUpdatedTimeInMillis = currentTimeInMillis;
                        estimatedPositionInMillis = receivedPositionInMillis;
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

    public bool LoadUrl(string url)
    {
        if (settings == null
            || !settings.EnableWebView)
        {
            Debug.LogWarning($"WebView cannot load URL because WebView is disabled in settings.");
            return false;
        }

        if (!WebViewUtils.CanHandleWebViewUrl(url))
        {
            Debug.LogWarning($"Cannot handle URL: {url}");
            return false;
        }

        string host = new Uri(url).Host;
        string hostWithoutWww = host.TrimStart("www.");
        string hostWithWww = $"www.{hostWithoutWww}";
        if (settings.AcceptedWebViewHosts.Contains(hostWithoutWww)
            || settings.AcceptedWebViewHosts.Contains(hostWithWww))
        {
            return DoLoadUrl(url);
        }
        Debug.LogWarning($"Asking to accept host before loading URI into WebView. host: '{host}', uri: '{url}'");

        MessageDialogControl messageDialogControl = uiManager.CreateDialogControl(Translation.Get(R.Messages.webView_askToOpenWebsiteDialog_title));
        messageDialogControl.Message = Translation.Get(R.Messages.webView_askToOpenWebsiteDialog_message,
            "host", host);

        messageDialogControl.AddInformationMessage($"You can open the embedded browser anytime by pressing F8 or Ctrl+B.");

        messageDialogControl.AddButton(Translation.Get(R.Messages.webView_askToOpenWebsiteDialog_confirm), _ =>
        {
            messageDialogControl.CloseDialog();
            settings.AcceptedWebViewHosts.Add(host);
            DoLoadUrl(url);
        });
        messageDialogControl.AddButton(Translation.Get(R.Messages.action_cancel),
            _ => messageDialogControl.CloseDialog());

        return false;
    }

    private bool DoLoadUrl(string url)
    {
        string webViewScript = WebViewUtils.GetWebViewScript(url);
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
            SetPositionInMillis(0);
            return true;
        }

        bool isLoadingUrlOfSameHost;
        try
        {
            isLoadingUrlOfSameHost = string.Equals(new Uri(loadedUrl).Host, new Uri(webView.Url).Host, StringComparison.InvariantCultureIgnoreCase);
        }
        catch
        {
            isLoadingUrlOfSameHost = false;
        }

        if (!javaScriptCanLoadUrl)
        {
            isContentLoaded = false;
        }

        loadedUrl = url;
        RunWhenWebViewInitialized(() =>
        {
            if (!hasShownControlsNotification)
            {
                hasShownControlsNotification = true;
                NotificationManager.CreateNotification(Translation.Get(R.Messages.webView_info_controls));
            }

            if (isContentLoaded && isLoadingUrlOfSameHost && javaScriptCanLoadUrl)
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

    private void UpdateWebViewCameraActive()
    {
        // Only render the WebView with the camera when it is visible to the user.
        webViewCamera.gameObject.SetActive(
            webViewCamera.targetTexture != null
            || IsWebViewCanvasControlEnabled);
        if (!webViewCamera.gameObject.activeSelf)
        {
            RenderTextureUtils.Clear(webViewCamera.targetTexture);
        }
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

    public void SetPositionInMillis(double value)
    {
        if (!IsWebViewInitialized)
        {
            return;
        }
        webView.ExecuteJavaScript($"setPlaybackPositionInMillis({value})");
        receivedPositionInMillis = value;
        estimatedPositionInMillis = value;
        estimatedPositionUpdatedTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        estimatedPositionUpdatedFrameCount = Time.frameCount;
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

    public void ReloadScripts()
    {
        WebViewUtils.ClearCache();
        loadedUrl = null;
        javaScriptCanLoadUrl = false;
        Debug.Log("Reloaded WebView scripts by clearing cache.");
    }

    public void SetWebViewRenderTexture(RenderTexture targetTexture)
    {
        if (webViewCamera.targetTexture != targetTexture)
        {
            webViewCamera.targetTexture = targetTexture;
            UpdateWebViewCameraActive();
        }
    }

    public void ResetWebViewRenderTexture()
    {
        SetWebViewRenderTexture(defaultRenderTexture);
    }
}
