using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SongEntryPlaceholderControl : ISlotListSlot
{
    public static readonly IComparer<SongEntryPlaceholderControl> comparerByCenterDistance
        = new SongEntryPlaceholderComparerByCenterDistance();

    public VisualElement VisualElement { get; private set; }
    private ISlotListSlot nextSlot;
    private ISlotListSlot previousSlot;

    public SongEntryPlaceholderControl(VisualElement visualElement)
    {
        this.VisualElement = visualElement;
    }

    public void SetNeighborSlots(ISlotListSlot previousSlot, ISlotListSlot nextSlot)
    {
        this.previousSlot = previousSlot;
        this.nextSlot = nextSlot;
    }
    
    public Vector2 GetSize()
    {
        Vector2 result = VisualElement.contentRect.size;
        if (float.IsNaN(result.x) || float.IsNaN(result.y))
        {
            result = new Vector2(VisualElement.style.width.value.value, VisualElement.style.height.value.value);
        }

        return result;
    }

    public Vector2 GetPosition()
    {
        Vector2 result = new Vector2(VisualElement.resolvedStyle.left, VisualElement.resolvedStyle.top);
        if (float.IsNaN(result.x) || float.IsNaN(result.y))
        {
            result = new Vector2(VisualElement.style.left.value.value, VisualElement.style.top.value.value);
        }

        return result;
    }

    public ISlotListSlot GetNextSlot()
    {
        return nextSlot;
    }

    public ISlotListSlot GetPreviousSlot()
    {
        return previousSlot;
    }

    public class SongEntryPlaceholderComparerByCenterDistance : IComparer<SongEntryPlaceholderControl>
    {
        public int Compare(SongEntryPlaceholderControl a, SongEntryPlaceholderControl b)
        {
            if (a == null && b == null)
            {
                return 0;
            }
            else if (a == null)
            {
                return -1;
            }
            else if (b == null)
            {
                return 1;
            }

            float centerPosition = 400;
            float aCenterDistance = Mathf.Abs(centerPosition - a.GetPosition().x);
            float bCenterDistance = Mathf.Abs(centerPosition - b.GetPosition().x);
            return aCenterDistance.CompareTo(bCenterDistance);
        }
    }
}
