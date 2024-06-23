#if HAS_VLC_UNITY
#else

using System;

namespace LibVLCSharp
{
    public class MediaPlayer : IDisposable
    {
        public MediaPlayer(LibVLC libVlc)
        {

        }

        public Media Media { get; set; }
        public IntPtr NativeReference { get; set; }
        public long Time { get; set; }
        public bool IsPlaying { get; set; }
        public double Length { get; set; }

        public MediaTrackList Tracks(object video)
        {
            throw new NotImplementedException();
        }

        public void SetAudioCallbacks(Action<IntPtr, IntPtr, uint, long> onVlcAudioPlayMuted, Action<IntPtr, long> onVlcAudioPauseMuted, Action<IntPtr, long> onVlcAudioResumeMuted, Action<IntPtr, long> onVlcAudioFlushMuted, Action<IntPtr> onVlcAudioDrainMuted)
        {
        }

        public IntPtr GetTexture(uint px, uint py, out bool b)
        {
            throw new NotImplementedException();
        }

        public void Size(int i, ref uint width, ref uint height)
        {
        }

        public void Dispose()
        {

        }

        public void Stop()
        {

        }

        public void Play()
        {
        }

        public void Pause()
        {
        }

        public void SetTime(long max)
        {

        }

        public void SetVolume(int volume)
        {
        }
    }
}

#endif
