using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class ShowFps : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        lastFpsLabelPositionInPx = Vector2.zero;
    }

    private static Vector2 lastFpsLabelPositionInPx;

    [ReadOnly]
    public int fps;

    [Inject]
    private Injector injector;

    [Inject]
    private UIDocument uiDocument;

    private float deltaTime;
    private int frameCount;

    private Label fpsLabel;

    private DragToMoveControl dragToMoveControl;

    public void OnInjectionFinished()
    {
        CreateLabel(uiDocument.rootVisualElement);

        dragToMoveControl = injector
            .WithRootVisualElement(fpsLabel)
            .WithBindingForInstance(gameObject)
            .CreateAndInject<DragToMoveControl>();
        dragToMoveControl.MovedEventStream
            .Subscribe(newPositionInPx => lastFpsLabelPositionInPx = newPositionInPx);
    }

    private void CreateLabel(VisualElement parent)
    {
        fpsLabel = new Label("FPS: ?");
        fpsLabel.AddToClassList("fpsLabel");
        parent.Add(fpsLabel);

        if (lastFpsLabelPositionInPx != Vector2.zero)
        {
            fpsLabel.style.left = lastFpsLabelPositionInPx.x;
            fpsLabel.style.top = lastFpsLabelPositionInPx.y;
        }
    }

    private void Update()
    {
        frameCount++;
        deltaTime += Time.deltaTime;

        if (deltaTime >= 0.5f)
        {
            fps = (int)Mathf.Ceil(frameCount / deltaTime);
            frameCount = 0;
            deltaTime -= 0.5f;

            if (fpsLabel != null)
            {
                fpsLabel.text = "FPS: " + fps;
            }
        }
    }

    private void OnDestroy()
    {
        fpsLabel?.RemoveFromHierarchy();
        dragToMoveControl?.Dispose();
    }
}
