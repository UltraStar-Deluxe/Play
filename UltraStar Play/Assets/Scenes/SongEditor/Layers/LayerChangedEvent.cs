public class LayerChangedEvent
{
    public ESongEditorLayer LayerEnum { get; private set; }

    public LayerChangedEvent(ESongEditorLayer layerEnum)
    {
        LayerEnum = layerEnum;
    }
}
