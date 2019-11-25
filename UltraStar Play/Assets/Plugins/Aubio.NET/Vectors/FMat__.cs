using System.Diagnostics.CodeAnalysis;

namespace Aubio.NET.Vectors
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal struct FMat__
    {
#pragma warning disable 649
        public readonly uint Length;
        public readonly uint Height;
#pragma warning disable 169
        public readonly unsafe float** Data;
#pragma warning restore 169
#pragma warning restore 649
    }
}