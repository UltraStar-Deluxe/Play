using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LiteNetLib;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MenuUiControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private Settings settings;

    [Inject]
    private InGameDebugConsoleManager inGameDebugConsoleManager;

    [Inject]
    private ClientSideCompanionClientManager clientSideCompanionClientManager;

    [Inject]
    private GameObject gameObject;

    [Inject]
    private MicSampleRecorderManager micSampleRecorderManager;

    [Inject(UxmlName = R.UxmlNames.connectionInfoLabel)]
    private Label connectionInfoLabel;

    [Inject(UxmlName = R.UxmlNames.showMenuButton)]
    private Button showMenuButton;

    [Inject(UxmlName = R.UxmlNames.hiddenCloseMenuButton)]
    private Button hiddenCloseMenuButton;

    [Inject(UxmlName = R.UxmlNames.closeMenuButton)]
    private Button closeMenuButton;

    [Inject(UxmlName = R.UxmlNames.menuOverlay)]
    private VisualElement menuOverlay;

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

    [Inject(UxmlName = R.UxmlNames.mouseSensitivityFloatField)]
    private FloatField mouseSensitivityFloatField;

    [Inject(UxmlName = R.UxmlNames.connectionServerPortTextField)]
    private IntegerField connectionServerPortTextField;

    [Inject(UxmlName = R.UxmlNames.connectionServerAddressTextField)]
    private TextField connectionServerAddressTextField;

    [Inject(UxmlName = R.UxmlNames.micDataDeliveryMethodField)]
    private EnumField micDataDeliveryMethodField;

    [Inject(UxmlName = R.UxmlNames.viewLogButton)]
    private Button viewLogButton;

    [Inject(UxmlName = R.UxmlNames.copyLogButton)]
    private Button copyLogButton;

    private LabeledChooserControl<string> recordingDeviceChooserControl;

    private List<string> SortedDeviceNames => Microphone.devices.OrderBy(device => device).ToList();

    public void OnInjectionFinished()
    {
        menuOverlay.ShowByDisplay();

        // Select recording device if none.
        if (settings.MicProfile.Name.IsNullOrEmpty()
            || !Microphone.devices.Contains(settings.MicProfile.Name))
        {
            settings.SetMicProfileName(Microphone.devices.FirstOrDefault());
        }

        // Recording device
        recordingDeviceChooserControl = new(recordingDeviceChooser, SortedDeviceNames, item => Translation.Of(item));
        recordingDeviceChooserControl.AutoSmallFont = false;
        recordingDeviceChooserControl.Bind(() => settings.MicProfile?.Name ?? "", newValue => settings.SetMicProfileName(newValue));

        UpdateRecordingDeviceList();
        micSampleRecorderManager.ConnectedMicDevicesChangesStream.Subscribe(_ => UpdateRecordingDeviceList());

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

        // Mouse sensitivity
        FieldBindingUtils.Bind(mouseSensitivityFloatField,
            () => settings.MousePadSensitivity,
            newValue => settings.MousePadSensitivity = newValue);

        // Show/hide menu overlay
        HideMenu();
        showMenuButton.RegisterCallbackButtonTriggered(_ => ShowMenu());
        hiddenCloseMenuButton.RegisterCallbackButtonTriggered(_ => HideMenu());
        closeMenuButton.RegisterCallbackButtonTriggered(_ => HideMenu());

        // View and copy log
        viewLogButton.RegisterCallbackButtonTriggered(_ => inGameDebugConsoleManager.ShowConsole());
        copyLogButton.RegisterCallbackButtonTriggered(_ =>
        {
            ClipboardUtils.CopyToClipboard(Log.GetLogHistoryAsText(ELogEventLevel.Verbose));
            NotificationManager.CreateNotification(Translation.Of("Copied log to clipboard"));
        });

        clientSideCompanionClientManager.ConnectEventStream
            .Subscribe(UpdateConnectionStatus)
            .AddTo(gameObject);

        UpdateTranslation();
    }

    private void UpdateRecordingDeviceList()
    {
        Debug.Log($"Updating recording device list. Available devices: {Microphone.devices.JoinWith(",")}");
        recordingDeviceChooserControl.Items = SortedDeviceNames;
        recordingDeviceChooserControl.Selection = settings.MicProfile.Name;
    }

    private void ShowMenu()
    {
        menuOverlay.style.left = new StyleLength(new Length(0, LengthUnit.Percent));
    }

    private void HideMenu()
    {
        menuOverlay.style.left = new StyleLength(new Length(100, LengthUnit.Percent));
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

    private void OnLanguageChanged(CultureInfo newValue)
    {
        if (Equals(newValue, TranslationConfig.Singleton.CurrentCultureInfo))
        {
            return;
        }
        SetCurrentLanguage(newValue);
        UpdateTranslation();
    }

    private void UpdateConnectionStatus(ConnectEvent connectEvent)
    {
        if (connectEvent.IsSuccess)
        {
            connectionInfoLabel.text = $"Connected to {connectEvent.ServerIpEndPoint.Address}:{connectEvent.ServerIpEndPoint.Port}";
        }
        else
        {
            connectionInfoLabel.text = "Not connected";
        }
    }

    private void OnDevModeEnabledChanged(bool isEnabled)
    {
        connectionInfoLabel.SetVisibleByDisplay(isEnabled);
    }

    private void UpdateTranslation()
    {
        recordingDeviceChooser.Label = Translation.Get(R.Messages.options_recording_title);
        languageChooser.label = Translation.Get(R.Messages.language);
        devModeToggle.label = Translation.Get(R.Messages.companionApp_devMode);
        closeMenuButton.text = Translation.Get(R.Messages.common_back);

        recordingDeviceChooserControl.UpdateLabelText();
    }
}
