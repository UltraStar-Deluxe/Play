#if HAS_VLC_UNITY
#else

using System;

namespace LibVLCSharp
{
    public class LibVLC : IDisposable
    {
        public LibVLC(bool enableDebugLogs)
        {
        }

        public string Changeset { get; set; }
        public event Action<object, VlcLogEvent> Log;

        public void Dispose()
        {
        }
    }

    public class VlcLogEvent
    {
        public string FormattedLog { get; set; }
    }
}

#endif
