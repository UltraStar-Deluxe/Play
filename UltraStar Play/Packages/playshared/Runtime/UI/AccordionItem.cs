using UnityEngine;
using UnityEngine.UIElements;

public class AccordionItem : VisualElement
{
    // UIToolkit factory class
    public new class UxmlFactory : UxmlFactory<AccordionItem, UxmlTraits> {};
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        // Additional XML attributes
        private readonly UxmlBoolAttributeDescription contentVisible = new() { name = "content-visible", defaultValue = true};
        private readonly UxmlStringAttributeDescription title = new() { name = "label", defaultValue = "Title"};

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            AccordionItem target = ve as AccordionItem;

            // Read additional attributes from XML
            // In the UIBuilder, the XML attributes and target object fields are synchronized implicitly by name.
            target.ContentVisible = contentVisible.GetValueFromBag(bag, cc);
            target.Title = title.GetValueFromBag(bag, cc);
            target.UpdateTargetHeight();
        }
    }

    public bool ContentVisible
    {
        get { return this.ClassListContains("expanded"); }
        set
        {
            if (value)
            {
                ShowAccordionContent();
            }
            else
            {
                HideAccordionContent();
            }
        }
    }

    public string Title
    {
        get { return TitleElement.text; }
        set { TitleElement.text = value; }
    }
    
    public override VisualElement contentContainer => ContentElement;
    
    private Label TitleElement { get; set; }
    private Button ToggleContentButton { get; set; }
    private VisualElement ContentElement { get; set; }
    
    private float targetContentHeight = -1;
    private bool shouldUpdateTargetHeight;
    
    private object control;

    public AccordionItem()
        : this("Title")
    {
    }
    
    public AccordionItem(string title)
    {
        // Load UXML and add as child element
        string path = "UIDocuments/AccordionItemUi";
        VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>(path);
        if (visualTreeAsset == null)
        {
            Debug.LogError("Could not load " + path);
            return;
        }
        visualTreeAsset.CloneTree(this);

        TitleElement = this.Q<Label>(R_PlayShared.UxmlNames.accordionItemTitle);
        ToggleContentButton = this.Q<Button>(R_PlayShared.UxmlNames.toggleAccordionItemContentButton);
        ContentElement = this.Q<VisualElement>(R_PlayShared.UxmlNames.accordionItemContent);
        
        ToggleContentButton.RegisterCallbackButtonTriggered(_ => ToggleContentVisible());

        ContentElement.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            if (!shouldUpdateTargetHeight)
            {
                return;
            }
            shouldUpdateTargetHeight = false;

            targetContentHeight = evt.newRect.height;
            if (!ContentVisible)
            {
                ContentElement.style.height = 0;
            }
            else
            {
                ContentElement.style.height = targetContentHeight;
            }
        });

        Title = title;
        
        UpdateTargetHeight();
    }

    public virtual void InitControl(object AccordionItemControl)
    {
        if (control != null)
        {
            throw new UnityException("Already initialized");
        }
        control = AccordionItemControl;
    }
    
    private void ToggleContentVisible()
    {
        if (ContentVisible)
        {
            HideAccordionContent();
        }
        else
        {
            ShowAccordionContent();
        }
    }

    public virtual void ShowAccordionContent()
    {
        if (ContentVisible)
        {
            return;
        }
        
        this.AddToClassList("expanded");
        if (targetContentHeight >= 0)
        {
            ContentElement.style.height = targetContentHeight;
        }

        if (parent is AccordionGroup accordionGroup)
        {
            accordionGroup.OnAccordionItemContentVisibleChanged(this);
        }
    }

    public virtual void HideAccordionContent()
    {
        if (!ContentVisible)
        {
            return;
        }

        this.RemoveFromClassList("expanded");
        if (targetContentHeight >= 0)
        {
            ContentElement.style.height = 0;
        }
        
        if (parent is AccordionGroup accordionGroup)
        {
            accordionGroup.OnAccordionItemContentVisibleChanged(this);
        }
    }

    public void UpdateTargetHeight()
    {
        ContentElement.style.height = new StyleLength(StyleKeyword.Auto);
        shouldUpdateTargetHeight = true;
    }
}
