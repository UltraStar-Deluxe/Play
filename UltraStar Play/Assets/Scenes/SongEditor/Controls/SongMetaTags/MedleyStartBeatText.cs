using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MedleyStartBeatText : AbstractSongMetaTagText
{
    protected override string GetSongMetaTagValue()
    {
        // TODO: add MedleyStartBeat to SongMeta
        return "";
    }

    protected override string GetUiTextPrefix()
    {
        return "Medleay start beat: ";
    }
}
