using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class LabelWithSpacer : VisualElement
{
    // UIToolkit factory classes
    public new class UxmlFactory : UxmlFactory<LabelWithSpacer, UxmlTraits> {};

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        // Additional XML attributes
        private readonly UxmlStringAttributeDescription labelText = new() { name = "label-text", defaultValue = "Group Title"};

        public override void Init(VisualElement visualElement, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(visualElement, bag, cc);
            LabelWithSpacer target = visualElement as LabelWithSpacer;

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

            target.Q<Label>().text = labelText.GetValueFromBag(bag, cc);
        }
    }
}
