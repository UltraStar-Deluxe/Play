public class ModName
{
    public string Value { get; }

    public ModName(string value)
    {
        this.Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public override int GetHashCode()
    {
        return Value != null ? Value.GetHashCode() : 0;
    }

    private bool Equals(ModName other)
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

        return Equals((ModName)obj);
    }

    public static bool operator ==(ModName obj1, ModName obj2)
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

    public static bool operator !=(ModName obj1, ModName obj2)
    {
        return !(obj1 == obj2);
    }
}
