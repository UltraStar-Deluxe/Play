using UnityEngine;

public static class RenderTextureUtils
{
    public static void Clear(RenderTexture renderTexture)
    {
        // https://forum.unity.com/threads/how-to-clear-a-render-texture-to-transparent-color-all-bytes-at-0.147431/
        RenderTexture oldActiveRenderTexture = UnityEngine.RenderTexture.active;
        UnityEngine.RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.clear);
        UnityEngine.RenderTexture.active = oldActiveRenderTexture;
    }
}
