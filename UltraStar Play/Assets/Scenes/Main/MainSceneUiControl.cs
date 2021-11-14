using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using PrimeInputActions;
using ProTrans;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MainSceneUiControl : MonoBehaviour, INeedInjection, ITranslator
{
    [InjectedInInspector]
    public TextAsset versionPropertiesTextAsset;

    [InjectedInInspector]
    public VisualTreeAsset quitGameDialogUxml;

    [Inject]
    private UIDocument uiDoc;

    [Inject(Key = R.UxmlNames.startButtonHashed)]
    private Button startButton;

    [Inject(Key = R.UxmlNames.settingsButtonHashed)]
    private Button settingsButton;

    [Inject(Key = R.UxmlNames.aboutButtonHashed)]
    private Button aboutButton;

    [Inject(Key = R.UxmlNames.quitButtonHashed)]
    private Button quitButton;

    [Inject(Key = R.UxmlNames.partyButtonHashed)]
    private Button partyButton;

    [Inject(Key = R.UxmlNames.jukeboxButtonHashed)]
    private Button jukeboxButton;

    [Inject(Key = R.UxmlNames.sceneSubtitleHashed)]
    private Label sceneSubtitle;
    
    [Inject(Key = R.UxmlNames.semanticVersionTextHashed)]
    private Label semanticVersionText;
    
    [Inject(Key = R.UxmlNames.commitHashTextHashed)]
    private Label commitHashText;
    
    [Inject(Key = R.UxmlNames.buildTimeStampTextHashed)]
    private Label buildTimeStampText;

    private SimpleUxmlDialog closeGameDialog;

    private void Start()
    {
        // Make all Buttons focusable
        uiDoc.rootVisualElement.Query<Button>().ForEach(button => button.focusable = true);

        startButton.RegisterCallbackButtonTriggered(() => SceneNavigator.Instance.LoadScene(EScene.SongSelectScene));
        startButton.Focus();
        settingsButton.RegisterCallbackButtonTriggered(() => SceneNavigator.Instance.LoadScene(EScene.OptionsScene));
        aboutButton.RegisterCallbackButtonTriggered(() => SceneNavigator.Instance.LoadScene(EScene.AboutScene));
        quitButton.RegisterCallbackButtonTriggered(() => OpenQuitGameDialog());

        InitButtonDescription(startButton, R.Messages.mainScene_button_sing_description);
        InitButtonDescription(settingsButton, R.Messages.mainScene_button_settings_description);
        InitButtonDescription(aboutButton, R.Messages.mainScene_button_about_description);
        InitButtonDescription(quitButton, R.Messages.mainScene_button_quit_description);
        InitButtonDescription(jukeboxButton, R.Messages.mainScene_button_description_noImplementation);
        InitButtonDescription(partyButton, R.Messages.mainScene_button_description_noImplementation);

        UpdateVersionInfoText();

        sceneSubtitle.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_sing_description);

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => ToggleCloseGameDialog());
    }

    private void ToggleCloseGameDialog()
    {
        if (closeGameDialog != null)
        {
            CloseQuitGameDialog();
        }
        else
        {
            OpenQuitGameDialog();
        }
    }

    private void InitButtonDescription(Button button, string i18nCode)
    {
        button.RegisterCallback<PointerEnterEvent>(_ => sceneSubtitle.text = TranslationManager.GetTranslation(i18nCode));
        button.RegisterCallback<FocusEvent>(_ => sceneSubtitle.text = TranslationManager.GetTranslation(i18nCode));
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && startButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        startButton.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_sing_label);
        partyButton.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_party_label);
        jukeboxButton.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_jukebox_label);
        settingsButton.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_settings_label);
        aboutButton.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_about_label);
        quitButton.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_quit_label);
    }

    private void UpdateVersionInfoText()
    {
        Dictionary<string, string> versionProperties = PropertiesFileParser.ParseText(versionPropertiesTextAsset.text);

        // Show the release number (e.g. release date, or some version number)
        versionProperties.TryGetValue("release", out string release);
        versionProperties.TryGetValue("name", out string releaseName);
        string displayName = releaseName.IsNullOrEmpty() ? release : releaseName;
        semanticVersionText.text = "Version: " + displayName;

        // Show the commit hash of the build
        versionProperties.TryGetValue("commit_hash", out string commitHash);
        commitHashText.text = "Commit: " + commitHash;
        
        // Show the build timestamp only for development builds
        if (Debug.isDebugBuild)
        {
            versionProperties.TryGetValue("build_timestamp", out string buildTimeStamp);
            buildTimeStampText.text = "Build timestamp: " + buildTimeStamp;
        }
        else
        {
            buildTimeStampText.text = "";
        }
    }

    private void CloseQuitGameDialog()
    {
        if (closeGameDialog == null)
        {
            return;
        }

        closeGameDialog.CloseDialog();
        closeGameDialog = null;
        // Must not immediately focus next button or it will trigger as well
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () => quitButton.Focus()));
    }

    private void OpenQuitGameDialog()
    {
        if (closeGameDialog != null)
        {
            return;
        }

        closeGameDialog = new SimpleUxmlDialog(
            quitGameDialogUxml,
            uiDoc.rootVisualElement,
            TranslationManager.GetTranslation(R.Messages.mainScene_quitDialog_title),
            TranslationManager.GetTranslation(R.Messages.mainScene_quitDialog_message));
        Button yesButton = closeGameDialog.AddButton(TranslationManager.GetTranslation(R.Messages.yes), () => ApplicationUtils.QuitOrStopPlayMode());
        yesButton.Focus();
        closeGameDialog.AddButton(TranslationManager.GetTranslation(R.Messages.no), () => CloseQuitGameDialog());
    }
}
