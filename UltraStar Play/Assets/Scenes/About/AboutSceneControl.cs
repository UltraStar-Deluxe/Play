using System.Collections.Generic;
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

    [Inject(UxmlName = R.UxmlNames.nextItemButton)]
    private Button nextItemButton;

    [Inject(UxmlName = R.UxmlNames.previousItemButton)]
    private Button previousItemButton;

    [Inject(UxmlName = R.UxmlNames.itemIndexLabel)]
    private Label itemIndexLabel;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    private int selectedTextIndex;

    private void Start()
    {
        ShowAboutText(selectedTextIndex);

        nextItemButton.RegisterCallbackButtonTriggered(() =>
        {
            selectedTextIndex++;
            if (selectedTextIndex >= textAssets.Count)
            {
                selectedTextIndex = 0;
            }
            ShowAboutText(selectedTextIndex);
        });
        previousItemButton.RegisterCallbackButtonTriggered(() =>
        {
            selectedTextIndex--;
            if (selectedTextIndex < 0)
            {
                selectedTextIndex = textAssets.Count - 1;
            }
            ShowAboutText(selectedTextIndex);
        });
        nextItemButton.Focus();

        backButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.MainScene));
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.MainScene));
    }

    private void ShowAboutText(int index)
    {
        aboutText.value = textAssets[index].text;
        itemIndexLabel.text = $"{index + 1} / {textAssets.Count}";
    }

    public void UpdateTranslation()
    {
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.aboutScene_title);
    }
}
