using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PartyModeSettingsControl : MonoBehaviour, INeedInjection, ITranslator
{
    const int MAX_PLAYERS_COUNT = 20;
    const int MAX_TEAMS_COUNT = 4;
    const int MAX_ROUNDS_COUNT = 20;
    const int MAX_SONG_SUBSET_COUNT = 6;

    public VisualTreeAsset playerPickerUxml;
    public VisualTreeAsset teamUxml;
    public VisualTreeAsset roundUxml;

    [Inject] private Injector injector;

    [Inject] private SceneNavigator sceneNavigator;

    [Inject] private TranslationManager translationManager;

    [Inject] private Settings settings;

    [Inject(UxmlName = R.UxmlNames.background)]
    private VisualElement sceneRoot;

    [Inject(UxmlName = R.UxmlNames.screensContainer)]
    private VisualElement screensContainer;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject(UxmlName = R.UxmlNames.continueButton)]
    private Button continueButton;

    [Inject(UxmlName = R.UxmlNames.middleButton)]
    private Button middleButton;

    [Inject(UxmlName = R.UxmlNames.scrollViewPlayers)]
    private ScrollView scrollViewPlayers;

    [Inject(UxmlName = R.UxmlNames.scrollViewRounds)]
    private ScrollView scrollViewRounds;

    [Inject(UxmlName = R.UxmlNames.numberOfPlayersContainer)]
    private VisualElement numberOfPlayersContainer;

    [Inject(UxmlName = R.UxmlNames.numberOfRoundsContainer)]
    private VisualElement numberOfRoundsContainer;

    [Inject(UxmlName = R.UxmlNames.songSelectionContainer)]
    private VisualElement songSelectionContainer;

    [Inject(UxmlName = R.UxmlNames.songSubsetCountContainer)]
    private VisualElement songSubsetCountContainer;

    [Inject(UxmlName = R.UxmlNames.playersListContainer)]
    private VisualElement playersListContainer;

    [Inject(UxmlName = R.UxmlNames.teamsListContainer)]
    private VisualElement teamsListContainer;

    [Inject(UxmlName = R.UxmlNames.numberOfTeamsContainer)]
    private VisualElement numberOfTeamsContainer;

    private PartyModeSettingsSceneData sceneData;
    private VisualElement[] teamsPlayersContainers;
    private VisualElement[] playerCards;
    private VisualElement[] roundContainers;
    private Action middleButtonAction;

    int currentUiScreen;

    // Defines the screens flow to show for each mode:
    static readonly string[] screensTeamsMode = { R.UxmlNames.nbOfPlayers, R.UxmlNames.teams, R.UxmlNames.options, R.UxmlNames.rounds };
    static readonly string[] screensFreeForAllMode = { R.UxmlNames.nbOfPlayers, R.UxmlNames.options, R.UxmlNames.rounds };

    string[] ScreensArray
    {
        get { return sceneData.Mode == EPartyModeType.Teams ? screensTeamsMode : screensFreeForAllMode; }
    }

    int CurrentUiScreen
    {
        get => currentUiScreen;
        set
        {
            currentUiScreen = Mathf.Clamp(value, 0, ScreensArray.Length - 1);
            string nextScreen = ScreensArray[currentUiScreen];
            foreach (VisualElement screen in screensContainer.Children())
            {
                screen.SetVisibleByDisplay(screen.name == nextScreen);
                if (screen.name == nextScreen)
                {
                    screen.style.display = DisplayStyle.Flex;
                    screen.Q<Button>()?.Focus(); // focus first button
                }
                else
                {
                    screen.style.display = DisplayStyle.None;
                }
            }

            // Specific screen logic:
            switch (nextScreen)
            {
                default:
                    middleButton.style.display = DisplayStyle.None;
                    break;

                case R.UxmlNames.teams:
                    middleButton.style.display = DisplayStyle.Flex;
                    middleButton.text = TranslationManager.GetTranslation(R.Messages.partymode_shuffle_teams);
                    middleButtonAction = ShuffleTeams;

                    UpdateTeamsList();
                    DistributeTeams();
                    break;

                case R.UxmlNames.options:
                    if (settings.PartyModeSettings.mode == EPartyModeType.FreeForAll)
                    {
                        // TODO adapt according to number of actual singing players (instead of only 2)
                        settings.PartyModeSettings.roundsCount = Mathf.CeilToInt(settings.PartyModeSettings.playersCount / 2f);
                        numberOfRoundsContainer.Q<ItemPicker>().ItemLabel.text = settings.PartyModeSettings.roundsCount.ToString();
                    }

                    break;
                case R.UxmlNames.rounds:
                    // Show the correct number of rounds in the UI
                    for (int i = 0; i < roundContainers.Length; i++)
                    {
                        roundContainers[i].SetVisibleByDisplay(i < settings.PartyModeSettings.roundsCount);
                    }

                    break;
            }
        }
    }

    private void Start()
    {
        sceneData = sceneNavigator.GetSceneData<PartyModeSettingsSceneData>(null);
        settings.PartyModeSettings.mode = sceneData.Mode;

        CurrentUiScreen = 0;

        SetupPlayerNamesUi();
        SetupTeamsListUi();
        SetupRoundsListUi();

        numberOfRoundsContainer.SetEnabled(settings.PartyModeSettings.mode == EPartyModeType.Teams);

        middleButton.style.width = new StyleLength(StyleKeyword.Auto);
        middleButton.RegisterCallbackButtonTriggered(() => middleButtonAction());
        backButton.RegisterCallbackButtonTriggered(Back);
        continueButton.RegisterCallbackButtonTriggered(Continue);

        {
            ItemPicker playerCountItemPicker = numberOfPlayersContainer.Q<ItemPicker>();
            playerCountItemPicker.minValue = 2;
            playerCountItemPicker.maxValue = MAX_PLAYERS_COUNT;
            playerCountItemPicker.stepValue = 1;
            playerCountItemPicker.wrapAround = false;
            new NumberPickerControl(playerCountItemPicker, settings.PartyModeSettings.playersCount)
                .Bind(() => settings.PartyModeSettings.playersCount,
                    newValue =>
                    {
                        settings.PartyModeSettings.playersCount = (int)newValue;
                        UpdatePlayersList();
                    });
        }
        {
            ItemPicker teamsCountItemPicker = numberOfTeamsContainer.Q<ItemPicker>();
            teamsCountItemPicker.minValue = 2;
            teamsCountItemPicker.maxValue = MAX_TEAMS_COUNT;
            teamsCountItemPicker.stepValue = 1;
            teamsCountItemPicker.wrapAround = true;
            new NumberPickerControl(teamsCountItemPicker, settings.PartyModeSettings.teamsCount)
                .Bind(() => settings.PartyModeSettings.teamsCount,
                    newValue =>
                    {
                        settings.PartyModeSettings.teamsCount = (int)newValue;
                        DistributeTeams();
                        UpdateTeamsList();
                    });
        }
        {
            ItemPicker numberOfRoundsItemPicker = numberOfRoundsContainer.Q<ItemPicker>();
            numberOfRoundsItemPicker.minValue = 1;
            numberOfRoundsItemPicker.maxValue = MAX_ROUNDS_COUNT;
            numberOfRoundsItemPicker.stepValue = 1;
            numberOfRoundsItemPicker.wrapAround = false;
            new NumberPickerControl(numberOfRoundsItemPicker, settings.PartyModeSettings.roundsCount)
                .Bind(() => settings.PartyModeSettings.roundsCount,
                    newValue => settings.PartyModeSettings.roundsCount = (int)newValue);
        }
        {
            ItemPicker songSelectionItemPicker = songSelectionContainer.Q<ItemPicker>();
            songSelectionItemPicker.wrapAround = true;
            var songSelectionItemPickerControl = new LabeledItemPickerControl<EPartySongSelection>(songSelectionItemPicker, EnumUtils.GetValuesAsList<EPartySongSelection>());
            songSelectionItemPickerControl.Bind(() => settings.PartyModeSettings.songSelection,
                newValue =>
                {
                    settings.PartyModeSettings.songSelection = newValue;
                    songSubsetCountContainer.SetVisibleByDisplay(newValue == EPartySongSelection.RandomSubset);
                });
            songSelectionItemPickerControl.GetLabelTextFunction = songSelection => songSelection.TranslatedName();
        }
        {
            ItemPicker songSubsetItemPicker = songSubsetCountContainer.Q<ItemPicker>();
            songSubsetItemPicker.minValue = 1;
            songSubsetItemPicker.maxValue = MAX_SONG_SUBSET_COUNT;
            songSubsetItemPicker.stepValue = 1;
            songSubsetItemPicker.wrapAround = false;
            new NumberPickerControl(songSubsetItemPicker, settings.PartyModeSettings.subsetSongsCount)
                .Bind(() => settings.PartyModeSettings.subsetSongsCount,
                    newValue => settings.PartyModeSettings.subsetSongsCount = (int)newValue);
        }

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ =>
            {
                sceneNavigator.LoadScene(EScene.MainScene);
            });
    }

    void SetupPlayerNamesUi()
    {
        string GetGuestName(int playerIndex)
        {
            return $"{TranslationManager.GetTranslation(R.Messages.guest)} {playerIndex + 1}";
        }

        List<string> playerNamesList = settings.PlayerProfiles.Select(player => player.Name).ToList();

        // Initialize arrays if necessary
        while (settings.PartyModeSettings.playersList.Count < MAX_PLAYERS_COUNT)
        {
            int index = settings.PartyModeSettings.playersList.Count;
            // Fill array with existing profile names by default, or guests
            settings.PartyModeSettings.playersList.Add(index < playerNamesList.Count ? playerNamesList[index] : GetGuestName(index));
        }

        // In case array is too big
        while (settings.PartyModeSettings.playersList.Count > MAX_PLAYERS_COUNT)
        {
            settings.PartyModeSettings.playersList.RemoveAt(settings.PartyModeSettings.playersList.Count - 1);
        }

        // Generate all the player selection UIs, but we will only display the number of active players.
        // Users can choose an existing player from the defined profiles, or enter a guest name using a text field.
        // This allows adding temporary players without necessarily adding a PlayerProfile through the options menu beforehand.
        for (int i = 0; i < MAX_PLAYERS_COUNT; i++)
        {
            int playerIndex = i;

            VisualElement player = playerPickerUxml.Instantiate().Q<VisualElement>(R.UxmlNames.playerChooser);
            player.Q<Label>().text = $"{TranslationManager.GetTranslation(R.Messages.player)} {playerIndex + 1}";
            playersListContainer.Add(player);
            string defaultPlayerName = GetGuestName(playerIndex);
            TextField guestNameTextField = player.Q<TextField>();
            Label labelPlayerName = player.Q<Label>("labelPlayerName");

            // Sets the player name, and ensure it is unique/not empty
            void SetPlayerName(int playerIndex, string playerName, bool forward = true)
            {
                if (string.IsNullOrEmpty(playerName))
                {
                    playerName = GetGuestName(playerIndex);
                }

                RECURSE:
                bool isGuestName = !settings.PlayerProfiles.Exists(item => item.Name == playerName);
                if (isGuestName)
                {
                    int existingIndex = settings.PartyModeSettings.playersList.IndexOf(playerName);
                    if (existingIndex >= 0 && existingIndex != playerIndex)
                    {
                        playerName = $"{playerName} {Random.Range(100, 999):000}";
                    }

                    guestNameTextField.SetValueWithoutNotify(playerName);
                    guestNameTextField.style.display = DisplayStyle.Flex;
                    labelPlayerName.style.display = DisplayStyle.None;

                    guestNameTextField.Focus();
                    guestNameTextField.SelectAll();
                }
                else
                {
                    int existingIndex = settings.PartyModeSettings.playersList.IndexOf(playerName);
                    if (existingIndex >= 0 && existingIndex != playerIndex)
                    {
                        int listIndex = playerNamesList.IndexOf(playerName);
                        int newIndex = forward ? listIndex + 1 : listIndex - 1;
                        if (newIndex >= playerNamesList.Count || newIndex < 0)
                        {
                            playerName = GetGuestName(playerIndex);
                            goto RECURSE;
                        }
                        else
                        {
                            playerName = playerNamesList[newIndex];
                            goto RECURSE;
                        }
                    }

                    labelPlayerName.text = playerName;
                    labelPlayerName.style.display = DisplayStyle.Flex;
                    guestNameTextField.style.display = DisplayStyle.None;
                }

                settings.PartyModeSettings.playersList[playerIndex] = playerName;
            }

            // Button logic for the player block
            Button buttonPrevious = player.Q<Button>("previousItemButton");
            Button buttonNext = player.Q<Button>("nextItemButton");
            buttonPrevious.RegisterCallbackButtonTriggered(() =>
            {
                string currentName = settings.PartyModeSettings.playersList[playerIndex];
                int index = playerNamesList.IndexOf(currentName);
                if (index < 0)
                {
                    index = playerNamesList.Count;
                }

                SetPlayerName(playerIndex, index == 0 ? defaultPlayerName : playerNamesList[index - 1], false);
            });
            buttonNext.RegisterCallbackButtonTriggered(() =>
            {
                string currentName = settings.PartyModeSettings.playersList[playerIndex];
                int index = playerNamesList.IndexOf(currentName);
                SetPlayerName(playerIndex, index == playerNamesList.Count - 1 ? GetGuestName(playerIndex) : playerNamesList[index + 1], true);
            });
            // Sanitize text field's guest name
            guestNameTextField.RegisterCallback<ChangeEvent<string>>(evt =>
            {
                SetPlayerName(playerIndex, evt.newValue);
            });

            // Initialize with last used name
            SetPlayerName(playerIndex, settings.PartyModeSettings.playersList[playerIndex]);
        }
    }

    void SetupTeamsListUi()
    {
        VisualElement lastTeamColumnMouseOver = null;
        int dragPointerId = -1;

        teamsPlayersContainers = new VisualElement[MAX_TEAMS_COUNT];
        for (int i = 0; i < MAX_TEAMS_COUNT; i++)
        {
            VisualElement team = teamUxml.Instantiate().Q<VisualElement>(R.UxmlNames.team);
            team.Q<Label>().text = $"Team {i + 1}";
            team.userData = i;
            teamsListContainer.Add(team);
            teamsPlayersContainers[i] = team.Q<VisualElement>(R.UxmlNames.playersContainer);

            team.RegisterCallback<PointerEnterEvent>(evt =>
            {
                lastTeamColumnMouseOver = team;
                team.EnableInClassList("drag-hover", evt.pointerId == dragPointerId);
            });
            team.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                if (lastTeamColumnMouseOver == team)
                {
                    lastTeamColumnMouseOver = null;
                }

                team.RemoveFromClassList("drag-hover");
            });
        }

        playerCards = new VisualElement[MAX_PLAYERS_COUNT];
        for (int i = 0; i < MAX_PLAYERS_COUNT; i++)
        {
            var playerCard = new Label { text = settings.PartyModeSettings.playersList[i], focusable = true };
            playerCard.AddToClassList("playerCard");
            playerCard.usageHints = UsageHints.DynamicTransform;
            playerCards[i] = playerCard;
            teamsPlayersContainers[0].Add(playerCard);
            playerCard.userData = 0;

            #region Drag and Drop System

            // Pointer/touch devices
            playerCard.RegisterCallback<PointerDownEvent>(evt =>
            {
                evt.PreventDefault();
                evt.StopPropagation();

                dragPointerId = evt.pointerId;

                playerCard.RemoveFromHierarchy();
                sceneRoot.Add(playerCard);
                playerCard.style.position = Position.Absolute;
                playerCard.transform.position = sceneRoot.WorldToLocal(evt.position);

                sceneRoot.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                sceneRoot.RegisterCallback<PointerUpEvent>(OnPointerUp);
                sceneRoot.CapturePointer(evt.pointerId);

                void OnPointerMove(PointerMoveEvent evt2)
                {
                    if (evt2.pointerId == dragPointerId)
                    {
                        evt2.PreventDefault();
                        evt2.StopPropagation();
                        playerCard.transform.position = evt2.position;
                    }
                }

                void OnPointerUp(PointerUpEvent evt2)
                {
                    if (evt2.pointerId == dragPointerId)
                    {
                        evt2.PreventDefault();
                        evt2.StopPropagation();
                        sceneRoot.ReleasePointer(dragPointerId);
                        dragPointerId = -1;

                        playerCard.style.position = Position.Relative;
                        playerCard.transform.position = Vector3.zero;

                        teamsPlayersContainers[lastTeamColumnMouseOver != null ? (int)lastTeamColumnMouseOver.userData : (int)playerCard.userData].Add(playerCard);
                        sceneRoot.Q<VisualElement>(null, "drag-hover")?.RemoveFromClassList("drag-hover");

                        playerCard.ReleasePointer(evt2.pointerId);
                        sceneRoot.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                        sceneRoot.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                    }
                }
            });

            #endregion
        }
    }

    void SetupRoundsListUi()
    {
        roundContainers = new VisualElement[MAX_ROUNDS_COUNT];
        for (int i = 0; i < MAX_ROUNDS_COUNT; i++)
        {
            VisualElement roundVe = roundUxml.Instantiate().contentContainer;
            roundContainers[i] = roundVe;
            scrollViewRounds.Add(roundVe);

            roundVe.Q<Label>("Label").text = $"{TranslationManager.GetTranslation(R.Messages.partymode_round)} {i + 1}";

            if (settings.PartyModeSettings.roundsList.Count < (i + 1))
            {
                settings.PartyModeSettings.roundsList.Add(new PartyModeRound());
            }

            PartyModeRound round = settings.PartyModeSettings.roundsList[i];
            // settings.PartyModeSettings.roundsList.Add(round);

            // Gather containers
            var winConditionContainer = roundVe.Q<VisualElement>(R.UxmlNames.winConditionContainer);
            var winScoreContainer = roundVe.Q<VisualElement>(R.UxmlNames.winScoreContainer);
            var winPhrasesContainer = roundVe.Q<VisualElement>(R.UxmlNames.winPhrasesContainer);
            var modifierTriggerContainer = roundVe.Q<VisualElement>(R.UxmlNames.modifierTriggerContainer);
            var triggerTimeContainer = roundVe.Q<VisualElement>(R.UxmlNames.triggerTimeContainer);
            var triggerScoreContainer = roundVe.Q<VisualElement>(R.UxmlNames.triggerScoreContainer);
            var triggerTimeMaxContainer = roundVe.Q<VisualElement>(R.UxmlNames.triggerTimeMaxContainer);
            var triggerScoreMaxContainer = roundVe.Q<VisualElement>(R.UxmlNames.triggerScoreMaxContainer);
            var triggerWhoContainer = roundVe.Q<VisualElement>(R.UxmlNames.triggerWhoContainer);

            void UpdateFieldsVisibility()
            {
                modifierTriggerContainer.SetVisibleByDisplay(round.singModifiers[0].modifierActions != 0);
                triggerWhoContainer.SetVisibleByDisplay(
                    (round.singModifiers[0].modifierActions.HasFlag(SingModifier.EModifierAction.HideNotes)
                     || round.singModifiers[0].modifierActions.HasFlag(SingModifier.EModifierAction.HideScore))
                    &&
                    (round.singModifiers[0].trigger.triggerType is SingModifier.ModifierTrigger.ETriggerType.AfterScore
                        or SingModifier.ModifierTrigger.ETriggerType.ScoreRange
                        or SingModifier.ModifierTrigger.ETriggerType.UntilScore)
                );

                winScoreContainer.SetVisibleByDisplay(round.winCondition.winType is EPartyWinCondition.HighestScoreWithAdvance or EPartyWinCondition.FirstToScore);
                winPhrasesContainer.SetVisibleByDisplay(round.winCondition.winType is EPartyWinCondition.LeadForNumberOfPhrases);

                triggerTimeContainer.SetVisibleByDisplay(
                    round.singModifiers[0].modifierActions != 0
                    && round.singModifiers[0].trigger.triggerType
                        is SingModifier.ModifierTrigger.ETriggerType.AfterTime
                        or SingModifier.ModifierTrigger.ETriggerType.UntilTime
                        or SingModifier.ModifierTrigger.ETriggerType.TimeRange
                );
                triggerScoreContainer.SetVisibleByDisplay(
                    round.singModifiers[0].modifierActions != 0
                    && round.singModifiers[0].trigger.triggerType
                        is SingModifier.ModifierTrigger.ETriggerType.AfterScore
                        or SingModifier.ModifierTrigger.ETriggerType.UntilScore
                        or SingModifier.ModifierTrigger.ETriggerType.ScoreRange
                );
                triggerTimeMaxContainer.SetVisibleByDisplay(round.singModifiers[0].modifierActions != 0
                                                            && round.singModifiers[0].trigger.triggerType is SingModifier.ModifierTrigger.ETriggerType.TimeRange);
                triggerScoreMaxContainer.SetVisibleByDisplay(round.singModifiers[0].modifierActions != 0
                                                             && round.singModifiers[0].trigger.triggerType is SingModifier.ModifierTrigger.ETriggerType.ScoreRange);
            }

            UpdateFieldsVisibility();

            {
                var winItemPicker = winConditionContainer.Q<ItemPicker>();
                winItemPicker.wrapAround = true;
                var winItemPickerControl = new LabeledItemPickerControl<EPartyWinCondition>(winItemPicker, EnumUtils.GetValuesAsList<EPartyWinCondition>());
                winItemPickerControl.Bind(
                    () => round.winCondition.winType,
                    newValue =>
                    {
                        round.winCondition.winType = newValue;
                        UpdateFieldsVisibility();
                    });
                winItemPickerControl.GetLabelTextFunction = condition => condition.TranslatedName();
            }
            {
                var winScoreItemPicker = winScoreContainer.Q<ItemPicker>();
                winScoreItemPicker.stepValue = 500;
                winScoreItemPicker.minValue = 500;
                winScoreItemPicker.maxValue = 10000;
                winScoreItemPicker.wrapAround = false;
                new NumberPickerControl(winScoreItemPicker, 1000).Bind(
                    () => round.winCondition.score,
                    (value) => round.winCondition.score = (int)value
                );
            }
            {
                var winPhrasesItemPicker = winPhrasesContainer.Q<ItemPicker>();
                winPhrasesItemPicker.stepValue = 1;
                winPhrasesItemPicker.minValue = 1;
                winPhrasesItemPicker.maxValue = 20;
                winPhrasesItemPicker.wrapAround = false;
                new NumberPickerControl(winPhrasesItemPicker, 5).Bind(
                    () => round.winCondition.phrases,
                    (value) => round.winCondition.phrases = (int)value
                );
            }
            {
                var modifierTriggerItemPicker = modifierTriggerContainer.Q<ItemPicker>();
                modifierTriggerItemPicker.wrapAround = true;
                var modifierTriggerItemPickerControl = new LabeledItemPickerControl<SingModifier.ModifierTrigger.ETriggerType>(modifierTriggerItemPicker, EnumUtils.GetValuesAsList<SingModifier.ModifierTrigger.ETriggerType>());
                modifierTriggerItemPickerControl.Bind(
                    () => round.singModifiers[0].trigger.triggerType,
                    newValue =>
                    {
                        round.singModifiers[0].trigger.triggerType = newValue;
                        UpdateFieldsVisibility();
                    });
                modifierTriggerItemPickerControl.GetLabelTextFunction = triggerType => triggerType.TranslatedName();
            }
            {
                string[] triggerWhoLabels = { "partymode_all_players", "partymode_individual_players" };
                var triggerWhoItemPicker = triggerWhoContainer.Q<ItemPicker>();
                triggerWhoItemPicker.wrapAround = true;
                var modifierTriggerItemPickerControl = new LabeledItemPickerControl<string>(triggerWhoItemPicker, new List<string>(triggerWhoLabels));
                modifierTriggerItemPickerControl.Bind(
                    () => round.singModifiers[0].applyToPlayersIndividually ? triggerWhoLabels[1] : triggerWhoLabels[0],
                    newValue => round.singModifiers[0].applyToPlayersIndividually = newValue == triggerWhoLabels[1]
                );
                modifierTriggerItemPickerControl.GetLabelTextFunction = label => TranslationManager.GetTranslation(label);
            }
            {
                var triggerScoreItemPicker = triggerScoreContainer.Q<ItemPicker>();
                triggerScoreItemPicker.stepValue = 500;
                triggerScoreItemPicker.minValue = 0;
                triggerScoreItemPicker.maxValue = 10000;
                triggerScoreItemPicker.wrapAround = false;
                new NumberPickerControl(triggerScoreItemPicker).Bind(
                    () => round.singModifiers[0].trigger.scoreMin,
                    (value) => round.singModifiers[0].trigger.scoreMin = (int)value
                );
            }
            {
                var triggerScoreMaxItemPicker = triggerScoreMaxContainer.Q<ItemPicker>();
                triggerScoreMaxItemPicker.stepValue = 500;
                triggerScoreMaxItemPicker.minValue = 500;
                triggerScoreMaxItemPicker.maxValue = 10000;
                triggerScoreMaxItemPicker.wrapAround = false;
                new NumberPickerControl(triggerScoreMaxItemPicker).Bind(
                    () => round.singModifiers[0].trigger.scoreMax,
                    (value) => round.singModifiers[0].trigger.scoreMax = (int)value
                );
            }
            {
                var triggerTimeItemPicker = triggerTimeContainer.Q<ItemPicker>();
                triggerTimeItemPicker.stepValue = 0.05f;
                triggerTimeItemPicker.minValue = 0.0f;
                triggerTimeItemPicker.maxValue = 0.9f;
                triggerTimeItemPicker.wrapAround = false;
                var control = new NumberPickerControl(triggerTimeItemPicker);
                control.Bind(
                    () => round.singModifiers[0].trigger.timeMin,
                    (value) => round.singModifiers[0].trigger.timeMin = (float)value
                );
                control.GetLabelTextFunction = value => $"{value * 100:0}%";
            }
            {
                var triggerTimeMaxItemPicker = triggerTimeMaxContainer.Q<ItemPicker>();
                triggerTimeMaxItemPicker.stepValue = 0.05f;
                triggerTimeMaxItemPicker.minValue = 0.1f;
                triggerTimeMaxItemPicker.maxValue = 1.0f;
                triggerTimeMaxItemPicker.wrapAround = false;
                var control = new NumberPickerControl(triggerTimeMaxItemPicker);
                control.Bind(
                    () => round.singModifiers[0].trigger.timeMax,
                    (value) => round.singModifiers[0].trigger.timeMax = (float)value
                );
                control.GetLabelTextFunction = value => $"{value * 100:0}%";
            }
            {
                var gameModifierEnumFlagsField = new EnumFlagsFieldRuntime<SingModifier.EModifierAction>(); // Can't create through UI Builder because it's a generic class
                gameModifierEnumFlagsField.AddNextTo(winPhrasesContainer);
                gameModifierEnumFlagsField.ItemLabel.text = "Game Modifier";
                gameModifierEnumFlagsField.Bind(
                    sceneRoot,
                    () => round.singModifiers[0].modifierActions,
                    value =>
                    {
                        round.singModifiers[0].modifierActions = value;
                        UpdateFieldsVisibility();
                    });
                // Note: rounds support multiple modifiers, but we're only allowing one at this moment, hence the [0].
                // TODO UI to support multiple modifiers per round
            }
        }
    }

    void DistributeTeams(bool shuffle = false)
    {
        VisualElement[] playerCardsCopy = playerCards.Where(item => item.style.display == DisplayStyle.Flex).ToArray();
        if (shuffle)
        {
            ObjectUtils.ShuffleList(playerCardsCopy);
        }

        for (int i = 0; i < playerCardsCopy.Length; i++)
        {
            int team = i % settings.PartyModeSettings.teamsCount;
            teamsPlayersContainers[team].Add(playerCardsCopy[i]);
        }
    }

    void ShuffleTeams()
    {
        DistributeTeams(true);
    }

    void UpdatePlayersList()
    {
        int index = 0;
        VisualElement scrollTo = null;
        foreach (VisualElement player in playersListContainer.Children())
        {
            player.SetVisibleByDisplay(index < settings.PartyModeSettings.playersCount);
            if (index < settings.PartyModeSettings.playersCount)
            {
                scrollTo = player;
            }

            index++;
        }

        IEnumerator CoroutineScrollToDelayed()
        {
            yield return null;
            scrollViewPlayers.ScrollTo(scrollTo);
        }

        StartCoroutine(CoroutineScrollToDelayed());
    }

    void UpdateTeamsList()
    {
        for (int i = 0; i < playerCards.Length; i++)
        {
            playerCards[i].SetVisibleByDisplay(i < settings.PartyModeSettings.playersCount);
            playerCards[i].Q<Label>().text = settings.PartyModeSettings.playersList[i];
        }

        int index = 0;
        foreach (VisualElement team in teamsListContainer.Children())
        {
            team.SetVisibleByDisplay(index < settings.PartyModeSettings.teamsCount);
            index++;
        }
    }

    void Back()
    {
        if (CurrentUiScreen == 0)
        {
            sceneNavigator.LoadScene(EScene.MainScene);
        }
        else
        {
            CurrentUiScreen--;
        }
    }

    void Continue()
    {
        if (CurrentUiScreen == ScreensArray.Length - 1)
        {
            PartyModeManager.NewGame(new PartyModeManager.PartyModeData()
            {
                mode = settings.PartyModeSettings.mode,
                rounds = settings.PartyModeSettings.roundsList.Where((round, index) => index < settings.PartyModeSettings.roundsCount).ToList(),
                playerNames = settings.PartyModeSettings.playersList.Where((player, index) => index < settings.PartyModeSettings.playersCount).ToList(),
                singingPlayersCount = settings.MicProfiles.Count(item => item.IsEnabled), // TODO verify that at least two microphones are enabled, and allow users to choose the number of singers
                songSelection = settings.PartyModeSettings.songSelection,
                songSubsetCount = settings.PartyModeSettings.subsetSongsCount
            });
            sceneNavigator.LoadScene(EScene.PartyModeVersus);
        }
        else
        {
            CurrentUiScreen++;
        }
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }

        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        continueButton.text = TranslationManager.GetTranslation(R.Messages.continue_);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.partymode_settings_title);
        numberOfPlayersContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.partymode_number_of_players);
        numberOfTeamsContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.partymode_number_of_teams);
    }
}
