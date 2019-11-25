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
    ///     https://aubio.org/doc/latest/lvec_8h.html
    /// </summary>
    public sealed class LVec : AubioObject, IVector<double>
    {
        #region Fields

        [NotNull]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal readonly unsafe LVec__* Handle;

        #endregion

        #region Implementation of IVector<double>

        public unsafe double this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();

                return lvec_get_sample(Handle, (uint) index);
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();

                lvec_set_sample(Handle, value, (uint) index);
            }
        }

        public unsafe int Length => (int) Handle->Length;

        public IEnumerator<double> GetEnumerator()
        {
            return new VectorEnumerator<double>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public unsafe void SetAll(double value)
        {
            lvec_set_all(Handle, (float) value);
        }

        public unsafe void Ones()
        {
            lvec_ones(Handle);
        }

        public unsafe void Zeros()
        {
            lvec_zeros(Handle);
        }

        #endregion

        #region Public Members

        internal unsafe LVec([NotNull] LVec__* handle, bool isDisposable)
            : base(isDisposable)
        {
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));

            Handle = handle;
        }

        [PublicAPI]
        public unsafe LVec(int length)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            var handle = new_lvec((uint) length);
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));

            Handle = handle;
        }

        [PublicAPI]
        public unsafe double* GetData()
        {
            return lvec_get_data(Handle);
        }

        #endregion

        #region Overrides of AubioObject

        protected override unsafe void DisposeNative()
        {
            del_lvec(Handle);
        }

        #endregion

        #region Native Methods

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe LVec__* new_lvec(
            uint length
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void del_lvec(
            LVec__* lVec
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe double* lvec_get_data(
            LVec__* lVec
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe double lvec_get_sample(
            LVec__* lVec,
            uint position
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void lvec_set_sample(
            LVec__* lVec,
            double value,
            uint position
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void lvec_ones(
            LVec__* lVec
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void lvec_print(
            LVec__* lVec
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void lvec_set_all(
            LVec__* lVec,
            float value
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void lvec_zeros(
            LVec__* lVec
        );

        #endregion
    }
}