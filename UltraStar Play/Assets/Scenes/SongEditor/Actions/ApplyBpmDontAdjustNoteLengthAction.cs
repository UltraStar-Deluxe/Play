using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ApplyBpmDontAdjustNoteLengthAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private SongMeta songMeta;

    public void Execute(float newBpm)
    {
        if (newBpm == songMeta.Bpm)
        {
            return;
        }

        if (newBpm <= 60)
        {
            UiManager.CreateNotification("New BPM is set much too low.");
            return;
        }

        songMeta.Bpm = newBpm;
    }

    public void ExecuteAndNotify(float newBpm)
    {
        Execute(newBpm);
        songMetaChangeEventStream.OnNext(new SongPropertyChangedEvent(ESongProperty.Bpm));
    }
}
