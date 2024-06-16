using System.Collections.Generic;
using AudioSynthesis.Midi;
using AudioSynthesis.Midi.Event;

public static class CustomMidiEventExtensions
{
    private static readonly List<MidiEventTypeEnum> midiEventTypeEnumValues = EnumUtils.GetValuesAsList<MidiEventTypeEnum>();
    private static readonly List<MetaEventTypeEnum> metaEventTypeEnumValues = EnumUtils.GetValuesAsList<MetaEventTypeEnum>();

    public static bool TryGetMidiEventTypeEnumFast(this MidiEvent midiEvent, out MidiEventTypeEnum midiEventTypeEnum)
    {
        // Prefer this method over MidiEventExtensions.TryGetMidiEventTypeEnum
        // because it uses a static reference to the enum values.
        foreach (MidiEventTypeEnum enumValue in midiEventTypeEnumValues)
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
    
    public static bool TryGetMetaEventTypeEnum(this MidiEvent midiEvent, out MetaEventTypeEnum metaEventTypeEnum)
    {
        if (midiEvent is not MetaEvent metaEvent)
        {
            metaEventTypeEnum = MetaEventTypeEnum.MidiPort;
            return false;
        }
        
        foreach (MetaEventTypeEnum enumValue in metaEventTypeEnumValues)
        {
            if ((int)enumValue == metaEvent.Data1)
            {
                metaEventTypeEnum = enumValue;
                return true;
            }
        }

        metaEventTypeEnum = MetaEventTypeEnum.MidiPort;
        return false;
    }
}
