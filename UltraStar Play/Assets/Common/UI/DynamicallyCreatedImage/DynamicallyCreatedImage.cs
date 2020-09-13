using System;
using System.Collections.Generic;
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

    // The texture might not be creatable until the layout engine has calculated the size of the RectTransform.
    // In this case the actions are stored in this list for later execution when the texture has been created.
    private List<Action> actionQueue = new List<Action>();
    private bool isTextureCreated;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rawImage = GetComponent<RawImage>();
        TryCreateTexture();
    }

    private void Update()
    {
        if (!isTextureCreated)
        {
            TryCreateTexture();
        }

        if (isTextureCreated
            && actionQueue.Count > 0)
        {
            foreach (Action action in actionQueue)
            {
                action.Invoke();
            }
            actionQueue.Clear();
        }
    }

    private void TryCreateTexture()
    {
        // The size of the RectTransform can be zero in the first frame, when inside a layout group.
        // See https://forum.unity.com/threads/solved-cant-get-the-rect-width-rect-height-of-an-element-when-using-layouts.377953/
        if (rectTransform.rect.width <= 0
            || rectTransform.rect.height <= 0)
        {
            rawImage.enabled = false;
            return;
        }
        isTextureCreated = true;
        rawImage.enabled = true;

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
        if (texture == null)
        {
            actionQueue.Add(() => ApplyTexture());
            return;
        }

        texture.Apply();
    }

    public void ClearTexture()
    {
        if (texture == null)
        {
            actionQueue.Add(() => ClearTexture());
            return;
        }

        texture.SetPixels(blank);
    }

    public void ShiftImageHorizontally(float xPercent)
    {
        if (rawImage == null)
        {
            actionQueue.Add(() => ShiftImageHorizontally(xPercent));
            return;
        }

        rawImage.uvRect = new Rect(xPercent, rawImage.uvRect.y, rawImage.uvRect.width, rawImage.uvRect.height);
    }

    public void ShiftImageVertically(float yPercent)
    {
        if (rawImage == null)
        {
            actionQueue.Add(() => ShiftImageVertically(yPercent));
            return;
        }

        rawImage.uvRect = new Rect(rawImage.uvRect.x, yPercent, rawImage.uvRect.width, rawImage.uvRect.height);
    }

    public void SetPixel(int x, int y, Color color)
    {
        if (texture == null)
        {
            actionQueue.Add(() => SetPixel(x, y, color));
            return;
        }

        texture.SetPixel(x, y, color);
    }

    public void DrawRectByWidthAndHeight(int xStart, int yStart, int width, int height, Color color)
    {
        if (texture == null)
        {
            actionQueue.Add(() => DrawRectByWidthAndHeight(xStart, yStart, width, height, color));
            return;
        }

        int xEnd = xStart + width;
        int yEnd = yStart + height;
        DrawRectByCorners(xStart, yStart, xEnd, yEnd, color);
    }

    public void DrawRectByCorners(int xStart, int yStart, int xEnd, int yEnd, Color color)
    {
        if (texture == null)
        {
            actionQueue.Add(() => DrawRectByCorners(xStart, yStart, xEnd, yEnd, color));
            return;
        }

        for (int x = xStart; x < xEnd; x++)
        {
            for (int y = yStart; y < yEnd; y++)
            {
                SetPixel(x, y, color);
            }
        }
    }

    /**
     * Draws a 1px thick line between points a and b.
     */
    public void DrawLine(int ax, int ay, int bx, int by, Color color)
    {
        if (texture == null)
        {
            actionQueue.Add(() => DrawLine(ax, ay, bx, by, color));
            return;
        }

        // Bresenham algorithm to draw lines.
        // See https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm
        int dx = Math.Abs(bx - ax), sx = ax < bx ? 1 : -1;
        int dy = -Math.Abs(by - ay), sy = ay < by ? 1 : -1;
        int err = dx + dy, e2; /* error value e_xy */

        while (true)
        {
            SetPixel(ax, ay, color);
            if (ax == bx && ay == by)
            {
                break;
            }
            e2 = 2 * err;
            if (e2 > dy)
            {
                /* e_xy+e_x > 0 */
                err += dy;
                ax += sx;
            }
            if (e2 < dx)
            {
                /* e_xy+e_y < 0 */
                err += dx;
                ay += sy;
            }
        }
    }

    /**
     * Draws a line (between points a and b) with thickness and anti-aliasing.
     */
    public void DrawLine(int ax, int ay, int bx, int by, float thickness, Color color)
    {
        if (texture == null)
        {
            actionQueue.Add(() => DrawLine(ax, ay, bx, by, thickness, color));
            return;
        }

        // Represent lines as capsule shapes,
        // implements anti-aliasing by signed distance field (SDF) and optimization with AABB
        // See https://github.com/miloyip/line/blob/master/line_sdfaabb.c

        // r is the radius of the capsule. Thus, use half the thickness.
        float r = thickness / 2;
        int x0 = (int)Mathf.Floor(Math.Min(ax, bx) - r);
        x0 = NumberUtils.Limit(x0, 0, TextureWidth - 1);
        int x1 = (int)Mathf.Ceil(Math.Max(ax, bx) + r);
        x1 = NumberUtils.Limit(x1, 0, TextureWidth - 1);

        int y0 = (int)Mathf.Floor(Math.Min(ay, by) - r);
        y0 = NumberUtils.Limit(y0, 0, TextureHeight - 1);
        int y1 = (int)Mathf.Ceil(Math.Max(ay, by) + r);
        y1 = NumberUtils.Limit(y1, 0, TextureHeight - 1);

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float alpha = Mathf.Max(Mathf.Min(0.5f - CapsuleSdf(x, y, ax, ay, bx, by, r), 1.0f), 0.0f);
                Color currentColor = texture.GetPixel(x, y);
                Color finalColor = AlphaBlend(currentColor, color, alpha);
                SetPixel(x, y, finalColor);
            }
        }
    }

    private float CapsuleSdf(float px, float py, float ax, float ay, float bx, float by, float r)
    {
        // See https://github.com/miloyip/line/blob/master/line_sdfaabb.c
        float pax = px - ax;
        float pay = py - ay;
        float bax = bx - ax;
        float bay = by - ay;
        float h = Mathf.Max(Mathf.Min((pax * bax + pay * bay) / (bax * bax + bay * bay), 1.0f), 0.0f);
        float dx = pax - bax * h, dy = pay - bay * h;
        return Mathf.Sqrt(dx * dx + dy * dy) - r;
    }

    private Color AlphaBlend(Color currentColor, Color targetColor, float alpha)
    {
        return new Color(currentColor.r * (1 - alpha) + targetColor.r * alpha,
            currentColor.g * (1 - alpha) + targetColor.g * alpha,
            currentColor.b * (1 - alpha) + targetColor.b * alpha,
            currentColor.a * (1 - alpha) + targetColor.a * alpha);
    }
}
