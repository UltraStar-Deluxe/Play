using System;
using System.Collections.Generic;
using UnityEngine;

public class SongEditorVoiceLayer : AbstractSongEditorLayer
{
    public string VoiceName { get; private set; }

    public SongEditorVoiceLayer(string voiceName)
    {
        this.VoiceName = voiceName;
    }

    public override string GetDisplayName()
    {
        return VoiceName.Replace("P", "Player ");
    }
}
