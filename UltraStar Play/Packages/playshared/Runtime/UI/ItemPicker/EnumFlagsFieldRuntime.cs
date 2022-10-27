using System;
using System.Linq;
using ProTrans;
using UnityEngine;
using UnityEngine.UIElements;

public static class EnumExtensions
{
    public static object SetFlag(this Enum enumValue, Enum flag)
    {
        return (int)(object)enumValue | (int)(object)flag;
    }
    public static object ClearFlag(this Enum enumValue, Enum flag)
    {
        return (int)(object)enumValue & ~(int)(object)flag;
    }

    public static string TranslatedName(this Enum enumValue)
    {
        return TranslationManager.GetTranslation($"{enumValue.GetType().Name}_{enumValue.ToString()}");
    }
}

public class EnumFlagsFieldRuntime<T> : VisualElement where T : Enum
{
    Label itemLabel;
    Label valueLabel;
    T[] enumValues;
    Func<T> getCurrentValue;

    Label ValueLabel => valueLabel ??= this.Q<Label>("Value");
    public Label ItemLabel => itemLabel ??= this.Q<Label>("Label");

    public void Bind(VisualElement root, Func<T> getValue, Action<T> setValue)
    {
        this.getCurrentValue = getValue;
        enumValues = (T[])Enum.GetValues(typeof(T));

        this.Q<Button>().RegisterCallbackButtonTriggered(() =>
        {
            EnumFlagsDialog.ShowDialog(root, ItemLabel.text,
                getValue(),
                flag =>
                {
                    setValue((T)getValue().SetFlag(flag));
                    UpdateValueLabel();
                },
                flag =>
                {
                    setValue((T)getValue().ClearFlag(flag));
                    UpdateValueLabel();
                });
        });

        UpdateValueLabel();
    }

    void UpdateValueLabel()
    {
        if (getCurrentValue == null) throw new Exception($"Can't update Value Label if the {nameof(EnumFlagsFieldRuntime<T>)} has not been bound.");

        T value = this.getCurrentValue();

        if ((int)(object)value == 0)
        {
            ValueLabel.text = "None";
            ValueLabel.tooltip = null;
            return;
        }

        foreach (T flag in enumValues)
        {
            if ((int)(object)value == (int)(object)flag)
            {
                ValueLabel.text = value.TranslatedName();
                ValueLabel.tooltip = null;
                return;
            }
        }

        ValueLabel.text = "Multiple values...";
        ValueLabel.tooltip = string.Join(", ", enumValues.Where(item => value.HasFlag(item)));
    }

    public EnumFlagsFieldRuntime()
    {
        if (!typeof(T).IsEnum) throw new Exception($"Can't bind an EnumFlagsField to a non-enum type ('{typeof(T)}').");

        // UI
        string path = "UIDocuments/EnumFlagsField";
        VisualTreeAsset uxmlFile = Resources.Load<VisualTreeAsset>(path);
        if (uxmlFile == null)
        {
            Debug.LogError("Could not load " + path);
            return;
        }
        this.Add(uxmlFile.Instantiate().contentContainer.Children().First());
    }
}
