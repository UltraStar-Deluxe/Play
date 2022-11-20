using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class StarParticleControl : INeedInjection
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    private VisualElement visualElementToFollow;
    public VisualElement VisualElementToFollow
    {
        get
        {
            return visualElementToFollow;
        }

        set
        {
            visualElementToFollow = value;
            lastVisualElementToFollowPosition = visualElementToFollow.style.left.value.value;
        }
    }
    private float lastVisualElementToFollowPosition;

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
            VisualElement.style.rotate = new StyleRotate(new Rotate(rotation));
        }
    }

    public float RotationVelocityInDegreesPerSecond { get; set; }

    public Vector2 VelocityInPercentPerSecond { get; set; }

    public void Update()
    {
        if (RotationVelocityInDegreesPerSecond != 0)
        {
            Rotation += RotationVelocityInDegreesPerSecond * Time.deltaTime;
        }

        if (VelocityInPercentPerSecond.x != 0 || VelocityInPercentPerSecond.y != 0)
        {
            Vector2 positionInPercent = new(VisualElement.style.left.value.value, VisualElement.style.top.value.value);
            Vector2 newPositionInPercent = positionInPercent + VelocityInPercentPerSecond * Time.deltaTime;
            SetPosition(newPositionInPercent);
        }
        FollowTargetVisualElement();
    }

    private void FollowTargetVisualElement()
    {
        if (visualElementToFollow != null)
        {
            float shift = visualElementToFollow.style.left.value.value - lastVisualElementToFollowPosition;
            float newLeft = VisualElement.style.left.value.value + shift;
            VisualElement.style.left = new StyleLength(new Length(newLeft, LengthUnit.Percent));
            lastVisualElementToFollowPosition = visualElementToFollow.style.left.value.value;
        }
    }

    public void SetPosition(Vector2 pos, LengthUnit lengthUnit = LengthUnit.Percent)
    {
        VisualElement.style.left = new StyleLength(new Length(pos.x, lengthUnit));
        VisualElement.style.top = new StyleLength(new Length(pos.y, lengthUnit));
    }

    public void SetScale(Vector2 scale)
    {
        VisualElement.style.scale = new StyleScale(new Scale(scale));
    }

    public void SetOpacity(float alpha)
    {
        VisualElement.style.opacity = alpha;
    }
}
