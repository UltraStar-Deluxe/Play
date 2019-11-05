
using System;

[Serializable]
public class Theme
{
    public string Name { get; set; }
    public Theme ParentTheme { get; private set; }

    public Theme(string name, Theme parentTheme)
    {
        Name = name;
        ParentTheme = parentTheme;
    }

    public override string ToString()
    {
        if (ParentTheme != null)
        {
            return $"{Name} inherits {ParentTheme.Name}";
        }
        else
        {
            return $"{Name}";
        }
    }
}