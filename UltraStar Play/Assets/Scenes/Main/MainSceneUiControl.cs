using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using PrimeInputActions;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MainSceneUiControl : MonoBehaviour, INeedInjection, ITranslator, UniInject.IBinder
{
    [InjectedInInspector]
    public TextAsset versionPropertiesTextAsset;

    [InjectedInInspector]
    public UIDocument uiDoc;

    [InjectedInInspector]
    public VisualTreeAsset quitGameDialogUxml;

    [Inject(key = "#startButton")]
    private Button startButton;

    [Inject(key = "#settingsButton")]
    private Button settingsButton;

    [Inject(key = "#aboutButton")]
    private Button aboutButton;

    [Inject(key = "#quitButton")]
    private Button quitButton;

    [Inject(key = "#partyButton")]
    private Button partyButton;

    [Inject(key = "#jukeboxButton")]
    private Button jukeboxButton;

    [Inject(key = "#sceneSubtitle")]
    private Label sceneSubtitle;
    
    [Inject(key = "#semanticVersionText")]
    private Label semanticVersionText;
    
    [Inject(key = "#commitHashText")]
    private Label commitHashText;
    
    [Inject(key = "#buildTimeStampText")]
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

        InitButtonDescription(startButton, R.String.mainScene_button_sing_description);
        InitButtonDescription(settingsButton, R.String.mainScene_button_settings_description);
        InitButtonDescription(aboutButton, R.String.mainScene_button_about_description);
        InitButtonDescription(quitButton, R.String.mainScene_button_quit_description);
        InitButtonDescription(jukeboxButton, R.String.mainScene_button_description_noImplementation);
        InitButtonDescription(partyButton, R.String.mainScene_button_description_noImplementation);

        UpdateVersionInfoText();

        UpdateTranslation();

        sceneSubtitle.text = I18NManager.GetTranslation(R.String.mainScene_button_sing_description);

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
        button.RegisterCallback<PointerEnterEvent>(_ => sceneSubtitle.text = I18NManager.GetTranslation(i18nCode));
        button.RegisterCallback<FocusEvent>(_ => sceneSubtitle.text = I18NManager.GetTranslation(i18nCode));
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && startButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        startButton.text = I18NManager.GetTranslation(R.String.mainScene_button_sing_label);
        settingsButton.text = I18NManager.GetTranslation(R.String.mainScene_button_settings_label);
        aboutButton.text = I18NManager.GetTranslation(R.String.mainScene_button_about_label);
        quitButton.text = I18NManager.GetTranslation(R.String.mainScene_button_quit_label);
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
            I18NManager.GetTranslation(R.String.mainScene_quitDialog_title),
            I18NManager.GetTranslation(R.String.mainScene_quitDialog_message));
        Button yesButton = closeGameDialog.AddButton(I18NManager.GetTranslation(R.String.yes), () => ApplicationUtils.QuitOrStopPlayMode());
        yesButton.Focus();
        closeGameDialog.AddButton(I18NManager.GetTranslation(R.String.no), () => CloseQuitGameDialog());
    }

    public List<UniInject.IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstance(uiDoc);
        return bb.GetBindings();
    }
}
