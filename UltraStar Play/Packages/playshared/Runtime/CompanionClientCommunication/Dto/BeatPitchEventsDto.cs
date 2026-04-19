using System;
using System.Collections.Generic;

[Serializable] // Serializable is required for UnityEngine.JsonUtility
public class BeatPitchEventsDto : CompanionAppMessageDto
{
    public List<BeatPitchEventDto> BeatPitchEvents { get; set; } = new List<BeatPitchEventDto>();

    public BeatPitchEventsDto()
    {
    }

    public BeatPitchEventsDto(BeatPitchEventDto beatPitchEvent)
        : this(new List<BeatPitchEventDto> { beatPitchEvent })
    {
    }

    public BeatPitchEventsDto(List<BeatPitchEventDto> beatPitchEvents)
        : base(CompanionAppMessageType.BeatPitchEvents)
    {
        BeatPitchEvents = beatPitchEvents;
    }
}
