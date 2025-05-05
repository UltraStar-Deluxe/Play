using UnityEngine;
using UnityEngine.UIElements;
using UniRx;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using System;

public class CoinCollectorGameModifierPlayerControl : INeedInjection, IInjectionFinishedListener
{
    public string modFolder;

    private const int CollectedCoinCountBonusThreshold = 10;

    [Inject]
    private GameObject gameObject;

    [Inject]
    private PlayerControl playerControl;

    [Inject]
    private Injector injector;

    [Inject(UxmlName = R.UxmlNames.playerImage)]
    private VisualElement playerImage;

    [Inject(UxmlName = R.UxmlNames.playerScoreLabel)]
    private VisualElement playerScoreLabel;

    private List<CoinControl> coinControls = new List<CoinControl>();
    private List<RecordedNoteControl> recordedNoteControls = new List<RecordedNoteControl>();

    private VisualElement coinCountContainer;
    private Label coinCountLabel;

    private int totalCollectedCoinCount;
    private int collectedCoinCountSinceLastBonus;

    public void OnInjectionFinished()
    {
        playerControl.PlayerUiControl.NoteDisplayer.TargetNoteControlCreatedEventStream
            .Subscribe(evt => OnCreatedTargetNoteControl(evt.TargetNoteControl, playerControl))
            .AddTo(gameObject);

        playerControl.PlayerUiControl.NoteDisplayer.RecordedNoteControlCreatedEventStream
            .Subscribe(evt => OnCreatedRecordedNoteControl(evt.RecordedNoteControl, playerControl))
            .AddTo(gameObject);

        CreateCoinsLabel();
    }

    private async void CreateCoinsLabel()
    {
        coinCountContainer = new VisualElement();
        coinCountContainer.name = "coinCountContainer";
        Sprite sprite = await ImageManager.LoadSpriteFromUriAsync($"{modFolder}/images/coins/Gold_1.png");
        coinCountContainer.style.backgroundImage = new StyleBackground(sprite);

        coinCountLabel = new Label();
        coinCountLabel.name = "coinCountLabel";
        coinCountContainer.Add(coinCountLabel);
        UpdateCoinsLabel();

        playerImage.Add(coinCountContainer);
    }

    private void UpdateCoinsLabel()
    {
        coinCountLabel.text = $"{collectedCoinCountSinceLastBonus}";
    }

    public void Update()
    {
        List<RecordedNoteControl> recordedNoteControlsCopy = recordedNoteControls.ToList();
        foreach (RecordedNoteControl recordedNoteControl in recordedNoteControlsCopy)
        {
            UpdateRecordedNoteControl(recordedNoteControl);
        }
    }

    private void UpdateRecordedNoteControl(RecordedNoteControl recordedNoteControl)
    {
        double EndBeat = recordedNoteControl.EndBeat;
        int targetEndBeat = recordedNoteControl.RecordedNote.TargetNote.EndBeat;
        int targetStartBeat = recordedNoteControl.RecordedNote.TargetNote.StartBeat;
        int targetCenterBeat = targetStartBeat + (targetEndBeat - targetStartBeat) / 2;
        if (Math.Abs(targetCenterBeat - EndBeat) < 1)
        {
            CollectCoin(recordedNoteControl);
        }
    }

    private void CollectCoin(RecordedNoteControl recordedNoteControl)
    {
        if (recordedNoteControl == null)
        {
            return;
        }
        recordedNoteControls.Remove(recordedNoteControl);

        CoinControl coinControl = coinControls
            .FirstOrDefault(it => it.TargetNoteControl.Note.StartBeat == recordedNoteControl.RecordedNote.TargetNote.StartBeat
                                  && it.TargetNoteControl.Note.EndBeat == recordedNoteControl.RecordedNote.TargetNote.EndBeat);
        if (coinControl == null)
        {
            return;
        }
        
        coinControl.VisualElement.RemoveFromHierarchy();
        coinControls.Remove(coinControl);

        collectedCoinCountSinceLastBonus++;
        totalCollectedCoinCount++;
        if (collectedCoinCountSinceLastBonus >= CollectedCoinCountBonusThreshold)
        {
            GiveCoinBonusPoints();
        }
        UpdateCoinsLabel();
    }

    private void GiveCoinBonusPoints()
    {
        collectedCoinCountSinceLastBonus -= CollectedCoinCountBonusThreshold;
        playerControl.PlayerScoreControl.SetModTotalScore(playerControl.PlayerScoreControl.CalculationData.ModTotalScore + 100);
        playerControl.PlayerUiControl.ShowTotalScore(playerControl.PlayerScoreControl.TotalScore);
        
        Debug.Log($"Added 100 points to score of player '{playerControl.PlayerProfile?.Name}'");

        // Highlight with Animation

        AnimationUtils.BounceVisualElementSize(gameObject, coinCountContainer, 1.5f);
        AnimationUtils.BounceVisualElementSize(gameObject, playerScoreLabel, 1.5f);
    }

    private void OnCreatedTargetNoteControl(TargetNoteControl targetNoteControl, PlayerControl playerControl)
    {
        bool hasCoin = UnityEngine.Random.Range(0, 4) == 0;
        if (!hasCoin)
        {
            return;
        }

        CoinControl coinControl = new CoinControl(modFolder, targetNoteControl);
        coinControls.Add(coinControl);
    }

    private void OnCreatedRecordedNoteControl(RecordedNoteControl recordedNoteControl, PlayerControl playerControl)
    {
        if (recordedNoteControl.RecordedNote.TargetNote == null
            || recordedNoteControl.RecordedNote.RoundedMidiNote != recordedNoteControl.RecordedNote.TargetNote.MidiNote)
        {
            return;
        }

        recordedNoteControls.Add(recordedNoteControl);
    }
}
