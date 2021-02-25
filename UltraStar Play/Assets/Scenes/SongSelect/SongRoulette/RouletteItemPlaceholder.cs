using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RouletteItemPlaceholder : MonoBehaviour, ISlotListSlot
{
    private RectTransform rectTransform;
    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            return rectTransform;
        }
    }

    private ISlotListSlot nextSlot;
    private ISlotListSlot previousSlot;

    public void SetNeighborSlots(ISlotListSlot previousSlot, ISlotListSlot nextSlot)
    {
        this.previousSlot = previousSlot;
        this.nextSlot = nextSlot;
    }
    
    public Vector2 GetSize()
    {
        return new Vector2(RectTransform.rect.width, RectTransform.rect.height);
    }

    public Vector2 GetPosition()
    {
        return RectTransform.position;
    }

    public ISlotListSlot GetNextSlot()
    {
        return nextSlot;
    }

    public ISlotListSlot GetPreviousSlot()
    {
        return previousSlot;
    }
}
