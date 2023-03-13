using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class AboutSceneControl : MonoBehaviour, INeedInjection, ITranslator
{
    [InjectedInInspector]
    public List<TextAsset> textAssets;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.aboutText)]
    private TextField aboutText;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;
    
    [Inject(UxmlName = R.UxmlNames.aboutTextsScrollView)]
    private ScrollView aboutTextsScrollView;

    private int selectedTextIndex;

    private readonly List<ToggleButton> toggleButtons = new();
    private ToggleButton lastActiveToggleButton;
    
    private void Start()
    {
        CreateAboutTextButtons();
        ShowAboutText(textAssets.FirstOrDefault());

        backButton.RegisterCallbackButtonTriggered(_ => sceneNavigator.LoadScene(EScene.MainScene));
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.MainScene));
    }

    private void CreateAboutTextButtons()
    {
        aboutTextsScrollView.Clear();
        toggleButtons.Clear();
        textAssets.ForEach(CreateAboutTextButton);

        ToggleButton firstToggleButton = toggleButtons.FirstOrDefault();
        firstToggleButton.SetActive(true);
        lastActiveToggleButton = firstToggleButton;
    }

    private void CreateAboutTextButton(TextAsset textAsset)
    {
        ToggleButton button = new();
        button.AddToClassList("transparentButton");
        button.text = textAsset.name;
        button.RegisterCallbackButtonTriggered(_ =>
        {
            ShowAboutText(textAsset);

            if (lastActiveToggleButton != null)
            {
                lastActiveToggleButton.SetActive(false);
            }
            lastActiveToggleButton = button;
            
            button.SetActive(true);
        });
        aboutTextsScrollView.Add(button);

        toggleButtons.Add(button);
    }

    private void ShowAboutText(TextAsset textAsset)
    {
        aboutText.value = textAsset.text;
    }

    public void UpdateTranslation()
    {
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.aboutScene_title);
    }
}
