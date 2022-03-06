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

public class MainSceneControl : MonoBehaviour, INeedInjection, ITranslator
{
    [InjectedInInspector]
    public TextAsset versionPropertiesTextAsset;

    [InjectedInInspector]
    public VisualTreeAsset quitGameDialogUxml;

    [Inject]
    private UIDocument uiDoc;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.sceneSubtitle)]
    private Label sceneSubtitle;

    [Inject(UxmlName = R.UxmlNames.startButton)]
    private Button startButton;

    [Inject(UxmlName = R.UxmlNames.settingsButton)]
    private Button settingsButton;

    [Inject(UxmlName = R.UxmlNames.aboutButton)]
    private Button aboutButton;

    [Inject(UxmlName = R.UxmlNames.quitButton)]
    private Button quitButton;

    [Inject(UxmlName = R.UxmlNames.partyButton)]
    private Button partyButton;

    [Inject(UxmlName = R.UxmlNames.jukeboxButton)]
    private Button jukeboxButton;

    [Inject(UxmlName = R.UxmlNames.semanticVersionText)]
    private Label semanticVersionText;
    
    [Inject(UxmlName = R.UxmlNames.commitHashText)]
    private Label commitHashText;
    
    [Inject(UxmlName = R.UxmlNames.buildTimeStampText)]
    private Label buildTimeStampText;

    private MessageDialogControl closeGameDialogControl;

    private void Start()
    {
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
        if (closeGameDialogControl != null)
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
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.mainScene_title);
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
        if (closeGameDialogControl == null)
        {
            return;
        }

        closeGameDialogControl.CloseDialog();
        closeGameDialogControl = null;
        // Must not immediately focus next button or it will trigger as well
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () => quitButton.Focus()));
    }

    private void OpenQuitGameDialog()
    {
        if (closeGameDialogControl != null)
        {
            return;
        }

        closeGameDialogControl = new MessageDialogControl(
            quitGameDialogUxml,
            uiDoc.rootVisualElement,
            TranslationManager.GetTranslation(R.Messages.mainScene_quitDialog_title),
            TranslationManager.GetTranslation(R.Messages.mainScene_quitDialog_message));
        Button yesButton = closeGameDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.yes), () => ApplicationUtils.QuitOrStopPlayMode());
        yesButton.Focus();
        closeGameDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.no), () => CloseQuitGameDialog());
    }
}
