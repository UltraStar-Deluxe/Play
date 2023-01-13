using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PartyModeRoundsConfigControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = nameof(roundUi))]
    private VisualTreeAsset roundUi;

    [Inject]
    private Settings settings;

    [Inject]
    private PartyModeSettings partyModeSettings;

    [Inject]
    private GameObject gameObject;

    [Inject(UxmlName = R.UxmlNames.roundsContainer)]
    private VisualElement roundsContainer;

    [Inject(UxmlName = R.UxmlNames.addRoundButton)]
    private Button addRoundButton;

    [Inject]
    private Injector injector;

    private readonly List<PartyModeRoundConfigControl> roundConfigControls = new();

    private int unfoldedRoundUiIndex;

    public void OnInjectionFinished()
    {
        addRoundButton.RegisterCallbackButtonTriggered(() => AddRound());

        UpdateRoundsUi();
    }

    private void UpdateRoundsUi()
    {
        roundsContainer.Clear();
        roundConfigControls.Clear();

        partyModeSettings.RoundsSettings.GameRoundSettings.ToList().ForEach(roundSettings => CreateRoundSettingsUi(roundSettings));

        for (int i = 0; i < roundConfigControls.Count; i++)
        {
            if (i == unfoldedRoundUiIndex)
            {
                roundConfigControls[i].Unfold(false);
            }
            else
            {
                roundConfigControls[i].Fold(false);
            }
        }
    }

    private void CreateRoundSettingsUi(GameRoundSettings roundSettings)
    {
        VisualElement roundConfigVisualElement = roundUi.CloneTree().Children().FirstOrDefault();
        roundsContainer.Add(roundConfigVisualElement);

        PartyModeRoundConfigControl roundConfigControl = injector
            .WithRootVisualElement(roundConfigVisualElement)
            .WithBindingForInstance(roundSettings)
            .CreateAndInject<PartyModeRoundConfigControl>();

        roundConfigControl.DeletedEventStream.Subscribe(gameRoundSettings => OnGameRoundDeleted(gameRoundSettings));
        roundConfigControl.UnfoldEventStream.Subscribe(gameRoundSettings => OnGameRoundUnfolded(gameRoundSettings));
        roundConfigControl.PresetsChangedEventStream.Subscribe(_ => UpdateRoundsUi());
        roundConfigControl.AppliedPresetEventStream.Subscribe(_ => UpdateRoundsUi());

        roundConfigControls.Add(roundConfigControl);
    }

    private void OnGameRoundUnfolded(GameRoundSettings gameRoundSettings)
    {
        // Fold all others
        roundConfigControls.ForEach(it =>
        {
            if (it.GameRoundSettings != gameRoundSettings)
            {
                it.Fold(false);
            }
        });
        unfoldedRoundUiIndex = partyModeSettings.RoundsSettings.GameRoundSettings.IndexOf(gameRoundSettings);
    }

    private void OnGameRoundDeleted(GameRoundSettings roundSettings)
    {
        if (partyModeSettings.RoundsSettings.GameRoundSettings.Count <= 1)
        {
            // Must play at least one round
            return;
        }

        partyModeSettings.RoundsSettings.GameRoundSettings.Remove(roundSettings);
        UpdateRoundsUi();
    }

    private void AddRound()
    {
        GameRoundSettings newRound = new();
        partyModeSettings.RoundsSettings.GameRoundSettings.Add(newRound);

        UpdateRoundsUi();

        PartyModeRoundConfigControl newRoundControl = roundConfigControls.FirstOrDefault(roundControl => ReferenceEquals(roundControl.GameRoundSettings, newRound));
        newRoundControl?.Unfold(true);
    }
}
