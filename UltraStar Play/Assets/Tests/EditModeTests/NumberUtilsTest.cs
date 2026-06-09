using NUnit.Framework;

public class NumberUtilsTest
{
    [Test]
    public void ShouldReturnShortestCircleDirection()
    {
        Assert.That(NumberUtils.ShortestCircleDirection(4, 6, MidiUtils.NoteCountInAnOctave) > 0) ;
        Assert.That(NumberUtils.ShortestCircleDirection(6, 4, MidiUtils.NoteCountInAnOctave) < 0) ;
        Assert.That(NumberUtils.ShortestCircleDirection(11, 2, MidiUtils.NoteCountInAnOctave) > 0) ;
        Assert.That(NumberUtils.ShortestCircleDirection(2, 11, MidiUtils.NoteCountInAnOctave) < 0) ;
    }

    [Test]
    public void FloatEqualsShouldConsiderTolerance()
    {
        Assert.IsTrue(99f.Equals(100, 1));
        Assert.IsTrue(0.01f.Equals(0, 0.01f));
        Assert.IsTrue((-42.001f).Equals(-42, 0.01f));
        Assert.IsTrue((-42f).Equals(-42.001f, 0.01f));
        Assert.IsTrue((-42.001f).Equals(-40f, 2.01f));

        Assert.IsFalse(0.01f.Equals(0, 0.001f));
    }
}
