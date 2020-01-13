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

public class VideoText : AbstractSongMetaTagText
{
    protected override string GetSongMetaTagValue()
    {
        return songMeta.Video;
    }

    protected override string GetUiTextPrefix()
    {
        return "Video: ";
    }
}
