using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using Random = UnityEngine.Random;


// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerCrownDisplayer : MonoBehaviour, INeedInjection, IInjectionFinishedListener, IExcludeFromSceneInjection
{
    [InjectedInInspector]
    public StarParticle crownChangePrefab;

    [Inject(optional = true)]
    private MicProfile micProfile;


    public void ShowCrown(bool visible)
    {
        bool wasVisible = gameObject.activeSelf;
        gameObject.SetActive(visible);

        if(visible && !wasVisible)
        {
            CreateCrownChangeEffect();
        }
    }

    private void CreateCrownChangeEffect()
    {
        if (crownChangePrefab != null)
        {
            StarParticle star = Instantiate(crownChangePrefab);

            star.transform.SetParent(transform);
            RectTransform starRectTransform = star.GetComponent<RectTransform>();
            starRectTransform.localPosition = Vector3.zero;
            LeanTween.scale(star.RectTransform, Vector3.zero, 1f)
                .setOnComplete(() => Destroy(star.gameObject));
        }
    }

    public void OnInjectionFinished()
    {
        if (micProfile != null)
        {
            GetComponentInChildren<Image>().color = micProfile.Color;
        }
    }
}
