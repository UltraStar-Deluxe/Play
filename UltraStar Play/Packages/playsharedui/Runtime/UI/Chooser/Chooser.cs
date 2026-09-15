using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class Chooser : VisualElement
{
    [UxmlAttribute("wrap-around")]
    public bool WrapAround { get; set; } = true;
    [UxmlAttribute("min-value")]
    public double MinValue { get; set; } = double.MinValue;
    [UxmlAttribute("max-value")]
    public double MaxValue { get; set; } = double.MaxValue;
    [UxmlAttribute("step-value")]
    public double StepValue { get; set; } = 1;

    [UxmlAttribute("label")]
    public string Label
    {
        get => LabelElement.text;
        set => LabelElement.text = value;
    }

    [UxmlAttribute("no-previous-button")]
    public bool NoPreviousButton
    {
        get => !PreviousItemButton.IsVisibleByDisplay();
        set => PreviousItemButton.SetVisibleByDisplay(!value);
    }

    [UxmlAttribute("no-next-button")]
    public bool NoNextButton
    {
        get => !NextItemButton.IsVisibleByDisplay();
        set => NextItemButton.SetVisibleByDisplay(!value);
    }

    public Button NextItemButton { get; private set; }
    public Button PreviousItemButton { get; private set; }
    public Label ItemLabel { get; private set; }
    public Image ItemImage { get; private set; }

    public Label LabelElement { get; private set; }

    private object control;

    public Chooser(string label) : this()
    {
        Label = label;
    }
    
    public Chooser()
    {
        // Load UXML and add as child element
        string path = "UIDocuments/Chooser";
        VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>(path);
        if (visualTreeAsset == null)
        {
            Debug.LogError("Could not load " + path);
            return;
        }
        visualTreeAsset.CloneTree(this);

        LabelElement = this.Q<Label>(R_PlayShared.UxmlNames.chooserLabel);
        LabelElement.RegisterValueChangedCallback(evt => UpdateLabelDisplay());
        LabelElement.RegisterCallback<GeometryChangedEvent>(evt => UpdateLabelDisplay());
        LabelElement.RegisterCallback<AttachToPanelEvent>(evt => UpdateLabelDisplay());
        
        ItemLabel = this.Q<Label>(R_PlayShared.UxmlNames.itemLabel);
        ItemImage = this.Q<Image>(R_PlayShared.UxmlNames.itemImage);
        
        PreviousItemButton = this.Q<Button>(R_PlayShared.UxmlNames.previousItemButton);
        
        NextItemButton = this.Q<Button>(R_PlayShared.UxmlNames.nextItemButton);
        
        UpdateLabelDisplay();
    }

    public virtual void InitControl(object chooserControl)
    {
        if (control != null)
        {
            throw new UnityException("Already initialized");
        }
        control = chooserControl;
    }
    
    private void UpdateLabelDisplay()
    {
        LabelElement.SetVisibleByDisplay(!LabelElement.text.IsNullOrEmpty());
    }
}
