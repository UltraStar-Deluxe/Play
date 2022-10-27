using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public static class EnumFlagsDialog
{
    public static void ShowDialog<T>(VisualElement root, string title, T currentValue, Action<T> setFlag, Action<T> clearFlag) where T : Enum
    {
        var background = new VisualElement
        {
            style =
            {
                backgroundColor = new StyleColor(new Color(0, 0, 0, 0.2f)),
                width = new StyleLength(new Length(100, LengthUnit.Percent)),
                height = new StyleLength(new Length(100, LengthUnit.Percent)), position = Position.Absolute,
                alignItems = Align.Center,
                justifyContent = Justify.Center,
                flexDirection = FlexDirection.Row
            }
        };

        var dialogUxml = Resources.Load<VisualTreeAsset>("UIDocuments/EnumFlagsDialog");
        var dialogVe = dialogUxml.Instantiate().contentContainer.Children().First();
        dialogVe.style.alignSelf = Align.Center;
        background.Add(dialogVe);

        dialogVe.Q<Label>("dialogTitle").text = title;
        dialogVe.Q<Button>("okButton").RegisterCallbackButtonTriggered(() =>
        {
            background.RemoveFromHierarchy();
        });

        dialogVe.style.height = new StyleLength(new Length(Screen.height * 0.8f, LengthUnit.Pixel));

        var entryUxml = Resources.Load<VisualTreeAsset>("UIDocuments/EnumFlagsEntry");
        var entryContainer = dialogVe.Q<ScrollView>("dialogContentContainer");
        foreach (T flag in Enum.GetValues(typeof(T)))
        {
            var entryVe = entryUxml.Instantiate().contentContainer.Children().First();
            entryVe.Q<Label>().text = flag.TranslatedName();

            var toggle = entryVe.Q<Toggle>();
            toggle.SetValueWithoutNotify(currentValue.HasFlag(flag));
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue) setFlag(flag);
                else clearFlag(flag);
            });
            entryContainer.Add(entryVe);
        }

        root.Add(background);
        background.Q<Toggle>().Focus();
    }
}
