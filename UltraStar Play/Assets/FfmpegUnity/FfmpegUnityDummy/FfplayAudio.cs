#if HAS_FFMPEG_UNITY
#else

using UnityEngine;

namespace FfmpegUnity
{
    public class FfplayAudio : MonoBehaviour
    {
        protected virtual void OnAudioFilterRead(float[] data, int channels)
        {
        }
    }
}

#endif
