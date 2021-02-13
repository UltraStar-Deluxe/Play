using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class SlotListControl
{
    private float offsetPositionPercent;
    private ESlotListDirection offsetDirection = ESlotListDirection.None;

    public List<ISlotListItem> ListItems { get; private set; } = new List<ISlotListItem>();

    private Subject<SlotChangeEvent> slotChangeEventStream = new Subject<SlotChangeEvent>();
    public IObservable<SlotChangeEvent> SlotChangeEventStream => slotChangeEventStream;
    
    public void OnDrag(ISlotListItem listItem, Vector2 dragDelta)
    {
        // Calculate direction
        offsetDirection = CalculateOffsetDirection(listItem, dragDelta);

        // Calculate offset from current slot
        ISlotListSlot targetSlot = GetTargetSlot(listItem, offsetDirection);
        offsetPositionPercent = targetSlot != null
            ? CalculateOffsetPositionPercent(listItem.GetPosition(), dragDelta, listItem.GetCurrentSlot().GetPosition(), targetSlot.GetPosition())
            : 0;

        // Apply this to all list items. This make them move in the same way towards their target slot.
        foreach (ISlotListItem otherListItem in ListItems)
        {
            ApplyOffsetPositionPercent(otherListItem);
        }
        
        // When the target has been reached, then adjust the current slot of all items
        if (offsetPositionPercent >= 0.6f)
        {
            slotChangeEventStream.OnNext(new SlotChangeEvent(offsetDirection, offsetPositionPercent));
        }
    }
    
    public void ApplyOffsetPositionPercent(ISlotListItem listItem)
    {
        ISlotListSlot currentSlot = listItem.GetCurrentSlot();
        ISlotListSlot targetSlot = GetTargetSlot(listItem, offsetDirection);
        if (targetSlot != null)
        {
            Vector2 interpolatedPosition = currentSlot.GetPosition() + ((targetSlot.GetPosition() - currentSlot.GetPosition()) * offsetPositionPercent);
            listItem.SetPosition(interpolatedPosition);
        }
        else
        {
            listItem.SetPosition(currentSlot.GetPosition());
        }
    }
    
    private float CalculateOffsetPositionPercent(Vector2 currentPosition, Vector2 dragDelta, Vector2 originalPosition, Vector2 targetPosition)
    {
        // Project dragDelta onto the vector "orinalPosition to targetPosition"
        Vector2 originalPositionToTargetPositionVector = targetPosition - originalPosition;
        Vector2 dragDeltaProjected = Vector3.Project(dragDelta, originalPositionToTargetPositionVector);

        float distanceToOriginalPosition = Vector2.Distance(currentPosition + dragDeltaProjected, originalPosition);
        float fullDistance = Vector2.Distance(originalPosition, targetPosition);
        return distanceToOriginalPosition / fullDistance;
    }

    private ESlotListDirection CalculateOffsetDirection(ISlotListItem listItem, Vector2 dragDelta)
    {
        Vector2 listItemPosition = listItem.GetPosition() + dragDelta;
        ISlotListSlot currentSlot = listItem.GetCurrentSlot();
        if (currentSlot == null)
        {
            return ESlotListDirection.None;
        }
        
        ISlotListSlot nextSlot = currentSlot.GetNextSlot();
        if (nextSlot != null)
        {
            
            float distanceNextSlotToCurrentSlot = Vector2.Distance(nextSlot.GetPosition(), currentSlot.GetPosition());
            float distanceNextSlotToItemPosition = Vector2.Distance(nextSlot.GetPosition(), listItemPosition);
            if (distanceNextSlotToItemPosition < distanceNextSlotToCurrentSlot)
            {
                return ESlotListDirection.TowardsNextSlot;
            }
        }
        
        ISlotListSlot previousSlot = currentSlot.GetPreviousSlot();
        if (previousSlot != null)
        {
            float distancePreviousSlotToCurrentSlot = Vector2.Distance(previousSlot.GetPosition(), currentSlot.GetPosition());
            float distancePreviousSlotToItemPosition = Vector2.Distance(previousSlot.GetPosition(), listItemPosition);
            if (distancePreviousSlotToItemPosition < distancePreviousSlotToCurrentSlot)
            {
                return ESlotListDirection.TowardsPreviousSlot;
            }
        }

        return ESlotListDirection.None;
    }

    public void InterpolateSize(ISlotListItem listItem)
    {
        ISlotListSlot currentSlot = listItem.GetCurrentSlot();
        if (currentSlot == null)
        {
            return;
        }
        
        ESlotListDirection direction = CalculateOffsetDirection(listItem, Vector2.zero);
        ISlotListSlot targetSlot = GetTargetSlot(listItem, direction);
        if (targetSlot == null)
        {
            listItem.SetSize(currentSlot.GetSize());
            return;
        }
        float offsetPercent = CalculateOffsetPositionPercent(
            listItem.GetPosition(), 
            Vector2.zero,
            currentSlot.GetPosition(), 
            targetSlot.GetPosition());
        InterpolateSize(listItem, targetSlot, offsetPercent);
    }

    private ISlotListSlot GetTargetSlot(ISlotListItem listItem, ESlotListDirection direction)
    {
        switch (direction)
        {
            case ESlotListDirection.None : return null;
            case ESlotListDirection.TowardsNextSlot : return listItem.GetCurrentSlot().GetNextSlot();
            case ESlotListDirection.TowardsPreviousSlot : return listItem.GetCurrentSlot().GetPreviousSlot();
        }
        return null;
    }

    private void InterpolateSize(ISlotListItem listItem, ISlotListSlot targetSlot, float offsetPercent)
    {
        Vector2 currentSlotSize = listItem.GetCurrentSlot().GetSize();
        if (targetSlot == null
            || offsetPercent == 0)
        {
            listItem.SetSize(currentSlotSize);
            return;
        }
        
        // Interpolate size
        Vector2 targetSlotSize = targetSlot.GetSize();
        Vector2 interpolatedSize = Vector2.Lerp(currentSlotSize, targetSlotSize, offsetPercent);
        listItem.SetSize(interpolatedSize);
    }
}
