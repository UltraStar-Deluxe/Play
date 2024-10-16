using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonOnlineMultiplayer;
using PortAudioForUnity;
using Serilog.Events;
using SimpleHttpServerForUnity;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DevelopmentOptionsControl : AbstractOptionsSceneControl, INeedInjection, IBinder
{
    [InjectedInInspector]
    public VisualTreeAsset uploadWorkshopItemDialogUi;

    [Inject(UxmlName = R.UxmlNames.showFpsToggle)]
    private Toggle showFpsToggle;

    [Inject(UxmlName = R.UxmlNames.systemAudioBackendDelayChooser)]
    private Chooser systemAudioBackendDelayChooser;

    [Inject(UxmlName = R.UxmlNames.portAudioOutputDeviceChooser)]
    private Chooser portAudioOutputDeviceChooser;

    [Inject(UxmlName = R.UxmlNames.portAudioHostApiChooser)]
    private Chooser portAudioHostApiChooser;

    [Inject(UxmlName = R.UxmlNames.portAudioDeviceInfoButton)]
    private Button portAudioDeviceInfoButton;

    [Inject(UxmlName = R.UxmlNames.maxConcurrentSongMediaConversionsChooser)]
    private Chooser maxConcurrentSongMediaConversionsChooser;

    [Inject(UxmlName = R.UxmlNames.pitchDetectionAlgorithmChooser)]
    private Chooser pitchDetectionAlgorithmChooser;

    [Inject(UxmlName = R.UxmlNames.saveVocalsAndInstrumentalAudioInFolderOfSongToggle)]
    private Toggle saveVocalsAndInstrumentalAudioInFolderOfSongToggle;

    [Inject(UxmlName = R.UxmlNames.analyzeBeatsWithoutTargetNoteToggle)]
    private Toggle analyzeBeatsWithoutTargetNoteToggle;

    [Inject(UxmlName = R.UxmlNames.animatedBackgroundItemToggle)]
    private Toggle animatedBackgroundItemToggle;

    [Inject(UxmlName = R.UxmlNames.enableDynamicThemesToggle)]
    private Toggle enableDynamicThemesToggle;

    [Inject(UxmlName = R.UxmlNames.customEventSystemOptInOnAndroidToggle)]
    private Toggle customEventSystemOptInOnAndroidToggle;

    [Inject(UxmlName = R.UxmlNames.useUniversalCharsetDetectorToggle)]
    private Toggle useUniversalCharsetDetectorToggle;

    [Inject(UxmlName = R.UxmlNames.enableWebViewToggle)]
    private Toggle enableWebViewToggle;

    [Inject(UxmlName = R.UxmlNames.wipeLyricsEffectToggle)]
    private Toggle wipeLyricsEffectToggle;

    [Inject(UxmlName = R.UxmlNames.connectionEndpointLabel)]
    private Label connectionEndpointLabel;

    [Inject(UxmlName = R.UxmlNames.httpEndpointExampleLabel)]
    private Label httpEndpointExampleLabel;

    [Inject(UxmlName = R.UxmlNames.showConsoleButton)]
    private Button showConsoleButton;

    [Inject(UxmlName = R.UxmlNames.openLogFolderButton)]
    private Button openLogFolderButton;

    [Inject(UxmlName = R.UxmlNames.copyLogButton)]
    private Button copyLogButton;

    [Inject(UxmlName = R.UxmlNames.reportIssueButton)]
    private Button reportIssueButton;

    [Inject(UxmlName = R.UxmlNames.openPersistentDataPathButton)]
    private Button openPersistentDataPathButton;

    [Inject(UxmlName = R.UxmlNames.openWebViewScriptsPathButton)]
    private Button openWebViewScriptsPathButton;

    [Inject(UxmlName = R.UxmlNames.messageBufferTimeTextField)]
    private IntegerField messageBufferTimeTextField;

    [Inject(UxmlName = R.UxmlNames.songScanMaxBatchCountChooser)]
    private IntegerField songScanMaxBatchCountChooser;

    [Inject(UxmlName = R.UxmlNames.simulateJitterInMillisField)]
    private IntegerField simulateJitterInMillisField;

    [Inject(UxmlName = R.UxmlNames.songSelectSongPreviewDelay)]
    private IntegerField songSelectSongPreviewDelay;

    [Inject]
    private ThemeManager themeManager;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private Injector injector;

    [Inject]
    private UltraStarPlayHttpServer httpServer;

    [Inject]
    private ServerSideCompanionClientManager serverSideCompanionClientManager;

    [Inject]
    private InGameDebugConsoleManager inGameDebugConsoleManager;

    [Inject(UxmlName = R.UxmlNames.audioSeparationCommandTextField)]
    private TextField audioSeparationCommandTextField;

    [Inject(UxmlName = R.UxmlNames.basicPitchCommandTextField)]
    private TextField basicPitchCommandTextField;

    [Inject(UxmlName = R.UxmlNames.clientDiscoveryPortTextField)]
    private IntegerField clientDiscoveryPortTextField;

    [Inject(UxmlName = R.UxmlNames.httpServerHostTextField)]
    private TextField httpServerHostTextField;

    [Inject(UxmlName = R.UxmlNames.httpServerPortTextField)]
    private IntegerField httpServerPortTextField;

    [Inject(UxmlName = R.UxmlNames.minimumLogLevelChooser)]
    private Chooser minimumLogLevelChooser;

    [Inject(UxmlName = R.UxmlNames.generatedFolderPathTextField)]
    private TextField generatedFolderPathTextField;

    [Inject(UxmlName = R.UxmlNames.ffmpegConversionCommandsJsonChooser)]
    private TextField ffmpegConversionCommandsJsonChooser;

    [Inject(UxmlName = R.UxmlNames.songVideoPlaybackChooser)]
    private Chooser songVideoPlaybackChooser;

    [Inject(UxmlName = R.UxmlNames.useFfmpegToPlayMediaFilesChooser)]
    private Chooser useFfmpegToPlayMediaFilesChooser;

    [Inject(UxmlName = R.UxmlNames.logFfmpegOutputToggle)]
    private Toggle logFfmpegOutputToggle;

    [Inject(UxmlName = R.UxmlNames.useVlcToPlayMediaFilesChooser)]
    private Chooser useVlcToPlayMediaFilesChooser;

    [Inject(UxmlName = R.UxmlNames.logVlcOutputToggle)]
    private Toggle logVlcOutputToggle;

    [Inject(UxmlName = R.UxmlNames.checkCodecIsSupportedToggle)]
    private Toggle checkCodecIsSupportedToggle;

    [Inject(UxmlName = R.UxmlNames.vfxEnabledToggle)]
    private Toggle vfxEnabledToggle;

    [Inject(UxmlName = R.UxmlNames.webViewCustomUserAgentTextField)]
    private TextField webViewCustomUserAgentTextField;

    [Inject(UxmlName = R.UxmlNames.writeUltraStarTxtFileWithByteOrderMarkToggle)]
    private Toggle writeUltraStarTxtFileWithByteOrderMarkToggle;

    [Inject(UxmlName = R.UxmlNames.beatAnalyzedEventNetworkDeliveryChooser)]
    private Chooser beatAnalyzedEventNetworkDeliveryChooser;

    [Inject(UxmlClass = "accordionItem")]
    private List<AccordionItem> accordionItems;

    [Inject(UxmlName = R.UxmlNames.uploadWorkshopItemButton)]
    private Button uploadWorkshopItemButton;

    [Inject(UxmlName = R.UxmlNames.defaultUltraStarFormatVersionForSave)]
    private Chooser defaultUltraStarFormatVersionForSave;

    [Inject(UxmlName = R.UxmlNames.upgradeUltraStarFormatVersionForSave)]
    private Chooser upgradeUltraStarFormatVersionForSave;

    private MessageDialogControl uploadWorkshopItemDialogControl;

    protected override void Start()
    {
        base.Start();

        // Fold accordion items
        accordionItems.ForEach(it => it.HideAccordionContent());

        // Bind fields
        FieldBindingUtils.Bind(showFpsToggle,
            () => settings.ShowFps,
            newValue => settings.ShowFps = newValue);

        FieldBindingUtils.Bind(saveVocalsAndInstrumentalAudioInFolderOfSongToggle,
            () => settings.SaveVocalsAndInstrumentalAudioInFolderOfSong,
            newValue => settings.SaveVocalsAndInstrumentalAudioInFolderOfSong = newValue);

        FieldBindingUtils.Bind(generatedFolderPathTextField,
            () => settings.GeneratedFolderPath,
            newValue =>
            {
                if (newValue == settings.GeneratedFolderPath)
                {
                    return;
                }

                if (newValue.IsNullOrEmpty())
                {
                    settings.GeneratedFolderPath = "";
                }
                else if (DirectoryUtils.Exists(newValue))
                {
                    settings.GeneratedFolderPath = newValue;
                }
                else
                {
                    generatedFolderPathTextField.value = settings.GeneratedFolderPath;
                }
            });
        new TextFieldHintControl(generatedFolderPathTextField);

        List<ELogEventLevel> logEventLevels = EnumUtils.GetValuesAsList<ELogEventLevel>()
            .OrderBy(logEventLevel => (int)logEventLevel)
            .ToList();
        new EnumChooserControl<ELogEventLevel>(minimumLogLevelChooser, logEventLevels)
            .Bind(() => settings.MinimumLogLevel,
                  newValue =>
                  {
                      settings.MinimumLogLevel = newValue;
                      UpdateLogEventLevel();
                  });

        new PitchDetectionAlgorithmChooserControl(pitchDetectionAlgorithmChooser)
            .Bind(() => settings.PitchDetectionAlgorithm,
                newValue => settings.PitchDetectionAlgorithm = newValue);

        FieldBindingUtils.Bind(analyzeBeatsWithoutTargetNoteToggle,
            () => settings.AnalyzeBeatsWithoutTargetNote,
            newValue => settings.AnalyzeBeatsWithoutTargetNote = newValue);

        FieldBindingUtils.Bind(animatedBackgroundItemToggle,
            () => settings.AnimatedBackground,
            newValue => settings.AnimatedBackground = newValue);

        FieldBindingUtils.Bind(enableDynamicThemesToggle,
            () => settings.EnableDynamicThemes,
            enableDynamicThemes =>
                {
                    if (!enableDynamicThemes)
                    {
                        themeManager.SetCurrentTheme(themeManager.GetDefaultTheme());
                    }
                    settings.EnableDynamicThemes = enableDynamicThemes;
                });

        FieldBindingUtils.Bind(songScanMaxBatchCountChooser,
            () => settings.SongScanMaxBatchCount,
            newValue => settings.SongScanMaxBatchCount = newValue);

        FieldBindingUtils.Bind(useUniversalCharsetDetectorToggle,
            () => settings.UseUniversalCharsetDetector,
            newValue => settings.UseUniversalCharsetDetector = newValue);

        customEventSystemOptInOnAndroidToggle.SetVisibleByDisplay(PlatformUtils.IsAndroid);
        FieldBindingUtils.Bind(customEventSystemOptInOnAndroidToggle,
            () => settings.EnableEventSystemOnAndroid,
            newValue =>
            {
                if (newValue != settings.EnableEventSystemOnAndroid)
                {
                    settings.EnableEventSystemOnAndroid = newValue;
                    RestartScene();
                }
            });

        connectionEndpointLabel.text = $"Connection endpoint: {serverSideCompanionClientManager.GetConnectionEndpoint()}";

        if (HttpServer.IsSupported)
        {
            httpEndpointExampleLabel.text = "HTTP endpoint example: " + httpServer.GetExampleEndpoint();
        }
        else
        {
            httpEndpointExampleLabel.text = Translation.Get(R.Messages.options_httpServerNotSupported);
        }

        // View and copy log
        showConsoleButton.RegisterCallbackButtonTriggered(_ => inGameDebugConsoleManager.ShowConsole());
        openLogFolderButton.RegisterCallbackButtonTriggered(_ => ApplicationUtils.OpenDirectory(Log.logFileFolder));
        reportIssueButton.RegisterCallbackButtonTriggered(_ => ApplicationUtils.OpenUrl(Translation.Get(R.Messages.uri_howToReportIssues)));
        copyLogButton.RegisterCallbackButtonTriggered(_ =>
        {
            ClipboardUtils.CopyToClipboard(Log.GetLogHistoryAsText(LogEventLevel.Verbose));
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_copiedToClipboard));
        });

        // Open persistent data path
        if (PlatformUtils.IsStandalone)
        {
            openPersistentDataPathButton.RegisterCallbackButtonTriggered(_ => ApplicationUtils.OpenDirectory(Application.persistentDataPath));
        }
        else
        {
            openPersistentDataPathButton.HideByDisplay();
        }

        // Message delay
        FieldBindingUtils.Bind(messageBufferTimeTextField,
            () => settings.CompanionClientMessageBufferTimeInMillis,
            newValue => settings.CompanionClientMessageBufferTimeInMillis = newValue);
        messageBufferTimeTextField.DisableChangeValueByDragging();

        // Spleeter command (audio separation)
        audioSeparationCommandTextField.DisableParseEscapeSequences();
        FieldBindingUtils.Bind(audioSeparationCommandTextField,
            () => settings.SongEditorSettings.AudioSeparationCommand,
            newValue => settings.SongEditorSettings.AudioSeparationCommand = newValue);

        // Basic Pitch command (pitch detection)
        basicPitchCommandTextField.DisableParseEscapeSequences();
        FieldBindingUtils.Bind(basicPitchCommandTextField,
            () => settings.SongEditorSettings.BasicPitchCommand,
            newValue => settings.SongEditorSettings.BasicPitchCommand = newValue);

        // Network config
        FieldBindingUtils.Bind(clientDiscoveryPortTextField,
            () => settings.ConnectionServerPort,
            newValue => settings.ConnectionServerPort = newValue);
        clientDiscoveryPortTextField.DisableChangeValueByDragging();

        FieldBindingUtils.Bind(httpServerHostTextField,
            () => settings.HttpServerHost,
            newValue => settings.HttpServerHost = newValue);

        FieldBindingUtils.Bind(httpServerPortTextField,
            () => settings.HttpServerPort,
            newValue => settings.HttpServerPort = newValue);
        httpServerPortTextField.DisableChangeValueByDragging();

        // File format
        FieldBindingUtils.Bind(writeUltraStarTxtFileWithByteOrderMarkToggle,
            () => settings.WriteUltraStarTxtFileWithByteOrderMark,
            newValue => settings.WriteUltraStarTxtFileWithByteOrderMark = newValue);

        // UltraStar format versions
        new EnumChooserControl<EKnownUltraStarSongFormatVersion>(defaultUltraStarFormatVersionForSave)
            .Bind(() => settings.DefaultUltraStarSongFormatVersionForSave,
                newValue => settings.DefaultUltraStarSongFormatVersionForSave = newValue);

        new EnumChooserControl<EUpgradeUltraStarSongFormatVersion>(upgradeUltraStarFormatVersionForSave)
            .Bind(() => settings.UpgradeUltraStarSongFormatVersionForSave,
                newValue => settings.UpgradeUltraStarSongFormatVersionForSave = newValue);

        // Ffmpeg playback / conversion
        FieldBindingUtils.Bind(ffmpegConversionCommandsJsonChooser,
            () => JsonConverter.ToJson(settings.FileFormatToFfmpegConversionArguments, true),
            newValueAsString =>
            {
                try
                {
                    Dictionary<string, string> newValueAsDict = JsonConverter.FromJson<Dictionary<string, string>>(newValueAsString);
                    settings.FileFormatToFfmpegConversionArguments = newValueAsDict;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogError(
                        $"Failed to update ffmpeg conversion commands with the following JSON: '{newValueAsString}', error message: {ex.Message}");
                }
            });

        // SongVideoPlayback
        new EnumChooserControl<ESongVideoPlayback>(songVideoPlaybackChooser)
            .Bind(() => settings.SongVideoPlayback,
                newValue => settings.SongVideoPlayback = newValue);

        // VLC
        new EnumChooserControl<EThirdPartyLibraryUsage>(useVlcToPlayMediaFilesChooser)
            .Bind(() => settings.VlcToPlayMediaFilesUsage,
                newValue => settings.VlcToPlayMediaFilesUsage = newValue);

        FieldBindingUtils.Bind(logVlcOutputToggle,
            () => settings.LogVlcOutput,
            newValue => settings.LogVlcOutput = newValue);


        // ffmpeg
        new EnumChooserControl<EThirdPartyLibraryUsage>(useFfmpegToPlayMediaFilesChooser)
            .Bind(() => settings.FfmpegToPlayMediaFilesUsage,
                newValue => settings.FfmpegToPlayMediaFilesUsage = newValue);

        FieldBindingUtils.Bind(logFfmpegOutputToggle,
            () => settings.LogFfmpegOutput,
            newValue => settings.LogFfmpegOutput = newValue);

        // Media file conversion
        FieldBindingUtils.Bind(checkCodecIsSupportedToggle,
            () => settings.CheckCodecIsSupported,
            newValue => settings.CheckCodecIsSupported = newValue);

        new NumberChooserControl(maxConcurrentSongMediaConversionsChooser, settings.MaxConcurrentSongMediaConversions).Bind(
            () => settings.MaxConcurrentSongMediaConversions,
            newValue => settings.MaxConcurrentSongMediaConversions = (int)Math.Max(newValue, 0));

        // PortAudio device info
        portAudioDeviceInfoButton.RegisterCallbackButtonTriggered(_ => ShowPortAudioDeviceInfo());

        // PortAudio host API
        new EnumChooserControl<PortAudioHostApi>(portAudioHostApiChooser, GetAvailablePortAudioHostApis())
            .Bind(() => settings.PortAudioHostApi,
                newValue => settings.PortAudioHostApi = newValue);

        // PortAudio output device
        LabeledChooserControl<string> portAudioOutputDeviceChooserControl = new(portAudioOutputDeviceChooser,
            GetAvailablePortAudioOutputDeviceNames(),
            item => item.IsNullOrEmpty() ? Translation.Get(R.Messages.common_default) : Translation.Of(item));
        portAudioOutputDeviceChooserControl.Bind(
            () => settings.PortAudioOutputDeviceName,
            newValue => settings.PortAudioOutputDeviceName = newValue);

        settings.ObserveEveryValueChanged(it => it.PortAudioHostApi)
            .Subscribe(newValue => portAudioOutputDeviceChooserControl.Items = GetAvailablePortAudioOutputDeviceNames())
            .AddTo(gameObject);

        // System audio backend delay
        UnitNumberChooserControl systemAudioBackendDelayChooserControl = new(systemAudioBackendDelayChooser, "ms");
        systemAudioBackendDelayChooserControl.Bind(
            () => settings.SystemAudioBackendDelayInMillis,
            newValue => settings.SystemAudioBackendDelayInMillis = (int)newValue);

        // Wipe lyrics
        FieldBindingUtils.Bind(wipeLyricsEffectToggle,
            () => settings.WipeLyrics,
            newValue => settings.WipeLyrics = newValue);

        // Song preview delay
        FieldBindingUtils.Bind(songSelectSongPreviewDelay,
            () => settings.SongPreviewDelayInMillis,
            newValue => settings.SongPreviewDelayInMillis = newValue);

        // Vfx enabled
        FieldBindingUtils.Bind(vfxEnabledToggle,
            () => settings.EnableVfx,
            newValue => settings.EnableVfx = newValue);

        // Online multiplayer
        new EnumChooserControl<ENetworkDelivery>(beatAnalyzedEventNetworkDeliveryChooser)
            .Bind(() => settings.BeatAnalyzedEventNetworkDelivery,
                newValue => settings.BeatAnalyzedEventNetworkDelivery = newValue);

        FieldBindingUtils.Bind(simulateJitterInMillisField,
            () => settings.OnlineMultiplayerSimulatedJitterInMillis,
            newValue => settings.OnlineMultiplayerSimulatedJitterInMillis = newValue);

        // Mods
        uploadWorkshopItemButton.RegisterCallbackButtonTriggered(evt => ShowUploadNewModDialog());
    }

    private void ShowUploadNewModDialog()
    {
        if (uploadWorkshopItemDialogControl != null)
        {
            return;
        }

        VisualElement visualElement = uploadWorkshopItemDialogUi.CloneTreeAndGetFirstChild();
        UploadWorkshopItemUiControl uploadWorkshopItemUiControl = injector
            .WithRootVisualElement(visualElement)
            .CreateAndInject<UploadWorkshopItemUiControl>();

        uploadWorkshopItemDialogControl = uiManager.CreateDialogControl(Translation.Get(R.Messages.steamWorkshop_uploadDialog_title));
        uploadWorkshopItemDialogControl.AddVisualElement(visualElement);
        uploadWorkshopItemDialogControl.DialogClosedEventStream
            .Subscribe(evt =>
            {
                uploadWorkshopItemUiControl.Dispose();
                uploadWorkshopItemDialogControl = null;
            });
        uploadWorkshopItemDialogControl.AddButton(Translation.Get(R.Messages.action_learnMore),
            _ => ApplicationUtils.OpenUrl(Translation.Get(R.Messages.uri_howToSteamWorkshop)));
        uploadWorkshopItemDialogControl.AddButton(Translation.Get(R.Messages.steamWorkshop_action_publish),
            _ => uploadWorkshopItemUiControl.PublishWorkshopItem());
        uploadWorkshopItemDialogControl.AddButton(Translation.Get(R.Messages.action_cancel),
            _ => uploadWorkshopItemDialogControl.CloseDialog());
    }

    private List<string> GetAvailablePortAudioOutputDeviceNames()
    {
        return new List<string>()
            {
                "",
            }
            .Union(PortAudioUtils.DeviceInfos
                .Where(deviceInfo => deviceInfo.MaxOutputChannels > 0
                                     && deviceInfo.HostApi == MicrophoneAdapter.GetHostApi())
                .Select(deviceInfo => deviceInfo.Name))
            .ToList();
    }

    private List<PortAudioHostApi> GetAvailablePortAudioHostApis()
    {
        return new List<PortAudioHostApi>()
            {
                PortAudioHostApi.Default
            }
            .Union(PortAudioUtils.HostApis
                .Select(portAudioHostApi => PortAudioConversionUtils.ConvertHostApi(portAudioHostApi))
                .ToList())
            .ToList();
    }

    private void ShowPortAudioDeviceInfo()
    {
        MessageDialogControl messageDialogControl = uiManager.CreateDialogControl(Translation.Get(R.Messages.options_development_portAudioDialog_title));
        messageDialogControl.AddButton(Translation.Get(R.Messages.options_development_action_copyCsv), _ => CopyPortAudioDeviceListCsv());
        messageDialogControl.AddButton(Translation.Get(R.Messages.action_close), _ => messageDialogControl.CloseDialog());

        Label defaultHostApiLabel = new Label();
        defaultHostApiLabel.text = $"Default host API: {PortAudioConversionUtils.GetDefaultHostApi()}";
        messageDialogControl.AddVisualElement(defaultHostApiLabel);

        foreach (HostApiInfo hostApiInfo in PortAudioUtils.HostApiInfos)
        {
            // Add group for this host API
            AccordionItem accordionItem = new(StringUtils.EscapeLineBreaks(hostApiInfo.Name));
            messageDialogControl.AddVisualElement(accordionItem);

            // Add label for each device of this host API
            foreach (DeviceInfo deviceInfo in PortAudioUtils.DeviceInfos)
            {
                if (deviceInfo.HostApi != hostApiInfo.HostApi)
                {
                    continue;
                }

                Label deviceInfoLabel = new();
                deviceInfoLabel.name = $"deviceInfoLabel";
                deviceInfoLabel.AddToClassList("deviceInfoLabel");
                string inputOutputIcons = GetInputOutputIcons(deviceInfo);
                deviceInfoLabel.text = $"{inputOutputIcons} '{deviceInfo.Name}'," +
                                       $" max input channels: {deviceInfo.MaxInputChannels}," +
                                       $" max output channels: {deviceInfo.MaxOutputChannels}," +
                                       $" default sample rate: {deviceInfo.DefaultSampleRate.ToStringInvariantCulture("0")}," +
                                       $" default low input latency: {deviceInfo.DefaultLowInputLatency.ToStringInvariantCulture()}," +
                                       $" default high input latency: {deviceInfo.DefaultHighInputLatency.ToStringInvariantCulture()}," +
                                       $" default low output latency: {deviceInfo.DefaultLowOutputLatency.ToStringInvariantCulture()}," +
                                       $" default high output latency: {deviceInfo.DefaultHighOutputLatency.ToStringInvariantCulture()}," +
                                       $" host API device index: {deviceInfo.HostApiDeviceIndex}," +
                                       $" global device index: {deviceInfo.GlobalDeviceIndex}";
                accordionItem.Add(deviceInfoLabel);
            }

            // Add label for default input / output device
            DeviceInfo defaultInputDevice = PortAudioUtils.DeviceInfos.FirstOrDefault(it => it.GlobalDeviceIndex == hostApiInfo.DefaultInputDeviceGlobalIndex);
            DeviceInfo defaultOutputDevice = PortAudioUtils.DeviceInfos.FirstOrDefault(it => it.GlobalDeviceIndex == hostApiInfo.DefaultOutputDeviceGlobalIndex);
            Label defaultDeviceLabel = new();
            defaultDeviceLabel.text = $"Default input device: '{defaultInputDevice?.Name}', default output device: '{defaultOutputDevice?.Name}'";
            accordionItem.Add(defaultDeviceLabel);
        }
    }

    private string GetInputOutputIcons(DeviceInfo deviceInfo)
    {
        string inputIcon = deviceInfo.MaxInputChannels > 0
            ? "🎤"
            : "";
        string outputIcon = deviceInfo.MaxOutputChannels > 0
            ? "🔈"
            : "";
        return $"{inputIcon}{outputIcon}";
    }

    private void CopyPortAudioDeviceListCsv()
    {
        // TODO: use CSV lib with proper link between column header and values
        StringBuilder sb = new();

        // Add header
        List<string> headers = new()
        {
            "host API",
            "input/output",
            "device name",
            "max input channels",
            "max output channels",
            "default sample rate",
            "default low input latency",
            "default high input latency",
            "default low output latency",
            "default high output latency",
            "host API device index",
            "global device index",
        };
        string headerCsv = headers
            .Select(it => $"\"{it}\"")
            .JoinWith(", ");
        sb.Append(headerCsv);
        sb.Append("\n");

        // Add values
        foreach (DeviceInfo deviceInfo in PortAudioUtils.DeviceInfos)
        {
            string nameWithoutLineBreaks = StringUtils.EscapeLineBreaks(deviceInfo.Name);
            List<string> values = new() {
                deviceInfo.HostApi.ToString(),
                GetInputOutputIcons(deviceInfo),
                nameWithoutLineBreaks,
                deviceInfo.MaxInputChannels.ToString(),
                deviceInfo.MaxOutputChannels.ToString(),
                deviceInfo.DefaultSampleRate.ToStringInvariantCulture("0"),
                deviceInfo.DefaultLowInputLatency.ToStringInvariantCulture(),
                deviceInfo.DefaultHighInputLatency.ToStringInvariantCulture(),
                deviceInfo.DefaultLowOutputLatency.ToStringInvariantCulture(),
                deviceInfo.DefaultHighOutputLatency.ToStringInvariantCulture(),
                deviceInfo.HostApiDeviceIndex.ToString(),
                deviceInfo.GlobalDeviceIndex.ToString(),
            };
            string valuesCsv = values
                .Select(it => $"\"{it}\"")
                .JoinWith(", ");
            sb.Append(valuesCsv);
            sb.Append("\n");
        }

        ClipboardUtils.CopyToClipboard(sb.ToString());

        NotificationManager.CreateNotification(Translation.Get(R.Messages.common_copiedToClipboard));
    }

    private void UpdateLogEventLevel()
    {
        if (Log.MinimumLogLevel == settings.MinimumLogLevel)
        {
            return;
        }

        Log.MinimumLogLevel = settings.MinimumLogLevel;

        Debug.Log("Changed minimum log level to " + settings.MinimumLogLevel + ". The following is for testing log levels...");

        Log.Verbose(() => "Serilog verbose log message");

        Log.Debug(() => "Serilog debug log message");

        Log.Information(() => "Serilog info log message");
        Debug.Log("Unity info log message");

        Log.Warning(() => "Serilog warning log message");
        Debug.LogWarning("Unity warning log message");

        Log.Error(() => "Serilog error log message");
        Debug.LogError("Unity error log message");

        Log.Exception(() => new Exception("Serilog exception log message"));
        Debug.LogException(new Exception("Unity exception message"));
    }

    private void RestartScene()
    {
        sceneNavigator.LoadScene(EScene.OptionsScene, new OptionsSceneData(EScene.DevelopmentOptionsScene));
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(this);
        return bb.GetBindings();
    }
}
