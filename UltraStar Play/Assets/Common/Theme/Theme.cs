
using System;

[Serializable]
public class Theme
{
    public static readonly Theme BaseTheme = new Theme("base", null);

    public string Name { get; set; }
    public string ParentThemeName { get; set; }

    private Theme parentTheme;
    public Theme ParentTheme
    {
        get
        {
            if (parentTheme == null || parentTheme.Name != ParentThemeName)
            {
                parentTheme = ThemeManger.Instance.GetTheme(ParentThemeName);
            }

            return parentTheme;
        }
    }

    public Theme(string name, string parentThemeName)
    {
        Name = name;
        ParentThemeName = parentThemeName;
    }

    public override string ToString()
    {
        return $"{Name} (parent:{ParentThemeName})";
    }
}