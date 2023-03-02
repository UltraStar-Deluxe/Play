using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// https://github.com/Unity-Technologies/UniversalRenderingExamples
// DrawFullscreenFeature.cs
public class BlitRendererFeature : ScriptableRendererFeature
{
    public enum BufferType
    {
        CameraColor,
        Custom 
    }
    
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        public Material blitMaterial = null;
        public int blitMaterialPassIndex = -1;
        public BufferType sourceType = BufferType.CameraColor;
        public BufferType destinationType = BufferType.CameraColor;
        public string sourceTextureId = "_SourceTexture";
        public string destinationTextureId = "_DestinationTexture";
    }

    public Settings settings = new Settings();
    
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering;
    
    private BlitRenderPass blitPass;
    
    public override void Create()
    {
        blitPass = new BlitRenderPass(name);
    }
 
    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        blitPass.renderPassEvent = settings.renderPassEvent;
        blitPass.settings = settings;
        renderer.EnqueuePass(blitPass);
    }
}
