using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Label = UnityEngine.UIElements.Label;
using UniRx;

public class ShowFps : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        lastFpsLabelPositionInPx = Vector2.zero;
    }

    private static Vector2 lastFpsLabelPositionInPx;

    public Text fpsText;

    [ReadOnly]
    public int fps;

    private float deltaTime;
    private int frameCount;

    private Label fpsLabel;

    private void Start()
    {
        UIDocument uiDocument = GameObjectUtils.FindComponentWithTag<UIDocument>("UIDocument");
        if (uiDocument != null)
        {
            CreateLabel(uiDocument.rootVisualElement);

            DragToMoveControl dragToMoveControl = new DragToMoveControl(uiDocument, fpsLabel, gameObject);
            dragToMoveControl.MovedEventStream
                .Subscribe(newPositionInPx => lastFpsLabelPositionInPx = newPositionInPx);
        }
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

            if (fpsText != null)
            {
                fpsText.text = "FPS: " + fps;
            }
            if (fpsLabel != null)
            {
                fpsLabel.text = "FPS: " + fps;
            }
        }
    }

    private void OnDestroy()
    {
        if (fpsLabel != null)
        {
            fpsLabel.RemoveFromHierarchy();
        }
    }
}
