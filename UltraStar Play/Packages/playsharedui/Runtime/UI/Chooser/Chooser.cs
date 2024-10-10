using UnityEngine;
using UnityEngine.UIElements;

public class Chooser : VisualElement
{
    // UIToolkit factory class
    public new class UxmlFactory : UxmlFactory<Chooser, UxmlTraits> {};
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        // Additional XML attributes
        private readonly UxmlBoolAttributeDescription wrapAround = new() { name = "wrap-around", defaultValue = true};
        private readonly UxmlDoubleAttributeDescription minValue = new() { name = "min-value", defaultValue = double.MinValue};
        private readonly UxmlDoubleAttributeDescription maxValue = new() { name = "max-value", defaultValue = double.MaxValue};
        private readonly UxmlDoubleAttributeDescription stepValue = new() { name = "step-value", defaultValue = 1};
        private readonly UxmlBoolAttributeDescription noPreviousButton = new() { name = "no-previous-button", defaultValue = false};
        private readonly UxmlBoolAttributeDescription noNextButton = new() { name = "no-next-button", defaultValue = false};
        private readonly UxmlStringAttributeDescription label = new() { name = "label", defaultValue = ""};

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            Chooser target = ve as Chooser;

            // Read additional attributes from XML
            // In the UIBuilder, the XML attributes and target object fields are synchronized implicitly by name.
            target.WrapAround = wrapAround.GetValueFromBag(bag, cc);
            target.MinValue = minValue.GetValueFromBag(bag, cc);
            target.MaxValue = maxValue.GetValueFromBag(bag, cc);
            target.StepValue = stepValue.GetValueFromBag(bag, cc);
            target.NoPreviousButton = noPreviousButton.GetValueFromBag(bag, cc);
            target.NoNextButton = noNextButton.GetValueFromBag(bag, cc);
            target.Label = label.GetValueFromBag(bag, cc);
        }
    }

    public bool WrapAround { get; set; } = true;
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public double StepValue { get; set; }
    public string Label
    {
        get => LabelElement.text;
        set
        {
            LabelElement.text = value;
            if (LabelElement.text.IsNullOrEmpty())
            {
                LabelElement.HideByDisplay();
            }
        }
    }

    public bool NoPreviousButton
    {
        get => !PreviousItemButton.IsVisibleByDisplay();
        set => PreviousItemButton.SetVisibleByDisplay(!value);
    }

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
        ItemLabel = this.Q<Label>(R_PlayShared.UxmlNames.itemLabel);
        ItemImage = this.Q<Image>(R_PlayShared.UxmlNames.itemImage);
        PreviousItemButton = this.Q<Button>(R_PlayShared.UxmlNames.previousItemButton);
        NextItemButton = this.Q<Button>(R_PlayShared.UxmlNames.nextItemButton);
    }

    public virtual void InitControl(object chooserControl)
    {
        if (control != null)
        {
            throw new UnityException("Already initialized");
        }
        control = chooserControl;
    }
}
