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

public class MusicGapText : AbstractSongMetaTagText
{

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    protected override void Start()
    {
        base.Start();
        songMetaChangeEventStream.Subscribe(OnSongMetaChanged);
    }

    private void OnSongMetaChanged(ISongMetaChangeEvent changeEvent)
    {
        if (changeEvent is MusicGapChangedEvent || changeEvent is LoadedMementoEvent)
        {
            UpdateUiText();
        }
    }

    protected override string GetSongMetaTagValue()
    {
        return songMeta.Gap.ToString("F3", CultureInfo.InvariantCulture);
    }

    protected override string GetUiTextPrefix()
    {
        return "Gap (ms): ";
    }
}
