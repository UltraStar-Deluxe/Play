public class SlotChangeEvent
{
    public ESlotListDirection Direction { get; private set; }
    public float OffsetPositionPercent { get; private set; }

    public SlotChangeEvent(ESlotListDirection direction, float offsetPositionPercent)
    {
        Direction = direction;
        OffsetPositionPercent = offsetPositionPercent;
    }
}
