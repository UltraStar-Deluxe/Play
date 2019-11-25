using System.ComponentModel;
using JetBrains.Annotations;

namespace Aubio.NET.Detection
{
    [PublicAPI]
    public enum PitchDetection
    {
        [Description("default")]
        Default,

        [Description("mcomb")]
        MultipleCombFilter,

        [Description("schmitt")]
        SchimttTrigger,

        [Description("fcomb")]
        FastHarmonicCombFilter,

        [Description("yin")]
        Yin,

        [Description("yinfft")]
        YinFft,

        [Description("yinfast")]
        YinFast,

        [Description("specacf")]
        SpectralAutoCorrelation
    }
}