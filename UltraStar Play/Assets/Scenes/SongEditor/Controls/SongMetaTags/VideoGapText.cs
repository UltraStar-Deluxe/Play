using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using System.Globalization;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class VideoGapText : AbstractSongMetaTagText
{
    protected override string GetSongMetaTagValue()
    {
        return songMeta.VideoGap.ToString("F3", CultureInfo.InvariantCulture);
    }

    protected override string GetUiTextPrefix()
    {
        return "Video gap (s): ";
    }
}
