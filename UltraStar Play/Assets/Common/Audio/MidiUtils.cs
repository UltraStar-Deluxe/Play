using System;

public static class MidiUtils
{
    // There are 49 halftones in the singable audio spectrum,
    // namely C2 (midi note 36, which is 65.41 Hz) to C6 (midi note 84, which is 1046.5023 Hz).
    public const int MidiNoteMin = 36;
    public const int MidiNoteMax = 84;
    public const int SingableNoteRange = 49;

    // Concert pitch A4 (440 Hz)
    public const int MidiNoteConcertPitch = 69;
    public const int MidiNoteConcertPitchFrequency = 440;

    public static int GetOctave(int midiNote)
    {
        // 12: "C0"
        // 13: "C#0"
        // 14: "D0"
        // ...
        // 24: "C1"
        int octave = (midiNote / 12) - 1;
        return octave;
    }

    public static string GetAbsoluteName(int midiNote)
    {
        int octave = GetOctave(midiNote);
        string absoluteName = GetRelativeName(midiNote) + octave;
        return absoluteName;
    }

    public static string GetRelativeName(int midiNote)
    {
        midiNote = GetRelativePitch(midiNote);

        switch (midiNote)
        {
            case 0: return "C";
            case 1: return "C#";
            case 2: return "D";
            case 3: return "D#";
            case 4: return "E";
            case 5: return "F";
            case 6: return "F#";
            case 7: return "G";
            case 8: return "G#";
            case 9: return "A";
            case 10: return "A#";
            case 11: return "B";
            default:
                return midiNote.ToString();
        }
    }

    public static int GetRelativePitch(int midiNote)
    {
        return midiNote % 12;
    }

    public static int GetRoundedMidiNote(int midiNote, int midiCents, int resolution)
    {
        int accurateMidiNote = (midiNote * 100) + midiCents;
        int accurateRoundedMidiNote = accurateMidiNote % resolution;
        int roundedMidiNote = accurateRoundedMidiNote / 100;
        return roundedMidiNote;
    }

    public static int GetRelativePitchDistance(int recordedMidiNote, int targetMidiNote)
    {
        int recordedRelativePitch = GetRelativePitch(recordedMidiNote);
        int targetRelativePitch = GetRelativePitch(targetMidiNote);

        // Distance when going from 2 to 10 via 3, 4, 5...
        int distanceUnwrapped = Math.Abs(targetRelativePitch - recordedRelativePitch);
        // Distance when going from 2 to 10 via 1, 11, 10
        int distanceWrapped = 12 - distanceUnwrapped;
        // Note that (distanceUnwrapped + distanceWrapped) == 12, which is going a full circle in any direction.

        // Distance in shortest direction is result distance
        return Math.Min(distanceUnwrapped, distanceWrapped);
    }
}
