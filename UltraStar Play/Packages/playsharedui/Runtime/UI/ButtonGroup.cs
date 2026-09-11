using UnityEngine.UIElements;

[UxmlElement]
public partial class ButtonGroup : VisualElement
{
    public enum ButtonGroupDirection { Horizontal, Vertical }

    private ButtonGroupDirection direction;
    [UxmlAttribute("direction")]
    public ButtonGroupDirection Direction
    {
        get => direction;
        set
        {
            direction = value;
            if (direction == ButtonGroupDirection.Horizontal)
            {
                this.AddToClassList("horizontal");
                this.RemoveFromClassList("vertical");
            }
            else if (direction == ButtonGroupDirection.Vertical)
            {
                this.AddToClassList("vertical");
                this.RemoveFromClassList("horizontal");
            }
        }
    }
    
    private SetFirstAndLastChildClassControl setFirstAndLastChildClassControl;
    
    public ButtonGroup()
    {
        Direction = ButtonGroupDirection.Vertical;
        setFirstAndLastChildClassControl = new(this);
    }
}
