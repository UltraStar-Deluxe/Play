using System.Linq;
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
    [Multiline]
    private string helpTextKey;
    public string HelpTextKey
    {
        get => helpTextKey;
        set
        {
            helpTextKey = value;
            HelpText = Translation.Get(value);
        }
    }

    public Translation HelpText
    {
        get => TooltipControl.TooltipText;
        set => TooltipControl.TooltipText = Translation.Of(value);
    }

    [UxmlAttribute]
    public float TargetElementMarginRight
    {
        get => targetElementMarginRight;
        set
        {
            targetElementMarginRight = value;
            UpdateTargetElement();
        }
    }
    private float targetElementMarginRight = 22f; // own width + margin-left + margin-right

    [UxmlAttribute]
    public bool TargetAboveSiblingElement {
        get => targetAboveSiblingElement;
        set
        {
            targetAboveSiblingElement = value;
            UpdateTargetElement();
        }
    }

    private bool targetAboveSiblingElement;
    private VisualElement targetElement;

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

        TooltipControl = new TooltipControl(this, Translation.Empty, false);

        this.RegisterCallbackButtonTriggered(evt => TooltipControl.ShowTooltip(new Vector2(worldBound.center.x, worldBound.yMax)));
        this.RegisterCallback<BlurEvent>(evt => TooltipControl.CloseTooltip());

        this.RegisterCallback<AttachToPanelEvent>(evt => UpdateTargetElement());
    }

    private void UpdateTargetElement()
    {
        SetTargetElement(targetAboveSiblingElement ? GetAboveSiblingElement() : null);
    }

    private void SetTargetElement(VisualElement newTargetElement)
    {
        if (targetElement != null)
        {
            targetElement.style.marginRight = new StyleLength(StyleKeyword.None);
            targetElement.UnregisterCallback<GeometryChangedEvent>(UpdatePosition);
            targetElement.MarkDirtyRepaint();
        }
        targetElement = newTargetElement;

        if (targetElement != null)
        {
            // Create space on right side of target
            targetElement.style.marginRight = targetElementMarginRight;
            targetElement.RegisterCallback<GeometryChangedEvent>(UpdatePosition);
            targetElement.MarkDirtyRepaint();
        }

        UpdatePosition(null);
    }

    private void UpdatePosition(GeometryChangedEvent geometryChangedEvent)
    {
        if (targetElement == null)
        {
            // Reset position to be relative
            style.position = new StyleEnum<Position>(StyleKeyword.None);
            style.top = new StyleLength(StyleKeyword.None);
            style.left = new StyleLength(StyleKeyword.None);
            MarkDirtyRepaint();
            return;
        }

        // Move to right side of target via absolute position
        style.position = new StyleEnum<Position>(Position.Absolute);
        VisualElementUtils.SetAbsoluteWorldBoundPosition(this,
            new Vector2(targetElement.worldBound.xMax, targetElement.worldBound.yMin));
        MarkDirtyRepaint();
    }

    private VisualElement GetAboveSiblingElement()
    {
        if (parent == null)
        {
            return null;
        }

        int indexOfAboveSibling = parent.IndexOf(this) - 1;
        return CollectionUtils.SafeGet(parent.Children().ToList(), indexOfAboveSibling, null);
    }
}
