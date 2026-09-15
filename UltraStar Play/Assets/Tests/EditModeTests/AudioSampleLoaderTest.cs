using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UniInject;
using UniInject.Extensions;
using UnityEngine;

public class AudioSampleLoaderTest
{
    protected static readonly string testSongsFolderPath = $"{Application.dataPath}/Tests/PlayModeTests/MediaFileFormatTests/MediaFileFormatTestSongs";

    [Test]
    public async Task ShouldLoadAudioSamplesFromVideoFileViaFfmpeg()
    {
        string videoFilePath = $"{testSongsFolderPath}/mp4.mp4";

        GameObject gameObject = new GameObject("AudioSampleLoaderTest");
        try
        {
            AudioSampleLoader audioSampleLoader = gameObject.AddComponent<AudioSampleLoader>();
            UniInjectUtils.GlobalInjector
                .WithBindingForInstance(new Settings())
                .Inject(audioSampleLoader);

            AudioClip audioClip = await audioSampleLoader.LoadAsAudioClip(videoFilePath);

            Assert.NotNull(audioClip);
            Assert.AreEqual(4, audioClip.length, 0.1);

            float[] samples = new float[audioClip.samples * audioClip.channels];
            audioClip.GetData(samples, 0);
            Assert.IsTrue(samples.Any(sample => sample != 0));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }
}
