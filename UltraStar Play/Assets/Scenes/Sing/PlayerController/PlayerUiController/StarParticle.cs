using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class StarParticle : MonoBehaviour
{
    public float TargetLifetimeInSeconds { get; set; }
    public float TargetScale { get; set; }
    public float RotationSpeed { get; set; }
    public float StartScale { get; set; }

    private RectTransform rectTransform;
    private Image image;

    private float scale;
    private float Scale
    {
        get
        {
            return scale;
        }
        set
        {
            scale = value;
            rectTransform.localScale = Vector3.one * scale;
        }
    }
    private float LifetimeInSeconds { get; set; }

    private float rotation;
    public float Rotation
    {
        get
        {
            return rotation;
        }
        set
        {
            rotation = value;
            rectTransform.eulerAngles = new Vector3(0, 0, rotation);
        }
    }

    public void Init()
    {
        rectTransform = GetComponent<RectTransform>();
        Rotation = Random.Range(0, 360);
        image = GetComponent<Image>();

        TargetLifetimeInSeconds = Random.Range(2f, 2f);
        RotationSpeed = 1f;
        TargetScale = Random.Range(0.5f, 1f);
        Scale = 0;
    }

    void Update()
    {
        LifetimeInSeconds += Time.deltaTime;

        Scale = StartScale + (TargetScale - StartScale) * (LifetimeInSeconds / TargetLifetimeInSeconds);
        Rotation += RotationSpeed;

        if (LifetimeInSeconds >= TargetLifetimeInSeconds)
        {
            Destroy(gameObject);
        }
    }
}
