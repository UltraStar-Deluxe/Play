using PrimeInputActions;
using ProTrans;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class GameOptionsUiControl : MonoBehaviour, INeedInjection, ITranslator
{
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.ratePlayersContainer)]
    private VisualElement ratePlayersContainer;

    [Inject(UxmlName = R.UxmlNames.combineDuetScoresContainer)]
    private VisualElement combineDuetScoresContainer;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject]
    private Settings settings;

    private void Start()
    {
        new BoolPickerControl(ratePlayersContainer.Q<ItemPicker>())
            .Bind(() => settings.GameSettings.RatePlayers,
                  newValue => settings.GameSettings.RatePlayers = newValue);

        new BoolPickerControl(combineDuetScoresContainer.Q<ItemPicker>())
            .Bind(() => settings.GameSettings.CombineDuetScores,
                newValue => settings.GameSettings.CombineDuetScores = newValue);

        backButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsScene));
        backButton.Focus();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.OptionsScene));
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        ratePlayersContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_ratePlayers);
        combineDuetScoresContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_combineDuetScores);
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.gameOptionsScene_title);
    }
}
