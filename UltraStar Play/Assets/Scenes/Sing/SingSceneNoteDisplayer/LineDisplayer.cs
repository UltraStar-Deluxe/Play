using System;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class LineDisplayer : INeedInjection, IInjectionFinishedListener
{
    public Color LineColor { get; set; }

    [Inject(UxmlName = R.UxmlNames.noteLines)]
    private VisualElement visualElement;

    [Inject]
    private SingSceneControl singSceneControl;

    private DynamicTexture dynamicTexture;

    private readonly List<Action> pendingActionList = new();

    public void OnInjectionFinished()
    {
        if (visualElement.resolvedStyle.width > 0
            && visualElement.resolvedStyle.height > 0)
        {
            CreateDynamicTexture();
        }
        else
        {
            // Wait until geometry has been calculated
            visualElement.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (dynamicTexture == null
                    && visualElement.resolvedStyle.width > 0
                    && visualElement.resolvedStyle.height > 0)
                {
                    CreateDynamicTexture();
                }
            });
        }
    }

    private void CreateDynamicTexture()
    {
        dynamicTexture = new DynamicTexture(singSceneControl.gameObject, visualElement);
        pendingActionList.ForEach(action => action());
        pendingActionList.Clear();
    }

    public void DrawLines(IEnumerable<float> noteRowToYPercent)
    {
        if (dynamicTexture == null)
        {
            pendingActionList.Add(() => DrawLines(noteRowToYPercent));
            return;
        }

        dynamicTexture.ClearTexture();
        noteRowToYPercent.ForEach(y => DrawLine(y));
        dynamicTexture.ApplyTexture();
    }

    private void DrawLine(float yPercent)
    {
        int y = (int)(dynamicTexture.TextureHeight * yPercent);
        for (int x = 0; x < dynamicTexture.TextureWidth; x++)
        {
            dynamicTexture.SetPixel(x, y, LineColor);
        }
    }
}
