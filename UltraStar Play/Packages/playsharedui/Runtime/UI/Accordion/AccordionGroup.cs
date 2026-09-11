using System.Linq;
using UnityEngine.UIElements;

[UxmlElement]
public partial class AccordionGroup : VisualElement
{
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
