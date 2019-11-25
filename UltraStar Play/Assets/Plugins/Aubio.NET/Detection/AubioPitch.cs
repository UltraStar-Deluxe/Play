using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using Aubio.NET.Vectors;
using JetBrains.Annotations;

namespace Aubio.NET.Detection
{
    public sealed class AubioPitch : AubioObject, ISampler
    {
        #region Fields

        [PublicAPI]
        [NotNull]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal readonly unsafe Pitch__* Handle;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private PitchUnit _unit = PitchUnit.Default;

        #endregion

        #region Implementation of ISampler

        public int SampleRate { get; }

        #endregion

        #region Public Members

        [PublicAPI]
        public unsafe AubioPitch(PitchDetection detection, int bufferSize = 1024, int hopSize = 256, int sampleRate = 44100)
        {
            if (bufferSize <= 1)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            if (hopSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(hopSize));

            if (bufferSize < hopSize)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            if (sampleRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(sampleRate));

            SampleRate = sampleRate;

            var attribute = detection.GetDescriptionAttribute();
            var method = attribute.Description;

            var handle = new_aubio_pitch(method, (uint)bufferSize, (uint)hopSize, (uint)sampleRate);
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));

            Handle = handle;
        }

        [PublicAPI]
        public unsafe float Confidence => aubio_pitch_get_confidence(Handle);

        [PublicAPI]
        public unsafe float Silence
        {
            get => aubio_pitch_get_silence(Handle);
            set
            {
                if (aubio_pitch_set_silence(Handle, value))
                    throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        [PublicAPI]
        public unsafe float Tolerance
        {
            get => aubio_pitch_get_tolerance(Handle);
            set
            {
                if (aubio_pitch_set_tolerance(Handle, value))
                    throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        [PublicAPI]
        public unsafe PitchUnit Unit
        {
            get => _unit;
            set
            {
                var attribute = value.GetDescriptionAttribute();
                var description = attribute.Description;

                if (aubio_pitch_set_unit(Handle, description))
                    throw new ArgumentOutOfRangeException(nameof(value));

                _unit = value;
            }
        }

        [PublicAPI]
        public unsafe void Do([NotNull] FVec input, [NotNull] FVec output)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (output == null)
                throw new ArgumentNullException(nameof(output));

            aubio_pitch_do(Handle, input.Handle, output.Handle);
        }

        #endregion

        #region Overrides of AubioObject

        protected override unsafe void DisposeNative()
        {
            del_aubio_pitch(Handle);
        }

        #endregion

        #region Native Methods

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe Pitch__* new_aubio_pitch(
            [MarshalAs(UnmanagedType.LPStr)] string method,
            uint bufferSize,
            uint hopSize,
            uint sampleRate
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void del_aubio_pitch(
            Pitch__* pitch
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void aubio_pitch_do(
            Pitch__* pitch,
            FVec__* input,
            FVec__* output
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe float aubio_pitch_get_confidence(
            Pitch__* pitch
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe float aubio_pitch_get_silence(
            Pitch__* pitch
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe float aubio_pitch_get_tolerance(
            Pitch__* pitch
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern unsafe bool aubio_pitch_set_silence(
            Pitch__* pitch,
            float silence
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern unsafe bool aubio_pitch_set_tolerance(
            Pitch__* pitch,
            float tolerance
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern unsafe bool aubio_pitch_set_unit(
            Pitch__* pitch,
            [MarshalAs(UnmanagedType.LPStr)] string pitchUnit
        );

        #endregion
    }
}