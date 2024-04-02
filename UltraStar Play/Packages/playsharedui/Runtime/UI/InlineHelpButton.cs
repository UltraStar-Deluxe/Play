using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class InlineHelpButton : Button
{
    [UxmlAttribute]
    public string MaterialIconName
    {
        get => MaterialIconElement.Icon;
        set => MaterialIconElement.Icon = value;
    }

    [UxmlAttribute]
    public string HelpText
    {
        get => TooltipControl.TooltipText;
        set => TooltipControl.TooltipText = value;
    }

    public MaterialIcon MaterialIconElement { get; private set; }
    public TooltipControl TooltipControl { get; private set; }

    public InlineHelpButton()
    {
        AddToClassList("inlineHelpButton");
        AddToClassList("transparentButton");

        MaterialIconElement = new MaterialIcon()
        {
            pickingMode = PickingMode.Ignore,
            Icon = "info_outline",
        };
        Add(MaterialIconElement);

        TooltipControl = new TooltipControl(this, "", false);

        this.RegisterCallbackButtonTriggered(evt =>
            TooltipControl.ShowTooltip(new Vector2(worldBound.center.x, worldBound.yMax)));
        this.RegisterCallback<BlurEvent>(evt =>
            TooltipControl.CloseTooltip());
    }
}
