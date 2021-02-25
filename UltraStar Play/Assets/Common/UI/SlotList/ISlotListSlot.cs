using UnityEngine;

public interface ISlotListSlot
{
    Vector2 GetPosition();
    Vector2 GetSize();
    ISlotListSlot GetNextSlot();
    ISlotListSlot GetPreviousSlot();
}
