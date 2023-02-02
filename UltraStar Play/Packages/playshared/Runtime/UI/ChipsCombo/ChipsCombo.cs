using UnityEngine;
using UnityEngine.UIElements;

public class ChipsCombo : VisualElement
{
    // UIToolkit factory class
    public new class UxmlFactory : UxmlFactory<ChipsCombo, UxmlTraits> {};
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        // Additional XML attributes
        private readonly UxmlStringAttributeDescription label = new() { name = "label", defaultValue = ""};

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            ChipsCombo target = ve as ChipsCombo;

            // Read additional attributes from XML
            // In the UIBuilder, the XML attributes and target object fields are synchronized implicitly by name.
            target.Label = label.GetValueFromBag(bag, cc);
        }
    }

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

    public Button ComboButton { get; private set; }
    public VisualElement ChipsList { get; private set; }
    private Label LabelElement { get; set; }

    private object control;

    public ChipsCombo()
    {
        // Load UXML and add as child element
        string path = "UIDocuments/ChipsCombo";
        VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>(path);
        if (visualTreeAsset == null)
        {
            Debug.LogError("Could not load " + path);
            return;
        }
        visualTreeAsset.CloneTree(this);

        LabelElement = this.Q<Label>("chipsComboLabel");
        ComboButton = this.Q<Button>("chipsComboButton");
        ChipsList = this.Q<VisualElement>("chipsComboChipsList");
    }

    public virtual void InitControl(object newControl)
    {
        if (control != null)
        {
            throw new UnityException("Already initialized");
        }
        control = newControl;
    }

    public void AddSeparator()
    {
        VisualElement separator = new();
        separator.AddToClassList("chipsComboListSeparator");
        ChipsList.Add(separator);
    }
}
