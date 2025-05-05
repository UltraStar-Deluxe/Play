using System.Collections.Generic;

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
