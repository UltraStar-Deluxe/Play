using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PortAudioForUnity;
using ProTrans;
using Serilog.Events;
using SimpleHttpServerForUnity;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DevelopmentOptionsControl : AbstractOptionsSceneControl, INeedInjection, ITranslator, IBinder
{
    [Inject(UxmlName = R.UxmlNames.showFpsToggle)]
    private Toggle showFpsToggle;

    [Inject(UxmlName = R.UxmlNames.systemAudioBackendDelayPicker)]
    private ItemPicker systemAudioBackendDelayPicker;

    [Inject(UxmlName = R.UxmlNames.portAudioOutputDevicePicker)]
    private ItemPicker portAudioOutputDevicePicker;

    [Inject(UxmlName = R.UxmlNames.portAudioHostApiPicker)]
    private ItemPicker portAudioHostApiPicker;

    [Inject(UxmlName = R.UxmlNames.portAudioDeviceInfoButton)]
    private Button portAudioDeviceInfoButton;

    [Inject(UxmlName = R.UxmlNames.maxConcurrentSongMediaConversionsPicker)]
    private ItemPicker maxConcurrentSongMediaConversionsPicker;

    [Inject(UxmlName = R.UxmlNames.pitchDetectionAlgorithmPicker)]
    private ItemPicker pitchDetectionAlgorithmPicker;

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

    [Inject(UxmlName = R.UxmlNames.showLogButton)]
    private Button showLogButton;

    [Inject(UxmlName = R.UxmlNames.copyLogButton)]
    private Button copyLogButton;

    [Inject(UxmlName = R.UxmlNames.openPersistentDataPathButton)]
    private Button openPersistentDataPathButton;

    [Inject(UxmlName = R.UxmlNames.openWebViewScriptsPathButton)]
    private Button openWebViewScriptsPathButton;

    [Inject(UxmlName = R.UxmlNames.messageBufferTimeTextField)]
    private IntegerField messageBufferTimeTextField;

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
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

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

    [Inject(UxmlName = R.UxmlNames.minimumLogLevelPicker)]
    private ItemPicker minimumLogLevelPicker;

    [Inject(UxmlName = R.UxmlNames.generatedFolderPathTextField)]
    private TextField generatedFolderPathTextField;

    [Inject(UxmlName = R.UxmlNames.ffmpegConversionCommandsJsonPicker)]
    private TextField ffmpegConversionCommandsJsonPicker;

    [Inject(UxmlName = R.UxmlNames.songVideoPlaybackPicker)]
    private ItemPicker songVideoPlaybackPicker;

    [Inject(UxmlName = R.UxmlNames.useFfmpegToPlayMediaFilesPicker)]
    private ItemPicker useFfmpegToPlayMediaFilesPicker;

    [Inject(UxmlName = R.UxmlNames.logFfmpegOutputToggle)]
    private Toggle logFfmpegOutputToggle;

    [Inject(UxmlName = R.UxmlNames.useVlcToPlayMediaFilesPicker)]
    private ItemPicker useVlcToPlayMediaFilesPicker;

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

    [Inject(UxmlName = R.UxmlNames.beatAnalyzedEventNetworkDeliveryPicker)]
    private ItemPicker beatAnalyzedEventNetworkDeliveryPicker;

    [Inject(UxmlClass = "accordionItem")]
    private List<AccordionItem> accordionItems;

    [Inject(UxmlName = R.UxmlNames.muteSteamVoiceChatMicrophonePicker)]
    private ItemPicker muteSteamVoiceChatMicrophonePicker;

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

        List<LogEventLevel> logEventLevels = EnumUtils.GetValuesAsList<LogEventLevel>()
            .OrderBy(logEventLevel => (int)logEventLevel)
            .ToList();
        new LabeledItemPickerControl<LogEventLevel>(minimumLogLevelPicker, logEventLevels)
            .Bind(() => settings.MinimumLogLevel,
                  newValue =>
                  {
                      settings.MinimumLogLevel = newValue;
                      UpdateLogEventLevel();
                  });

        new PitchDetectionAlgorithmPickerControl(pitchDetectionAlgorithmPicker)
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

        FieldBindingUtils.Bind(enableWebViewToggle,
            () => settings.EnableWebView,
            newValue => settings.EnableWebView = newValue);

        FieldBindingUtils.Bind(webViewCustomUserAgentTextField,
            () => settings.CustomUserAgent,
            newValue => settings.CustomUserAgent = newValue);

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

        connectionEndpointLabel.text = $"Connection endpoint: {serverSideConnectRequestManager.GetConnectionEndpoint()}";

        if (HttpServer.IsSupported)
        {
            httpEndpointExampleLabel.text = "HTTP endpoint example: " + httpServer.GetExampleEndpoint();
        }
        else
        {
            httpEndpointExampleLabel.text = TranslationManager.GetTranslation(R.Messages.options_httpServerNotSupported);
        }

        // View and copy log
        showLogButton.RegisterCallbackButtonTriggered(_ => inGameDebugConsoleManager.ShowConsole());
        copyLogButton.RegisterCallbackButtonTriggered(_ =>
        {
            ClipboardUtils.CopyToClipboard(Log.GetLogHistoryAsText(LogEventLevel.Verbose));
            UiManager.CreateNotification("Copied log to clipboard");
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

        // Open WebView scripts path
        if (PlatformUtils.IsStandalone)
        {
            openWebViewScriptsPathButton.RegisterCallbackButtonTriggered(_ => ApplicationUtils.OpenDirectory(ApplicationUtils.GetWebViewScriptsAbsolutePath()));
        }
        else
        {
            openWebViewScriptsPathButton.HideByDisplay();
        }

        // Message delay
        FieldBindingUtils.Bind(messageBufferTimeTextField,
            () => settings.ConnectedClientMessageBufferTimeInMillis,
            newValue => settings.ConnectedClientMessageBufferTimeInMillis = newValue);
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

        // Ffmpeg playback / conversion
        FieldBindingUtils.Bind(ffmpegConversionCommandsJsonPicker,
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
        new EnumItemPickerControl<ESongVideoPlayback>(songVideoPlaybackPicker)
        {
            GetLabelTextFunction = item => item.ToDisplayString()
        }.Bind(() => settings.SongVideoPlayback,
                newValue => settings.SongVideoPlayback = newValue);

        // VLC
        new EnumItemPickerControl<EThirdPartyLibraryUsage>(useVlcToPlayMediaFilesPicker)
            .Bind(() => settings.VlcToPlayMediaFilesUsage,
                newValue => settings.VlcToPlayMediaFilesUsage = newValue);

        FieldBindingUtils.Bind(logVlcOutputToggle,
            () => settings.LogVlcOutput,
            newValue => settings.LogVlcOutput = newValue);


        // ffmpeg
        new EnumItemPickerControl<EThirdPartyLibraryUsage>(useFfmpegToPlayMediaFilesPicker)
            .Bind(() => settings.FfmpegToPlayMediaFilesUsage,
                newValue => settings.FfmpegToPlayMediaFilesUsage = newValue);

        FieldBindingUtils.Bind(logFfmpegOutputToggle,
            () => settings.LogFfmpegOutput,
            newValue => settings.LogFfmpegOutput = newValue);

        // Media file conversion
        FieldBindingUtils.Bind(checkCodecIsSupportedToggle,
            () => settings.CheckCodecIsSupported,
            newValue => settings.CheckCodecIsSupported = newValue);

        new NumberPickerControl(maxConcurrentSongMediaConversionsPicker, settings.MaxConcurrentSongMediaConversions).Bind(
            () => settings.MaxConcurrentSongMediaConversions,
            newValue => settings.MaxConcurrentSongMediaConversions = (int)Math.Max(newValue, 0));

        // PortAudio device info
        portAudioDeviceInfoButton.RegisterCallbackButtonTriggered(_ => ShowPortAudioDeviceInfo());

        // PortAudio host API
        new LabeledItemPickerControl<PortAudioHostApi>(portAudioHostApiPicker, GetAvailablePortAudioHostApis())
            .Bind(() => settings.PortAudioHostApi,
                newValue => settings.PortAudioHostApi = newValue);

        // PortAudio output device
        LabeledItemPickerControl<string> portAudioOutputDevicePickerControl = new LabeledItemPickerControl<string>(portAudioOutputDevicePicker, GetAvailablePortAudioOutputDeviceNames());
        portAudioOutputDevicePickerControl.Bind(
            () => settings.PortAudioOutputDeviceName,
            newValue => settings.PortAudioOutputDeviceName = newValue);
        portAudioOutputDevicePickerControl.GetLabelTextFunction = item => item.IsNullOrEmpty() ? "Default" : item;

        settings.ObserveEveryValueChanged(it => it.PortAudioHostApi)
            .Subscribe(newValue => portAudioOutputDevicePickerControl.Items = GetAvailablePortAudioOutputDeviceNames())
            .AddTo(gameObject);

        // System audio backend delay
        UnitNumberPickerControl systemAudioBackendDelayPickerControl = new(systemAudioBackendDelayPicker, "ms");
        systemAudioBackendDelayPickerControl.Bind(
            () => settings.SystemAudioBackendDelayInMillis,
            newValue => settings.SystemAudioBackendDelayInMillis = (int)newValue);

        // Wipe lyrics
        FieldBindingUtils.Bind(wipeLyricsEffectToggle,
            () => settings.WipeLyrics,
            newValue => settings.WipeLyrics = newValue);

        // Vfx enabled
        FieldBindingUtils.Bind(vfxEnabledToggle,
            () => settings.EnableVfx,
            newValue => settings.EnableVfx = newValue);

        // Online multiplayer
        new EnumItemPickerControl<NetworkDelivery>(beatAnalyzedEventNetworkDeliveryPicker)
            .Bind(() => settings.BeatAnalyzedEventNetworkDelivery,
                newValue => settings.BeatAnalyzedEventNetworkDelivery = newValue);

        new EnumItemPickerControl<EMuteSteamVoiceChatMicrophone>(muteSteamVoiceChatMicrophonePicker)
            .Bind(() => settings.MuteSteamVoiceChatMicrophone,
                newValue => settings.MuteSteamVoiceChatMicrophone = newValue);

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
        MessageDialogControl messageDialogControl = uiManager.CreateDialogControl("PortAudio host APIs and devices");
        messageDialogControl.AddButton("Copy CSV", _ => CopyPortAudioDeviceListCsv());
        messageDialogControl.AddButton("Close", _ => messageDialogControl.CloseDialog());

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

        UiManager.CreateNotification("Copied to clipboard");
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

    public void UpdateTranslation()
    {
        showFpsToggle.label = TranslationManager.GetTranslation(R.Messages.options_showFps);
        pitchDetectionAlgorithmPicker.Label = TranslationManager.GetTranslation(R.Messages.options_pitchDetectionAlgorithm);
        analyzeBeatsWithoutTargetNoteToggle.label = TranslationManager.GetTranslation(R.Messages.options_analyzeBeatsWithoutTargetNote);
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(this);
        return bb.GetBindings();
    }
}
