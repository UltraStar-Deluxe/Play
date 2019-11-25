using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Aubio.NET
{
    [PublicAPI]
    public static class AubioUtils
    {
        public static void Cleanup()
        {
            aubio_cleanup();
        }

        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern void aubio_cleanup();
    }
}