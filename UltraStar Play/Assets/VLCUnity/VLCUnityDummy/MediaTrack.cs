#if HAS_VLC_UNITY
#else

namespace LibVLCSharp
{
    public class MediaTrackList
    {
        public int Count { get; set; }

        public MediaTrack this[int index]
        {
            get => null;
            set
            {
            }
        }
    }

    public class MediaTrack
    {
        public MediaTrackData Data { get; set; }

    }

    public class MediaTrackData
    {
        public MediaTrackDataVideo Video { get; set; }
    }

    public class MediaTrackDataVideo
    {
        public VideoOrientation Orientation { get; set; }
    }
}

#endif
