using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LiteNetLib;
using ProTrans;
using Serilog.Events;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MainSceneControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener, IBinder
{
    private const int ConnectRequestCountShowTroubleshootingHintThreshold = 3;

    [InjectedInInspector]
    public VisualTreeAsset playerSelectPlayerEntryUi;

    [InjectedInInspector]
    public TextAsset versionPropertiesTextAsset;

    [InjectedInInspector]
    public SongListRequestor songListRequestor;

    [InjectedInInspector]
    public ClientSideMicDataSender clientSideMicDataSender;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private ClientSideCompanionClientManager clientSideCompanionClientManager;

    [Inject]
    private Settings settings;

    [Inject]
    private InGameDebugConsoleManager inGameDebugConsoleManager;

    [Inject(UxmlName = R.UxmlNames.toggleRecordingButton)]
    private Button toggleRecordingButton;

    [Inject(UxmlName = R.UxmlNames.connectionStatusText)]
    private Label connectionStatusText;

    [Inject(UxmlName = R.UxmlNames.recordingDeviceInfo)]
    private Label recordingDeviceInfo;

    [Inject(UxmlName = R.UxmlNames.connectionInfoLabel)]
    private Label connectionInfoLabel;

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

    [Inject(UxmlClass = R.UssClasses.onlyVisibleWhenConnected)]
    private List<VisualElement> onlyVisibleWhenConnected;

    [Inject(UxmlClass = R.UssClasses.onlyVisibleWhenNotConnected)]
    private List<VisualElement> onlyVisibleWhenNotConnected;

    [Inject(UxmlClass = R.UssClasses.onlyVisibleWhenDevModeEnabled)]
    private List<VisualElement> onlyVisibleWhenDevModeEnabled;

    private AudioWaveFormVisualization audioWaveFormVisualization;

    [Inject(UxmlName = R.UxmlNames.recordingDeviceChooser)]
    private Chooser recordingDeviceChooser;

    [Inject(UxmlName = R.UxmlNames.languageChooser)]
    private DropdownField languageChooser;

    [Inject(UxmlName = R.UxmlNames.devModeToggle)]
    private Toggle devModeToggle;

    [Inject(UxmlName = R.UxmlNames.targetFpsChooser)]
    private Chooser targetFpsChooser;

    [Inject(UxmlName = R.UxmlNames.minimumLogLevelChooser)]
    private Chooser minimumLogLevelChooser;

    [Inject]
    private TranslationManager translationManager;

    [Inject(UxmlName = R.UxmlNames.showMicViewButton)]
    private Button showMicViewButton;

    [Inject(UxmlName = R.UxmlNames.micViewContainer)]
    private VisualElement micViewContainer;

    [Inject(UxmlName = R.UxmlNames.showSongViewButton)]
    private Button showSongViewButton;

    [Inject(UxmlName = R.UxmlNames.showInputSimulationButton)]
    private Button showInputSimulationButton;

    [Inject(UxmlName = R.UxmlNames.songViewContainer)]
    private VisualElement songViewContainer;

    [Inject(UxmlName = R.UxmlNames.inputSimulationContainer)]
    private VisualElement inputSimulationContainer;

    [Inject(UxmlName = R.UxmlNames.recordingDeviceColorIndicator)]
    private VisualElement recordingDeviceColorIndicator;

    [Inject(UxmlName = R.UxmlNames.mouseSensitivityFloatField)]
    private FloatField mouseSensitivityFloatField;

    [Inject(UxmlName = R.UxmlNames.connectionServerPortTextField)]
    private IntegerField connectionServerPortTextField;

    [Inject(UxmlName = R.UxmlNames.connectionServerAddressTextField)]
    private TextField connectionServerAddressTextField;

    [Inject(UxmlName = R.UxmlNames.micDataDeliveryMethodField)]
    private EnumField micDataDeliveryMethodField;

    [Inject(UxmlName = R.UxmlNames.tabGroup)]
    private VisualElement tabGroup;

    [Inject]
    private Injector injector;

    [Inject(UxmlName = R.UxmlNames.viewLogButton)]
    private Button viewLogButton;

    [Inject]
    private MainGameHttpClient mainGameHttpClient;

    [Inject(UxmlName = R.UxmlNames.copyLogButton)]
    private Button copyLogButton;

    private LabeledChooserControl<string> recordingDeviceChooserControl;

    private float frameCountTime;
    private int frameCount;

    private readonly InputSimulationControl inputSimulationControl = new();
    private readonly SongListControl songListControl = new();
    private readonly BuildInfoUiControl buildInfoUiControl = new();

    public void OnInjectionFinished()
    {
        injector.Inject(songListControl);
        injector.Inject(inputSimulationControl);
        injector
            .WithBindingForInstance(versionPropertiesTextAsset)
            .Inject(buildInfoUiControl);

        mainGameHttpClient.Permissions
            .Subscribe(permissions => OnPermissionsChanged(permissions));

        // Select recording device if none.
        if (settings.MicProfile.Name.IsNullOrEmpty()
            || !Microphone.devices.Contains(settings.MicProfile.Name))
        {
            settings.SetMicProfileName(Microphone.devices.FirstOrDefault());
        }

        menuOverlay.ShowByDisplay();

        clientSideMicDataSender.IsRecording.Subscribe(OnRecordingStateChanged);
        clientSideMicDataSender.FinalSampleRate.Subscribe(_ => UpdateRecordingDeviceInfo());

        // All controls are hidden until a connection has been established.
        onlyVisibleWhenConnected.ForEach(it => it.HideByDisplay());
        onlyVisibleWhenNotConnected.ForEach(it => it.ShowByDisplay());
        connectionThroubleshootingText.HideByDisplay();
        serverErrorResponseText.HideByDisplay();

        toggleRecordingButton.RegisterCallbackButtonTriggered(_ => ToggleRecording());

        clientNameTextField.value = settings.ClientName;
        clientNameTextField.RegisterCallback<NavigationSubmitEvent>(_ => OnClientNameTextFieldSubmit());
        clientNameTextField.RegisterCallback<BlurEvent>(_ => OnClientNameTextFieldSubmit());

        visualizeAudioToggle.value = settings.ShowAudioWaveForm;
        audioWaveForm.SetVisibleByVisibility(settings.ShowAudioWaveForm);
        visualizeAudioToggle.RegisterValueChangedCallback(changeEvent =>
        {
            audioWaveForm.SetVisibleByVisibility(changeEvent.newValue);
            settings.ShowAudioWaveForm = changeEvent.newValue;
        });

        clientSideCompanionClientManager.ConnectEventStream
            .Subscribe(UpdateConnectionStatus);

        audioWaveForm.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            int textureWidth = 512;
            int textureHeight = 256;
            audioWaveFormVisualization = new AudioWaveFormVisualization(
                gameObject,
                audioWaveForm,
                textureWidth,
                textureHeight,
                "main scene audio wave form visualization");
        });

        mouseSensitivityFloatField.value = settings.MousePadSensitivity;
        mouseSensitivityFloatField.RegisterValueChangedCallback(evt => settings.MousePadSensitivity = evt.newValue);

        // Only show some controls when dev mode is enabled.
        UpdateDevModeControlsVisibility();
        settings.ObserveEveryValueChanged(it => it.IsDevModeEnabled)
            .Subscribe(_ => UpdateDevModeControlsVisibility())
            .AddTo(gameObject);

        InitTabGroup();
        InitMenu();

        UpdateTranslation();
    }

    private void Start()
    {
        settings.ObserveEveryValueChanged(it => it.MicProfile)
            .Subscribe(_ => OnMicProfileChanged());
    }

    private void UpdateDevModeControlsVisibility()
    {
        onlyVisibleWhenDevModeEnabled.ForEach(it => it.SetVisibleByDisplay(settings.IsDevModeEnabled));
    }

    private void OnPermissionsChanged(List<HttpApiPermission> permissions)
    {
        showInputSimulationButton.SetVisibleByDisplay(permissions.Contains(HttpApiPermission.WriteInputSimulation));
        inputSimulationContainer.HideByDisplay();
    }

    private void InitTabGroup()
    {
        TabGroupControl tabGroupControl = new TabGroupControl();
        tabGroupControl.AllowNoContainerVisible = false;
        tabGroupControl.AddTabGroupButton(showMicViewButton, micViewContainer);
        tabGroupControl.AddTabGroupButton(showSongViewButton, songViewContainer);
        tabGroupControl.AddTabGroupButton(showInputSimulationButton, inputSimulationContainer);
        tabGroupControl.ShowContainer(micViewContainer);

        showSongViewButton.RegisterCallbackButtonTriggered(_ => songListControl.Show());
    }

    private void InitMenu()
    {
        // Recording device
        List<string> deviceNames = Microphone.devices.ToList();
        deviceNames.Sort();
        recordingDeviceChooserControl = new(recordingDeviceChooser, deviceNames,
            item => Translation.Of(item));
        recordingDeviceChooserControl.AutoSmallFont = false;
        recordingDeviceChooserControl.Selection = settings.MicProfile.Name;
        recordingDeviceChooserControl.SelectionAsObservable.Subscribe(newValue => settings.SetMicProfileName(newValue));

        // Language
        LanguageChooserControl languageChooserControl = new LanguageChooserControl(languageChooser);
        languageChooserControl.SelectionAsObservable.Subscribe(newValue => OnLanguageChanged(newValue));

        // Dev Mode
        FieldBindingUtils.Bind(devModeToggle,
            () => settings.IsDevModeEnabled,
            newValue => settings.IsDevModeEnabled = newValue);
        settings
            .ObserveEveryValueChanged(it => it.IsDevModeEnabled)
            .Subscribe(newValue => OnDevModeEnabledChanged(newValue));

        // Minimum log level
        new EnumChooserControl<ELogEventLevel>(minimumLogLevelChooser).Bind(
            () => settings.MinimumLogLevel,
            newValue =>
            {
                settings.MinimumLogLevel = newValue;
                Log.MinimumLogLevel = newValue;
            });

        // Target FPS
        LabeledChooserControl<int> targetFpsChooserControl = new(targetFpsChooser, new List<int> { -1, 5, 10, 15, 20, 30, 60, 90, 120 },
            item => item > 0 ? Translation.Of(item.ToString()) : Translation.Of("Auto"));
        targetFpsChooserControl.Bind(
            () => settings.TargetFps,
            newValue => settings.TargetFps = newValue);

        // Network config
        FieldBindingUtils.Bind(connectionServerPortTextField,
            () => settings.ConnectionServerPort,
            newValue => settings.ConnectionServerPort = newValue);
        connectionServerPortTextField.DisableChangeValueByDragging();

        FieldBindingUtils.Bind(connectionServerAddressTextField,
            () => settings.ConnectionServerAddress,
            newValue => settings.ConnectionServerAddress = newValue);

        FieldBindingUtils.Bind(micDataDeliveryMethodField,
            () => settings.MicDataDeliveryMethod,
            newValue => settings.MicDataDeliveryMethod = (DeliveryMethod)newValue);

        // Show/hide menu overlay
        HideMenu();
        showMenuButton.RegisterCallbackButtonTriggered(_ => ShowMenu());
        hiddenCloseMenuButton.RegisterCallbackButtonTriggered(_ => HideMenu());
        closeMenuButton.RegisterCallbackButtonTriggered(_ => HideMenu());

        // View and copy log
        viewLogButton.RegisterCallbackButtonTriggered(_ => inGameDebugConsoleManager.ShowConsole());
        copyLogButton.RegisterCallbackButtonTriggered(_ =>
        {
            ClipboardUtils.CopyToClipboard(Log.GetLogHistoryAsText(LogEventLevel.Verbose));
            UiManager.CreateNotification("Copied log to clipboard");
        });
    }

    private void OnLanguageChanged(CultureInfo newValue)
    {
        if (Equals(newValue, TranslationConfig.Singleton.CurrentCultureInfo))
        {
            return;
        }
        SetCurrentLanguage(newValue);
        UpdateTranslation();
    }

    private void SetCurrentLanguage(CultureInfo cultureInfo)
    {
        try
        {
            TranslationConfig.Singleton.CurrentCultureInfo = cultureInfo;
            settings.CultureInfoName = cultureInfo.ToString();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to set current CultureInfo to '{cultureInfo}': {ex.Message}");
        }
    }

    private void OnDevModeEnabledChanged(bool isEnabled)
    {
        recordingDeviceInfo.SetVisibleByDisplay(isEnabled);
        connectionInfoLabel.SetVisibleByDisplay(isEnabled);
    }

    private void ShowMenu()
    {
        menuOverlay.style.left = new StyleLength(new Length(0, LengthUnit.Percent));
    }

    private void HideMenu()
    {
        menuOverlay.style.left = new StyleLength(new Length(100, LengthUnit.Percent));
    }

    public void UpdateTranslation()
    {
        sceneTitle.text = Translation.Get(R.Messages.companionApp_title);
        connectionStatusText.text = Translation.Get(R.Messages.companionApp_connecting);
        recordingDeviceChooser.Label = Translation.Get(R.Messages.options_recording_title);
        languageChooser.label = Translation.Get(R.Messages.language);
        devModeToggle.label = Translation.Get(R.Messages.companionApp_devMode);
        visualizeAudioToggle.label = Translation.Get(R.Messages.companionApp_visualizeMicInput);
        closeMenuButton.text = Translation.Get(R.Messages.common_back);

        recordingDeviceChooserControl.UpdateLabelText();
        songListControl.UpdateTranslation();
    }

    private void Update()
    {
        if (audioWaveForm.IsVisibleByDisplay()
            && audioWaveForm.IsVisibleByVisibility()
            && audioWaveFormVisualization != null)
        {
            audioWaveFormVisualization.DrawAudioWaveForm(clientSideMicDataSender.MicSamples);
        }
    }

    private void LateUpdate()
    {
        songListControl?.LateUpdate();
    }

    private void OnClientNameTextFieldSubmit()
    {
        if (clientNameTextField.value == settings.ClientName)
        {
            return;
        }

        if (clientNameTextField.value.IsNullOrEmpty()
            && !settings.ClientName.IsNullOrEmpty())
        {
            // ClientName must not be empty, so restore last value.
            clientNameTextField.value = settings.ClientName;
        }
        else
        {
            // Apply new ClientName
            settings.ClientName = clientNameTextField.value;
            // Reconnect to let the main know about the new clientName.
            clientSideCompanionClientManager.DisconnectFromServer();
        }
    }

    private void OnMicProfileChanged()
    {
        // Update MicProfile of sample recorder.
        int newFinalSampleRate = MicSampleRecorder.GetFinalSampleRate(settings.MicProfile.Name, settings.MicProfile.SampleRate);
        if (clientSideMicDataSender.MicProfile == null
            || settings.MicProfile.Name != clientSideMicDataSender.MicProfile.Name
            || newFinalSampleRate != clientSideMicDataSender.FinalSampleRate.Value
            || settings.MicProfile.DelayInMillis != clientSideMicDataSender.MicProfile.DelayInMillis
            || settings.MicProfile.Amplification != clientSideMicDataSender.MicProfile.Amplification
            || settings.MicProfile.NoiseSuppression != clientSideMicDataSender.MicProfile.NoiseSuppression)
        {
            clientSideMicDataSender.MicProfile = settings.MicProfile;
        }

        UpdateRecordingDeviceInfo();
    }

    private void UpdateRecordingDeviceInfo()
    {
        recordingDeviceInfo.text = $"Sample Rate:{clientSideMicDataSender.FinalSampleRate}Hz, " +
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
            Debug.Log("Setting Screen.sleepTimeout to SleepTimeout.NeverSleep");
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
        else
        {
            toggleRecordingButton.RemoveFromClassList("stopRecordingButton");

            // Reset stand-by behavior
            Debug.Log("Setting Screen.sleepTimeout to SleepTimeout.SystemSetting");
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }
    }

    private void UpdateConnectionStatus(ConnectEvent connectEvent)
    {
        if (connectEvent.IsSuccess)
        {
            connectionInfoLabel.text = $"Connected to {connectEvent.ServerIpEndPoint.Address}:{connectEvent.ServerIpEndPoint.Port}";
            connectionStatusText.text = Translation.Get(R.Messages.companionApp_connectedTo, "remote" , connectEvent.ServerIpEndPoint.Address);

            onlyVisibleWhenConnected.ForEach(it => it.ShowByDisplay());
            onlyVisibleWhenNotConnected.ForEach(it => it.HideByDisplay());

            SetErrorResponseTextAndVisibility("");
            SetThroubleshootingTextAndVisibility("");

            audioWaveForm.SetVisibleByVisibility(settings.ShowAudioWaveForm);
            toggleRecordingButton.Focus();
        }
        else
        {
            connectionInfoLabel.text = "Not connected";
            connectionStatusText.text = connectEvent.ConnectRequestCount > 0
                ? Translation.Get(R.Messages.companionApp_connectingWithFailedAttempts, "count", connectEvent.ConnectRequestCount)
                : Translation.Get(R.Messages.companionApp_connecting);

            onlyVisibleWhenConnected.ForEach(it => it.HideByDisplay());
            onlyVisibleWhenNotConnected.ForEach(it => it.ShowByDisplay());

            SetErrorResponseTextAndVisibility(connectEvent.ErrorMessage);
            SetThroubleshootingTextAndVisibility(
                connectEvent.ErrorMessage.IsNullOrEmpty()
                && connectEvent.ConnectRequestCount > ConnectRequestCountShowTroubleshootingHintThreshold
                    ? Translation.Get(R.Messages.companionApp_troubleShootingHints)
                    : "");
        }
    }

    private void SetThroubleshootingTextAndVisibility(string text)
    {
        connectionThroubleshootingText.text = text;
        connectionThroubleshootingText.SetVisibleByDisplay(!text.IsNullOrEmpty());
    }

    private void SetErrorResponseTextAndVisibility(string text)
    {
        serverErrorResponseText.text = text;
        serverErrorResponseText.SetVisibleByDisplay(!text.IsNullOrEmpty());
    }

    private void ToggleRecording()
    {
        if (clientSideMicDataSender.IsRecording.Value)
        {
            clientSideMicDataSender.StopRecording();
        }
        else
        {
            clientSideMicDataSender.StartRecording();
        }
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(clientSideMicDataSender);
        bb.BindExistingInstance(inputSimulationControl);
        bb.BindExistingInstance(songListRequestor);
        bb.Bind(nameof(playerSelectPlayerEntryUi)).ToExistingInstance(playerSelectPlayerEntryUi);
        return bb.GetBindings();
    }

    public void OnDestroy()
    {
        songListControl.Dispose();
    }
}
