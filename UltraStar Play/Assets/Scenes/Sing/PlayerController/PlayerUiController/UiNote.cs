using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UiNote : MonoBehaviour
{
    public StarParticle goldenStarPrefab;
    public StarParticle perfectStarPrefab;

    private RectTransform rectTransform;

    public Note Note { get; set; }
    public bool isGolden;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (isGolden)
        {
            CreateGoldenNoteEffect();
        }
        else
        {
            DestroyStars();
        }
    }

    private void DestroyStars()
    {
        foreach (StarParticle starParticle in GetComponentsInChildren<StarParticle>())
        {
            Destroy(starParticle.gameObject);
        }
    }

    private void CreateGoldenNoteEffect()
    {
        int starCount = GetComponentsInChildren<StarParticle>().Length;
        int targetStarCount = Mathf.Max(6, (int)rectTransform.rect.width / 10);
        if (starCount < targetStarCount)
        {
            CreateGoldenStar();
        }
    }

    private void CreateGoldenStar()
    {
        StarParticle star = Instantiate(goldenStarPrefab);
        star.transform.SetParent(transform);
        RectTransform starRectTransform = star.GetComponent<RectTransform>();
        float anchorX = Random.Range(0f, 1f);
        float anchorY = Random.Range(0f, 1f);
        starRectTransform.anchorMin = new Vector2(anchorX, anchorY);
        starRectTransform.anchorMax = new Vector2(anchorX, anchorY);
        starRectTransform.anchoredPosition = Vector2.zero;

        star.RectTransform.localScale = Vector3.one * Random.Range(0, 0.5f);
        LeanTween.scale(star.RectTransform, Vector3.one * Random.Range(0.5f, 1f), Random.Range(1f, 2f))
            .setOnComplete(() => Destroy(star.gameObject));
    }

    public void CreatePerfectNoteEffect()
    {
        int targetStarCount = Mathf.Max(6, (int)rectTransform.rect.width / 20);
        for (int i = 0; i < targetStarCount; i++)
        {
            StarParticle star = CreatePerfectStar();
            // Change parent, because this note will be destoyed soon.
            star.transform.SetParent(transform.parent);
        }
    }

    private StarParticle CreatePerfectStar()
    {
        StarParticle star = Instantiate(perfectStarPrefab);
        star.transform.SetParent(transform);
        RectTransform starRectTransform = star.GetComponent<RectTransform>();
        float anchorX = Random.Range(0f, 1f);
        float anchorY = Random.Range(0f, 1f);
        starRectTransform.anchorMin = new Vector2(anchorX, anchorY);
        starRectTransform.anchorMax = new Vector2(anchorX, anchorY);
        starRectTransform.anchoredPosition = Vector2.zero;

        star.RectTransform.localScale = Vector3.one * Random.Range(0.5f, 0.8f);
        LeanTween.scale(star.RectTransform, Vector3.zero, 1f)
            .setOnComplete(() => Destroy(star.gameObject));
        return star;
    }
}
