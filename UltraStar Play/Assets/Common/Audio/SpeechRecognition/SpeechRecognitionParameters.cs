using System;
using System.Collections.Generic;
using System.Linq;

public class SpeechRecognitionParameters
{
    public int SampleRate { get; private set; }
    public string ModelPath { get; private set; }
    public List<string> Phrases { get; private set; }

    public SpeechRecognitionParameters(int sampleRate, string modelPath, List<string> phrases)
    {
        ObjectUtils.AssertNotNull(modelPath, nameof(modelPath));
        ObjectUtils.AssertNotNull(phrases, nameof(phrases));

        SampleRate = sampleRate;
        ModelPath = modelPath;
        Phrases = phrases;
    }

    protected bool Equals(SpeechRecognitionParameters other)
    {
        return SampleRate == other.SampleRate
               && ModelPath == other.ModelPath
               && Phrases.SequenceEqual(other.Phrases);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((SpeechRecognitionParameters)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SampleRate, ModelPath, Phrases);
    }

    public bool ModelParametersEquals(SpeechRecognitionParameters otherSpeechRecognitionParameters)
    {
        return otherSpeechRecognitionParameters != null
               && ModelPath.Equals(otherSpeechRecognitionParameters.ModelPath);
    }

    public bool RecognizerParametersEquals(SpeechRecognitionParameters otherSpeechRecognitionParameters)
    {
        return otherSpeechRecognitionParameters != null
               && SampleRate == otherSpeechRecognitionParameters.SampleRate
               && Phrases.SequenceEqual(otherSpeechRecognitionParameters.Phrases);
    }
}
