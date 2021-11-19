using UnityEngine;
using UnityEngine.UIElements;

public class ItemPicker : VisualElement
{
    public bool wrapAround;
    public double minValue;
    public double maxValue;
    public double stepValue;

    private Button nextItemButton;
    public Button NextItemButton
    {
        get
        {
            if (nextItemButton == null)
            {
                nextItemButton = this.Q<Button>(R.UxmlNames.nextItemButton);
            }
            return nextItemButton;
        }
    }

    private Button previousItemButton;
    public Button PreviousItemButton
    {
        get
        {
            if (previousItemButton == null)
            {
                previousItemButton = this.Q<Button>(R.UxmlNames.previousItemButton);
            }
            return previousItemButton;
        }
    }

    private Label itemLabel;
    public Label ItemLabel
    {
        get
        {
            if (itemLabel == null)
            {
                itemLabel = this.Q<Label>(R.UxmlNames.itemLabel);
            }
            return itemLabel;
        }
    }

    private object control;

    public virtual void InitControl(object itemPickerControl)
    {
        if (control != null)
        {
            throw new UnityException("Already initialized");
        }
        control = itemPickerControl;
    }

    // UIToolkit factory classes
    public new class UxmlFactory : UxmlFactory<ItemPicker, UxmlTraits> {};

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        // Additional XML attributes
        private readonly UxmlBoolAttributeDescription wrapAround = new UxmlBoolAttributeDescription { name = "wrapAround", defaultValue = false};
        private readonly UxmlDoubleAttributeDescription minValue = new UxmlDoubleAttributeDescription { name = "minValue", defaultValue = double.MinValue};
        private readonly UxmlDoubleAttributeDescription maxValue = new UxmlDoubleAttributeDescription { name = "maxValue", defaultValue = double.MaxValue};
        private readonly UxmlDoubleAttributeDescription stepValue = new UxmlDoubleAttributeDescription { name = "stepValue", defaultValue = 1};

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            ItemPicker target = ve as ItemPicker;

            // Read additional attributes from XML
            target.wrapAround = wrapAround.GetValueFromBag(bag, cc);
            target.minValue = minValue.GetValueFromBag(bag, cc);
            target.maxValue = maxValue.GetValueFromBag(bag, cc);
            target.stepValue = stepValue.GetValueFromBag(bag, cc);

            // Load UXML and add as child element
            string path = "UIDocuments/ItemPicker";
            VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>(path);
            if (visualTreeAsset == null)
            {
                Debug.LogError("Could not load " + path);
                return;
            }
            TemplateContainer itemPickerVisualElement = visualTreeAsset.CloneTree();
            target.Add(itemPickerVisualElement);
        }
    }
}
