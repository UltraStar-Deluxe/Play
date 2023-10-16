using System.Collections;
using UnityEngine.TestTools;

public class VideoFileFormatTests : AbstractMediaFileFormatTests
{
    /////////////////////////////////////////////////////////
    // Video formats supported by Unity at runtime
    /////////////////////////////////////////////////////////

    [UnityTest]
    public IEnumerator AviTest()
    {
        yield return VideoFileTest("avi-");
    }

    [UnityTest]
    public IEnumerator Mp4Test()
    {
        yield return VideoFileTest("mp4-");
    }

    [UnityTest]
    public IEnumerator Mp4Av1Test()
    {
        yield return VideoFileTest("mp4-av1-");
    }

    [UnityTest]
    public IEnumerator Mp4HvecTest()
    {
        yield return VideoFileTest("mp4-hvec-");
    }

    [UnityTest]
    public IEnumerator WebmVp8Test()
    {
        yield return VideoFileTest("webm-vp8-");
    }

    /////////////////////////////////////////////////////////
    // common video formats supported by ffmpeg
    /////////////////////////////////////////////////////////
    [UnityTest]
    public IEnumerator F4vTest()
    {
        yield return VideoFileTest("f4v-");
    }

    [UnityTest]
    public IEnumerator FlvTest()
    {
        yield return VideoFileTest("flv-");
    }

    [UnityTest]
    public IEnumerator MkvTest()
    {
        yield return VideoFileTest("mkv-");
    }

    [UnityTest]
    public IEnumerator MovTest()
    {
        yield return VideoFileTest("mov-");
    }

    [UnityTest]
    public IEnumerator Mpeg2Test()
    {
        yield return VideoFileTest("mpeg2-");
    }

    [UnityTest]
    public IEnumerator WebVp9Test()
    {
        yield return VideoFileTest("webm-vp9-");
    }

    [UnityTest]
    public IEnumerator WmvTest()
    {
        yield return VideoFileTest("wmv-");
    }
}
