using System.Collections;
using UnityEngine.TestTools;

public class AudioFileFormatTests : AbstractMediaFileFormatTests
{
    /////////////////////////////////////////////////////////
    // Audio formats supported by Unity at runtime
    /////////////////////////////////////////////////////////

    [UnityTest]
    public IEnumerator Mp3Test()
    {
        return AudioFileTest("mp3-");
    }

    // [UnityTest]
    // public IEnumerator Mp3UnityReturnsWrongDurationTest()
    // {
    //     // A bug report for this issue has been opened.
    //     // See https://github.com/achimmihca/UnityAudioClipMp3WrongDuration
    //     return AudioFileTest("mp3-UnityReturnsWrongDuration-");
    // }

    [UnityTest]
    public IEnumerator OggTest()
    {
        return AudioFileTest("ogg-");
    }

    [UnityTest]
    public IEnumerator WavTest()
    {
        return AudioFileTest("wav-");
    }

    /////////////////////////////////////////////////////////
    // common audio formats supported by ffmpeg
    /////////////////////////////////////////////////////////
    [UnityTest]
    public IEnumerator AacTest()
    {
        return AudioFileTest("aac-");
    }

    [UnityTest]
    public IEnumerator AiffTest()
    {
        return AudioFileTest("aiff-");
    }

    [UnityTest]
    public IEnumerator FlacTest()
    {
        return AudioFileTest("flac-");
    }

    [UnityTest]
    public IEnumerator M4aTest()
    {
        return AudioFileTest("m4a-");
    }

    [UnityTest]
    public IEnumerator WmaTest()
    {
        return AudioFileTest("wma-");
    }
}
