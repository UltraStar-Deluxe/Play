public class LayerChangedEvent
{
    public ESongEditorLayer LayerEnum { get; private set; }
    public EVoiceId VoiceId { get; private set; }

    public bool IsVoiceLayerEvent { get; private set; }

    public LayerChangedEvent(ESongEditorLayer layerEnum)
    {
        LayerEnum = layerEnum;
    }

    public LayerChangedEvent(EVoiceId voiceId)
    {
        VoiceId = voiceId;
        IsVoiceLayerEvent = true;
    }
}
