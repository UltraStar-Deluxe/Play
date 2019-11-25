using System.ComponentModel;
using JetBrains.Annotations;

namespace Aubio.NET.Vectors
{
    [PublicAPI]
    public enum FVecWindowType
    {
        [Description("default")]
        Default,

        [Description("ones")]
        Ones,

        [Description("rectangle")]
        Rectangle,

        [Description("hamming")]
        Hamming,

        [Description("hanning")]
        Hanning,

        [Description("hanningz")]
        Hanningz,

        [Description("blackman")]
        Blackman,

        [Description("blackman_harris")]
        BlackmanHarris,

        [Description("gaussian")]
        Gaussian,

        [Description("welch")]
        Welch,

        [Description("parzen")]
        Parzen
    }
}