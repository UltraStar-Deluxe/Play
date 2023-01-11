using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PartyModeSceneControl : MonoBehaviour, INeedInjection, IBinder
{
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject(UxmlName = R.UxmlNames.partyModeTeamConfigUi)]
    private VisualElement partyModeTeamConfigUi;

    [Inject(UxmlName = R.UxmlNames.partyModeSongSelectionConfigUi)]
    private VisualElement partyModeSongSelectionConfigUi;

    [Inject(UxmlName = R.UxmlNames.partyModeRoundConfigUi)]
    private VisualElement partyModeRoundConfigUi;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject(UxmlName = R.UxmlNames.continueButton)]
    private Button continueButton;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    private readonly PartyModeSettings partyModeSettings = new();
    private readonly PartyModeSettingsChangeEventStream partyModeSettingsChangeEventStream = new();
    private readonly ReactiveProperty<EPartyModeConfigPart> configPart = new(EPartyModeConfigPart.Teams);

	private void Start()
    {
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => OnBack());

        backButton.RegisterCallbackButtonTriggered(() => OnBack());
        continueButton.RegisterCallbackButtonTriggered(() => OnContinue());

        configPart.Subscribe(_ => UpdateConfigPart());
        UpdateConfigPart();
    }

    private void UpdateConfigPart()
    {
        VisualElement GetCurrentConfigPartVisualElement()
        {
            switch (configPart.Value)
            {
                case EPartyModeConfigPart.Teams:
                    return partyModeTeamConfigUi;
                case EPartyModeConfigPart.SongSelection:
                    return partyModeSongSelectionConfigUi;
                case EPartyModeConfigPart.Rounds:
                    return partyModeRoundConfigUi;
            }

            throw new ArgumentException($"Unhandled config part {configPart.Value}");
        }

        // Show only the current config part UI
        List<VisualElement> configUis = new List<VisualElement>
        {
            partyModeTeamConfigUi,
            partyModeSongSelectionConfigUi,
            partyModeRoundConfigUi,
        };
        configUis.ForEach(configUi => configUi.HideByDisplay());
        GetCurrentConfigPartVisualElement().ShowByDisplay();

        // Update the scene title
        sceneTitle.text = $"Party Mode - {StringUtils.ToTitleCase(configPart.Value.ToString())}";
    }

    private void OnBack()
    {
        if (configPart.Value == EPartyModeConfigPart.Teams)
        {
            sceneNavigator.LoadScene(EScene.MainScene);
        }
        else if (configPart.Value == EPartyModeConfigPart.SongSelection)
        {
            configPart.Value = EPartyModeConfigPart.Teams;
        }
        else if (configPart.Value == EPartyModeConfigPart.Rounds)
        {
            configPart.Value = EPartyModeConfigPart.SongSelection;
        }
	}

    private void OnContinue()
    {
        if (configPart.Value == EPartyModeConfigPart.Teams)
        {
            configPart.Value = EPartyModeConfigPart.SongSelection;
        }
        else if (configPart.Value == EPartyModeConfigPart.SongSelection)
        {
            configPart.Value = EPartyModeConfigPart.Rounds;
        }
        else if (configPart.Value == EPartyModeConfigPart.Rounds)
        {
            // All config done, start the first party round
            SongSelectSceneData songSelectSceneData = new();
            songSelectSceneData.PartyModeSettings = partyModeSettings;
            sceneNavigator.LoadScene(EScene.SongSelectScene, songSelectSceneData);
        }
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(partyModeSettingsChangeEventStream);
        bb.BindExistingInstance(partyModeSettings);
        return bb.GetBindings();
    }
}
