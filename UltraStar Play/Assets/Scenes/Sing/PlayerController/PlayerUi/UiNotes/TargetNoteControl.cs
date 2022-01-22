using System.Collections;
using System.Collections.Generic;
using PrimeInputActions;
using UnityEngine;
using UniInject;
using UnityEngine.UIElements;

public class TargetNoteControl : INeedInjection, IInjectionFinishedListener
{
    // [InjectedInInspector]
    // public StarParticle goldenStarPrefab;
    //
    // [InjectedInInspector]
    // public StarParticle perfectStarPrefab;

    [Inject]
    public Note Note { get; private set; }

    [Inject(UxmlName = R.UxmlNames.targetNoteLabel)]
    public Label Label { get; private set; }

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement visualElement;

    [Inject(UxmlName = R.UxmlNames.recordedNote)]
    private VisualElement recordedNote;

    [Inject(UxmlName = R.UxmlNames.targetNoteImage)]
    private VisualElement image;

    [Inject(Optional = true)]
    private MicProfile micProfile;

    // private readonly List<StarParticle> stars = new List<StarParticle>();

    public void OnInjectionFinished()
    {
        recordedNote.HideByDisplay();

        SetStyleByMicProfile();
    }

    public void Update()
    {
        // if (Note.IsGolden)
        // {
        //     RemoveDestroyedStarsFromList();
        //     CreateGoldenNoteEffect();
        // }
        // else
        // {
        //     DestroyStars();
        // }
    }

    private void SetStyleByMicProfile()
    {
        if (micProfile == null)
        {
            return;
        }

        Color color = micProfile.Color;

        // Make freestyle and rap notes transparent
        Color finalColor = Note.Type is ENoteType.Freestyle or ENoteType.Rap or ENoteType.RapGolden
            ? color.WithAlpha(0.3f)
            : color;

        image.style.unityBackgroundImageTintColor = new StyleColor(finalColor);
    }

    // private void RemoveDestroyedStarsFromList()
    // {
    //     foreach (StarParticle star in new List<StarParticle>(stars))
    //     {
    //         if (!star)
    //         {
    //             stars.Remove(star);
    //         }
    //     }
    // }
    //
    // private void DestroyStars()
    // {
    //     foreach (StarParticle star in stars)
    //     {
    //         if (star)
    //         {
    //             Destroy(star.gameObject);
    //         }
    //     }
    //     stars.Clear();
    // }
    //
    // private void DestroyLyrics()
    // {
    //     if (label != null
    //         && label.gameObject != null)
    //     {
    //         Destroy(label.gameObject);
    //     }
    //     if (lyricsUiTextRectTransform != null
    //         && lyricsUiTextRectTransform.gameObject != null)
    //     {
    //         Destroy(lyricsUiTextRectTransform.gameObject);
    //     }
    // }
    //
    // private void CreateGoldenNoteEffect()
    // {
    //     // Create several particles. Longer notes require more particles because they have more space to fill.
    //     int starCount = stars.Count;
    //     int targetStarCount = Mathf.Max(6, (int)RectTransform.rect.width / 10);
    //     if (starCount < targetStarCount)
    //     {
    //         CreateGoldenStar();
    //     }
    // }
    //
    // private void CreateGoldenStar()
    // {
    //     StarParticle star = Instantiate(goldenStarPrefab);
    //     star.transform.SetParent(RectTransform);
    //     star.RectTransformToFollow = RectTransform;
    //     RectTransform starRectTransform = star.GetComponent<RectTransform>();
    //     float anchorX = Random.Range(0f, 1f);
    //     float anchorY = Random.Range(0f, 1f);
    //     starRectTransform.anchorMin = new Vector2(anchorX, anchorY);
    //     starRectTransform.anchorMax = new Vector2(anchorX, anchorY);
    //     starRectTransform.anchoredPosition = Vector2.zero;
    //
    //     star.RectTransform.localScale = Vector3.one * Random.Range(0, 0.5f);
    //     LeanTween.scale(star.RectTransform, Vector3.one * Random.Range(0.5f, 1f), Random.Range(1f, 2f))
    //         .setOnComplete(() => Destroy(star.gameObject));
    //
    //     // Move to another parent to ensure that it is drawn in front of the notes.
    //     star.transform.SetParent(uiEffectsContainer);
    //
    //     stars.Add(star);
    // }
    //
    // public void CreatePerfectNoteEffect()
    // {
    //     CreatePerfectStar();
    // }
    //
    // private void CreatePerfectStar()
    // {
    //     StarParticle star = Instantiate(perfectStarPrefab);
    //     star.transform.SetParent(transform);
    //     star.RectTransformToFollow = RectTransform;
    //     RectTransform starRectTransform = star.GetComponent<RectTransform>();
    //     float anchorX = 1;
    //     float anchorY = 0.9f;
    //     starRectTransform.anchorMin = new Vector2(anchorX, anchorY);
    //     starRectTransform.anchorMax = new Vector2(anchorX, anchorY);
    //     starRectTransform.anchoredPosition = Vector2.zero;
    //     starRectTransform.localEulerAngles = new Vector3(0, 0, Random.Range(0, 180));
    //
    //     star.RectTransform.localScale = Vector3.one * 1f;
    //     LeanTween.scale(star.RectTransform, Vector3.zero, 1f)
    //         .setOnComplete(() => Destroy(star.gameObject));
    //
    //     // Move to another parent to ensure that it is drawn in front of the notes.
    //     star.transform.SetParent(uiEffectsContainer);
    // }

    public void Dispose()
    {
        visualElement.RemoveFromHierarchy();
        // DestroyStars();
        // DestroyLyrics();
    }
}
