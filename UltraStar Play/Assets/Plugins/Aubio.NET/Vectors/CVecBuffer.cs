using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Aubio.NET.Vectors
{
    internal abstract class CVecBuffer : IVector<float>
    {
        internal unsafe CVecBuffer([NotNull] CVec cVec, [NotNull] float* data, int length)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            CVec = cVec ?? throw new ArgumentNullException(nameof(cVec));
        }

        protected CVec CVec { get; }

        public abstract float this[int index] { get; set; }

        public int Length => CVec.Length;

        public IEnumerator<float> GetEnumerator()
        {
            return new VectorEnumerator<float>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public abstract void SetAll(float value);

        public abstract void Ones();

        public abstract void Zeros();

        public abstract unsafe float* GetData();
    }
}