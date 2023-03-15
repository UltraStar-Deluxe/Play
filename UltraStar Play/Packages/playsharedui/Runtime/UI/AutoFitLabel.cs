using UnityEngine;
using UnityEngine.UIElements;

public class AutoFitLabel : Label
{
    public new class UxmlFactory : UxmlFactory<AutoFitLabel, UxmlTraits> { }

    public new class UxmlTraits : Label.UxmlTraits
    {
        readonly UxmlFloatAttributeDescription minFontSizeInPx = new() { name = "min-font-size", defaultValue = 10, restriction = new UxmlValueBounds { min = "1" } };
        readonly UxmlFloatAttributeDescription maxFontSizeInPx = new() { name = "max-font-size", defaultValue = 50, restriction = new UxmlValueBounds { min = "1" } };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);

            AutoFitLabel instance = ve as AutoFitLabel;
            instance.MinFontSizeInPx = Mathf.Max(minFontSizeInPx.GetValueFromBag(bag, cc), 1);
            instance.MaxFontSizeInPx = Mathf.Max(maxFontSizeInPx.GetValueFromBag(bag, cc), 1);
        }
    }

    public float MinFontSizeInPx
    {
        get => autoFitLabelControl.MinFontSizeInPx;
        set => autoFitLabelControl.MinFontSizeInPx = value;
    }

    public float MaxFontSizeInPx 
    {
        get => autoFitLabelControl.MaxFontSizeInPx;
        set => autoFitLabelControl.MaxFontSizeInPx = value;
    }

    private readonly AutoFitLabelControl autoFitLabelControl;

    public AutoFitLabel()
    {
        autoFitLabelControl = new(this);
    }
}
