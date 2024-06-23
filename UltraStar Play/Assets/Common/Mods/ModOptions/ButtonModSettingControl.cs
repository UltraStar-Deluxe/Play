using UnityEngine.UIElements;

public class ButtonModSettingControl : IModSettingControl
{
    private readonly string label;
    private readonly EventCallback<EventBase> onClick;

    public ButtonModSettingControl(string label, EventCallback<EventBase> onClick)
    {
        this.label = label;
        this.onClick = onClick;
    }

    public VisualElement CreateVisualElement()
    {
        Button button = new Button();
        button.text = label;
        button.RegisterCallbackButtonTriggered(evt => onClick(evt));
        return button;
    }
}
