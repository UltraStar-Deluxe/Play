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

public class MainSceneControl : MonoBehaviour, INeedInjection, ITranslator
{
    private const int ConnectRequestCountShowTroubleshootingHintThreshold = 3;

    private const string MenuOverlayVisibleStyleClass = "shown";

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

    [Inject(UxmlName = R.UxmlNames.connectionStatusText)]
    private Label connectionStatusText;

    [Inject(UxmlName = R.UxmlNames.recordingDeviceInfo)]
    private Label recordingDeviceInfo;
    
    [Inject(UxmlName = R.UxmlNames.clientNameTextField)]
    private TextField clientNameTextField;

    [Inject(UxmlName = R.UxmlNames.visualizeAudioLabel)]
    private Label visualizeAudioLabel;

    [Inject(UxmlName = R.UxmlNames.visualizeAudioToggle)]
    private Toggle visualizeAudioToggle;
    
    [Inject(UxmlName = R.UxmlNames.audioWaveForm)]
    private VisualElement audioWaveForm;

    [Inject(UxmlName = R.UxmlNames.connectionThroubleshootingText)]
    private Label connectionThroubleshootingText;
    
    [Inject(UxmlName = R.UxmlNames.serverErrorResponseText)]
    private Label serverErrorResponseText;

    [Inject(UxmlName = R.UxmlNames.songListView)]
    private ScrollView songListView;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.showMenuButton)]
    private Button showMenuButton;

    [Inject(UxmlName = R.UxmlNames.hiddenCloseMenuButton)]
    private Button hiddenCloseMenuButton;

    [Inject(UxmlName = R.UxmlNames.closeMenuButton)]
    private Button closeMenuButton;

    [Inject(UxmlName = R.UxmlNames.menuOverlay)]
    private VisualElement menuOverlay;

    [Inject(UxmlClass = R.UxmlClasses.onlyVisibleWhenConnected)]
    private List<VisualElement> onlyVisibleWhenConnected;

    [Inject(UxmlClass = R.UxmlClasses.onlyVisibleWhenNotConnected)]
    private List<VisualElement> onlyVisibleWhenNotConnected;

    private AudioWaveFormVisualization audioWaveFormVisualization;

    [Inject(UxmlName = R.UxmlNames.recordingDeviceLabel)]
    private Label recordingDeviceLabel;

    [Inject(UxmlName = R.UxmlNames.recordingDevicePicker)]
    private ItemPicker recordingDevicePicker;

    [Inject(UxmlName = R.UxmlNames.recordingDeviceContainer)]
    private VisualElement recordingDeviceContainer;

    [Inject(UxmlName = R.UxmlNames.languageLabel)]
    private Label languageLabel;

    [Inject(UxmlName = R.UxmlNames.languagePicker)]
    private ItemPicker languagePicker;

    [Inject(UxmlName = R.UxmlNames.devModeLabel)]
    private Label devModeLabel;

    [Inject(UxmlName = R.UxmlNames.devModePicker)]
    private ItemPicker devModePicker;

    [Inject]
    private TranslationManager translationManager;

    [Inject(UxmlName = R.UxmlNames.showMicViewButton)]
    private Button showMicViewButton;

    [Inject(UxmlName = R.UxmlNames.micViewContainer)]
    private VisualElement micViewContainer;

    [Inject(UxmlName = R.UxmlNames.showSongViewButton)]
    private Button showSongViewButton;

    [Inject(UxmlName = R.UxmlNames.songViewContainer)]
    private VisualElement songViewContainer;

    [Inject(UxmlName = R.UxmlNames.songSearchTextField)]
    private TextField songSearchTextField;

    [Inject(UxmlName = R.UxmlNames.songSearchHint)]
    private Label songSearchHint;

    [Inject(UxmlName = R.UxmlNames.recordingDeviceColorIndicator)]
    private VisualElement recordingDeviceColorIndicator;

    private float frameCountTime;
    private int frameCount;

    private void Start()
    {
        // Select recording device if none.
        if (settings.MicProfile.Name.IsNullOrEmpty()
            || !Microphone.devices.Contains(settings.MicProfile.Name))
        {
            settings.SetMicProfileName(Microphone.devices.FirstOrDefault());
        }

        settings.ObserveEveryValueChanged(it => it.MicProfile)
            .Subscribe(_ => OnMicProfileChanged());
        clientSideMicSampleRecorder.IsRecording
            .Subscribe(OnRecordingStateChanged);

        // All controls are hidden until a connection has been established.
        onlyVisibleWhenConnected.ForEach(it => it.HideByDisplay());
        onlyVisibleWhenNotConnected.ForEach(it => it.ShowByDisplay());
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
        
        UpdateVersionInfoText();

        audioWaveForm.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            audioWaveFormVisualization = new AudioWaveFormVisualization(gameObject, audioWaveForm);
        });

        InitTabGroup();
        InitMenu();
        InitSongSearch();
    }

    private void InitSongSearch()
    {
        songSearchTextField.value = "";
        songSearchTextField.RegisterValueChangedCallback(evt =>
        {
            songSearchHint.SetVisibleByDisplay(evt.newValue.IsNullOrEmpty());
            UpdateSongList();
        });
    }

    private void InitTabGroup()
    {
        TabGroupControl tabGroupControl = new TabGroupControl();
        tabGroupControl.AllowNoContainerVisible = false;
        tabGroupControl.AddTabGroupButton(showMicViewButton, micViewContainer);
        tabGroupControl.AddTabGroupButton(showSongViewButton, songViewContainer);
        tabGroupControl.ShowContainer(micViewContainer);

        showSongViewButton.RegisterCallbackButtonTriggered(() =>
        {
            clientSideMicSampleRecorder.StopRecording();
            ShowSongList();
        });
    }

    private void InitMenu()
    {
        // Recording device
        if (Microphone.devices.IsNullOrEmpty()
            || Microphone.devices.Length <= 1)
        {
            recordingDeviceContainer.HideByDisplay();
        }
        else
        {
            LabeledItemPickerControl<string> recordingDevicePickerControl = new(recordingDevicePicker, Microphone.devices.ToList());
            recordingDevicePickerControl.SelectItem(settings.MicProfile.Name);
            recordingDevicePickerControl.Selection.Subscribe(newValue => settings.SetMicProfileName(newValue));
        }

        // Language
        translationManager.currentLanguage = settings.GameSettings.language;
        translationManager.UpdateTranslatorsInScene();
        LabeledItemPickerControl<SystemLanguage> languagePickerControl = new LabeledItemPickerControl<SystemLanguage>(languagePicker, translationManager.GetTranslatedLanguages());
        languagePickerControl.SelectItem(settings.GameSettings.language);
        languagePickerControl.Selection.Subscribe(newValue => settings.GameSettings.language = newValue);
        settings.ObserveEveryValueChanged(it => it.GameSettings.language)
            .Subscribe(newValue =>
            {
                translationManager.currentLanguage = newValue;
                translationManager.UpdateTranslatorsInScene();
            });

        // Dev Mode
        BoolPickerControl devModePickerControl = new BoolPickerControl(devModePicker);
        devModePickerControl.SelectItem(settings.IsDevModeEnabled);
        devModePickerControl.Selection.Subscribe(newValue => settings.IsDevModeEnabled = newValue);
        settings
            .ObserveEveryValueChanged(it => it.IsDevModeEnabled)
            .Subscribe(newValue => OnDevModeEnabledChanged(newValue));

        // Show/hide menu overlay
        HideMenu();
        showMenuButton.RegisterCallbackButtonTriggered(() => ShowMenu());
        hiddenCloseMenuButton.RegisterCallbackButtonTriggered(() => HideMenu());
        closeMenuButton.RegisterCallbackButtonTriggered(() => HideMenu());
    }

    private void OnDevModeEnabledChanged(bool isEnabled)
    {
        fpsText.SetVisibleByDisplay(isEnabled);
        recordingDeviceInfo.SetVisibleByDisplay(isEnabled);
    }

    private void ShowMenu()
    {
        menuOverlay.AddToClassList(MenuOverlayVisibleStyleClass);
    }

    private void HideMenu()
    {
        menuOverlay.RemoveFromClassList(MenuOverlayVisibleStyleClass);
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && connectionStatusText == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.companionApp_title);
        connectionStatusText.text = TranslationManager.GetTranslation(R.Messages.companionApp_connecting);
        recordingDeviceLabel.text = TranslationManager.GetTranslation(R.Messages.options_recording_title);
        languageLabel.text = TranslationManager.GetTranslation(R.Messages.language);
        devModeLabel.text = TranslationManager.GetTranslation(R.Messages.devMode);
        visualizeAudioLabel.text = TranslationManager.GetTranslation(R.Messages.companionApp_visualizeMicInput);
        showMicViewButton.text = TranslationManager.GetTranslation(R.Messages.companionApp_button_showMicrophone);
        showSongViewButton.text = TranslationManager.GetTranslation(R.Messages.companionApp_button_showSongs);
        closeMenuButton.text = TranslationManager.GetTranslation(R.Messages.back);

        // Search text field hint
        string searchPropertiesText = new List<string>
        {
            TranslationManager.GetTranslation(R.Messages.songProperty_artist),
            TranslationManager.GetTranslation(R.Messages.songProperty_title),
        }.ToCsv(", ", "", "");
        songSearchHint.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_searchTextFieldHint, "properties", searchPropertiesText);
    }

    private void UpdateSongList()
    {
        songListView.Clear();
        if (songListRequestor.LoadedSongsDto == null
            || songListRequestor.LoadedSongsDto.SongList.IsNullOrEmpty())
        {
            AddSongListLabel("No songs found");
            return;
        }

        List<SongDto> songDtos = new List<SongDto>(songListRequestor.LoadedSongsDto.SongList)
            .Where(songDto => SongSearchMatches(songDto))
            .ToList();
        songDtos.Sort((a,b) => string.Compare(a.Artist, b.Artist, StringComparison.InvariantCulture));

        foreach (SongDto songDto in songDtos)
        {
            AddSongListLabel(songDto.Artist + " - " + songDto.Title);
        }

        if (!songListRequestor.LoadedSongsDto.IsSongScanFinished)
        {
            AddSongListLabel("...");
        }
    }

    private bool SongSearchMatches(SongDto songDto)
    {
        string searchText = songSearchTextField.value.ToLowerInvariant();
        return searchText.IsNullOrEmpty()
               || songDto.Title.ToLowerInvariant().Contains(searchText)
               || songDto.Artist.ToLowerInvariant().Contains(searchText);
    }

    private void HandleSongListEvent(SongListEvent evt)
    {
        if (!evt.ErrorMessage.IsNullOrEmpty())
        {
            songListView.Clear();
            AddSongListLabel(evt.ErrorMessage);
            return;
        }

        UpdateSongList();
    }

    private void ShowSongList()
    {
        if (!songListRequestor.SuccessfullyLoadedAllSongs)
        {
            songListView.Clear();
            AddSongListLabel("Loading song list...");
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
            fpsText.text = $"FPS: {fps}";
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

    private void OnMicProfileChanged()
    {
        int sampleRate = ClientSideMicSampleRecorder.GetFinalSampleRate(settings.MicProfile.Name, settings.MicProfile.SampleRate);
        recordingDeviceInfo.text = $"Sample Rate:{sampleRate}Hz, " +
                                   $"Delay: {settings.MicProfile.DelayInMillis}ms, " +
                                   $"Amp: {settings.MicProfile.Amplification}, " +
                                   $"Supp: {settings.MicProfile.NoiseSuppression}";
        recordingDeviceColorIndicator.style.backgroundColor = new StyleColor(settings.MicProfile.Color);
    }

    private void OnRecordingStateChanged(bool isRecording)
    {
        if (isRecording)
        {
            toggleRecordingButton.AddToClassList("stopRecordingButton");

            // Prevent stand-by when recording
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
        else
        {
            toggleRecordingButton.RemoveFromClassList("stopRecordingButton");

            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }
    }

    private void UpdateConnectionStatus(ConnectEvent connectEvent)
    {
        if (connectEvent.IsSuccess)
        {
            connectionStatusText.text = TranslationManager.GetTranslation(R.Messages.companionApp_connectedTo, "remote" , connectEvent.ServerIpEndPoint.Address);
            onlyVisibleWhenConnected.ForEach(it => it.ShowByDisplay());
            onlyVisibleWhenNotConnected.ForEach(it => it.HideByDisplay());
            audioWaveForm.SetVisibleByDisplay(settings.ShowAudioWaveForm);
            connectionThroubleshootingText.HideByDisplay();
            serverErrorResponseText.HideByDisplay();
            toggleRecordingButton.Focus();
        }
        else
        {
            connectionStatusText.text = connectEvent.ConnectRequestCount > 0
                ? TranslationManager.GetTranslation(R.Messages.companionApp_connectingWithFailedAttempts, "count", connectEvent.ConnectRequestCount)
                : TranslationManager.GetTranslation(R.Messages.companionApp_connecting);
            
            onlyVisibleWhenConnected.ForEach(it => it.HideByDisplay());
            onlyVisibleWhenNotConnected.ForEach(it => it.ShowByDisplay());
            if (connectEvent.ConnectRequestCount > ConnectRequestCountShowTroubleshootingHintThreshold)
            {
                connectionThroubleshootingText.ShowByDisplay();
                connectionThroubleshootingText.text = TranslationManager.GetTranslation(R.Messages.companionApp_troubleShootingHints);
            }

            if (!connectEvent.ErrorMessage.IsNullOrEmpty())
            {
                serverErrorResponseText.ShowByDisplay();
                serverErrorResponseText.text = connectEvent.ErrorMessage;
            }
        }
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
        label.style.marginBottom = 20;
        songListView.Add(label);
    }
}
