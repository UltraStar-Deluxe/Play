public class ViewportEvent
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public ViewportEvent(int x, int y, int w, int h)
    {
        X = x;
        Y = y;
        Width = w;
        Height = h;
    }
}