using System;
using System.Collections.Generic;
using System.Linq;
using AudioSynthesis.Midi;
using AudioSynthesis.Midi.Event;

public static class SongEditorMidiEventExtensions
{
    public static bool TryGetMetaEventTypeEnum(this MidiEvent midiEvent, out MetaEventTypeEnum metaEventTypeEnum)
    {
        if (midiEvent is not MetaEvent metaEvent)
        {
            metaEventTypeEnum = MetaEventTypeEnum.MidiPort;
            return false;
        }
        
        List<MetaEventTypeEnum> enumValues = EnumUtils.GetValuesAsList<MetaEventTypeEnum>();
        foreach (MetaEventTypeEnum enumValue in enumValues)
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
