using JetBrains.Annotations;

namespace Aubio.NET.Vectors
{
    [PublicAPI]
    public struct CVecComplex
    {
        public readonly float Norm;
        public readonly float Phas;

        public CVecComplex(float norm, float phas)
        {
            Norm = norm;
            Phas = phas;
        }

        public override string ToString()
        {
            return $"{nameof(Norm)}: {Norm}, {nameof(Phas)}: {Phas}";
        }
    }
}