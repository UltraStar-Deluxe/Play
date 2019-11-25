using System.Diagnostics.CodeAnalysis;

namespace Aubio.NET.Vectors
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal struct CVec__
    {
#pragma warning disable 649
        public readonly uint Length;
#pragma warning disable 169
        public readonly unsafe float* Norm;
        public readonly unsafe float* Phas;
#pragma warning restore 169
#pragma warning restore 649
    }
}