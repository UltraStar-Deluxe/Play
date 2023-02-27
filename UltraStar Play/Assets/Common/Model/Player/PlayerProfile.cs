using System;

[Serializable]
public class PlayerProfile
{
    public const string DefaultImagePath = "DefaultPlayerProfileImage.png";
    public const string WebcamImagePath = "WEBCAM";

    public string Name { get; set; } = "New Player";
    public EDifficulty Difficulty { get; set; } = EDifficulty.Medium;
    public string ImagePath { get; set; } = DefaultImagePath;
    public bool IsEnabled { get; set; } = true;
    public bool IsSelected { get; set; } = true;

    public PlayerProfile()
    {
    }

    public PlayerProfile(string name, EDifficulty difficulty, string imagePath = DefaultImagePath)
    {
        this.Name = name;
        this.Difficulty = difficulty;
        this.ImagePath = imagePath;
    }

    public override string ToString()
    {
        return $"PlayerProfile(Name: {Name})";
    }
}
