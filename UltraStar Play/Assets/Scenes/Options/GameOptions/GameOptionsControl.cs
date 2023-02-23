using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class GameOptionsControl : AbstractOptionsSceneControl, INeedInjection, ITranslator
{
    [Inject(UxmlName = R.UxmlNames.scoreModeContainer)]
    private VisualElement scoreModeContainer;

    protected override void Start()
    {
        base.Start();
        
        new ScoreModeItemPickerControl(scoreModeContainer.Q<ItemPicker>())
            .Bind(() => settings.GameSettings.ScoreMode,
                  newValue => settings.GameSettings.ScoreMode = newValue);
    }

    public void UpdateTranslation()
    {
        scoreModeContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_scoreMode);
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.options_game_title);
    }
}
