using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

public class RmvpePitchDetector : IDisposable
{
    public const int ExpectedSampleRate = 16000;

    private const string InputTensorName = "waveform";
    private const string OutputTensorName = "pitchf";
    private const string ThresholdTensorName = "threshold";
    private const int HopLength = 160;

    private readonly InferenceSession session;

    public RmvpePitchDetector(string modelPath)
    {
        using var d = new DisposableStopwatch("Create RmvpePitchDetector");
        
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException("Model file not found", modelPath);
        }

        session = new InferenceSession(modelPath);
    }

    public RmvpePitchResult DetectPitch(float[] audioSamples, int sampleRate = 16000)
    {
        using var d = new DisposableStopwatch("DetectPitch");
        
        if (sampleRate != ExpectedSampleRate)
        {
            throw new ArgumentException($"Expected sample rate {ExpectedSampleRate}, but got {sampleRate}");
        }

        // New model expects waveform [1, N] and threshold [1]
        var waveformTensor = new DenseTensor<float>(audioSamples, new[] { 1, audioSamples.Length });

        var inputs = new List<NamedOnnxValue>();
        inputs.Add(NamedOnnxValue.CreateFromTensor(InputTensorName, waveformTensor));
        if (ThresholdTensorName != null)
        {
            var thresholdTensor = new DenseTensor<float>(new float[] { 0.5f }, new[] { 1 });
            inputs.Add(NamedOnnxValue.CreateFromTensor(ThresholdTensorName, thresholdTensor));
        }

        using (var results = session.Run(inputs))
        {
            var outputValue = results.First(v => v.Name == OutputTensorName);
            var output = outputValue.AsTensor<float>();

            // Output shape [1, T] or [T]
            int numFrames = output.Dimensions[output.Dimensions.Length - 1];

            var pitchEstimates = new List<RmvpePitchEstimate>();
            float[] f0Array = new float[numFrames];

            for (int t = 0; t < numFrames; t++)
            {
                float f0 = output.Dimensions.Length == 1 ? output[t] : output[0, t];
                f0Array[t] = f0;

                if (f0 > 0)
                {
                    double timestamp = (double)t * HopLength / ExpectedSampleRate;
                    pitchEstimates.Add(new RmvpePitchEstimate { Time = timestamp, Frequency = f0, Confidence = 1.0f });
                }
            }

            return new RmvpePitchResult { Estimates = pitchEstimates, Frequencies = f0Array };
        }
    }

    public void Dispose()
    {
        session?.Dispose();
    }
}
