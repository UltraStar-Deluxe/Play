using System;

namespace LibVLCSharp
{
    public class Media : IDisposable
    {
        public Media(Uri uri)
        {
        }

        public double Duration { get; set; }
        public string Mrl { get; set; }

        public void Dispose()
        {

        }
    }
}
