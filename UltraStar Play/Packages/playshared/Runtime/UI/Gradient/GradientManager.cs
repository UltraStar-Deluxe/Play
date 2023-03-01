using System.Collections.Generic;
using UnityEngine;

public static class GradientManager
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        DestroyGradientTextures();
    }
    
    private static readonly Dictionary<GradientConfig, Texture2D> gradientConfigToTexture = new();

    public static void DestroyGradientTextures()
    {
        gradientConfigToTexture.ForEach(entry => GameObject.Destroy(entry.Value));
        gradientConfigToTexture.Clear();
    }
    
    public static Texture2D GetGradientTexture(GradientConfig gradientConfig)
    {
        if (gradientConfigToTexture.TryGetValue(gradientConfig, out Texture2D texture))
        {
            return texture;
        }

        Texture2D texture2D = TextureUtils.CreateGradientTexture(16, 16);
        TextureUtils.FillTextureWithGradient(texture2D, gradientConfig.startColor, gradientConfig.endColor, gradientConfig.angleDegrees);
        gradientConfigToTexture[gradientConfig] = texture2D;
        return texture2D;
    }

    public static List<GradientConfig> GetGradientConfigsForTransition(GradientConfig fromGradient, GradientConfig toGradient, float animTimeInSeconds)
    {
        List<GradientConfig> gradientConfigs = new();
        int iterations = (int)(Application.targetFrameRate * animTimeInSeconds) + 1;
        for (int i = 0; i < iterations; i++)
        {
            GradientConfig gradientConfig = new()
            {
                startColor = Color32.Lerp(fromGradient.startColor, toGradient.startColor, i / (float)iterations),
                endColor = Color32.Lerp(fromGradient.endColor, toGradient.endColor, i / (float)iterations),
                angleDegrees = Mathf.Lerp(fromGradient.angleDegrees, toGradient.angleDegrees, i / (float)iterations)
            };
            gradientConfigs.Add(gradientConfig);
        }
        return gradientConfigs;
    }
}
