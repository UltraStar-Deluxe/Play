using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

abstract public class AbstractSongMetaTagText : MonoBehaviour, INeedInjection
{
    [Inject]
    protected SongMeta songMeta;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    protected Text uiText;

    abstract protected string GetUiTextPrefix();
    abstract protected string GetSongMetaTagValue();

    protected virtual void Start()
    {
        UpdateUiText();
    }

    protected void UpdateUiText()
    {
        uiText.text = GetUiTextPrefix() + GetSongMetaTagValue();
    }
}
