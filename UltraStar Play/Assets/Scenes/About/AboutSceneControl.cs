using System;
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

    [Inject(UxmlName = R.UxmlNames.aboutTextScrollView)]
    private ScrollView aboutTextScrollView;

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

        TextAsset initialAboutTextAsset = textAssets.FirstOrDefault();
        ShowAboutText(initialAboutTextAsset.name, initialAboutTextAsset.text);

        backButton.RegisterCallbackButtonTriggered(_ => sceneNavigator.LoadScene(EScene.MainScene));
        backButton.Focus();
        
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
        
        ThemeManager.ApplyThemeSpecificStylesToVisualElements(aboutTextsScrollView);
    }

    private void CreateAboutTextButton(TextAsset textAsset)
    {
        ToggleButton button = new();
        button.AddToClassList("mb-2");
        button.text = textAsset.name;
        button.RegisterCallbackButtonTriggered(_ =>
        {
            ShowAboutText(textAsset.name, textAsset.text);

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

    private void ShowAboutText(string title, string text)
    {
        aboutTextScrollView.Clear();
        
        // A Unity label has a maximum length. So the text needs to be split into multiple labels.
        // Otherwise there is a warning message: "Generated text will be truncated because it exceeds 49152 vertices"
        // Split text into parts of 10000 characters.
        int maxCharactersPerLabel = 10000;
        int numberOfLabels = 1 + (text.Length / 10000);
        if (numberOfLabels > 1)
        {
            Debug.Log($"Splitting about text '{title}' into {numberOfLabels} labels.");
        }
        
        string[] textParts = new string[numberOfLabels];
        for (int i = 0; i < numberOfLabels; i++)
        {
            int startIndex = i * maxCharactersPerLabel;
            int length = Math.Min(maxCharactersPerLabel, text.Length - startIndex);
            textParts[i] = text.Substring(startIndex, length);
            
            TextField textField = new TextField();
            textField.isReadOnly = true;
            textField.pickingMode = PickingMode.Ignore;
            textField.AddToClassList("multiline");
            textField.AddToClassList("noBackground");
            textField.AddToClassList("aboutText");
            textField.value = textParts[i];
            aboutTextScrollView.Add(textField);
        }
    }

    public void UpdateTranslation()
    {
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.aboutScene_title);
    }
}
