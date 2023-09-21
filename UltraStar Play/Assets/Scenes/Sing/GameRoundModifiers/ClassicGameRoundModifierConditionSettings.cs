using System;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class ClassicGameRoundModifierConditionSettings
{
    public EClassicGameRoundModifierCondition condition = EClassicGameRoundModifierCondition.Always;

    /**
     * Range in percent (0 to 100).
     * x is from, y is until.
     */
    public Vector2Int conditionRangePercent = new(25, 75);

    public VisualElement CreateConfigurationVisualElement()
    {
        VisualElement root = new();

        MinMaxSlider timeRangeField = new();
        timeRangeField.lowLimit = 0;
        timeRangeField.highLimit = 100;
        timeRangeField.label = "Time Range %";
        FieldBindingUtils.Bind(timeRangeField,
            () => conditionRangePercent,
            newValue => conditionRangePercent = new Vector2Int((int)newValue.x, (int)newValue.y));

        MinMaxSlider scoreRangeField = new();
        scoreRangeField.lowLimit = 0;
        scoreRangeField.highLimit = 100;
        scoreRangeField.label = "Score Range %";
        FieldBindingUtils.Bind(scoreRangeField,
            () => conditionRangePercent,
            newValue => conditionRangePercent = new Vector2Int((int)newValue.x, (int)newValue.y));

        SliderInt playerAdvanceField = new();
        playerAdvanceField.lowValue = 0;
        playerAdvanceField.highValue = 100;
        playerAdvanceField.label = "Player Advance %";
        FieldBindingUtils.Bind(playerAdvanceField,
            () => conditionRangePercent.x,
            newValue => conditionRangePercent.x = newValue);

        void UpdateFields()
        {
            if (condition is EClassicGameRoundModifierCondition.TimeRange)
            {
                timeRangeField.value = conditionRangePercent;
            }
            else if (condition is EClassicGameRoundModifierCondition.ScoreRange)
            {
                scoreRangeField.value = conditionRangePercent;
            }
            else if (condition is EClassicGameRoundModifierCondition.PlayerAdvance)
            {
                playerAdvanceField.value = conditionRangePercent.x;
            }
        }
        UpdateFields();

        ItemPicker conditionChooser = new();
        conditionChooser.Label = "";
        EnumItemPickerControl<EClassicGameRoundModifierCondition> conditionChooserControl = new(conditionChooser);
        conditionChooserControl.Bind(
            () => condition,
            newValue =>
            {
                condition = newValue;
                timeRangeField.SetVisibleByDisplay(condition is EClassicGameRoundModifierCondition.TimeRange);
                scoreRangeField.SetVisibleByDisplay(condition is EClassicGameRoundModifierCondition.ScoreRange);
                playerAdvanceField.SetVisibleByDisplay(condition is EClassicGameRoundModifierCondition.PlayerAdvance);
                UpdateFields();
            });

        root.Add(conditionChooser);
        root.Add(timeRangeField);
        root.Add(scoreRangeField);
        root.Add(playerAdvanceField);

        return root;
    }

    public bool IsValueInRange(double valueInPercent)
    {
        return conditionRangePercent.x <= valueInPercent
               && valueInPercent <= conditionRangePercent.y;
    }
}
