using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Pitch
{
    public class CircularBuffer<T> : IDisposable
    {
        int m_bufSize;
        int m_begBufOffset;
        int m_availBuf;
        long m_startPos;   // total circular buffer position
        T[] m_buffer;

        /// <summary>
        /// Constructor
        /// </summary>
        public CircularBuffer()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bufCount"></param>
        public CircularBuffer(int bufCount)
        {
            SetSize(bufCount);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            SetSize(0);
        }

        /// <summary>
        /// Reset to the beginning of the buffer
        /// </summary>
        public void Reset()
        {
            m_begBufOffset = 0;
            m_availBuf = 0;
            m_startPos = 0;
        }

        /// <summary>
        /// Set the buffer to the specified size
        /// </summary>
        /// <param name="newSize"></param>
        public void SetSize(int newSize)
        {
            Reset();

            if (m_bufSize == newSize)
                return;

            if (m_buffer != null)
                m_buffer = null;

            m_bufSize = newSize;

            if (m_bufSize > 0)
                m_buffer = new T[m_bufSize];
        }

        /// <summary>
        /// Clear the buffer
        /// </summary>
        public void Clear()
        {
            Array.Clear(m_buffer, 0, m_buffer.Length);
        }

        /// <summary>
        /// Get or set the start position
        /// </summary>
        public long StartPosition
        {
            get { return m_startPos; }
            set { m_startPos = value; }
        }

        /// <summary>
        /// Get the end position
        /// </summary>
        public long EndPosition
        {
            get { return m_startPos + m_availBuf; }
        }

        /// <summary>
        /// Get or set the amount of avaliable space
        /// </summary>
        public int Available
        {
            get { return m_availBuf; }
            set { m_availBuf = Math.Min(value, m_bufSize); }
        }

        /// <summary>
        /// Write data into the buffer
        /// </summary>
        /// <param name="m_pInBuffer"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public int WriteBuffer(T[] m_pInBuffer, int count)
        {
            count = Math.Min(count, m_bufSize);

            var startPos = m_availBuf != m_bufSize ? m_availBuf : m_begBufOffset;
            var pass1Count = Math.Min(count, m_bufSize - startPos);
            var pass2Count = count - pass1Count;

            PitchDsp.CopyBuffer(m_pInBuffer, 0, m_buffer, startPos, pass1Count);

            if (pass2Count > 0)
                PitchDsp.CopyBuffer(m_pInBuffer, pass1Count, m_buffer, 0, pass2Count);

            if (pass2Count == 0)
            {
                // did not wrap around
                if (m_availBuf != m_bufSize)
                    m_availBuf += count;   // have never wrapped around
                else
                {
                    m_begBufOffset += count;
                    m_startPos += count;
                }
            }
            else
            {
                // wrapped around
                if (m_availBuf != m_bufSize)
                    m_startPos += pass2Count;  // first time wrap-around
                else
                    m_startPos += count;

                m_begBufOffset = pass2Count;
                m_availBuf = m_bufSize;
            }

            return count;
        }

        /// <summary>
        /// Read from the buffer
        /// </summary>
        /// <param name="outBuffer"></param>
        /// <param name="startRead"></param>
        /// <param name="readCount"></param>
        /// <returns></returns>
        public bool ReadBuffer(T[] outBuffer, long startRead, int readCount)
        {
            var endRead = (int)(startRead + readCount);
            var endAvail = (int)(m_startPos + m_availBuf);

            if (startRead < m_startPos || endRead > endAvail)
                return false;

            var startReadPos = (int)(((startRead - m_startPos) + m_begBufOffset) % m_bufSize);
            var block1Samples = Math.Min(readCount, m_bufSize - startReadPos);
            var block2Samples = readCount - block1Samples;

            PitchDsp.CopyBuffer(m_buffer, startReadPos, outBuffer, 0, block1Samples);

            if (block2Samples > 0)
                PitchDsp.CopyBuffer(m_buffer, 0, outBuffer, block1Samples, block2Samples);

            return true;
        }
    }
}
