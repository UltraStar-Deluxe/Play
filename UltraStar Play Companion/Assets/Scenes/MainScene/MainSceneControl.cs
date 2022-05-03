using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using ProTrans;
using Button = UnityEngine.UIElements.Button;
using Toggle = UnityEngine.UIElements.Toggle;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MainSceneControl : MonoBehaviour, INeedInjection
{
    private const int ConnectRequestCountShowTroubleshootingHintThreshold = 3;
    
    [InjectedInInspector]
    public TextAsset versionPropertiesTextAsset;

    [InjectedInInspector]
    public SongListRequestor songListRequestor;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private ClientSideConnectRequestManager clientSideConnectRequestManager;

    [Inject]
    private ClientSideMicSampleRecorder clientSideMicSampleRecorder;

    [Inject]
    private Settings settings;
    
    [Inject(Key = "#semanticVersionText")]
    private Label semanticVersionText;

    [Inject(Key = "#buildTimeStampText")]
    private Label buildTimeStampText;

    [Inject(Key = "#commitHashText")]
    private Label commitHashText;
    
    [Inject(Key = "#fpsText")]
    private Label fpsText;
    
    [Inject(Key = "#toggleRecordingButton")]
    private Button toggleRecordingButton;
    
    [Inject(Key = "#recordingDeviceButtonContainer")]
    private VisualElement recordingDeviceButtonContainer;
    
    [Inject(Key = "#connectionStatusText")]
    private Label connectionStatusText;

    [Inject(Key = "#selectedRecordingDeviceText")]
    private Label selectedRecordingDeviceText;
    
    [Inject(Key = "#clientNameTextField")]
    private TextField clientNameTextField;
    
    [Inject(Key = "#visualizeAudioToggle")]
    private Toggle visualizeAudioToggle;
    
    [Inject(Key = "#audioWaveForm")]
    private VisualElement audioWaveForm;

    [Inject(Key = "#connectionThroubleshootingText")]
    private Label connectionThroubleshootingText;
    
    [Inject(Key = "#serverErrorResponseText")]
    private Label serverErrorResponseText;

    [Inject(Key = "#songListContainer")]
    private VisualElement songListContainer;
    
    [Inject(Key = "#songListView")]
    private ScrollView songListView;
    
    [Inject(Key = "#showSongListButton")]
    private Button showSongListButton;
    
    [Inject(Key = "#closeSongListButton")]
    private Button closeSongListButton;
    
    [Inject(Key = "#sceneTitle")]
    private Label sceneTitle;

    private AudioWaveFormVisualization audioWaveFormVisualization;

    private float frameCountTime;
    private int frameCount;
    
    private void Start()
    {
        settings.ObserveEveryValueChanged(it => it.MicProfile)
            .Subscribe(_ => UpdateSelectedRecordingDeviceText());
        clientSideMicSampleRecorder.IsRecording
            .Subscribe(OnRecordingStateChanged);

        // All controls are hidden until a connection has been established.
        uiDocument.rootVisualElement.Query(null, "onlyVisibleWhenConnected").ForEach(it => it.HideByDisplay());
        connectionThroubleshootingText.HideByDisplay();
        serverErrorResponseText.HideByDisplay();
        
        toggleRecordingButton.RegisterCallbackButtonTriggered(ToggleRecording);

        clientNameTextField.value = settings.ClientName;
        clientNameTextField.RegisterCallback<NavigationSubmitEvent>(_ => OnClientNameTextFieldSubmit());
        clientNameTextField.RegisterCallback<BlurEvent>(_ => OnClientNameTextFieldSubmit());
        
        visualizeAudioToggle.value = settings.ShowAudioWaveForm;
        audioWaveForm.SetVisibleByDisplay(settings.ShowAudioWaveForm);
        visualizeAudioToggle.RegisterValueChangedCallback(changeEvent =>
        {
            audioWaveForm.SetVisibleByDisplay(changeEvent.newValue);
            settings.ShowAudioWaveForm = changeEvent.newValue;
        });
        
        clientSideConnectRequestManager.ConnectEventStream
            .Subscribe(UpdateConnectionStatus);

        songListRequestor.SongListEventStream.Subscribe(evt => HandleSongListEvent(evt));
        
        showSongListButton.RegisterCallbackButtonTriggered(() => ShowSongList());
        closeSongListButton.RegisterCallbackButtonTriggered(() => songListContainer.HideByDisplay());
        
        UpdateVersionInfoText();

        audioWaveForm.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            audioWaveFormVisualization = new AudioWaveFormVisualization(gameObject, audioWaveForm);
        });
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && connectionStatusText == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        connectionStatusText.text = TranslationManager.GetTranslation(R.Messages.connecting);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.title);
        visualizeAudioToggle.label = TranslationManager.GetTranslation(R.Messages.visualizeMicInput);
        showSongListButton.text = TranslationManager.GetTranslation(R.Messages.songList_show);
        closeSongListButton.text = TranslationManager.GetTranslation(R.Messages.songList_hide);
    }
    
    private void HandleSongListEvent(SongListEvent evt)
    {
        songListView.Clear();
        if (!evt.ErrorMessage.IsNullOrEmpty())
        {
            AddSongListLabel(evt.ErrorMessage);
            return;
        }

        evt.LoadedSongsDto.SongList.Sort((a,b) => string.Compare(a.Artist, b.Artist, StringComparison.InvariantCulture));
        foreach (SongDto songDto in evt.LoadedSongsDto.SongList)
        {
            AddSongListLabel(songDto.Artist + " - " + songDto.Title);
        }

        if (!evt.LoadedSongsDto.IsSongScanFinished)
        {
            AddSongListLabel("...");
        }
    }

    private void ShowSongList()
    {
        songListContainer.ShowByDisplay();
        
        if (!songListRequestor.SuccessfullyLoadedAllSongs)
        {
            songListView.Clear();
            AddSongListLabel("Loading songs list...");
            songListRequestor.RequestSongList();
        }
    }

    private void Update()
    {
        if (audioWaveForm.style.display != DisplayStyle.None
            && audioWaveFormVisualization != null)
        {
            audioWaveFormVisualization.DrawWaveFormMinAndMaxValues(clientSideMicSampleRecorder.MicSampleBuffer);
        }
        UpdateFps();
    }

    private void UpdateFps()
    {
        frameCountTime += Time.deltaTime;
        frameCount++;
        if (frameCountTime > 1)
        {
            int fps = (int)(frameCount / frameCountTime);
            fpsText.text = TranslationManager.GetTranslation(R.Messages.fps, "value", fps);
            frameCount = 0;
            frameCountTime = 0;
        }
    }

    private void OnClientNameTextFieldSubmit()
    {
        settings.ClientName = clientNameTextField.value;
        // Reconnect to let the main know about the new clientName.
        clientSideConnectRequestManager.CloseConnectionAndReconnect();
    }

    private void UpdateSelectedRecordingDeviceText()
    {
        int sampleRate = ClientSideMicSampleRecorder.GetFinalSampleRate(settings.MicProfile.Name, settings.MicProfile.SampleRate);
        selectedRecordingDeviceText.text = $"{settings.MicProfile.Name}\n" +
                                           $"(sampleRate:{sampleRate} Hz," +
                                           $"delay: {settings.MicProfile.DelayInMillis} ms," +
                                           $"amp: {settings.MicProfile.Amplification}," +
                                           $"supp: {settings.MicProfile.NoiseSuppression})";
    }

    private void OnRecordingStateChanged(bool isRecording)
    {
        if (isRecording)
        {
            toggleRecordingButton.text = TranslationManager.GetTranslation(R.Messages.stopRecording);
            toggleRecordingButton.AddToClassList("stopRecordingButton");
        }
        else
        {
            toggleRecordingButton.text = TranslationManager.GetTranslation(R.Messages.startRecording);
            toggleRecordingButton.RemoveFromClassList("stopRecordingButton");
        }
    }

    private void UpdateConnectionStatus(ConnectEvent connectEvent)
    {
        if (connectEvent.IsSuccess)
        {
            connectionStatusText.text = TranslationManager.GetTranslation(R.Messages.connectedTo, "remote" , connectEvent.ServerIpEndPoint.Address);
            uiDocument.rootVisualElement.Query(null, "onlyVisibleWhenConnected").ForEach(it => it.ShowByDisplay());
            audioWaveForm.SetVisibleByDisplay(settings.ShowAudioWaveForm);
            connectionThroubleshootingText.HideByDisplay();
            serverErrorResponseText.HideByDisplay();
            toggleRecordingButton.Focus();
            UpdateRecordingDeviceButtons();
        }
        else
        {
            connectionStatusText.text = connectEvent.ConnectRequestCount > 0
                ? TranslationManager.GetTranslation(R.Messages.connectingWithFailedAttempts, "count", connectEvent.ConnectRequestCount)
                : TranslationManager.GetTranslation(R.Messages.connecting);
            
            uiDocument.rootVisualElement.Query(null, "onlyVisibleWhenConnected").ForEach(it => it.HideByDisplay());
            if (connectEvent.ConnectRequestCount > ConnectRequestCountShowTroubleshootingHintThreshold)
            {
                connectionThroubleshootingText.ShowByDisplay();
                connectionThroubleshootingText.text = TranslationManager.GetTranslation(R.Messages.troubleShootingHints);
            }

            if (!connectEvent.ErrorMessage.IsNullOrEmpty())
            {
                serverErrorResponseText.ShowByDisplay();
                serverErrorResponseText.text = connectEvent.ErrorMessage;
            }
        }
    }

    private void UpdateRecordingDeviceButtons()
    {
        recordingDeviceButtonContainer.Clear();
        if (Microphone.devices.Length <= 1)
        {
            // No real choice
            return;
        }
        
        Microphone.devices.ForEach(deviceName =>
        {
            Button deviceButton = new Button();
            deviceButton.RegisterCallbackButtonTriggered(
                () => SetMicProfileName(deviceName));
            deviceButton.text = $"{deviceName}";
            recordingDeviceButtonContainer.Add(deviceButton);
        });
    }

    private void SetMicProfileName(string deviceName)
    {
        MicProfile micProfile = new MicProfile(settings.MicProfile);
        micProfile.Name = deviceName;
        settings.MicProfile = micProfile;
    }

    private void ToggleRecording()
    {
        if (clientSideMicSampleRecorder.IsRecording.Value)
        {
            clientSideMicSampleRecorder.StopRecording();
        }
        else
        {
            clientSideMicSampleRecorder.StartRecording();
        }
    }

    private void UpdateVersionInfoText()
    {
        Dictionary<string, string> versionProperties = PropertiesFileParser.ParseText(versionPropertiesTextAsset.text);

        // Show the release number (e.g. release date, or some version number)
        versionProperties.TryGetValue("release", out string release);
        semanticVersionText.text = TranslationManager.GetTranslation(R.Messages.version, "value", release);

        // Show the commit hash of the build
        versionProperties.TryGetValue("commit_hash", out string commitHash);
        commitHashText.text = TranslationManager.GetTranslation(R.Messages.commit, "value", commitHash);
        
        // Show the build timestamp only for development builds
        if (Debug.isDebugBuild)
        {
            versionProperties.TryGetValue("build_timestamp", out string buildTimeStamp);
            buildTimeStampText.text = TranslationManager.GetTranslation(R.Messages.buildTimeStamp, "value", buildTimeStamp);
        }
        else
        {
            buildTimeStampText.text = "";
        }
    }

    private void AddSongListLabel(string text)
    {
        Label label = new Label(text);
        label.AddToClassList("songListElement");
        label.style.whiteSpace = WhiteSpace.Normal;
        songListView.Add(label);
    }
}
