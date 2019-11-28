using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Threading;

namespace Pitch
{
    /// <summary>
    /// Tracks pitch
    /// </summary>
    public class PitchTracker
    {
        private const int kOctaveSteps = 96;
        private const int kStepOverlap = 4;
        private const float kMinFreq = 50.0f;               // A1, Midi note 33, 55.0Hz
        private const float kMaxFreq = 1600.0f;             // A#6. Midi note 92
        private const int kStartCircular = 40;		        // how far into the sample buffer do we start checking (allow for filter settling)
        private const float kDetectOverlapSec = 0.005f;
        private const float kMaxOctaveSecRate = 10.0f;

        private const float kAvgOffset = 0.005f;	        // time offset between pitch averaging values
        private const int kAvgCount = 1;			        // number of average pitch samples to take
        private const float kCircularBufSaveTime = 1.0f;    // Amount of samples to store in the history buffer

        private PitchDsp m_dsp;
        private CircularBuffer<float> m_circularBufferLo;
        private CircularBuffer<float> m_circularBufferHi;
        private double m_sampleRate;
        private float m_detectLevelThreshold = 0.01f;       // -40dB
        private int m_pitchRecordsPerSecond = 50;           // default is 50, or one record every 20ms

        private float[] m_pitchBufLo;
        private float[] m_pitchBufHi;
        private int m_pitchBufSize;
        private int m_samplesPerPitchBlock;
        private int m_curPitchIndex;
        private long m_curPitchSamplePos;

        private int m_detectOverlapSamples;
        private float m_maxOverlapDiff;

        private bool m_recordPitchRecords;
        private int m_pitchRecordHistorySize;
        private List<PitchRecord> m_pitchRecords = new List<PitchRecord>();
        private PitchRecord m_curPitchRecord = new PitchRecord();

        private IIRFilter m_iirFilterLoLo;
        private IIRFilter m_iirFilterLoHi;
        private IIRFilter m_iirFilterHiLo;
        private IIRFilter m_iirFilterHiHi;

        public delegate void PitchDetectedHandler(PitchTracker sender, PitchRecord pitchRecord);
        public event PitchDetectedHandler PitchDetected; 

        /// <summary>
        /// Constructor
        /// </summary>
        public PitchTracker()
        {
        }

        /// <summary>
        /// Set the sample rate
        /// </summary>
        public double SampleRate
        {
            set
            {
                if (m_sampleRate == value)
                    return;

                m_sampleRate = value;
                Setup();
            }
        }

        /// <summary>
        /// Set the detect level threshold, The value must be between 0.0001f and 1.0f (-80 dB to 0 dB)
        /// </summary>
        public float DetectLevelThreshold
        {
            set
            {
                var newValue = Math.Max(0.0001f, Math.Min(1.0f, value));

                if (m_detectLevelThreshold == newValue)
                    return;

                m_detectLevelThreshold = newValue;
                Setup();
            }
        }

        /// <summary>
        /// Return the samples per pitch block
        /// </summary>
        public int SamplesPerPitchBlock
        {
            get { return m_samplesPerPitchBlock; }
        }

        /// <summary>
        /// Get or set the number of pitch records per second (default is 50, or one record every 20ms)
        /// </summary>
        public int PitchRecordsPerSecond
        {
            get { return m_pitchRecordsPerSecond; }
            set
            {
                m_pitchRecordsPerSecond = Math.Max(1, Math.Min(100, value));
                Setup(); 
            }
        }

        /// <summary>
        /// Get or set whether pitch records should be recorded into a history buffer
        /// </summary>
        public bool RecordPitchRecords
        {
            get { return m_recordPitchRecords; }
            set
            {
                if (m_recordPitchRecords == value)
                    return;
                
                m_recordPitchRecords = value;

                if (!m_recordPitchRecords)
                    m_pitchRecords = new List<PitchRecord>();
            }
        }

        /// <summary>
        /// Get or set the max number of pitch records to keep. A value of 0 means no limit.
        /// Don't leave this at 0 when RecordPitchRecords is true and this is used in a realtime
        /// application since the buffer will grow indefinately!
        /// </summary>
        public int PitchRecordHistorySize
        {
            get { return m_pitchRecordHistorySize; }
            set 
            { 
                m_pitchRecordHistorySize = value;

                m_pitchRecords.Capacity = m_pitchRecordHistorySize;
            }
        }

        /// <summary>
        /// Get the current pitch records
        /// </summary>
        public IList PitchRecords
        {
            get { return m_pitchRecords.AsReadOnly(); }
        }

        /// <summary>
        /// Get the latest pitch record
        /// </summary>
        public PitchRecord CurrentPitchRecord
        {
            get { return m_curPitchRecord; }
        }

        /// <summary>
        /// Get the current pitch position
        /// </summary>
        public long CurrentPitchSamplePosition
        {
            get { return m_curPitchSamplePos; }
        }

        /// <summary>
        /// Get the minimum frequency that can be detected
        /// </summary>
        public static float MinDetectFrequency
        {
            get { return kMinFreq; }
        }

