using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using JetBrains.Annotations;

namespace Aubio.NET.Vectors
{
    /// <summary>
    ///     https://aubio.org/doc/latest/fvec_8h.html
    /// </summary>
    public sealed class FMat : AubioObject, IEnumerable<IEnumerable<float>>
    {
        #region Fields

        [PublicAPI]
        [NotNull]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal readonly unsafe FMat__* Handle;

        #endregion

        #region Public Members

        internal unsafe FMat([NotNull] FMat__* handle, bool isDisposable)
            : base(isDisposable)
        {
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));

            Handle = handle;
        }

        public unsafe FMat(int rows, int columns)
        {
            if (rows <= 0)
                throw new ArgumentOutOfRangeException(nameof(rows));

            if (columns <= 0)
                throw new ArgumentOutOfRangeException(nameof(columns));

            var handle = new_fmat((uint) rows, (uint) columns);
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));

            Handle = handle;
        }

        [PublicAPI]
        public unsafe int Rows => (int) Handle->Height;

        [PublicAPI]
        public unsafe int Columns => (int) Handle->Length;

        [PublicAPI]
        public unsafe float this[int row, int column]
        {
            get
            {
                if (row < 0 || row >= Rows)
                    throw new ArgumentOutOfRangeException(nameof(row));

                if (column < 0 || column >= Columns)
                    throw new ArgumentOutOfRangeException(nameof(column));

                return fmat_get_sample(Handle, (uint) row, (uint) column);
            }
            set
            {
                if (row < 0 || row >= Rows)
                    throw new ArgumentOutOfRangeException(nameof(row));

                if (column < 0 || column >= Columns)
                    throw new ArgumentOutOfRangeException(nameof(column));

                fmat_set_sample(Handle, value, (uint) row, (uint) column);
            }
        }

        public IEnumerator<IEnumerable<float>> GetEnumerator()
        {
            IEnumerable<IEnumerable<float>> Rows()
            {
                IEnumerable<float> Row(int row)
                {
                    for (var col = 0; col < Columns; col++)
                        yield return this[row, col];
                }

                for (var row = 0; row < this.Rows; row++)
                    yield return Row(row);
            }

            var enumerable = Rows();
            var enumerator = enumerable.GetEnumerator();
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [PublicAPI]
        public unsafe void Copy([NotNull] FMat target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (target.Rows != Rows || target.Columns != Columns)
                throw new ArgumentOutOfRangeException(nameof(target));

            fmat_copy(Handle, target.Handle);
        }

        [PublicAPI]
        public unsafe FVec GetChannel(int row)
        {
            if (row < 0 || row >= Rows)
                throw new ArgumentOutOfRangeException(nameof(row));

            // return a non-disposable copy since they just assign pointers in this call

            var output = new FVec(Columns, false);

            fmat_get_channel(Handle, (uint) row, output.Handle);

            return output;
        }

        [PublicAPI]
        public unsafe float* GetChannelData(int channel)
        {
            if (channel < 0 || channel >= Rows)
                throw new ArgumentOutOfRangeException(nameof(channel));

            return fmat_get_channel_data(Handle, (uint) channel);
        }

        [PublicAPI]
        public unsafe float** GetData()
        {
            return fmat_get_data(Handle);
        }

        [PublicAPI]
        public unsafe void Ones()
        {
            fmat_ones(Handle);
        }

        [PublicAPI]
        public unsafe void Print()
        {
            fmat_print(Handle);
        }

        [PublicAPI]
        public unsafe void Rev()
        {
            fmat_rev(Handle);
        }

        [PublicAPI]
        public unsafe void Set(float value)
        {
            fmat_set(Handle, value);
        }

        [PublicAPI]
        public unsafe void VecMul([NotNull] FVec scale, [NotNull] FVec output)
        {
            if (scale == null)
                throw new ArgumentNullException(nameof(scale));

            if (output == null)
                throw new ArgumentNullException(nameof(output));

            if (scale.Length != Columns)
                throw new ArgumentOutOfRangeException(nameof(scale));

            if (output.Length != Rows)
                throw new ArgumentOutOfRangeException(nameof(output));

            fmat_vecmul(Handle, scale.Handle, output.Handle);
        }

        [PublicAPI]
        public unsafe void Weight([NotNull] FMat weight)
        {
            if (weight == null)
                throw new ArgumentNullException(nameof(weight));

            fmat_weight(Handle, weight.Handle);
        }

        [PublicAPI]
        public unsafe void Zeros()
        {
            fmat_zeros(Handle);
        }

        #endregion

        #region Overrides of AubioObject

        protected override unsafe void DisposeNative()
        {
            del_fmat(Handle);
        }

        #endregion

        #region Native Methods

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe FMat__* new_fmat(
            uint height,
            uint length
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void del_fmat(
            FMat__* fMat
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void fmat_copy(
            FMat__* fMat,
            FMat__* target
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void fmat_get_channel(
            FMat__* fMat,
            uint channel,
            FVec__* output
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe float* fmat_get_channel_data(
            FMat__* fMat,
            uint channel
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe float** fmat_get_data(
            FMat__* fMat
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe float fmat_get_sample(
            FMat__* fMat,
            uint channel,
            uint position
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void fmat_ones(
            FMat__* fMat
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void fmat_print(
            FMat__* fMat
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void fmat_rev(
            FMat__* fMat
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void fmat_set(
            FMat__* fMat,
            float value
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void fmat_set_sample(
            FMat__* fMat,
            float value,
            uint channel,
            uint position
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void fmat_vecmul(
            FMat__* fMat,
            FVec__* scale,
            FVec__* output
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void fmat_weight(
            FMat__* fMat,
            FMat__* weight
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void fmat_zeros(
            FMat__* fMat
        );

        #endregion
    }
}