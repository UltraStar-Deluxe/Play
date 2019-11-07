
using System;

[Serializable]
public class Settings
{
    public GameSettings GameSettings { get; set; } = new GameSettings();
    public GraphicSettings GraphicSettings { get; set; } = new GraphicSettings();
}