        /// <summary>
        /// Get the maximum frequency that can be detected
        /// </summary>
        public static float MaxDetectFrequency
        {
            get { return kMaxFreq; }
        }

        /// <summary>
        /// Get the frequency step
        /// </summary>
        public static double FrequencyStep
        {
            get { return Math.Pow(2.0, 1.0 / kOctaveSteps); }
        }

        /// <summary>
        /// Get the number of samples that the detected pitch is offset from the input buffer.
        /// This is just an estimate to sync up the samples and detected pitch
        /// </summary>
        public int DetectSampleOffset
        {
            get { return (m_pitchBufSize + m_detectOverlapSamples) / 2; }
        }

        /// <summary>
        /// Reset the pitch tracker. Call this when the sample position is
        /// not consecutive from the previous position
        /// </summary>
        public void Reset()
        {
            m_curPitchIndex = 0;
            m_curPitchSamplePos = 0;
            m_pitchRecords.Clear();
            m_iirFilterLoLo.Reset();
            m_iirFilterLoHi.Reset();
            m_iirFilterHiLo.Reset();
            m_iirFilterHiHi.Reset();
            m_circularBufferLo.Reset();
            m_circularBufferLo.Clear();
            m_circularBufferHi.Reset();
            m_circularBufferHi.Clear();
            m_pitchBufLo.Clear();
            m_pitchBufHi.Clear();

            m_circularBufferLo.StartPosition = -m_detectOverlapSamples;
            m_circularBufferLo.Available = m_detectOverlapSamples;
            m_circularBufferHi.StartPosition = -m_detectOverlapSamples;
            m_circularBufferHi.Available = m_detectOverlapSamples;
        }

        /// <summary>
        /// Process the passed in buffer of data. During this call, the PitchDetected event will
        /// be fired zero or more times, depending how many pitch records will fit in the new
        /// and previously cached buffer.
        ///
        /// This means that there is no size restriction on the buffer that is passed into ProcessBuffer.
        /// For instance, ProcessBuffer can be called with one very large buffer that contains all of the
        /// audio to be processed (many PitchDetected events will be fired), or just a small buffer at
        /// a time which is more typical for realtime applications. In the latter case, the PitchDetected
        /// event might not be fired at all since additional calls must first be made to accumulate enough
        /// data do another pitch detect operation.
        /// </summary>
        /// <param name="inBuffer">Input buffer. Samples must be in the range -1.0 to 1.0</param>
        /// <param name="sampleCount">Number of samples to process. Zero means all samples in the buffer</param>
        public void ProcessBuffer(float[] inBuffer, int sampleCount = 0)
        {
            if (inBuffer == null)
                throw new ArgumentNullException("inBuffer", "Input buffer cannot be null");

            var samplesProcessed = 0;
            var srcLength = sampleCount == 0 ? inBuffer.Length : Math.Min(sampleCount, inBuffer.Length);

            while (samplesProcessed < srcLength)
            {
                int frameCount = Math.Min(srcLength - samplesProcessed, m_pitchBufSize + m_detectOverlapSamples);

                m_iirFilterLoLo.FilterBuffer(inBuffer, samplesProcessed, m_pitchBufLo, 0, frameCount);
                m_iirFilterLoHi.FilterBuffer(m_pitchBufLo, 0, m_pitchBufLo, 0, frameCount);

                m_iirFilterHiLo.FilterBuffer(inBuffer, samplesProcessed, m_pitchBufHi, 0, frameCount);
                m_iirFilterHiHi.FilterBuffer(m_pitchBufHi, 0, m_pitchBufHi, 0, frameCount);

                m_circularBufferLo.WriteBuffer(m_pitchBufLo, frameCount);
                m_circularBufferHi.WriteBuffer(m_pitchBufHi, frameCount);

                // Loop while there is enough samples in the circular buffer
                while (m_circularBufferLo.ReadBuffer(m_pitchBufLo, m_curPitchSamplePos, m_pitchBufSize + m_detectOverlapSamples))
                {
                    float pitch1;
                    float pitch2 = 0.0f;
                    float detectedPitch = 0.0f;

                    m_circularBufferHi.ReadBuffer(m_pitchBufHi, m_curPitchSamplePos, m_pitchBufSize + m_detectOverlapSamples);

                    pitch1 = m_dsp.DetectPitch(m_pitchBufLo, m_pitchBufHi, m_pitchBufSize);

                    if (pitch1 > 0.0f)
                    {
                        // Shift the buffers left by the overlapping amount
                        m_pitchBufLo.Copy(m_pitchBufLo, m_detectOverlapSamples, 0, m_pitchBufSize);
                        m_pitchBufHi.Copy(m_pitchBufHi, m_detectOverlapSamples, 0, m_pitchBufSize);

                        pitch2 = m_dsp.DetectPitch(m_pitchBufLo, m_pitchBufHi, m_pitchBufSize);

                        if (pitch2 > 0.0f)
                        {
                            float fDiff = Math.Max(pitch1, pitch2) / Math.Min(pitch1, pitch2) - 1.0f;

                            if (fDiff < m_maxOverlapDiff)
                                detectedPitch = (pitch1 + pitch2) * 0.5f;
                        }
                    }

                    // Log the pitch record
                    AddPitchRecord(detectedPitch);

                    m_curPitchSamplePos += m_samplesPerPitchBlock;
                    m_curPitchIndex++;
                }

                samplesProcessed += frameCount;
            }
        }

