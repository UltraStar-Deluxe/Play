using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SceneChangeAnimations;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class UltraStarPlaySceneChangeAnimationControl : SceneChangeAnimationControl, INeedInjection
{
    private static UltraStarPlaySceneChangeAnimationControl instance;
    public static UltraStarPlaySceneChangeAnimationControl Instance
    {
        get
        {
            if (instance == null)
            {
                UltraStarPlaySceneChangeAnimationControl instanceInScene = GameObjectUtils.FindComponentWithTag<UltraStarPlaySceneChangeAnimationControl>("SceneChangeAnimationControl");
                if (instanceInScene != null)
                {
                    GameObjectUtils.TryInitSingleInstanceWithDontDestroyOnLoad(ref instance, ref instanceInScene);
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        UltraStarPlaySceneChangeAnimationControl self = this;
        GameObjectUtils.TryInitSingleInstanceWithDontDestroyOnLoad(ref instance, ref self);
    }

    public void StartSceneChangeAnimation(VisualElement visualElement)
    {
        LeanTween.value(gameObject, 0, 1, 0.3f)
            .setOnUpdate((float animTimePercent) =>
            {
                visualElement.style.opacity = 1 - animTimePercent;

                float scale = 1 + animTimePercent * 2f;
                visualElement.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1)));
            })
            .setOnComplete(() => visualElement.RemoveFromHierarchy())
            .setEaseInSine();
    }
}
