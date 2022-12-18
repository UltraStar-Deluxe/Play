using System;
using System.Collections.Generic;
using System.Linq;

public class VoskModelParameters
{
    public int SampleRate { get; private set; }
    public string ModelPath { get; private set; }
    public List<string> Phrases { get; private set; }

    public VoskModelParameters(int sampleRate, string modelPath, List<string> phrases)
    {
        ObjectUtils.AssertNotNull(modelPath, nameof(modelPath));
        ObjectUtils.AssertNotNull(phrases, nameof(phrases));

        SampleRate = sampleRate;
        ModelPath = modelPath;
        Phrases = phrases;
    }

    protected bool Equals(VoskModelParameters other)
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

        return Equals((VoskModelParameters)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SampleRate, ModelPath, Phrases);
    }

    public bool ModelParametersEquals(VoskModelParameters otherVoskModelParameters)
    {
        return otherVoskModelParameters != null
               && ModelPath.Equals(otherVoskModelParameters.ModelPath);
    }

    public bool RecognizerParametersEquals(VoskModelParameters otherVoskModelParameters)
    {
        return otherVoskModelParameters != null
               && SampleRate == otherVoskModelParameters.SampleRate
               && Phrases.SequenceEqual(otherVoskModelParameters.Phrases);
    }
}
