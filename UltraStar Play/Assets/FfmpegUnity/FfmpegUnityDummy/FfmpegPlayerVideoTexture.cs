#if HAS_FFMPEG_UNITY
#else

using UnityEngine;

namespace FfmpegUnity
{
    public class FfmpegPlayerVideoTexture
    {
        public RenderTexture VideoTexture { get; set; }
    }
}

#endif
