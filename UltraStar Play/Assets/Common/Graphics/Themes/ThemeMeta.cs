using System.IO;

public class ThemeMeta
{
    public string AbsoluteFilePath { get; private set; }
    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(AbsoluteFilePath);

    private ThemeJson themeJson;
    public ThemeJson ThemeJson
    {
        get
        {
            if (themeJson == null)
            {
                string json = File.ReadAllText(AbsoluteFilePath);
                themeJson = ThemeJson.LoadFromJson(json);
            }

            return themeJson;
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
