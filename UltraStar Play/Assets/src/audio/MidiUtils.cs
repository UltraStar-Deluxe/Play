using System;
using System.Collections;
using System.Collections.Generic;

public static class MidiUtils {

    public static string MidiNoteToAbsoluteName(int midiNote) {
        // 12: "C0"
        // 13: "C#0"
        // 14: "D0"
        // ...
        // 24: "C1"
        int octave = (midiNote / 12) - 1;
        return MidiNoteToRelativeName(midiNote) + octave;
    }

    public static string MidiNoteToRelativeName(int midiNote) {
        midiNote %= 12;

        switch(midiNote) {
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
}