using NUnit.Framework;

public class TimeUtilsTest
{
    [Test]
    public void ShouldParseDuration()
    {
        TestDuration(" 0.5 s ", 500);
        TestDuration("0,8s", 800);
        TestDuration("900ms", 900);
        TestDuration("1500ms", 1500);
        TestDuration("12s", 12000);
    }

    private void TestDuration(string durationString, int durationInMillis)
    {
        TimeUtils.TryParseDuration(durationString, out long parsedDurationInMilliseconds);
        Assert.AreEqual(durationInMillis, parsedDurationInMilliseconds);
    }
}
