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
    ///     https://aubio.org/doc/latest/cvec_8h.html
    /// </summary>
    public sealed class CVec : AubioObject, IVector<CVecComplex>
    {
        #region Fields

        [NotNull]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal readonly unsafe CVec__* Handle;

        #endregion

        #region Implementation of IVector<CVecComplex>

        public CVecComplex this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();

                var norm = Norm[index];
                var phas = Phas[index];
                var complex = new CVecComplex(norm, phas);
                return complex;
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();

                Norm[index] = value.Norm;
                Phas[index] = value.Phas;
            }
        }

        [PublicAPI]
        public unsafe int Length => (int) Handle->Length;

        public void SetAll(CVecComplex complex)
        {
            Norm.SetAll(complex.Norm);
            Phas.SetAll(complex.Phas);
        }

        public void Ones()
        {
            Norm.Ones();
            Phas.Ones();
        }

        [PublicAPI]
        public unsafe void Zeros()
        {
            cvec_zeros(Handle);
        }

        public IEnumerator<CVecComplex> GetEnumerator()
        {
            return new VectorEnumerator<CVecComplex>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Public members

        [PublicAPI]
        public unsafe CVec(int length)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            var handle = new_cvec((uint) length);
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));

            Handle = handle;

            Norm = new CVecBufferNorm(this, handle->Norm, (int) handle->Length); // length is different here !
            Phas = new CVecBufferPhas(this, handle->Phas, (int) handle->Length); // length is different here !
        }

        [PublicAPI]
        public IVector<float> Norm { get; }

        [PublicAPI]
        public IVector<float> Phas { get; }

        [PublicAPI]
        public unsafe void Copy([NotNull] CVec target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (target.Length != Length)
                throw new ArgumentOutOfRangeException(nameof(target));

            cvec_copy(Handle, target.Handle);
        }

        [PublicAPI]
        public unsafe void LogMag(float lambda)
        {
            cvec_logmag(Handle, lambda);
        }

        [PublicAPI]
        public unsafe void Print()
        {
            cvec_print(Handle);
        }

        #endregion

        #region Overrides of AubioObject

        protected override unsafe void DisposeNative()
        {
            del_cvec(Handle);
        }

        #endregion

        #region Native Methods

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe CVec__* new_cvec(
            uint length
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void del_cvec(
            CVec__* cVec
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void cvec_copy(
            CVec__* cVec,
            CVec__* target
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void cvec_logmag(
            CVec__* cVec,
            float lambda
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void cvec_print(
            CVec__* cVec
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport("aubio", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void cvec_zeros(
            CVec__* cVec
        );

        #endregion
    }
}