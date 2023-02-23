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

    [Inject(UxmlName = R.UxmlNames.languageChooser)]
    private ItemPicker languageChooser;
    
    protected override void Start()
    {
        base.Start();
        
        new ScoreModeItemPickerControl(scoreModeContainer.Q<ItemPicker>())
            .Bind(() => settings.GameSettings.ScoreMode,
                  newValue => settings.GameSettings.ScoreMode = newValue);

        InitLanguageChooser();
    }

    public void UpdateTranslation()
    {
        scoreModeContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_scoreMode);
    }
    
    private void InitLanguageChooser()
    {
        new LabeledItemPickerControl<SystemLanguage>(
                languageChooser,
                translationManager.GetTranslatedLanguages())
            .Bind(() => translationManager.currentLanguage,
                newValue => SetLanguage(newValue));
    }
    
    private void SetLanguage(SystemLanguage newValue)
    {
        if (settings.GameSettings.language == newValue
            && translationManager.currentLanguage == newValue)
        {
            return;
        }

        settings.GameSettings.language = newValue;
        translationManager.currentLanguage = settings.GameSettings.language;
        translationManager.ReloadTranslationsAndUpdateScene();
    }
}
