using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class ChipsCombo : VisualElement
{
    [UxmlAttribute("label")]
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
