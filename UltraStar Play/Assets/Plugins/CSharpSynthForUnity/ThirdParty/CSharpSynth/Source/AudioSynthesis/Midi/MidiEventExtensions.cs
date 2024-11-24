using System;
using System.Linq;
using AudioSynthesis.Midi;
using AudioSynthesis.Midi.Event;

public static class MidiEventExtensions
{

    public static bool TryGetMidiEventTypeEnum(this MidiEvent midiEvent, out MidiEventTypeEnum midiEventTypeEnum)
    {
        var enumValues = Enum.GetValues(typeof(MidiEventTypeEnum))
            .Cast<MidiEventTypeEnum>()
            .ToList();

        foreach (var enumValue in enumValues)
        {
            if ((int)enumValue == midiEvent.Command)
            {
                midiEventTypeEnum = enumValue;
                return true;
            }
        }

        midiEventTypeEnum = MidiEventTypeEnum.Controller;
        return false;
    }
}