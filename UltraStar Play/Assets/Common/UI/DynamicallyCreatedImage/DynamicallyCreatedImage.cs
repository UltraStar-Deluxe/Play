using System;
using UnityEngine;
using UnityEngine.UI;

// Creates an image in the size of the RectTransform to draw on that image.
[RequireComponent(typeof(RawImage))]
public class DynamicallyCreatedImage : MonoBehaviour
{
    public Color backgroundColor = new Color(0, 0, 0, 0);

    // blank image array (background color in every pixel)
    private Color[] blank;
    private Texture2D texture;
    private RawImage rawImage;
    private RectTransform rectTransform;

    public int TextureWidth { get; set; } = 256;
    public int TextureHeight { get; set; } = 256;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rawImage = GetComponent<RawImage>();
        CreateTexture();
    }

    private void CreateTexture()
    {
        // The size of the RectTransform can be zero in the first frame, when inside a layout group.
        // See https://forum.unity.com/threads/solved-cant-get-the-rect-width-rect-height-of-an-element-when-using-layouts.377953/
        if (rectTransform.rect.width != 0)
        {
            TextureWidth = (int)rectTransform.rect.width;
        }
        if (rectTransform.rect.height != 0)
        {
            TextureHeight = (int)rectTransform.rect.height;
        }

        // create the texture and assign to the rawImage
        texture = new Texture2D(TextureWidth, TextureHeight);
        rawImage.texture = texture;

        // create a 'blank screen' image 
        blank = new Color[TextureWidth * TextureHeight];
        for (int i = 0; i < blank.Length; i++)
        {
            blank[i] = backgroundColor;
        }

        // reset the texture to the background color
        ClearTexture();
        ApplyTexture();
    }

    public void ApplyTexture()
    {
        // upload to the graphics card 
        texture.Apply();
    }

    public void ClearTexture()
    {
        texture.SetPixels(blank);
    }

    public void ShiftImageHorizontally(float xPercent)
    {
        rawImage.uvRect = new Rect(xPercent, rawImage.uvRect.y, rawImage.uvRect.width, rawImage.uvRect.height);
    }

    public void ShiftImageVertically(float yPercent)
    {
        rawImage.uvRect = new Rect(rawImage.uvRect.x, yPercent, rawImage.uvRect.width, rawImage.uvRect.height);
    }

    public void SetPixel(int x, int y, Color color)
    {
        texture.SetPixel(x, y, color);
    }

    public void DrawRectByWidthAndHeight(int xStart, int yStart, int width, int height, Color color)
    {
        int xEnd = xStart + width;
        int yEnd = yStart + height;
        DrawRectByCorners(xStart, yStart, xEnd, yEnd, color);
    }

    public void DrawRectByCorners(int xStart, int yStart, int xEnd, int yEnd, Color color)
    {
        for (int x = xStart; x < xEnd; x++)
        {
            for (int y = yStart; y < yEnd; y++)
            {
                SetPixel(x, y, color);
            }
        }
    }
}