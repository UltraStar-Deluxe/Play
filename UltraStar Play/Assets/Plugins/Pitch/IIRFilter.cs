using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pitch
{
    /// <summary>
    /// Infinite impulse response filter (old style analog filters)
    /// </summary>
    class IIRFilter
    {
        /// <summary>
        /// The type of filter
        /// </summary>
        public enum FilterType
        {
            None = 0,
            LP,
            HP,
            BP
        }

        /// <summary>
        /// The filter prototype
        /// </summary>
        public enum ProtoType
        {
            None = 0,
            Butterworth,
            Chebyshev,
        }

        const int kHistMask = 31;
        const int kHistSize = 32;

        private int m_order;
        private ProtoType m_protoType;
        private FilterType m_filterType;

        private float m_fp1;
        private float m_fp2;
        private float m_fN;
        private float m_ripple;
        private float m_sampleRate;
        private double[] m_real;
        private double[] m_imag;
        private double[] m_z;
        private double[] m_aCoeff;
        private double[] m_bCoeff;
        private double[] m_inHistory;
        private double[] m_outHistory;
        private int m_histIdx;
        private bool m_invertDenormal;

        public IIRFilter()
        {
        }

        /// <summary>
        /// Returns true if all the filter parameters are valid
        /// </summary>
        public bool FilterValid
        {
            get
            {
                if (m_order < 1 || m_order > 16 ||
                    m_protoType == ProtoType.None ||
                    m_filterType == FilterType.None ||
                    m_sampleRate <= 0.0f ||
                    m_fN <= 0.0f)
                    return false;

                switch (m_filterType)
                {
                    case FilterType.LP:
                        if (m_fp2 <= 0.0f)
                            return false;
                        break;

                    case FilterType.BP:
                        if (m_fp1 <= 0.0f || m_fp2 <= 0.0f || m_fp1 >= m_fp2)
                            return false;
                        break;

                    case FilterType.HP:
                        if (m_fp1 <= 0.0f)
                            return false;
                        break;
                }

                // For bandpass, the order must be even
                if (m_filterType == FilterType.BP && (m_order & 1) != 0)
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Set the filter prototype
        /// </summary>
        public ProtoType Proto
        {
            get { return m_protoType; }

            set
            {
                m_protoType = value;
                Design();
            }
        }

        /// <summary>
        /// Set the filter type
        /// </summary>
        public FilterType Type
        {
            get { return m_filterType; }

            set
            {
                m_filterType = value;
                Design();
            }
        }

        public int Order
        {
            get { return m_order; }

            set
            {
                m_order = Math.Min(16, Math.Max(1, Math.Abs(value)));

                if (m_filterType == FilterType.BP && Odd(m_order))
                    m_order++;

                Design();
            }
        }

        public float SampleRate
        {
            get { return m_sampleRate; }

            set
            {
                m_sampleRate = value;
                m_fN = 0.5f * m_sampleRate;
                Design();
            }
        }

        public float FreqLow
        {
            get { return m_fp1; }

            set
            {
                m_fp1 = value;
                Design();
            }
        }

        public float FreqHigh
        {
            get { return m_fp2; }

            set
            {
                m_fp2 = value;
                Design();
            }
        }

        public float Ripple
        {
            get { return m_ripple; }

            set
            {
                m_ripple = value;
                Design();
            }
        }

        /// <summary>
        /// Returns true if n is odd
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private bool Odd(int n)
        {
            return (n & 1) == 1;
        }

        /// <summary>
        /// Square
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        private float Sqr(float value)
        {
            return value * value;
        }

        /// <summary>
        /// Square
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        private double Sqr(double value)
        {
            return value * value;
        }

        /// <summary>
        /// Determines poles and zeros of IIR filter
        /// based on bilinear transform method
        /// </summary>
        private void LocatePolesAndZeros()
        {
            m_real = new double[m_order + 1];
            m_imag = new double[m_order + 1];
            m_z = new double[m_order + 1];
            double ln10 = Math.Log(10.0);

            // Butterworth, Chebyshev parameters
            int n = m_order;

            if (m_filterType == FilterType.BP)
                n = n / 2;

            int ir = n % 2;
            int n1 = n + ir;
            int n2 = (3 * n + ir) / 2 - 1;
            double f1;

            switch (m_filterType)
            {
                case FilterType.LP:
                    f1 = m_fp2;
                    break;

                case FilterType.HP:
                    f1 = m_fN - m_fp1;
                    break;

                case FilterType.BP:
                    f1 = m_fp2 - m_fp1;
                    break;

                default:
                    f1 = 0.0;
                    break;
            }

            double tanw1 = Math.Tan(0.5 * Math.PI * f1 / m_fN);
            double tansqw1 = Sqr(tanw1);

            // Real and Imaginary parts of low-pass poles
            double t, a = 1.0, r = 1.0, i = 1.0;

            for (int k = n1; k <= n2; k++)
            {
                t = 0.5 * (2 * k + 1 - ir) * Math.PI / (double)n;

                switch (m_protoType)
                {
                    case ProtoType.Butterworth:
                        double b3 = 1.0 - 2.0 * tanw1 * Math.Cos(t) + tansqw1;
                        r = (1.0 - tansqw1) / b3;
                        i = 2.0 * tanw1 * Math.Sin(t) / b3;
                        break;

                    case ProtoType.Chebyshev:
                        double d = 1.0 - Math.Exp(-0.05 * m_ripple * ln10);
                        double e = 1.0 / Math.Sqrt(1.0 / Sqr(1.0 - d) - 1.0);
                        double x = Math.Pow(Math.Sqrt(e * e + 1.0) + e, 1.0 / (double)n);
                        a = 0.5 * (x - 1.0 / x);
                        double b = 0.5 * (x + 1.0 / x);
                        double c3 = a * tanw1 * Math.Cos(t);
                        double c4 = b * tanw1 * Math.Sin(t);
                        double c5 = Sqr(1.0 - c3) + Sqr(c4);
                        r = 2.0 * (1.0 - c3) / c5 - 1.0;
                        i = 2.0 * c4 / c5;
                        break;
                }

                int m = 2 * (n2 - k) + 1;
                m_real[m + ir] = r;
                m_imag[m + ir] = Math.Abs(i);
                m_real[m + ir + 1] = r;
                m_imag[m + ir + 1] = -Math.Abs(i);
            }

            if (Odd(n))
            {
                if (m_protoType == ProtoType.Butterworth)
                    r = (1.0 - tansqw1) / (1.0 + 2.0 * tanw1 + tansqw1);

                if (m_protoType == ProtoType.Chebyshev)
                    r = 2.0 / (1.0 + a * tanw1) - 1.0;

                m_real[1] = r;
                m_imag[1] = 0.0;
            }

            switch (m_filterType)
            {
                case FilterType.LP:
                    for (int m = 1; m <= n; m++)
                        m_z[m] = -1.0;
                    break;

                case FilterType.HP:
                    // Low-pass to high-pass transformation
                    for (int m = 1; m <= n; m++)
                    {
                        m_real[m] = -m_real[m];
                        m_z[m] = 1.0;
                    }
                    break;

                case FilterType.BP:
                    // Low-pass to bandpass transformation
                    for (int m = 1; m <= n; m++)
                    {
                        m_z[m] = 1.0;
                        m_z[m + n] = -1.0;
                    }

                    double f4 = 0.5 * Math.PI * m_fp1 / m_fN;
                    double f5 = 0.5 * Math.PI * m_fp2 / m_fN;
                    double aa = Math.Cos(f4 + f5) / Math.Cos(f5 - f4);
                    double aR, aI, h1, h2, p1R, p2R, p1I, p2I;

                    for (int m1 = 0; m1 <= (m_order - 1) / 2; m1++)
                    {
                        int m = 1 + 2 * m1;
                        aR = m_real[m];
                        aI = m_imag[m];

                        if (Math.Abs(aI) < 0.0001)
                        {
                            h1 = 0.5 * aa * (1.0 + aR);
                            h2 = Sqr(h1) - aR;
                            if (h2 > 0.0)
                            {
                                p1R = h1 + Math.Sqrt(h2);
                                p2R = h1 - Math.Sqrt(h2);
                                p1I = 0.0;
                                p2I = 0.0;
                            }
                            else
                            {
                                p1R = h1;
                                p2R = h1;
                                p1I = Math.Sqrt(Math.Abs(h2));
                                p2I = -p1I;
                            }
                        }
                        else
                        {
                            double fR = aa * 0.5 * (1.0 + aR);
                            double fI = aa * 0.5 * aI;
                            double gR = Sqr(fR) - Sqr(fI) - aR;
                            double gI = 2 * fR * fI - aI;
                            double sR = Math.Sqrt(0.5 * Math.Abs(gR + Math.Sqrt(Sqr(gR) + Sqr(gI))));
                            double sI = gI / (2.0 * sR);
                            p1R = fR + sR;
                            p1I = fI + sI;
                            p2R = fR - sR;
                            p2I = fI - sI;
                        }

                        m_real[m] = p1R;
                        m_real[m + 1] = p2R;
                        m_imag[m] = p1I;
                        m_imag[m + 1] = p2I;
                    }

                    if (Odd(n))
                    {
                        m_real[2] = m_real[n + 1];
                        m_imag[2] = m_imag[n + 1];
                    }

                    for (int k = n; k >= 1; k--)
                    {
                        int m = 2 * k - 1;
                        m_real[m] = m_real[k];
                        m_real[m + 1] = m_real[k];
                        m_imag[m] = Math.Abs(m_imag[k]);
                        m_imag[m + 1] = -Math.Abs(m_imag[k]);
                    }

                    break;
            }
        }

        /// <summary>
        /// Calculate all the values
        /// </summary>
        public void Design()
        {
            if (!this.FilterValid)
                return;

            m_aCoeff = new double[m_order + 1];
            m_bCoeff = new double[m_order + 1];
            m_inHistory = new double[kHistSize];
            m_outHistory = new double[kHistSize];

            double[] newA = new double[m_order + 1];
            double[] newB = new double[m_order + 1];

            // Find filter poles and zeros
            LocatePolesAndZeros();

            // Compute filter coefficients from pole/zero values
            m_aCoeff[0] = 1.0;
            m_bCoeff[0] = 1.0;

            for (int i = 1; i <= m_order; i++)
            {
                m_aCoeff[i] = 0.0;
                m_bCoeff[i] = 0.0;
            }

            int k = 0;
            int n = m_order;
            int pairs = n / 2;

            if (Odd(m_order))
            {
                // First subfilter is first order
                m_aCoeff[1] = -m_z[1];
                m_bCoeff[1] = -m_real[1];
                k = 1;
            }

            for (int p = 1; p <= pairs; p++)
            {
                int m = 2 * p - 1 + k;
                double alpha1 = -(m_z[m] + m_z[m + 1]);
                double alpha2 = m_z[m] * m_z[m + 1];
                double beta1 = -2.0 * m_real[m];
                double beta2 = Sqr(m_real[m]) + Sqr(m_imag[m]);

                newA[1] = m_aCoeff[1] + alpha1 * m_aCoeff[0];
                newB[1] = m_bCoeff[1] + beta1 * m_bCoeff[0];

                for (int i = 2; i <= n; i++)
                {
                    newA[i] = m_aCoeff[i] + alpha1 * m_aCoeff[i - 1] + alpha2 * m_aCoeff[i - 2];
                    newB[i] = m_bCoeff[i] + beta1 * m_bCoeff[i - 1] + beta2 * m_bCoeff[i - 2];
                }

                for (int i = 1; i <= n; i++)
                {
                    m_aCoeff[i] = newA[i];
                    m_bCoeff[i] = newB[i];
                }
            }

            // Ensure the filter is normalized
            FilterGain(1000);
        }

        /// <summary>
        /// Reset the history buffers
        /// </summary>
        public void Reset()
        {
            if (m_inHistory != null)
                m_inHistory.Clear();

            if (m_outHistory != null)
                m_outHistory.Clear();

            m_histIdx = 0;
        }

        /// <summary>
        /// Reset the filter, and fill the appropriate history buffers with the specified value
        /// </summary>
        /// <param name="historyValue"></param>
        public void Reset(double startValue)
        {
            m_histIdx = 0;

            if (m_inHistory == null || m_outHistory == null)
                return;

            m_inHistory.Fill(startValue);

            if (m_inHistory != null)
            {
                switch (m_filterType)
                {
                    case FilterType.LP:
                        m_outHistory.Fill(startValue);
                        break;

                    default:
                        m_outHistory.Clear();
                        break;
                }
            }
        }

        /// <summary>
        /// Apply the filter to the buffer
        /// </summary>
        /// <param name="bufIn"></param>
        public void FilterBuffer(float[] srcBuf, long srcPos, float[] dstBuf, long dstPos, long nLen)
        {
            const double kDenormal = 0.000000000000001;
            double denormal = m_invertDenormal ? -kDenormal : kDenormal;
            m_invertDenormal = !m_invertDenormal;

            for (int sampleIdx = 0; sampleIdx < nLen; sampleIdx++)
            {
                double sum = 0.0f;

                m_inHistory[m_histIdx] = srcBuf[srcPos + sampleIdx] + denormal;

                for (int idx = 0; idx < m_aCoeff.Length; idx++)
                    sum += m_aCoeff[idx] * m_inHistory[(m_histIdx - idx) & kHistMask];

                for (int idx = 1; idx < m_bCoeff.Length; idx++)
                    sum -= m_bCoeff[idx] * m_outHistory[(m_histIdx - idx) & kHistMask];

                m_outHistory[m_histIdx] = sum;
                m_histIdx = (m_histIdx + 1) & kHistMask;
                dstBuf[dstPos + sampleIdx] = (float)sum;
            }
        }

        public float FilterSample(float inVal)
        {
            double sum = 0.0f;

            m_inHistory[m_histIdx] = inVal;

            for (int idx = 0; idx < m_aCoeff.Length; idx++)
                sum += m_aCoeff[idx] * m_inHistory[(m_histIdx - idx) & kHistMask];

            for (int idx = 1; idx < m_bCoeff.Length; idx++)
                sum -= m_bCoeff[idx] * m_outHistory[(m_histIdx - idx) & kHistMask];

            m_outHistory[m_histIdx] = sum;
            m_histIdx = (m_histIdx + 1) & kHistMask;

            return (float)sum;
        }

        /// <summary>
        /// Get the gain at the specified number of frequency points
        /// </summary>
        /// <param name="freqPoints"></param>
        /// <returns></returns>
        public float[] FilterGain(int freqPoints)
        {
            // Filter gain at uniform frequency intervals
            float[] g = new float[freqPoints];
            double theta, s, c, sac, sas, sbc, sbs;
            float gMax = -100.0f;
            float sc = 10.0f / (float)Math.Log(10.0f);
            double t = Math.PI / (freqPoints - 1);

            for (int i = 0; i < freqPoints; i++)
            {
                theta = i * t;

                if (i == 0)
                    theta = Math.PI * 0.0001;

                if (i == freqPoints - 1)
                    theta = Math.PI * 0.9999;

                sac = 0.0f;
                sas = 0.0f;
                sbc = 0.0f;
                sbs = 0.0f;

                for (int k = 0; k <= m_order; k++)
                {
                    c = Math.Cos(k * theta);
                    s = Math.Sin(k * theta);
                    sac += c * m_aCoeff[k];
                    sas += s * m_aCoeff[k];
                    sbc += c * m_bCoeff[k];
                    sbs += s * m_bCoeff[k];
                }

                g[i] = sc * (float)Math.Log((Sqr(sac) + Sqr(sas)) / (Sqr(sbc) + Sqr(sbs)));
                gMax = Math.Max(gMax, g[i]);
            }

            // Normalize to 0 dB maximum gain
            for (int i = 0; i < freqPoints; i++)
                g[i] -= gMax;

            // Normalize numerator (a) coefficients
            float normFactor = (float)Math.Pow(10.0, -0.05 * gMax);

            for (int i = 0; i <= m_order; i++)
                m_aCoeff[i] *= normFactor;

            return g;
        }
    }
}
