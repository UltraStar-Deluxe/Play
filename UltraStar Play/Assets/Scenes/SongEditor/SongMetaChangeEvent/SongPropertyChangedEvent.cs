public class SongPropertyChangedEvent : SongMetaChangedEvent
{
    public ESongProperty SongProperty { get; private set; }

    public SongPropertyChangedEvent(ESongProperty songProperty)
    {
        this.SongProperty = songProperty;
    }
}
