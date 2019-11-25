using System;
using System.Runtime.InteropServices;
using System.Security;
using JetBrains.Annotations;

namespace Aubio.NET
{
    [PublicAPI]
    public static class Converters
    {
        public static float SamplesToMilliseconds(int sampleRate, int samples)
        {
            return samples * 1000.0f / sampleRate;
        }

        public static float SamplesToSeconds(int sampleRate, int samples)
        {
            var milliseconds = SamplesToMilliseconds(sampleRate, samples);
            var seconds = MillisecondsToSeconds(milliseconds);
            return seconds;
        }

        public static int MillisecondsToSamples(int sampleRate, float milliseconds)
        {
            var samples = (int) (milliseconds * sampleRate * 0.001f);
            return samples;
        }

        public static float MillisecondsToSeconds(float milliseconds)
        {
            var seconds = milliseconds * 0.001f;
            return seconds;
        }

        public static float SecondsToMilliseconds(float seconds)
        {
            var milliseconds = seconds * 1000.0f;
            return milliseconds;
        }

        public static int SecondsToSamples(int sampleRate, float seconds)
        {
            var milliseconds = SecondsToMilliseconds(seconds);
            var samples = MillisecondsToSamples(sampleRate, milliseconds);
            return samples;
        }

        public static float BinToMidi(float bin, float sampleRate, float fftSize)
        {
            return aubio_bintomidi(bin, sampleRate, fftSize);
        }

        public static float MidiToBin(float midi, float sampleRate, float fftSize)
        {
            return aubio_miditobin(midi, sampleRate, fftSize);
        }

        public static float BinToFreq(float bin, float sampleRate, float fftSize)
        {
            return aubio_bintofreq(bin, sampleRate, fftSize);
        }

        public static float FreqToBin(float freq, float sampleRate, float fftSize)
        {
            return aubio_freqtobin(freq, sampleRate, fftSize);
        }

        public static float FreqToMidi(float freq)
        {
            return aubio_freqtomidi(freq);
        }

        public static float MidiToFreq(float midi)
        {
            return aubio_miditofreq(midi);
        }

        public static float Unwrap2Pi(float phase)
        {
            return aubio_unwrap2pi(phase);
        }

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern float aubio_unwrap2pi(
            float phase
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern float aubio_bintomidi(
            float bin,
            float sampleRate,
            float fftSize
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern float aubio_miditobin(
            float midi,
            float sampleRate,
            float fftSize
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern float aubio_bintofreq(
            float bin,
            float sampleRate,
            float fftSize
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern float aubio_freqtobin(
            float freq,
            float sampleRate,
            float fftSize
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern float aubio_freqtomidi(
            float freq
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern float aubio_miditofreq(
            float midi
        );
    }
}