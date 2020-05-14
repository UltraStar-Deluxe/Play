using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using System.IO;

public class SongRouletteItemContextMenuHandler : AbstractContextMenuHandler
{
    public SongMeta SongMeta { get; set; }

    protected override void FillContextMenu(ContextMenu contextMenu)
    {
#if UNITY_STANDALONE
        contextMenu.AddItem("Open song folder", () => SongMetaUtils.OpenDirectory(SongMeta));
#endif
    }
}
