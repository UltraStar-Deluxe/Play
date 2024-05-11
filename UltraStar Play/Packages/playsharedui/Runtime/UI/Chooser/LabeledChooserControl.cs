using System;
using System.Collections.Generic;
using UniRx;

public class LabeledChooserControl<T> : ListedChooserControl<T>
{
    private readonly string smallFontUssClass = "smallFont";
    private readonly Func<T, Translation> getLabelTextFunction;

    public bool AutoSmallFont { get; set; } = true;

    public LabeledChooserControl(Chooser chooser, List<T> items,
         Func<T, Translation> getLabelTextFunction)
        : base(chooser)
    {
        this.getLabelTextFunction = getLabelTextFunction;
        Selection.Subscribe(UpdateLabelText);
        Items = items;
        UpdateLabelText(SelectedItem);
    }

    public void UpdateLabelText()
    {
        UpdateLabelText(SelectedItem);
    }

    private void UpdateLabelText(T item)
    {
        Chooser.ItemLabel.SetTranslatedText(getLabelTextFunction(item));

        if (AutoSmallFont)
        {
            if (Chooser.ItemLabel.text.Length > 28
                || Chooser.ItemLabel.text.Contains("\n"))
            {
                Chooser.ItemLabel.AddToClassListIfNew(smallFontUssClass);
            }
            else
            {
                Chooser.ItemLabel.RemoveFromClassList(smallFontUssClass);
            }
        }
    }
}
