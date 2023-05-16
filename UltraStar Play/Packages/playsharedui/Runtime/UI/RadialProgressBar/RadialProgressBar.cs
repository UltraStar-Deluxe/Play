using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/**
 * An element that displays progress inside a partially filled circle
 */
public class RadialProgressBar : VisualElement
{
    public new class UxmlFactory : UxmlFactory<RadialProgressBar, UxmlTraits> { }
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlFloatAttributeDescription Progress = new UxmlFloatAttributeDescription() { name = "progress", defaultValue = 0 };
        UxmlBoolAttributeDescription ShowLabel = new UxmlBoolAttributeDescription() { name = "show-label", defaultValue = false };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);

            RadialProgressBar target = ve as RadialProgressBar;
            target.ProgressInPercent = Progress.GetValueFromBag(bag, cc);
            target.ShowLabel = ShowLabel.GetValueFromBag(bag, cc);
        }
    }
    
    // These are USS class names for the control overall and the label.
    private static readonly string ussClassName = "radial-progress-bar";
    private static readonly string ussLabelClassName = "radial-progress-bar__label";

    // These objects allow C# code to access custom USS properties.
    static readonly CustomStyleProperty<Color> trackColorStyle = new("--track-color");
    static readonly CustomStyleProperty<Color> progressColorStyle = new("--progress-color");
    static readonly CustomStyleProperty<float> borderSizeStyle = new("--border-size");
    static readonly CustomStyleProperty<bool> roundLineCapStyle = new("--round-line-cap");

    // This is the label that displays the percentage.
    private Label labelElement;

    /// <summary>
    /// A value between 0 and 100
    /// </summary>
    private float progressInPercent;
    public float ProgressInPercent
    {
        // The progress property is exposed in C#.
        get => progressInPercent;
        set
        {
            // Whenever the progress property changes, MarkDirtyRepaint() is named. This causes a call to the
            // generateVisualContents callback.
            progressInPercent = value;
            labelElement.text = Mathf.Clamp(Mathf.Round(value), 0, 100) + "%";
            MarkDirtyRepaint();
        }
    }

    public float Value
    {
        get => ProgressInPercent;
        set => ProgressInPercent = value;
    }

    private bool overwriteStrokeWidth;
    private float strokeWidth;
    public float StrokeWidth
    {
        get => strokeWidth;
        set
        {
            overwriteStrokeWidth = true;
            strokeWidth = value;
            MarkDirtyRepaint();
        }
    }
    
    public bool ShowLabel
    {
        get => labelElement.IsVisibleByDisplay();
        set => labelElement.SetVisibleByDisplay(value);
    }

    private bool overwriteLineCap;
    private LineCap lineCap;
    public LineCap LineCap
    {
        get => lineCap;
        set
        {
            overwriteLineCap = true;
            lineCap = value;
            MarkDirtyRepaint();
        }
    }

    private bool overwriteProgressColor;
    private Color progressColor;
    public Color ProgressColor
    {
        get => progressColor;
        set
        {
            overwriteProgressColor = true;
            progressColor = value;
            MarkDirtyRepaint();
        }
    }
    
    private bool overwriteTrackColor;
    private Color trackColor;
    public Color TrackColor
    {
        get => trackColor;
        set
        {
            overwriteTrackColor = true;
            trackColor = value;
            MarkDirtyRepaint();
        }
    }
    
    public readonly float highValue = 100;
    public readonly float lowValue = 0;
    
    // This default constructor is RadialProgressBar's only constructor.
    public RadialProgressBar()
    {
        // Create a Label, add a USS class name, and add it to this visual tree.
        labelElement = new Label();
        labelElement.AddToClassList(ussLabelClassName);
        Add(labelElement);

        // Add the USS class name for the overall control.
        AddToClassList(ussClassName);

        // Register a callback after custom style resolution.
        RegisterCallback<CustomStyleResolvedEvent>(evt => CustomStylesResolved(evt));

        // Register a callback to generate the visual content of the control.
        generateVisualContent = OnGenerateVisualContent;
        
        ProgressInPercent = 0.0f;
    }

    static void CustomStylesResolved(CustomStyleResolvedEvent evt)
    {
        RadialProgressBar element = (RadialProgressBar)evt.currentTarget;
        element.UpdateCustomStyles();
    }

    // After the custom colors are resolved, this method uses them to color the meshes and (if necessary) repaint
    // the control.
    void UpdateCustomStyles()
    {
        if (!overwriteProgressColor
            && customStyle.TryGetValue(progressColorStyle, out Color newProgressColor))
        {
            progressColor = newProgressColor;
        }

        if (!overwriteTrackColor
            && customStyle.TryGetValue(trackColorStyle, out Color newTrackColor))
        {
            trackColor = newTrackColor;
        }

        if (!overwriteStrokeWidth
            && customStyle.TryGetValue(borderSizeStyle, out float newBorderSize))
        {
            strokeWidth = newBorderSize;
        }
        
        if (!overwriteLineCap
            && customStyle.TryGetValue(roundLineCapStyle, out bool newRoundLineCap))
        {
            lineCap = newRoundLineCap ? LineCap.Round : LineCap.Butt;
        }
        
        MarkDirtyRepaint();
    }

    void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Painter2D painter = mgc.painter2D;
        float radius = (contentRect.width / 2) - (StrokeWidth / 2);
        float startAngle = -90;
        float endAngle = startAngle + (359.9999f * ProgressInPercent / 100);
        Vector2 center = contentRect.center;
        
        // Draw Track
        painter.BeginPath();
        painter.strokeColor = TrackColor;
        painter.lineWidth = StrokeWidth;
        painter.lineCap = LineCap;
        painter.Arc(contentRect.center, radius, 0, 360);
        painter.Stroke();
        
        // Draw Progress
        if (ProgressInPercent > 0)
        {
            painter.BeginPath();
            painter.strokeColor = ProgressColor;
            painter.lineWidth = StrokeWidth;
            painter.lineCap = LineCap;
            painter.Arc(center, radius, startAngle, endAngle);
            painter.Stroke();
        }
    }
}
