using System.Collections.Generic;
using UnityEngine;

public static class TextureUtils
{
    /**
     * Creates a small texture to be filled with a gradient.
     * The graphics engine will interpolate pixels as needed from the small texture.
     */
    public static Texture2D CreateGradientTexture(int width, int height)
    {
        Texture2D texture2D = new(width, height, TextureFormat.RGBA32, false);
        texture2D.filterMode = FilterMode.Bilinear;
        texture2D.wrapMode = TextureWrapMode.Mirror;
        return texture2D;
    }

    public static void FillTextureWithGradient(
        Texture2D texture2D,
        Color startColor,
        Color endColor,
        float angleDegrees)
    {
        Vector2 textureSize = new(texture2D.width, texture2D.height);
        Vector2 centerPos = textureSize * 0.5f;
        Rect textureRect = new(0, 0, texture2D.width, texture2D.height);
        
        // To be more similar to CSS background linear-gradient, rotate given angle.
        // See https://www.w3schools.com/css/tryit.asp?filename=trycss3_gradient-linear_angles
        float angleRadians = Mathf.Deg2Rad * (-angleDegrees + 90);
        Vector2 direction = new(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));

        Vector2 positionOutsideTextureInDirection = centerPos - (direction * textureSize * 2);
        LineUtils.TryGetIntersection(centerPos, positionOutsideTextureInDirection, textureRect, out Vector2 startPos);

        Vector2 positionOutsideTextureInOppositeDirection = centerPos + (direction * textureSize * 2);
        LineUtils.TryGetIntersection(centerPos, positionOutsideTextureInOppositeDirection, textureRect, out Vector2 endPos);
        
        FillTextureWithGradient(texture2D, startColor, endColor, startPos, endPos);
    }
    
    // See StackOverflow: https://stackoverflow.com/questions/521493/creating-a-linear-gradient-in-2d-array
    // Answer: https://stackoverflow.com/a/528001/4412885
    public static void FillTextureWithGradient(
        Texture2D texture2D,
        Color startColor,
        Color endColor,
        Vector2 startPos,
        Vector2 endPos)
    {
        float xS = startPos.x / (float)texture2D.width;
        float xE = endPos.x / (float)texture2D.width;
        float yS = startPos.y / (float)texture2D.height;
        float yE = endPos.y / (float)texture2D.height;
        float xD = xE - xS;
        float yD = yE - yS;
        float mod = 1.0f / ( xD * xD + yD * yD );

        Color GradientColorAt(int x, int y)
        {
            float xFactor = (float)x / (float)texture2D.width;
            float yFactor = (float)y / (float)texture2D.height;
            float gradPos = ( ( xFactor - xS ) * xD + ( yFactor - yS ) * yD ) * mod;

            float mag = gradPos > 0
                ? gradPos < 1.0f
                    ? gradPos
                    : 1.0f
                : 0.0f;
            return Color.Lerp(startColor, endColor, mag);
        }
    
        for (int y = 0; y < texture2D.height; y++)
        {
            for (int x = 0; x < texture2D.width; x++)
            {
                Color color = GradientColorAt(x, y);
                texture2D.SetPixel(x, y, color);
            }
        }
        texture2D.Apply();
    }
}
