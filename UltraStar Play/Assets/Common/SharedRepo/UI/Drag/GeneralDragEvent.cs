public class GeneralDragEvent
{
    public int InputButton { get; private set; }

    public DragCoordinate ScreenCoordinateInPixels { get; private set; }
    public DragCoordinate ScreenCoordinateInPercent { get; private set; }
    public DragCoordinate LocalCoordinateInPixels { get; private set; }
    public DragCoordinate LocalCoordinateInPercent { get; private set; }

    public GeneralDragEvent(
        DragCoordinate screenCoordinateInPixels,
        DragCoordinate screenCoordinateInPercent,
        DragCoordinate localCoordinateInPixels,
        DragCoordinate localCoordinateInPercent,
        int inputButton)
    {
        ScreenCoordinateInPixels = screenCoordinateInPixels;
        ScreenCoordinateInPercent = screenCoordinateInPercent;

        LocalCoordinateInPixels = localCoordinateInPixels;
        LocalCoordinateInPercent = localCoordinateInPercent;

        InputButton = inputButton;
    }
}
