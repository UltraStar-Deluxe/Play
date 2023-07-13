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
        return VideoFileTest("avi-");
    }

    [UnityTest]
    public IEnumerator Mp4Test()
    {
        return VideoFileTest("mp4-");
    }

    [UnityTest]
    public IEnumerator Mp4Av1Test()
    {
        return VideoFileTest("mp4-av1-");
    }

    [UnityTest]
    public IEnumerator Mp4HvecTest()
    {
        return VideoFileTest("mp4-hvec-");
    }

    [UnityTest]
    public IEnumerator WebmVp8Test()
    {
        return VideoFileTest("webm-vp8-");
    }

    /////////////////////////////////////////////////////////
    // common video formats supported by ffmpeg
    /////////////////////////////////////////////////////////
    [UnityTest]
    public IEnumerator F4vTest()
    {
        return VideoFileTest("f4v-");
    }

    [UnityTest]
    public IEnumerator FlvTest()
    {
        return VideoFileTest("flv-");
    }

    [UnityTest]
    public IEnumerator MkvTest()
    {
        return VideoFileTest("mkv-");
    }

    [UnityTest]
    public IEnumerator MovTest()
    {
        return VideoFileTest("mov-");
    }

    [UnityTest]
    public IEnumerator Mpeg2Test()
    {
        return VideoFileTest("mpeg2-");
    }

    [UnityTest]
    public IEnumerator WebVp9Test()
    {
        return VideoFileTest("webm-vp9-");
    }

    [UnityTest]
    public IEnumerator WmvTest()
    {
        return VideoFileTest("wmv-");
    }
}
