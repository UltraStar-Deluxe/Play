using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSettings
{
    public SystemLanguage language = SystemLanguage.English;
    public List<string> songDirs = new();
    public bool RatePlayers { get; set; } = true;
    public bool CombineDuetScores { get; set; } = true;
}
