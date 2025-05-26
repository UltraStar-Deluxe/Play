using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SetMusicGapAction : INeedInjection
{

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongMetaChangedEventStream songMetaChangedEventStream;

    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject]
    private PanelHelper panelHelper;

    [Inject]
    private MoveNotesAction moveNotesAction;

    [Inject]
    private SentenceFitToNoteAction sentenceFitToNoteAction;

    public void Execute(double positionInMillis, bool keepNotesPosition = false)
    {
        double oldGapInMillis = songMeta.GapInMillis;
        songMeta.GapInMillis = positionInMillis;

        if (keepNotesPosition)
        {
            double offsetInMillis = songMeta.GapInMillis - oldGapInMillis;
            int offsetInBeats = (int)SongMetaBpmUtils.MillisToBeatsWithoutGap(songMeta, offsetInMillis);
            moveNotesAction.MoveNotesHorizontal(-offsetInBeats, SongMetaUtils.GetAllNotes(songMeta));
            sentenceFitToNoteAction.Execute(SongMetaUtils.GetAllSentences(songMeta));
        }
    }

    public void ExecuteAndNotify(double positionInMillis, bool keepNotesPosition = false)
    {
        Execute(positionInMillis, keepNotesPosition);
        songMetaChangedEventStream.OnNext(new SongPropertyChangedEvent(ESongProperty.Gap));
    }
}
