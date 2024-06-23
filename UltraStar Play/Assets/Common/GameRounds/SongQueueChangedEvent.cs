using System.Collections.Generic;

public class SongQueueChangedEvent
{
    public List<SongQueueEntryDto> SongQueueEntryDtos { get; private set; }

    public SongQueueChangedEvent(List<SongQueueEntryDto> songQueueEntryDtos)
    {
        SongQueueEntryDtos = songQueueEntryDtos;
    }
    
    public SongQueueChangedEvent(SongQueueEntryDto songQueueEntryDto)
        : this(new List<SongQueueEntryDto>() { songQueueEntryDto })
    {
    }
}
