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

    public void Update()
    {
        Rotation += 1f;
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

    public void SetPosition(Vector2 pos)
    {
        VisualElement.style.left = new StyleLength(new Length(pos.x, LengthUnit.Percent));
        VisualElement.style.bottom = new StyleLength(new Length(pos.y, LengthUnit.Percent));
    }

    public void SetScale(Vector2 scale)
    {
        VisualElement.style.scale = new StyleScale(new Scale(scale));
    }
}
