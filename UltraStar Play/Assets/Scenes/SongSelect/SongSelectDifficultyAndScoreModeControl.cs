using System;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class SongSelectDifficultyAndScoreModeControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private Settings settings;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    [Inject]
    private GameObject gameObject;

    [Inject(UxmlName = R.UxmlNames.currentDifficultyLabel)]
    private Label currentDifficultyLabel;

    [Inject(UxmlName = R.UxmlNames.nextDifficultyButton)]
    private Button nextDifficultyButton;

    [Inject(UxmlName = R.UxmlNames.previousDifficultyButton)]
    private Button previousDifficultyButton;

    [Inject(UxmlName = R.UxmlNames.coopModeToggle)]
    private Toggle coopModeToggle;

    public void OnInjectionFinished()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectScene.InitDifficultyAndScoreMode");

        // Set difficulty for all players
        settings.ObserveEveryValueChanged(it => it.Difficulty)
            .Subscribe(newValue =>
            {
                settings.PlayerProfiles
                    .Union(nonPersistentSettings.LobbyMemberPlayerProfiles)
                    .ForEach(it => it.Difficulty = newValue);
            });

        nextDifficultyButton.RegisterCallbackButtonTriggered(_ => SetNextDifficulty());
        previousDifficultyButton.RegisterCallbackButtonTriggered(_ => SetPreviousDifficulty());

        UpdateDifficultyAndScoreModeControls();

        FieldBindingUtils.Bind(
            gameObject,
            coopModeToggle,
            () => SettingsUtils.IsCoopModeEnabled(settings),
            newValue =>
            {
                SettingsUtils.SetCoopModeEnabled(settings, newValue);
                UpdateDifficultyAndScoreModeControls();
            });
    }

    private void SetPreviousDifficulty()
    {
        if (settings.ScoreMode == EScoreMode.None)
        {
            settings.ScoreMode = EScoreMode.Individual;
            SetDifficulty(EDifficulty.Hard);
        }
        else
        {
            switch (settings.Difficulty)
            {
                case EDifficulty.Easy:
                    SetNoScoreMode();
                    break;
                case EDifficulty.Medium:
                    SetDifficulty(EDifficulty.Easy);
                    break;
                case EDifficulty.Hard:
                    SetDifficulty(EDifficulty.Medium);
                    break;
            }
        }
    }

    private void SetNextDifficulty()
    {
        if (settings.ScoreMode == EScoreMode.None)
        {
            settings.ScoreMode = EScoreMode.Individual;
            SetDifficulty(EDifficulty.Easy);
        }
        else
        {
            switch (settings.Difficulty)
            {
                case EDifficulty.Easy:
                    SetDifficulty(EDifficulty.Medium);
                    break;
                case EDifficulty.Medium:
                    SetDifficulty(EDifficulty.Hard);
                    break;
                case EDifficulty.Hard:
                    SetNoScoreMode();
                    break;
            }
        }
    }

    private void SetNoScoreMode()
    {
        settings.ScoreMode = EScoreMode.None;
        UpdateDifficultyAndScoreModeControls();
    }

    private void SetDifficulty(EDifficulty difficulty)
    {
        settings.Difficulty = difficulty;
        if (settings.ScoreMode == EScoreMode.None)
        {
            settings.ScoreMode = EScoreMode.Individual;
        }
        UpdateDifficultyAndScoreModeControls();
    }

    private void UpdateDifficultyAndScoreModeControls()
    {
        if (settings.ScoreMode == EScoreMode.None)
        {
            currentDifficultyLabel.SetTranslatedText(Translation.Get(R.Messages.options_difficulty_noScores));
        }
        else
        {
            currentDifficultyLabel.SetTranslatedText(Translation.Get(settings.Difficulty));
        }
    }

}
