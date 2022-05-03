using System;
using System.Collections.Generic;
using System.Linq;
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
    
    [Inject(UxmlName = R.UxmlNames.semanticVersionText)]
    private Label semanticVersionText;

    [Inject(UxmlName = R.UxmlNames.buildTimeStampText)]
    private Label buildTimeStampText;

    [Inject(UxmlName = R.UxmlNames.commitHashText)]
    private Label commitHashText;
    
    [Inject(UxmlName = R.UxmlNames.fpsText)]
    private Label fpsText;
    
    [Inject(UxmlName = R.UxmlNames.toggleRecordingButton)]
    private Button toggleRecordingButton;
    
    [Inject(UxmlName = R.UxmlNames.recordingDevicePicker)]
    private ItemPicker recordingDevicePicker;

    [Inject(UxmlName = R.UxmlNames.nextRecordingDeviceButton)]
    private Button nextRecordingDeviceButton;

    [Inject(UxmlName = R.UxmlNames.connectionStatusText)]
    private Label connectionStatusText;

    [Inject(UxmlName = R.UxmlNames.selectedRecordingDeviceText)]
    private Label selectedRecordingDeviceText;
    
    [Inject(UxmlName = R.UxmlNames.clientNameTextField)]
    private TextField clientNameTextField;
    
    [Inject(UxmlName = R.UxmlNames.visualizeAudioToggle)]
    private Toggle visualizeAudioToggle;
    
    [Inject(UxmlName = R.UxmlNames.audioWaveForm)]
    private VisualElement audioWaveForm;

    [Inject(UxmlName = R.UxmlNames.connectionThroubleshootingText)]
    private Label connectionThroubleshootingText;
    
    [Inject(UxmlName = R.UxmlNames.serverErrorResponseText)]
    private Label serverErrorResponseText;

    [Inject(UxmlName = R.UxmlNames.songListContainer)]
    private VisualElement songListContainer;
    
    [Inject(UxmlName = R.UxmlNames.songListView)]
    private ScrollView songListView;
    
    [Inject(UxmlName = R.UxmlNames.showSongListButton)]
    private Button showSongListButton;
    
    [Inject(UxmlName = R.UxmlNames.closeSongListButton)]
    private Button closeSongListButton;
    
    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    private AudioWaveFormVisualization audioWaveFormVisualization;

    private LabeledItemPickerControl<string> recordingDevicePickerControl;

    private float frameCountTime;
    private int frameCount;
    
    private void Start()
    {
        InitRecordingDeviceNameControls();
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

    private void InitRecordingDeviceNameControls()
    {
        string initialRecordingDeviceName = Microphone.devices.Contains(settings.MicProfile.Name)
            ? settings.MicProfile.Name
            : Microphone.devices.FirstOrDefault();
        SetMicProfileName(initialRecordingDeviceName);

        if (Microphone.devices.IsNullOrEmpty()
            || Microphone.devices.Length <= 1)
        {
            recordingDevicePicker.HideByDisplay();
            nextRecordingDeviceButton.HideByDisplay();
        }
        else
        {
            recordingDevicePickerControl = new LabeledItemPickerControl<string>(recordingDevicePicker, Microphone.devices.ToList());
            recordingDevicePickerControl.SelectItem(settings.MicProfile.Name);
            recordingDevicePickerControl.Selection.Subscribe(newValue => SetMicProfileName(newValue));
            nextRecordingDeviceButton.RegisterCallbackButtonTriggered(() => recordingDevicePickerControl.SelectNextItem());
        }
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
