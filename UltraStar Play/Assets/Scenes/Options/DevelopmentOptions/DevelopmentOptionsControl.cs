using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CommonOnlineMultiplayer;
using SimpleHttpServerForUnity;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DevelopmentOptionsControl : AbstractOptionsSceneControl, INeedInjection, IBinder, IInjectionFinishedListener
{
    [InjectedInInspector]
    public VisualTreeAsset uploadWorkshopItemDialogUi;

    [Inject(UxmlName = R.UxmlNames.showFpsToggle)]
    private Toggle showFpsToggle;

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
    private DialogManager dialogManager;

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

    [Inject(UxmlName = R.UxmlNames.songVideoPlaybackChooser)]
    private Chooser songVideoPlaybackChooser;

    [Inject(UxmlName = R.UxmlNames.useVlcToPlayMediaFilesChooser)]
    private Chooser useVlcToPlayMediaFilesChooser;

    [Inject(UxmlName = R.UxmlNames.logVlcOutputToggle)]
    private Toggle logVlcOutputToggle;

    [Inject(UxmlName = R.UxmlNames.vlcOptionsTextField)]
    private TextField vlcOptionsTextField;

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

    private readonly PortAudioOptionsControl portAudioOptionsControl = new();

    public void OnInjectionFinished()
    {
        injector.Inject(portAudioOptionsControl);
    }

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

        FieldBindingUtils.Bind(enableWebViewToggle,
            () => settings.EnableWebView,
            newValue => settings.EnableWebView = newValue);

        FieldBindingUtils.Bind(webViewCustomUserAgentTextField,
            () => settings.CustomUserAgent,
            newValue => settings.CustomUserAgent = newValue);

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
            ClipboardUtils.CopyToClipboard(Log.GetLogHistoryAsText(ELogEventLevel.Verbose));
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

        // Open WebView scripts path
        if (PlatformUtils.IsStandalone)
        {
            openWebViewScriptsPathButton.RegisterCallbackButtonTriggered(_ => ApplicationUtils.OpenDirectory(WebViewUtils.GetDefaultWebViewScriptsAbsolutePath()));
        }
        else
        {
            openWebViewScriptsPathButton.HideByDisplay();
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

        FieldBindingUtils.Bind(vlcOptionsTextField,
            () => settings.VlcOptions.JoinWith("\n"),
            newValue => settings.VlcOptions = Regex
                .Split(newValue, @"\n")
                .Select(it => it.Trim())
                .Where(it => !it.IsNullOrEmpty())
                .ToList());

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

        uploadWorkshopItemDialogControl = dialogManager.CreateDialogControl(Translation.Get(R.Messages.steamWorkshop_uploadDialog_title));
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
