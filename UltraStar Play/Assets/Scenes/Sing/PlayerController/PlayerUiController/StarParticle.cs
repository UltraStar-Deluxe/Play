using UnityEngine;

public class StarParticle : MonoBehaviour
{
    private RectTransform rectTransformToFollow;
    public RectTransform RectTransformToFollow
    {
        get
        {
            return rectTransformToFollow;
        }

        set
        {
            rectTransformToFollow = value;
            lastRectTransformToFollowLocalPosition = rectTransformToFollow.localPosition;
        }
    }
    private Vector3 lastRectTransformToFollowLocalPosition;

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

    void LateUpdate()
    {
        FollowRectTransform();
    }

    public void FollowRectTransform()
    {
        if (rectTransformToFollow != null)
        {
            Vector3 shift = rectTransformToFollow.localPosition - lastRectTransformToFollowLocalPosition;
            RectTransform.localPosition = RectTransform.localPosition + shift;
            lastRectTransformToFollowLocalPosition = rectTransformToFollow.localPosition;
        }
    }
}
