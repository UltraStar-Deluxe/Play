using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MidiUtils
{
    // There are 49 halftones in the singable audio spectrum,
    // namely C2 (midi note 36, which is 65.41 Hz) to C6 (midi note 84, which is 1046.5023 Hz).
    public const int SingableNoteMin = 36;
    public const int SingableNoteMax = 84;
    public const int SingableNoteRange = 49;
    public const int MaxMidiNote = 127;

    // Concert pitch A4 (440 Hz)
    public const int MidiNoteConcertPitch = 69;
    public const int MidiNoteConcertPitchFrequency = 440;

    // White keys: C = 0, D = 2, E = 4, F = 5, G = 7, A = 9, B = 11
    private static readonly int[] whiteKeyRelativeMidiNotes = { 0, 2, 4, 5, 7, 9, 11 };
    // Black keys: C# = 1, D# = 3, F# = 6, G# = 8, A# = 10
    private static readonly int[] blackKeyRelativeMidiNotes = { 1, 3, 6, 8, 10 };

    private static Dictionary<int, string> midiNoteToAbsoluteName = CreateMidiNoteToAbsoluteNameMap();
    private static readonly Dictionary<string, int> absoluteNameToMidiNote = CreateAbsoluteNameToMidiNoteMap();

    private static Dictionary<int, string> CreateMidiNoteToAbsoluteNameMap()
    {
        Dictionary<int, string> result = new Dictionary<int, string>();
        for (int midiNote = 21; midiNote <= 127; midiNote++)
        {
            result.Add(midiNote, GetAbsoluteName(midiNote));
        }

        return result;
    }

    private static Dictionary<string, int> CreateAbsoluteNameToMidiNoteMap()
    {
        return CreateMidiNoteToAbsoluteNameMap().ToInvertedDictionary();
    }

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

    public static int GetRelativePitchDistance(int fromMidiNote, int toMidiNote)
    {
        int fromRelativeMidiNote = GetRelativePitch(fromMidiNote);
        int toRelativeMidiNote = GetRelativePitch(toMidiNote);

        // Distance when going from 2 to 10 via 3, 4, 5...
        int distanceUnwrapped = Math.Abs(toRelativeMidiNote - fromRelativeMidiNote);
        // Distance when going from 2 to 10 via 1, 11, 10
        int distanceWrapped = 12 - distanceUnwrapped;
        // Note that (distanceUnwrapped + distanceWrapped) == 12, which is going a full circle in any direction.

        // Distance in shortest direction is result distance
        return Math.Min(distanceUnwrapped, distanceWrapped);
    }

    public static int GetRelativePitchDistanceSigned(int fromMidiNote, int toMidiNote)
    {
        int toRelativeMidiNote = MidiUtils.GetRelativePitch(toMidiNote);
        int fromRelativeMidiNote = MidiUtils.GetRelativePitch(fromMidiNote);
        // Distance when going from 2 to 10 via 3, 4, 5... -> (8)
        // Distance when going from 10 to 2 via 9, 8, 7... -> (-8)
        int distanceUnwrapped = toRelativeMidiNote - fromRelativeMidiNote;
        // Distance when going from 2 to 10 via 1, 0, 11, 10 -> (-4)
        // Distance when going from 10 to 2 via 11, 0, 1, 2 -> (4)
        int distanceWrapped = distanceUnwrapped >= 0
            ? distanceUnwrapped - 12
            : distanceUnwrapped + 12;
        int distance = Math.Abs(distanceUnwrapped) < Math.Abs(distanceWrapped)
            ? distanceUnwrapped
            : distanceWrapped;
        return distance;
    }

    public static bool IsBlackPianoKey(int midiNote)
    {
        return blackKeyRelativeMidiNotes.Contains(GetRelativePitch(midiNote));
    }

    public static bool IsWhitePianoKey(int midiNote)
    {
        return whiteKeyRelativeMidiNotes.Contains(GetRelativePitch(midiNote));
    }

    public static float[] PrecalculateHalftoneFrequencies(int noteMin, int noteRange)
    {
        float[] frequencies = new float[noteRange];
        for (int index = 0; index < frequencies.Length; index++)
        {
            float concertPitchOctaveOffset = ((noteMin + index) - MidiUtils.MidiNoteConcertPitch) / 12f;
            frequencies[index] = (float)(MidiUtils.MidiNoteConcertPitchFrequency * Math.Pow(2f, concertPitchOctaveOffset));
        }
        return frequencies;
    }

    public static int[] PrecalculateHalftoneDelays(int sampleRateHz, float[] halftoneFrequencies)
    {
        int[] noteDelays = new int[halftoneFrequencies.Length];
        for (int index = 0; index < halftoneFrequencies.Length; index++)
        {
            noteDelays[index] = Convert.ToInt32(sampleRateHz / halftoneFrequencies[index]);
        }
        return noteDelays;
    }

    public static int GetMidiNoteOnOctaveOfTargetMidiNote(int midiNote, int targetMidiNote)
    {
        int relativeSignedDistance = GetRelativePitchDistanceSigned(targetMidiNote, midiNote);
        int midiNoteOnOctaveOfTargetNote = targetMidiNote + relativeSignedDistance;
        if (GetRelativePitch(midiNote) != GetRelativePitch(midiNoteOnOctaveOfTargetNote))
        {
            // Should never happen
            Debug.LogError($"The midiNote rounded to the targetMidiNote differs not only in the octave but also in pitch. This should never happen:"
                + $"midiNote {midiNote}, targetMidiNote: {targetMidiNote}, displayed: {midiNoteOnOctaveOfTargetNote}");
        }
        return midiNoteOnOctaveOfTargetNote;
    }

    public static bool TryParseMidiNoteName(string midiNoteName, out int midiNote)
    {
        if (absoluteNameToMidiNote.TryGetValue(midiNoteName.ToUpperInvariant(), out int lookupMidiNote))
        {
            midiNote = lookupMidiNote;
            return true;
        }
        midiNote = 0;
        return false;
    }
}
