using UnityEngine;

public class StarParticle : MonoBehaviour
{
    private RectTransform rectTransform;
    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            return rectTransform;
        }
    }

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

    void Update()
    {
        Rotation += 1f;
    }
}
