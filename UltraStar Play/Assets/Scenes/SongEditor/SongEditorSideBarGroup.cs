using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class SongEditorSideBarGroup : VisualElement
{
    // UIToolkit factory class
    public new class UxmlFactory : UxmlFactory<SongEditorSideBarGroup, UxmlTraits> {};
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        // Additional XML attributes
        private readonly UxmlStringAttributeDescription label = new() { name = "label", defaultValue = "Group Title"};

        public override void Init(VisualElement visualElement, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(visualElement, bag, cc);
            var target = visualElement as SongEditorSideBarGroup;

            // Read additional attributes from XML.
            // In the UIBuilder, the XML attributes and target object fields are synchronized implicitly by name.
            target.Label = label.GetValueFromBag(bag, cc);
        }
    }

    // Parent of nested elements.
    public override VisualElement contentContainer { get; }

    public string Label
    {
        get => labelElement.text;
        set => labelElement.text = value;
    }

    private readonly Label labelElement;

    public SongEditorSideBarGroup()
    {
        // Load UXML and add as child element
        const string path = "SongEditorSideBarGroupUi";
        var visualTreeAsset = Resources.Load<VisualTreeAsset>(path);
        if (visualTreeAsset == null)
        {
            Debug.LogError("Could not load " + path);
            return;
        }
        visualTreeAsset.CloneTree(this);

        VisualElement groupContainer = this.Q<VisualElement>("groupContainer");
        groupContainer.AddToClassList("songEditorSideBarGroupContentContainer");
        contentContainer = groupContainer;
        labelElement = this.Q<Label>("groupTitle");
        Label = "Group Title";
    }
}
