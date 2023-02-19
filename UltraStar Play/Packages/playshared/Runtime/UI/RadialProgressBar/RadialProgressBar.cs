using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/**
 * An element that displays progress inside a partially filled circle
 */
// https://docs.unity3d.com/Manual/UIE-radial-progress.html
public class RadialProgressBar : VisualElement
{
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        // The progress property is exposed to UXML.
        UxmlFloatAttributeDescription m_ProgressAttribute = new UxmlFloatAttributeDescription() { name = "progress" };
        UxmlBoolAttributeDescription m_ShowLabelAttribute = new UxmlBoolAttributeDescription() { name = "show-label", defaultValue = true};
        UxmlFloatAttributeDescription m_BorderSizeAttribute = new UxmlFloatAttributeDescription() { name = "border-size", defaultValue = 6f};

        // The Init method is used to assign to the C# progress property from the value of the progress UXML
        // attribute.
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);

            RadialProgressBar target = ve as RadialProgressBar;
            target.progress = m_ProgressAttribute.GetValueFromBag(bag, cc);
            target.showLabel = m_ShowLabelAttribute.GetValueFromBag(bag, cc);
            target.borderSize = m_BorderSizeAttribute.GetValueFromBag(bag, cc);
        }
    }

    // A Factory class is needed to expose this control to UXML.
    public new class UxmlFactory : UxmlFactory<RadialProgressBar, UxmlTraits> { }

    // These are USS class names for the control overall and the label.
    public static readonly string ussClassName = "radial-progress-bar";
    public static readonly string ussLabelClassName = "radial-progress-bar__label";

    // These objects allow C# code to access custom USS properties.
    static CustomStyleProperty<Color> s_TrackColor = new CustomStyleProperty<Color>("--track-color");
    static CustomStyleProperty<Color> s_ProgressColor = new CustomStyleProperty<Color>("--progress-color");

    // These are the meshes this control uses.
    EllipseMesh m_TrackMesh;
    EllipseMesh m_ProgressMesh;

    // This is the label that displays the percentage.
    Label m_Label;

    // This is the number of outer vertices to generate the circle.
    const int k_NumSteps = 200;

    // This is the number that the Label displays as a percentage.
    float m_Progress;

    /// <summary>
    /// A value between 0 and 100
    /// </summary>
    public float progress
    {
        // The progress property is exposed in C#.
        get => m_Progress;
        set
        {
            // Whenever the progress property changes, MarkDirtyRepaint() is named. This causes a call to the
            // generateVisualContents callback.
            m_Progress = value;
            m_Label.text = Mathf.Clamp(Mathf.Round(value), 0, 100) + "%";
            MarkDirtyRepaint();
        }
    }

    public float borderSize
    {
        get => m_ProgressMesh.borderSize;
        set
        {
            m_TrackMesh.borderSize = value;
            m_ProgressMesh.borderSize = value;
            MarkDirtyRepaint();
        }
    }
    
    public bool showLabel
    {
        get => m_Label.IsVisibleByDisplay();
        set => m_Label.SetVisibleByDisplay(value);
    }

    public float value
    {
        get => progress;
        set => progress = value;
    }

    private bool overwriteProgressColor;
    public Color progressColor
    {
        get => m_ProgressMesh.color;
        set
        {
            overwriteProgressColor = true;
            m_ProgressMesh.color = value;
            MarkDirtyRepaint();
        }
    }
    
    private bool overwriteTrackColor;
    public Color trackColor
    {
        get => m_TrackMesh.color;
        set
        {
            overwriteTrackColor = true;
            m_TrackMesh.color = value;
            MarkDirtyRepaint();
        }
    }
    
    public readonly float highValue = 100;
    public readonly float lowValue = 0;
    
    // This default constructor is RadialProgressBar's only constructor.
    public RadialProgressBar()
    {
        // Create a Label, add a USS class name, and add it to this visual tree.
        m_Label = new Label();
        m_Label.AddToClassList(ussLabelClassName);
        Add(m_Label);

        // Create meshes for the track and the progress.
        m_ProgressMesh = new EllipseMesh(k_NumSteps);
        m_TrackMesh = new EllipseMesh(k_NumSteps);

        // Add the USS class name for the overall control.
        AddToClassList(ussClassName);

        // Register a callback after custom style resolution.
        RegisterCallback<CustomStyleResolvedEvent>(evt => CustomStylesResolved(evt));

        // Register a callback to generate the visual content of the control.
        generateVisualContent += context => GenerateVisualContent(context);

        progress = 0.0f;
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
            && customStyle.TryGetValue(s_ProgressColor, out var progressColor))
        {
            m_ProgressMesh.color = progressColor;
        }

        if (!overwriteTrackColor
            && customStyle.TryGetValue(s_TrackColor, out var trackColor))
        {
            m_TrackMesh.color = trackColor;
        }

        if (m_ProgressMesh.isDirty || m_TrackMesh.isDirty)
        {
            MarkDirtyRepaint();
        }
    }

    // The GenerateVisualContent() callback method calls DrawMeshes().
    static void GenerateVisualContent(MeshGenerationContext context)
    {
        RadialProgressBar element = (RadialProgressBar)context.visualElement;
        element.DrawMeshes(context);
    }

    // DrawMeshes() uses the EllipseMesh utility class to generate an array of vertices and indices, for both the
    // "track" ring (in grey) and the progress ring (in green). It then passes the geometry to the MeshWriteData
    // object, as returned by the MeshGenerationContext.Allocate() method. For the "progress" mesh, only a slice of
    // the index arrays is used to progressively reveal parts of the mesh.
    void DrawMeshes(MeshGenerationContext context)
    {
        float halfWidth = contentRect.width * 0.5f;
        float halfHeight = contentRect.height * 0.5f;

        if (halfWidth < 2.0f || halfHeight < 2.0f)
            return;

        m_ProgressMesh.width = halfWidth;
        m_ProgressMesh.height = halfHeight;
        m_ProgressMesh.borderSize = borderSize;
        m_ProgressMesh.UpdateMesh();

        m_TrackMesh.width = halfWidth;
        m_TrackMesh.height = halfHeight;
        m_TrackMesh.borderSize = borderSize;
        m_TrackMesh.UpdateMesh();

        // Draw track mesh first
        var trackMeshWriteData = context.Allocate(m_TrackMesh.vertices.Length, m_TrackMesh.indices.Length);
        trackMeshWriteData.SetAllVertices(m_TrackMesh.vertices);
        trackMeshWriteData.SetAllIndices(m_TrackMesh.indices);

        // Keep progress between 0 and 100
        float clampedProgress = Mathf.Clamp(m_Progress, 0.0f, 100.0f);

        // Determine how many triangles are used to depending on progress, to achieve a partially filled circle
        int sliceSize = Mathf.FloorToInt((k_NumSteps * clampedProgress) / 100.0f);

        if (sliceSize == 0)
            return;

        // Every step is 6 indices in the corresponding array
        sliceSize *= 6;

        var progressMeshWriteData = context.Allocate(m_ProgressMesh.vertices.Length, sliceSize);
        progressMeshWriteData.SetAllVertices(m_ProgressMesh.vertices);

        var tempIndicesArray = new NativeArray<ushort>(m_ProgressMesh.indices, Allocator.Temp);
        progressMeshWriteData.SetAllIndices(tempIndicesArray.Slice(0, sliceSize));
        tempIndicesArray.Dispose();
    }

}
