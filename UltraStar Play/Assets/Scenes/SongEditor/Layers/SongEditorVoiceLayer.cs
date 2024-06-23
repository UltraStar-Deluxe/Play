public class SongEditorVoiceLayer : AbstractSongEditorLayer
{
    public EVoiceId VoiceId { get; private set; }

    public SongEditorVoiceLayer(EVoiceId voiceId)
    {
        this.VoiceId = voiceId;
    }

    public override Translation GetDisplayName()
    {
        return Translation.Get(VoiceId);
    }
}
