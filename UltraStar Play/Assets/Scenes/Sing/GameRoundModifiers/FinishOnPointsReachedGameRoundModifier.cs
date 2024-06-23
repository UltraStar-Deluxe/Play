using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class FinishOnPointsReachedGameRoundModifier : GameRoundModifier
{
    public override double DisplayOrder => 20;

    public int pointsThreshold = 9000;
    public int PointsThreshold {
        get
        {
            return pointsThreshold;
        }
        set
        {
            pointsThreshold = value;
            pointsThreshold = NumberUtils.Limit(pointsThreshold, 100, 9900);
        }
    }

    public override GameRoundModifierControl CreateControl()
    {
        FinishOnPointsReachedGameRoundModifierControl modifierControl = GameObjectUtils
            .CreateGameObjectWithComponent<FinishOnPointsReachedGameRoundModifierControl>();
        modifierControl.pointsThreshold = PointsThreshold;
        return modifierControl;
    }

    public override VisualElement CreateConfigurationVisualElement()
    {
        IntegerField integerField = new IntegerField();
        integerField.SetTranslatedLabel(Translation.Get(R.Messages.gameRoundModifier_condition_score));
        FieldBindingUtils.Bind(integerField,
            () => PointsThreshold,
            newValue =>
            {
                PointsThreshold = newValue;
                if (PointsThreshold != integerField.value)
                {
                    integerField.value = PointsThreshold;
                }
            });
        return integerField;
    }

    public class FinishOnPointsReachedGameRoundModifierControl : GameRoundModifierControl
    {
        public int pointsThreshold;

        [Inject]
        private SingSceneControl singSceneControl;

        [Inject]
        private SingSceneFinisher singSceneFinisher;

        [Inject]
        private SingSceneData singSceneData;

        private bool isFinished;

        private void Start()
        {
            singSceneControl.PlayerControls
                .Select(playerControl => playerControl.PlayerScoreControl.ScoreChangedEventStream)
                .Merge()
                .Subscribe(evt => UpdateFinish())
                .AddTo(gameObject);
        }

        private void UpdateFinish()
        {
            if (isFinished
                || singSceneData.IsMedley)
            {
                return;
            }

            PlayerControl playerControlAbovePointsThreshold = singSceneControl
                .PlayerControls
                .FirstOrDefault(playerControl => playerControl.PlayerScoreControl.TotalScore >= pointsThreshold);
            if (playerControlAbovePointsThreshold != null)
            {
                Debug.Log($"Triggering finish because player '{playerControlAbovePointsThreshold.PlayerProfile?.Name}' has more than {pointsThreshold} points");
                singSceneFinisher.TriggerEarlySongFinish();
            }
        }
    }
}
