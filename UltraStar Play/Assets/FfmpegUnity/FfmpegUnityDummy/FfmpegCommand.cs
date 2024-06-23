#if HAS_FFMPEG_UNITY
#else

using System;
using UnityEngine;

namespace FfmpegUnity
{
    public class FfmpegCommand : MonoBehaviour
    {
        public string Options { get; set; }
        public bool IsRunning { get; set; }
        public bool IsFinished { get; set; }
        public TimeSpan DurationTime { get; set; }
        public double Progress { get; set; }
        public bool ExecuteOnStart { get; set; }
        public bool GetProgressOnScript { get; set; }
        public bool PrintStdErr { get; set; }

        public void StartFfmpeg()
        {
        }

        public void StopFfmpeg()
        {
        }
    }
}

#endif
