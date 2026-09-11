using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class LabelWithSpacer : VisualElement
{
    [UxmlAttribute("label-text")]
    public string LabelText
    {
        get => label != null ? label.text : "";
        set
        {
            if (label != null)
            {
                label.text = value;
            }
        }
    }

    private readonly Label label;

    public LabelWithSpacer()
    {
        // Load UXML and add as child element
        string path = "SongEditorSideBarGroupUi";
        VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>(path);
        if (visualTreeAsset == null)
        {
            Debug.LogError("Could not load " + path);
            return;
        }
        visualTreeAsset.CloneTree()
            .Children()
            .ToList()
            .ForEach(child => hierarchy.Add(child));

        label = this.Q<Label>();
        LabelText = "Group Title";
    }
}
