using System.IO;

public class ModFolder
{
    public string Value { get; }

    public ModName ModName => new ModName(Path.GetFileName(Value));

    public ModFolder(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public override int GetHashCode()
    {
        return Value != null ? Value.GetHashCode() : 0;
    }

    private bool Equals(ModFolder other)
    {
        return Value == other.Value;
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

        return Equals((ModFolder)obj);
    }

    public static bool operator ==(ModFolder obj1, ModFolder obj2)
    {
        if (ReferenceEquals(obj1, null) && ReferenceEquals(obj2, null))
        {
            return true;
        }

        if (ReferenceEquals(obj1, null) || ReferenceEquals(obj2, null))
        {
            return false;
        }

        return obj1.Equals(obj2);
    }

    public static bool operator !=(ModFolder obj1, ModFolder obj2)
    {
        return !(obj1 == obj2);
    }
}
