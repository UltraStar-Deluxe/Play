using System.IO;

public class ThemeMeta
{
    public string AbsoluteFilePath { get; private set; }
    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(AbsoluteFilePath);

    public ThemeMeta(string absoluteFilePath)
    {
        this.AbsoluteFilePath = absoluteFilePath;
    }

    public override string ToString()
    {
        return base.ToString() + $"({FileNameWithoutExtension})";
    }
}
