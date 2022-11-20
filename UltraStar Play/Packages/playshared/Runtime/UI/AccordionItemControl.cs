using UniInject;
using UnityEngine.UIElements;

public class AccordionItemControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    protected VisualElement accordionItemRootVisualElement;

    [Inject(UxmlName = "accordionItemTitle")]
    protected Label accordionItemTitle;

    [Inject(UxmlName = "toggleAccordionItemContentButton")]
    protected Button toggleAccordionItemContentButton;

    [Inject(UxmlName = "accordionItemContent")]
    protected VisualElement accordionItemContent;

    public bool IsContentVisible => accordionItemRootVisualElement.ClassListContains("expanded");
    public VisualElement VisualElement => accordionItemRootVisualElement;

    public string Title
    {
        get
        {
            return accordionItemTitle.text;
        }
        set
        {
            accordionItemTitle.text = value;
        }
    }

    public void OnInjectionFinished()
    {
        toggleAccordionItemContentButton.RegisterCallbackButtonTriggered(() => ToggleContentVisible());
        accordionItemContent.Clear();
        HideAccordionContent();
    }

    private void ToggleContentVisible()
    {
        if (IsContentVisible)
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
        accordionItemRootVisualElement.AddToClassList("expanded");
    }

    public virtual void HideAccordionContent()
    {
        accordionItemRootVisualElement.RemoveFromClassList("expanded");
    }

    public void AddVisualElement(VisualElement visualElement)
    {
        accordionItemContent.Add(visualElement);
    }
}
