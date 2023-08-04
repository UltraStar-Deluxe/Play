using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using Vuplex.WebView;

public class WebViewManager : AbstractSingletonBehaviour, INeedInjection
{
    public static WebViewManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<WebViewManager>();

    [InjectedInInspector]
    public CanvasWebViewPrefab webViewPrefabPrefab;
    
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

    private bool javaScriptCanLoadUrl;
    
    public bool IsWebViewCanvasControlEnabled => webViewCanvas.renderMode is RenderMode.ScreenSpaceOverlay;

    private CanvasWebViewPrefab webViewPrefabInstance;

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
            .Subscribe(_ => UpdateVolume())
            .AddTo(gameObject);
        RegisterInputActions();

        if (!settings.DisableWebView)
        {
            InstantiateWebView();
        }
    }

    protected override void OnDestroySingleton()
    {
        base.OnDestroySingleton();
        if (webViewPrefabInstance != null)
        {
            webViewPrefabInstance.Initialized -= OnWebViewPrefabInstanceInitialized;
        }
    }

    private void InstantiateWebView()
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

    private void OnWebViewPrefabInstanceInitialized(object sender, EventArgs e)
    {
        webView = webViewPrefabInstance.WebView;
        webViewPrefabInstance.WebView.MessageEmitted += OnWebViewMessageReceived;
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
                        // double oldEstimatedPlaybackPositionInMillis = EstimatedPlaybackPositionInMillis;
                        // double oldEstimatedPlaybackPositionInMillisOffset = numberWebViewMessageDto.value -
                        //                                                     oldEstimatedPlaybackPositionInMillis;
                        // Log.Verbose(() => $"Received new playback position. Old estimate offset: {oldEstimatedPlaybackPositionInMillisOffset}");
                        
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

    public bool LoadUrl(string url)
    {
        if (settings == null
            || settings.DisableWebView)
        {
            return false;
        }

        if (!WebViewUtils.CanHandleWebViewUrl(url))
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

        messageDialogControl.AddInformationMessage($"You can open the embedded browser anytime by pressing F8 or Ctrl+B.");
        
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
        if (!IsWebViewInitialized)
        {
            return;
        }
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
}
