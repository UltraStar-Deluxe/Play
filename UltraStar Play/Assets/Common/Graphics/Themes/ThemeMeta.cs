using System.IO;

public class ThemeMeta
{
    public string AbsoluteFilePath { get; private set; }
    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(AbsoluteFilePath);

    private ThemeSettings themeSettings;
    public ThemeSettings ThemeSettings
    {
        get
        {
            if (themeSettings == null)
            {
                string json = File.ReadAllText(AbsoluteFilePath);
                themeSettings = ThemeSettings.LoadFromJson(json);
            }

            return themeSettings;
        }
    }

    public ThemeMeta(string absoluteFilePath)
    {
        this.AbsoluteFilePath = absoluteFilePath;
    }

    public override string ToString()
    {
        return base.ToString() + $"({FileNameWithoutExtension})";
    }
}
