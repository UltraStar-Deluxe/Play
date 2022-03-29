using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class SongEntryPlaceholderControl : ISlotListSlot
{
    public static readonly IComparer<SongEntryPlaceholderControl> comparerByCenterDistance
        = new SongEntryPlaceholderComparerByCenterDistance();

    public static readonly IComparer<SongEntryPlaceholderControl> comparerByPosition
        = new SongEntryPlaceholderComparerByPosition();

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

    public int GetCenterDistanceIndex(List<SongEntryPlaceholderControl> allPlaceholderControls, SongEntryPlaceholderControl centerPlaceholderControl)
    {
        if (this == centerPlaceholderControl)
        {
            return 0;
        }

        // first item left of center => returns -1
        // first item right of center => returns 1
        // second item left of center => returns -2
        // second item right of center => returns 2
        // ...

        // Count items between this and the center.
        float currentX = GetPosition().x;
        float centerX = centerPlaceholderControl.GetPosition().x;
        if (currentX < centerX)
        {
            // Left of center
            List<SongEntryPlaceholderControl> placeholdersInBetween = allPlaceholderControls
                .Where(it => currentX < it.GetPosition().x && it.GetPosition().x < centerX)
                .ToList();
            return -(placeholdersInBetween.Count + 1);
        }
        else
        {
            // Right of center
            List<SongEntryPlaceholderControl> placeholdersInBetween = allPlaceholderControls
                .Where(it => centerX < it.GetPosition().x && it.GetPosition().x < currentX)
                .ToList();
            return placeholdersInBetween.Count + 1;
        }
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
            float aCenterDistance = Mathf.Abs(centerPosition - a.VisualElement.worldBound.center.x);
            float bCenterDistance = Mathf.Abs(centerPosition - b.VisualElement.worldBound.center.x);
            return aCenterDistance.CompareTo(bCenterDistance);
        }
    }

    public class SongEntryPlaceholderComparerByPosition : IComparer<SongEntryPlaceholderControl>
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

            return a.GetPosition().x.CompareTo(b.GetPosition().x);
        }
    }
}
