using System;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class ShowFpsControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener
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

    [Inject]
    private ISettings settings;

    private float deltaTime;
    private int frameCount;

    private Label fpsLabel;

    private DragToMoveControl dragToMoveControl;

    public void OnInjectionFinished()
    {
        settings.ObserveEveryValueChanged(it => it.ShowFps)
            .Subscribe(_ => UpdateFpsLabelInstance());
        UpdateFpsLabelInstance();
    }

    private void UpdateFpsLabelInstance()
    {
        RemoveFpsLabel();
        if (settings.ShowFps)
        {
            CreateFpsLabel();
        }
    }

    private void RemoveFpsLabel()
    {
        if (fpsLabel != null)
        {
            fpsLabel.RemoveFromHierarchy();
            fpsLabel = null;
        }

        if (dragToMoveControl != null)
        {
            dragToMoveControl.Dispose();
            dragToMoveControl = null;
        }
    }

    private void CreateFpsLabel()
    {
        fpsLabel = new Label("FPS: " + fps);
        fpsLabel.AddToClassList("fpsLabel");
        uiDocument.rootVisualElement.Add(fpsLabel);

        if (lastFpsLabelPositionInPx != Vector2.zero)
        {
            fpsLabel.style.left = lastFpsLabelPositionInPx.x;
            fpsLabel.style.top = lastFpsLabelPositionInPx.y;
        }

        dragToMoveControl = injector
            .WithRootVisualElement(fpsLabel)
            .WithBindingForInstance(gameObject)
            .CreateAndInject<DragToMoveControl>();
        dragToMoveControl.MovedEventStream
            .Subscribe(newPositionInPx => lastFpsLabelPositionInPx = newPositionInPx);
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
        RemoveFpsLabel();
    }
}
