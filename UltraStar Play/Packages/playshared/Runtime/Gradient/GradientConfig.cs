using System;
using UnityEngine;

public class GradientConfig : IEquatable<GradientConfig>
{
    public Color32 startColor;
    public Color32 endColor;
    public float angleDegrees;

    public GradientConfig()
    {
    }
    
    public GradientConfig(Color32 startColor, Color32 endColor, float angleDegrees = 0)
    {
        this.startColor = startColor;
        this.endColor = endColor;
        this.angleDegrees = angleDegrees;
    }

    public bool Equals(GradientConfig other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return startColor.Equals(other.startColor) && endColor.Equals(other.endColor) && angleDegrees.Equals(other.angleDegrees);
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

        return Equals((GradientConfig)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(startColor, endColor, angleDegrees);
    }

    public override string ToString()
    {
        return GradientConfigUtils.ToCssSyntax(this);
    }
}
