using System.ComponentModel;
using JetBrains.Annotations;

namespace Aubio.NET.Detection
{
    [PublicAPI]
    public enum PitchUnit
    {
        [Description("default")]
        Default = 0,

        [Description("hertz")]
        Hertz,

        [Description("midi")]
        Midi,

        [Description("cent")]
        Cent,

        [Description("bin")]
        Bin
    }
}