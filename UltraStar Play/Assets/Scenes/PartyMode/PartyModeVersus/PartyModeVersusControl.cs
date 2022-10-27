using System.Collections;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

// Scene that shows before a round in Party Mode
// Displays the next Players, the winning condition, and the modifiers if any

public class PartyModeVersusControl : MonoBehaviour, INeedInjection, ITranslator
{
    [Inject] private SceneNavigator sceneNavigator;

    [Inject] private TranslationManager translationManager;

    [Inject] private Settings settings;

    [Inject(UxmlName = R.UxmlNames.labelTop)]
    private Label labelTop;

    [Inject(UxmlName = R.UxmlNames.labelPlayer1)]
    private Label labelPlayer1;

    [Inject(UxmlName = R.UxmlNames.labelPlayer2)]
    private Label labelPlayer2;

    [Inject(UxmlName = R.UxmlNames.labelVs)]
    private Label labelVs;

    [Inject(UxmlName = R.UxmlNames.winConditionTitle)]
    private Label winConditionTitle;

    [Inject(UxmlName = R.UxmlNames.winConditionDescription)]
    private Label winConditionDescription;

    [Inject(UxmlName = R.UxmlNames.modifierTitle)]
    private Label modifierTitle;

    [Inject(UxmlName = R.UxmlNames.modifierDescription)]
    private Label modifierDescription;

    [Inject(UxmlName = R.UxmlNames.continueButton)]
    private Button continueButton;

    [Inject(UxmlName = R.UxmlNames.subtextContainer)]
    private VisualElement subtextContainer;

    private void Start()
    {
        var roundData = PartyModeManager.NextRoundData();

        labelTop.text = $"Round {roundData.number:00}/{PartyModeManager.CurrentPartyData.rounds.Count:00}";

        // TODO support for more than 2 players
        labelPlayer1.text = roundData.playerNames[0];
        labelPlayer2.text = roundData.playerNames[1];

        winConditionTitle.text = roundData.round.winCondition.winType.TranslatedName();
        string winDescription = TranslationManager.GetTranslation($"{roundData.round.winCondition.winType}_description")
            .Replace("[SCORE]", roundData.round.winCondition.score.ToString())
            .Replace("[PHRASES]", roundData.round.winCondition.phrases.ToString());
        winConditionDescription.text = winDescription;

        // TODO find enabled flags, and add descriptions separated with line break
        modifierTitle.text = roundData.round.singModifiers[0].modifierActions.ToString();
        modifierDescription.text = "blablabla";

        if (roundData.round.singModifiers[0].modifierActions == 0)
        {
            modifierTitle.style.display = DisplayStyle.None;
            modifierDescription.style.display = DisplayStyle.None;
        }

        continueButton.RegisterCallbackButtonTriggered(() =>
        {
            SongSelectSceneData data = new()
            {
                IsPartyMode = true,
                SongMetaSet = PartyModeManager.GetSongMetasSubset()
            };
            sceneNavigator.LoadScene(EScene.SongSelectScene, data);
        });

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ =>
            {
                sceneNavigator.LoadScene(EScene.MainScene);
            });

        // Element animations
        labelPlayer1.style.scale = new StyleScale(new Scale(Vector3.one * 1.3f));
        labelPlayer2.style.scale = new StyleScale(new Scale(Vector3.one * 1.3f));
        labelVs.style.scale = new StyleScale(new Scale(Vector3.one * 3f));

        labelPlayer1.style.opacity = 0;
        labelPlayer2.style.opacity = 0;
        labelVs.style.opacity = 0;
        subtextContainer.style.opacity = 0;

        continueButton.style.display = DisplayStyle.None;
        continueButton.style.scale = new StyleScale(new Scale(Vector3.one * 1.5f));
        continueButton.style.opacity = 0f;

        IEnumerator CoroutineStylesAnimations()
        {
            yield return null;
            labelPlayer1.style.scale = new StyleScale(new Scale(Vector3.one));
            labelPlayer2.style.scale = new StyleScale(new Scale(Vector3.one));
            labelVs.style.scale = new StyleScale(new Scale(Vector3.one));

            labelPlayer1.style.opacity = 1;
            labelPlayer2.style.opacity = 1;
            labelVs.style.opacity = 1;

            // Small animation to shuffle names before revealing players

            int count = roundData.allPlayerNames.Count;
            if (count > 2)
            {
                WaitForSeconds wait = new (0.05f);
                float target = Time.realtimeSinceStartup + 1.5f;
                while (Time.realtimeSinceStartup < target)
                {
                    string oldName1 = labelPlayer1.text;
                    string oldName2 = labelPlayer2.text;

                    while (labelPlayer1.text == oldName1)
                    {
                        labelPlayer1.text = roundData.allPlayerNames[Random.Range(0, count)];
                    }

                    while (labelPlayer2.text == oldName2)
                    {
                        labelPlayer2.text = roundData.allPlayerNames[Random.Range(0, count)];
                    }

                    yield return wait;
                }
            }

            labelPlayer1.text = roundData.playerNames[0];
            labelPlayer2.text = roundData.playerNames[1];

            subtextContainer.style.opacity = 1.0f;

            continueButton.style.display = DisplayStyle.Flex;
            continueButton.style.scale = new StyleScale(new Scale(Vector3.one));
            continueButton.style.opacity = 1.0f;
        }

        StartCoroutine(CoroutineStylesAnimations());
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && continueButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }

        continueButton.text = TranslationManager.GetTranslation(R.Messages.letsgo);
    }
}
