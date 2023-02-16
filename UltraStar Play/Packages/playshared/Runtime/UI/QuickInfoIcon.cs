using UnityEngine.UIElements;

public class QuickInfoIcon : MaterialIcon
{
    public new class UxmlFactory : UxmlFactory<QuickInfoIcon, UxmlTraits> { }

    public new class UxmlTraits : Label.UxmlTraits
    {
        readonly UxmlStringAttributeDescription tooltipText = new() { name = "tooltip-text", defaultValue = "" };
        readonly UxmlStringAttributeDescription icon = new() { name = "icon", defaultValue = "help" };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);

            QuickInfoIcon target = ve as QuickInfoIcon;
            target.TooltipText = tooltipText.GetValueFromBag(bag, cc);
            target.Icon = icon.GetValueFromBag(bag, cc);
        }
    }

    public string TooltipText
    {
        get => tooltipControl.TooltipText;
        set => tooltipControl.TooltipText = value;
    }

    private readonly TooltipControl tooltipControl;

    public QuickInfoIcon()
    {
        tooltipControl = new(this);
        Icon = "help";
    }
}
