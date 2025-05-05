using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class RenderTextureManager : AbstractSingletonBehaviour, INeedInjection
{
    public static RenderTextureManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<RenderTextureManager>();

    [Inject]
    private UiManager uiManager;

    private readonly List<RenderTextureConsumer> renderTextureConsumers = new();

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        uiManager.ScreenSizeChangedEventStream
            // Throttle the event stream to avoid that the RenderTextures are recreated too often (e.g. when changing window size).
            .Throttle(new TimeSpan(0, 0, 0, 0, 1000))
            .Subscribe(evt => UpdateScreenSizedRenderTextures(evt));
    }

    private void UpdateScreenSizedRenderTextures(ScreenSizeChangedEvent evt)
    {
        Debug.Log($"Recreating screen sized RenderTextures because screen size changed: {evt}");
        Camera[] cameras = FindObjectsOfType<Camera>();
        renderTextureConsumers.ToList().ForEach(consumer =>
        {
            // A RenderTexture must not be destroyed when it is set as targetTexture of a camera.
            List<Camera> camerasUsingRenderTexture = cameras.Where(cam => cam.targetTexture == consumer.renderTexture).ToList();
            camerasUsingRenderTexture.ForEach(cam => cam.targetTexture = null);

            Destroy(consumer.renderTexture);
            consumer.renderTexture = consumer.isScreenSized
                ? DoCreateScreenSizedRenderTexture()
                : DoCreateScreenAspectRatioRenderTexture();
            consumer.useRenderTexture(consumer.renderTexture);

            // Reassign the RenderTexture to the cameras.
            camerasUsingRenderTexture.ForEach(cam => cam.targetTexture = consumer.renderTexture);
        });
    }

    public RenderTexture GetExistingRenderTexture(string renderTextureName)
    {
        RenderTextureConsumer existingConsumer = renderTextureConsumers.FirstOrDefault(consumer => consumer.renderTexture.name == renderTextureName);
        if (existingConsumer != null)
        {
            return existingConsumer.renderTexture;
        }

        return null;
    }

    public void GetOrCreateScreenAspectRatioRenderTexture(string renderTextureName, Action<RenderTexture> useRenderTexture)
    {
        GetOrCreateRenderTexture(renderTextureName, false, useRenderTexture);
    }

    public void GetOrCreateScreenSizedRenderTexture(string renderTextureName, Action<RenderTexture> useRenderTexture)
    {
        GetOrCreateRenderTexture(renderTextureName, true, useRenderTexture);
    }

    private void GetOrCreateRenderTexture(string renderTextureName, bool isScreenSized, Action<RenderTexture> useRenderTexture)
    {
        RenderTexture existingScreenSizedRenderTexture = GetExistingRenderTexture(renderTextureName);
        if (existingScreenSizedRenderTexture != null)
        {
            useRenderTexture(existingScreenSizedRenderTexture);
            return;
        }

        RenderTexture renderTexture = DoCreateScreenSizedRenderTexture();
        renderTexture.name = renderTextureName;
        useRenderTexture(renderTexture);

        renderTextureConsumers.Add(new ()
        {
            id = renderTextureName,
            useRenderTexture = useRenderTexture,
            renderTexture = renderTexture,
            isScreenSized = isScreenSized,
        });

        useRenderTexture(renderTexture);
    }

    private RenderTexture DoCreateScreenSizedRenderTexture()
    {
        return new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
    }

    private RenderTexture DoCreateScreenAspectRatioRenderTexture()
    {
        if (Screen.width <= 1920)
        {
            return DoCreateScreenSizedRenderTexture();
        }

        // Save memory by using a lower resolution with same aspect ratio.
        float aspectRatio = (float)Screen.width / Screen.height;
        int height = 720;
        int width = (int)(height * aspectRatio);

        return new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
    }

    protected override void OnDestroySingleton()
    {
        renderTextureConsumers.ForEach(consumer => Destroy(consumer.renderTexture));
    }

    private class RenderTextureConsumer
    {
        public string id;
        public Action<RenderTexture> useRenderTexture;
        public RenderTexture renderTexture;
        public bool isScreenSized;
    }
}
