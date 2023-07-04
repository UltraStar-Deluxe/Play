using NUnit.Framework;

public class NumerUtilsTest
{
    [Test]
    public void ShortestCircleDirectionTest()
    {
        Assert.That(NumberUtils.ShortestCircleDirection(4, 6, MidiUtils.NoteCountInAnOctave) > 0) ;
        Assert.That(NumberUtils.ShortestCircleDirection(6, 4, MidiUtils.NoteCountInAnOctave) < 0) ;
        Assert.That(NumberUtils.ShortestCircleDirection(11, 2, MidiUtils.NoteCountInAnOctave) > 0) ;
        Assert.That(NumberUtils.ShortestCircleDirection(2, 11, MidiUtils.NoteCountInAnOctave) < 0) ;
    }    
}
