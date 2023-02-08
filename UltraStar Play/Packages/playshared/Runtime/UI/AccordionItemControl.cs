using System.Collections.Generic;
using UniInject;
using UnityEngine.UIElements;

public class AccordionItemControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    protected VisualElement accordionItemRootVisualElement;

    [Inject(UxmlName = R_PlayShared.UxmlNames.accordionItemTitle)]
    protected Label accordionItemTitle;

    [Inject(UxmlName = R_PlayShared.UxmlNames.toggleAccordionItemContentButton)]
    protected Button toggleAccordionItemContentButton;

    [Inject(UxmlName = R_PlayShared.UxmlNames.accordionItemContent)]
    protected VisualElement accordionItemContent;

    public bool IsContentVisible => accordionItemRootVisualElement.ClassListContains("expanded");
    public VisualElement VisualElement => accordionItemRootVisualElement;

    private float targetContentHeight = -1;

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
        accordionItemContent.style.height = targetContentHeight;
    }

    public virtual void HideAccordionContent()
    {
        accordionItemRootVisualElement.RemoveFromClassList("expanded");
        if (targetContentHeight >= 0)
        {
            accordionItemContent.style.height = 0;
        }
    }

    public void AddVisualElement(VisualElement visualElement, bool updateTargetHeight=true)
    {
        accordionItemContent.Add(visualElement);

        if (updateTargetHeight)
        {
            accordionItemContent.style.height = new StyleLength(StyleKeyword.Auto);
            accordionItemContent.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
            {
                targetContentHeight = evt.newRect.height;
                if (!IsContentVisible)
                {
                    accordionItemContent.style.height = 0;
                }
                else
                {
                    accordionItemContent.style.height = targetContentHeight;
                }
            });
        }
    }

    public void AddVisualElements(List<VisualElement> visualElements)
    {
        for (int i = 0; i < visualElements.Count; i++)
        {
            VisualElement visualElement = visualElements[i];
            // Update height if the last element is added
            bool updateTargetHeight = i >= visualElements.Count - 1;
            AddVisualElement(visualElement, updateTargetHeight);
        }
    }
}
