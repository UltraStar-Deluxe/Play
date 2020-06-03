using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniInject;

public class UiNote : MonoBehaviour
{
    [InjectedInInspector]
    public StarParticle goldenStarPrefab;

    [InjectedInInspector]
    public StarParticle perfectStarPrefab;

    [InjectedInInspector]
    public Image image;

    [InjectedInInspector]
    public ImageHueHelper imageHueHelper;

    [InjectedInInspector]
    public Text lyricsUiText;
    [InjectedInInspector]
    public RectTransform lyricsUiTextRectTransform;

    public Note Note { get; set; }
    public bool isGolden;

    public RectTransform RectTransform { get; private set; }

    private RectTransform uiEffectsContainer;

    private readonly List<StarParticle> stars = new List<StarParticle>();

    public void Init(Note note, RectTransform uiEffectsContainer)
    {
        this.Note = note;
        this.isGolden = note.IsGolden;
        this.uiEffectsContainer = uiEffectsContainer;
    }

    void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (isGolden)
        {
            RemoveDestroyedStarsFromList();
            CreateGoldenNoteEffect();
        }
        else
        {
            DestroyStars();
        }
    }

    void OnDestroy()
    {
        DestroyStars();
    }

    public void SetColorOfMicProfile(MicProfile micProfile)
    {
        if (micProfile != null)
        {
            imageHueHelper.SetHueByColor(micProfile.Color);
        }

        // Make freestyle and rap notes transparent
        switch (Note.Type)
        {
            case ENoteType.Freestyle:
            case ENoteType.Rap:
            case ENoteType.RapGolden:
                image.SetAlpha(0.3f);
                break;
        }
    }

    private void RemoveDestroyedStarsFromList()
    {
        foreach (StarParticle star in new List<StarParticle>(stars))
        {
            if (!star)
            {
                stars.Remove(star);
            }
        }
    }

    private void DestroyStars()
    {
        foreach (StarParticle star in stars)
        {
            if (star)
            {
                Destroy(star.gameObject);
            }
        }
        stars.Clear();
    }

    private void CreateGoldenNoteEffect()
    {
        // Create several particles. Longer notes require more particles because they have more space to fill.
        int starCount = stars.Count;
        int targetStarCount = Mathf.Max(6, (int)RectTransform.rect.width / 10);
        if (starCount < targetStarCount)
        {
            CreateGoldenStar();
        }
    }

    private void CreateGoldenStar()
    {
        StarParticle star = Instantiate(goldenStarPrefab);
        star.transform.SetParent(RectTransform);
        star.RectTransformToFollow = RectTransform;
        RectTransform starRectTransform = star.GetComponent<RectTransform>();
        float anchorX = Random.Range(0f, 1f);
        float anchorY = Random.Range(0f, 1f);
        starRectTransform.anchorMin = new Vector2(anchorX, anchorY);
        starRectTransform.anchorMax = new Vector2(anchorX, anchorY);
        starRectTransform.anchoredPosition = Vector2.zero;

        star.RectTransform.localScale = Vector3.one * Random.Range(0, 0.5f);
        LeanTween.scale(star.RectTransform, Vector3.one * Random.Range(0.5f, 1f), Random.Range(1f, 2f))
            .setOnComplete(() => Destroy(star.gameObject));

        // Move to another parent to ensure that it is drawn in front of the notes.
        star.transform.SetParent(uiEffectsContainer);

        stars.Add(star);
    }

    public void CreatePerfectNoteEffect()
    {
        CreatePerfectStar();
    }

    private void CreatePerfectStar()
    {
        StarParticle star = Instantiate(perfectStarPrefab);
        star.transform.SetParent(transform);
        star.RectTransformToFollow = RectTransform;
        RectTransform starRectTransform = star.GetComponent<RectTransform>();
        float anchorX = 1;
        float anchorY = 0.9f;
        starRectTransform.anchorMin = new Vector2(anchorX, anchorY);
        starRectTransform.anchorMax = new Vector2(anchorX, anchorY);
        starRectTransform.anchoredPosition = Vector2.zero;
        starRectTransform.localEulerAngles = new Vector3(0, 0, Random.Range(0, 180));

        star.RectTransform.localScale = Vector3.one * 1f;
        LeanTween.scale(star.RectTransform, Vector3.zero, 1f)
            .setOnComplete(() => Destroy(star.gameObject));

        // Move to another parent to ensure that it is drawn in front of the notes.
        star.transform.SetParent(uiEffectsContainer);
    }
}
