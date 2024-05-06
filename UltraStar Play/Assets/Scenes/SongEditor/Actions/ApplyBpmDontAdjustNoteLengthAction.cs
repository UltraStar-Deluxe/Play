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
        if (newBpm == songMeta.BeatsPerMinute)
        {
            return;
        }

        if (newBpm <= 60)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                "reason", "value too low"));
            return;
        }

        songMeta.BeatsPerMinute = newBpm;
    }

    public void ExecuteAndNotify(float newBpm)
    {
        Execute(newBpm);
        songMetaChangeEventStream.OnNext(new SongPropertyChangedEvent(ESongProperty.Bpm));
    }
}
