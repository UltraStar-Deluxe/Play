public class SongPropertyChangedEvent : SongMetaChangeEvent
{
    public ESongProperty SongProperty { get; private set; }

    public SongPropertyChangedEvent(ESongProperty songProperty)
    {
        this.SongProperty = songProperty;
    }
}
