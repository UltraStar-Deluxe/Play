using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class SongEditorSideBarGroup : VisualElement
{
    public override VisualElement contentContainer => this.Q<VisualElement>("groupContainer");

    // UIToolkit factory classes
    public new class UxmlFactory : UxmlFactory<SongEditorSideBarGroup, UxmlTraits> {};

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        // Additional XML attributes
        private readonly UxmlStringAttributeDescription label = new() { name = "label", defaultValue = "Group Title"};

        public override void Init(VisualElement visualElement, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(visualElement, bag, cc);
            SongEditorSideBarGroup target = visualElement as SongEditorSideBarGroup;

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
                .ForEach(child => target.hierarchy.Add(child));

            target.Q<Label>("groupTitle").text = label.GetValueFromBag(bag, cc);
        }
    }
}
