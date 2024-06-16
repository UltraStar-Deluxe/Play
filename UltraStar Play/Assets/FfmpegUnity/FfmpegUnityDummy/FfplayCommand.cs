#if HAS_FFMPEG_UNITY
#else

using UnityEngine;

namespace FfmpegUnity
{
    public class FfplayCommand : MonoBehaviour
    {
        public FfmpegPlayerVideoTexture VideoTexture { get; set; }
        public string InputPath { get; set; }
        public bool Paused { get; set; }
        public bool IsRunning { get; set; }
        public double CurrentTime { get; set; }
        public double Duration { get; set; }
        public AudioSource AudioSourceComponent { get; set; }

        public void Play()
        {
        }

        public void Stop()
        {
        }

        public void TogglePause()
        {
        }

        public void SeekTime(double value)
        {
        }
    }
}

#endif
