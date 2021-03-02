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

public class SongSelectFuzzySearchText : MonoBehaviour, INeedInjection
{
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Text uiText;
    
    [Inject]
    private SongSelectSceneInputControl songSelectSceneInputControl;
    
	private void Start()
    {
        uiText.text = "";
        
        songSelectSceneInputControl.FuzzySearchText
            .Subscribe(newFuzzySearchText => uiText.text = newFuzzySearchText);
    }
}
