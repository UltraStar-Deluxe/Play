public class SongEditorVoiceLayer : AbstractSongEditorLayer
{
    public EVoiceId VoiceId { get; private set; }

    public SongEditorVoiceLayer(EVoiceId voiceId)
    {
        this.VoiceId = voiceId;
    }

    public override string GetDisplayName()
    {
        return VoiceId
            .ToString()
            .Replace("P", "Player ");
    }
}
