public class NoteAreaRect
{
    public int MinMillis { get; private set; }
    public int MaxMillis { get; private set; }
    public int LengthInMillis => MaxMillis - MinMillis;

    public int MinBeat { get; private set; }
    public int MaxBeat { get; private set; }
    public int LengthInBeats => MaxBeat - MinBeat;

    public int MinMidiNote { get; private set; }
    public int MaxMidiNote { get; private set; }
    public int LengthInMidiNotes => MaxMidiNote - MinMidiNote;

    private NoteAreaRect()
    {
    }

    public static NoteAreaRect CreateFromMillis(SongMeta songMeta, int minMillis, int maxMillis, int minMidiNote, int maxMidiNote)
    {
        if (maxMillis < minMillis)
        {
            ObjectUtils.Swap(ref minMillis, ref maxMillis);
        }
        if (maxMidiNote < minMidiNote)
        {
            ObjectUtils.Swap(ref minMidiNote, ref maxMidiNote);
        }

        NoteAreaRect rect = new();
        rect.MinMillis = minMillis;
        rect.MaxMillis = maxMillis;
        rect.MinBeat = (int)SongMetaBpmUtils.MillisToBeats(songMeta, minMillis);
        rect.MaxBeat = (int)SongMetaBpmUtils.MillisToBeats(songMeta, maxMillis);

        rect.MinMidiNote = minMidiNote;
        rect.MaxMidiNote = maxMidiNote;
        return rect;
    }

    public static NoteAreaRect CreateFromBeats(SongMeta songMeta, int minBeat, int maxBeat, int minMidiNote, int maxMidiNote)
    {
        if (maxBeat < minBeat)
        {
            ObjectUtils.Swap(ref minBeat, ref maxBeat);
        }
        if (maxMidiNote < minMidiNote)
        {
            ObjectUtils.Swap(ref minMidiNote, ref maxMidiNote);
        }

        NoteAreaRect rect = new();
        rect.MinBeat = minBeat;
        rect.MaxBeat = maxBeat;
        rect.MinMillis = (int)SongMetaBpmUtils.BeatsToMillis(songMeta, minBeat);
        rect.MaxMillis = (int)SongMetaBpmUtils.BeatsToMillis(songMeta, maxBeat);

        rect.MinMidiNote = minMidiNote;
        rect.MaxMidiNote = maxMidiNote;

        return rect;
    }
}
