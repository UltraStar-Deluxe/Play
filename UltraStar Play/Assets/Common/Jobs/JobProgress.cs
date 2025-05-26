using System.Threading;

public class JobProgress
{
    public CancellationTokenSource CancellationTokenSource { get; private set; }

    public long StartTimeInMillis { get; set; }
    public long EndTimeInMillis { get; set; }

    public long EstimatedTotalDurationInMillis { get; set; }
    public double EstimatedCurrentProgressInPercent
    {
        get
        {
            if (EndTimeInMillis > 0)
            {
                return 100;
            }

            if (EstimatedTotalDurationInMillis <= 0
                || StartTimeInMillis == 0)
            {
                return 0;
            }

            double progressInPercent = 100.0 * (double)CurrentDurationInMillis / EstimatedTotalDurationInMillis;
            if (progressInPercent > 99)
            {
                progressInPercent = 99;
            }
            return progressInPercent;
        }

        set
        {
            double progressFactor = value / 100.0;
            if (progressFactor <= 0)
            {
                return;
            }
            EstimatedTotalDurationInMillis = (long)(CurrentDurationInMillis * (1 / progressFactor));
        }
    }

    public long CurrentDurationInMillis
    {
        get
        {
            if (StartTimeInMillis == 0)
            {
                return 0;
            }
            if (EndTimeInMillis > 0)
            {
                return EndTimeInMillis - StartTimeInMillis;
            }
            return TimeUtils.GetUnixTimeMilliseconds() - StartTimeInMillis;
        }
    }

    public JobProgress(CancellationTokenSource cancellationTokenSource)
    {
        CancellationTokenSource = cancellationTokenSource;
    }
}
