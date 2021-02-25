using UnityEngine;

public interface ISlotListItem
{
    Vector2 GetPosition();
    ISlotListSlot GetCurrentSlot();
    void SetSize(Vector2 newSize);
    void SetPosition(Vector2 getPosition);
}
