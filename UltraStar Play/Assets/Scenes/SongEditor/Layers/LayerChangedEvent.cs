public class LayerChangedEvent
{
    public ESongEditorLayer LayerEnum { get; private set; }
    public string VoiceId { get; private set; }
    public bool IsVoiceLayerEvent => !VoiceId.IsNullOrEmpty();

    public LayerChangedEvent(ESongEditorLayer layerEnum)
    {
        LayerEnum = layerEnum;
    }

    public LayerChangedEvent(string voiceId)
    {
        VoiceId = voiceId;
    }
}
