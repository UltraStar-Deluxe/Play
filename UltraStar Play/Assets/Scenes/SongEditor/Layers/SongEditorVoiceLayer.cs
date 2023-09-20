public class SongEditorVoiceLayer : AbstractSongEditorLayer
{
    public string VoiceId { get; private set; }

    public SongEditorVoiceLayer(string voiceId)
    {
        this.VoiceId = voiceId;
    }

    public override string GetDisplayName()
    {
        return VoiceId.Replace("P", "Player ");
    }
}
