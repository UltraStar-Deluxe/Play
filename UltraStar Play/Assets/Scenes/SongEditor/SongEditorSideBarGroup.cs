using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class SongEditorSideBarGroup : VisualElement
{
    // Parent of nested elements.
    public override VisualElement contentContainer { get; }

    [UxmlAttribute]
    public string Label
    {
        get => accordionItem != null ? accordionItem.Title : "";
        set
        {
            if (accordionItem != null)
            {
                accordionItem.Title = value;
            }
        }
    }

    private readonly AccordionItem accordionItem;

    public SongEditorSideBarGroup()
    {
        // Load UXML and add as child element
        string path = "SongEditorSideBarGroupUi";
        var visualTreeAsset = Resources.Load<VisualTreeAsset>(path);
        if (visualTreeAsset == null)
        {
            Debug.LogError("Could not load " + path);
            return;
        }
        visualTreeAsset.CloneTree(this);

        accordionItem = this.Q<AccordionItem>();
        contentContainer = this.Q<VisualElement>("accordionItemContent");
        Label = "Group Title";
    }
}