        /// <summary>
        /// Setup
        /// </summary>
        private void Setup()
        {
            if (m_sampleRate < 1.0f)
                return;

            m_dsp = new PitchDsp(m_sampleRate, kMinFreq, kMaxFreq, m_detectLevelThreshold);

            m_iirFilterLoLo = new IIRFilter();
            m_iirFilterLoLo.Proto = IIRFilter.ProtoType.Butterworth;
            m_iirFilterLoLo.Type = IIRFilter.FilterType.HP;
            m_iirFilterLoLo.Order = 5;
            m_iirFilterLoLo.FreqLow = 45.0f;
            m_iirFilterLoLo.SampleRate = (float)m_sampleRate;

            m_iirFilterLoHi = new IIRFilter();
            m_iirFilterLoHi.Proto = IIRFilter.ProtoType.Butterworth;
            m_iirFilterLoHi.Type = IIRFilter.FilterType.LP;
            m_iirFilterLoHi.Order = 5;
            m_iirFilterLoHi.FreqHigh = 280.0f;
            m_iirFilterLoHi.SampleRate = (float)m_sampleRate;

            m_iirFilterHiLo = new IIRFilter();
            m_iirFilterHiLo.Proto = IIRFilter.ProtoType.Butterworth;
            m_iirFilterHiLo.Type = IIRFilter.FilterType.HP;
            m_iirFilterHiLo.Order = 5;
            m_iirFilterHiLo.FreqLow = 45.0f;
            m_iirFilterHiLo.SampleRate = (float)m_sampleRate;

            m_iirFilterHiHi = new IIRFilter();
            m_iirFilterHiHi.Proto = IIRFilter.ProtoType.Butterworth;
            m_iirFilterHiHi.Type = IIRFilter.FilterType.LP;
            m_iirFilterHiHi.Order = 5;
            m_iirFilterHiHi.FreqHigh = 1500.0f;
            m_iirFilterHiHi.SampleRate = (float)m_sampleRate;

            m_detectOverlapSamples = (int)(kDetectOverlapSec * m_sampleRate);
            m_maxOverlapDiff = kMaxOctaveSecRate * kDetectOverlapSec;

            m_pitchBufSize = (int)(((1.0f / (float)kMinFreq) * 2.0f + ((kAvgCount - 1) * kAvgOffset)) * m_sampleRate) + 16;
            m_pitchBufLo = new float[m_pitchBufSize + m_detectOverlapSamples];
            m_pitchBufHi = new float[m_pitchBufSize + m_detectOverlapSamples];
            m_samplesPerPitchBlock = (int)Math.Round(m_sampleRate / m_pitchRecordsPerSecond); 

            m_circularBufferLo = new CircularBuffer<float>((int)(kCircularBufSaveTime * m_sampleRate + 0.5f) + 10000);
            m_circularBufferHi = new CircularBuffer<float>((int)(kCircularBufSaveTime * m_sampleRate + 0.5f) + 10000);
        }

        /// <summary>
        /// The pitch was detected - add the record
        /// </summary>
        /// <param name="pitch"></param>
        private void AddPitchRecord(float pitch)
        {
            var midiNote = 0;
            var midiCents = 0;

            PitchDsp.PitchToMidiNote(pitch, out midiNote, out midiCents);

            var record = new PitchRecord();

            record.RecordIndex = m_curPitchIndex;
            record.Pitch = pitch;
            record.MidiNote = midiNote;
            record.MidiCents = midiCents;

            m_curPitchRecord = record;

            if (m_recordPitchRecords)
            {
                if (m_pitchRecordHistorySize > 0 && m_pitchRecords.Count >= m_pitchRecordHistorySize)
                    m_pitchRecords.RemoveAt(0);

                m_pitchRecords.Add(record);
            }
            
            if (this.PitchDetected != null)
                this.PitchDetected(this, record);
        }

        /// <summary>
        /// Stores one record
        /// </summary>
        public struct PitchRecord
        {
            /// <summary>
            /// The index of the pitch record since the last Reset call
            /// </summary>
            public int RecordIndex { get; set; }

            /// <summary>
            /// The detected pitch
            /// </summary>
            public float Pitch { get; set; }

            /// <summary>
            /// The detected MIDI note, or 0 for no pitch
            /// </summary>
            public int MidiNote { get; set; }

            /// <summary>
            /// The offset from the detected MIDI note in cents, from -50 to +50.
            /// </summary>
            public int MidiCents { get; set; }
        }
    }
}

