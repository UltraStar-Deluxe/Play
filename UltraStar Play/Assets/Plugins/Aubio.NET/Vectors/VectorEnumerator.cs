using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Aubio.NET.Vectors
{
    internal sealed class VectorEnumerator<T> : IEnumerator<T>
    {
        private readonly IVector<T> _vector;
        private T _current;
        private int _index;

        public VectorEnumerator([NotNull] IVector<T> vector)
        {
            _vector = vector ?? throw new ArgumentNullException(nameof(vector));
            Reset();
        }

        void IDisposable.Dispose()
        {
        }

        public bool MoveNext()
        {
            if (++_index >= _vector.Length)
                return false;

            _current = _vector[_index];
            return true;
        }

        public void Reset()
        {
            _index = -1;
        }

        [SuppressMessage("ReSharper", "ConvertToAutoPropertyWithPrivateSetter")]
        public T Current => _current;

        object IEnumerator.Current => Current;
    }
}