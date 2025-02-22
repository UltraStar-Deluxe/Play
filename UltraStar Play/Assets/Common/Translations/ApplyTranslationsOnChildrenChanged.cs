using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ApplyTranslationsOnChildrenChanged : AbstractSingletonBehaviour, INeedInjection
{
    public static ApplyTranslationsOnChildrenChanged Instance => DontDestroyOnLoadManager.FindComponentOrThrow<ApplyTranslationsOnChildrenChanged>();

    [Inject]
    private UiManager uiManager;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        uiManager.ChildrenChangedEventStream
            .Subscribe(evt => TranslationManager.ApplyTranslations(evt.targetChild));
    }
}
