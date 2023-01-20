public class LayerChangedEvent
{
    public ESongEditorLayer LayerEnum { get; private set; }
    public string VoiceName { get; private set; }
    public bool IsVoiceLayerEvent => !VoiceName.IsNullOrEmpty();

    public LayerChangedEvent(ESongEditorLayer layerEnum)
    {
        LayerEnum = layerEnum;
    }

    public LayerChangedEvent(string voiceName)
    {
        VoiceName = voiceName;
    }
}
