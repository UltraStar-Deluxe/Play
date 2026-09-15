using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class AutoFitLabel : Label
{
    [UxmlAttribute("min-font-size")]
    public float MinFontSizeInPx
    {
        get => autoFitLabelControl.MinFontSizeInPx;
        set => autoFitLabelControl.MinFontSizeInPx = Mathf.Max(value, 1);
    }

    [UxmlAttribute("max-font-size")]
    public float MaxFontSizeInPx 
    {
        get => autoFitLabelControl.MaxFontSizeInPx;
        set => autoFitLabelControl.MaxFontSizeInPx = Mathf.Max(value, 1);
    }

    private readonly AutoFitLabelControl autoFitLabelControl;

    public AutoFitLabel()
    {
        autoFitLabelControl = new(this);
    }
}
