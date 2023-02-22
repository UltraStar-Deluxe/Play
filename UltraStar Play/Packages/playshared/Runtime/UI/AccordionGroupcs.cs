using System.Linq;
using UnityEngine.UIElements;

public class AccordionGroup : VisualElement
{
    // UIToolkit factory class
    public new class UxmlFactory : UxmlFactory<AccordionGroup, UxmlTraits> {};
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
    }

    private readonly SetFirstAndLastChildClassControl setFirstAndLastChildClassControl;

    public AccordionGroup()
    {
        setFirstAndLastChildClassControl = new(this);
    }

    public void OnAccordionItemContentVisibleChanged(AccordionItem accordionItem)
    {
        // Fold other items
        if (accordionItem.ContentVisible
            && this.Children().Contains(accordionItem))
        {
            this.Children()
                .ForEach(child =>
                {
                    if (child is AccordionItem item && item != accordionItem)
                    {
                        item.HideAccordionContent();
                    } 
                });
        }
    }
}
