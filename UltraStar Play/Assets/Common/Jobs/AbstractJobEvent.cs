public abstract class AbstractJobEvent
{
    public Job Job { get; private set; }

    protected AbstractJobEvent(Job job)
    {
        Job = job;
    }
}
