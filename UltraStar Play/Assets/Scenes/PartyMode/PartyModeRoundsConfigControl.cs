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

    private int expandedRoundUiIndex;

    public void OnInjectionFinished()
    {
        addRoundButton.RegisterCallbackButtonTriggered(() => AddRound());

        UpdateRoundsUi();
    }

    private void UpdateRoundsUi()
    {
        roundsContainer.Clear();
        roundConfigControls.Clear();

        partyModeSettings.RoundsSettings.GameRoundSettings.ForEach(roundSettings => CreateRoundSettingsUi(roundSettings));
    }

    private void CreateRoundSettingsUi(GameRoundSettings roundSettings)
    {
        VisualElement roundConfigVisualElement = roundUi.CloneTree().Children().FirstOrDefault();
        roundsContainer.Add(roundConfigVisualElement);

        PartyModeRoundConfigControl roundConfigControl = injector
            .WithRootVisualElement(roundConfigVisualElement)
            .WithBindingForInstance(roundSettings)
            .CreateAndInject<PartyModeRoundConfigControl>();

        roundConfigControl.DeleteRoundButton.RegisterCallbackButtonTriggered(() => DeleteRound(roundSettings));
        roundConfigControl.DeleteRoundButton.SetEnabled(partyModeSettings.RoundsSettings.GameRoundSettings.Count > 1);

        roundConfigControls.Add(roundConfigControl);
    }

    private void DeleteRound(GameRoundSettings roundSettings)
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
    }
}
