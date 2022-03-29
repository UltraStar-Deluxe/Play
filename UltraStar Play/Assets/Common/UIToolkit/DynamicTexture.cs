using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UIElements;

/**
 * Holds a texture to draw pixels on a VisualElement (for UI Toolkit)
 * Example: 
 * <pre>
 *      VisualElement dynImage = uiDoc.rootVisualElement.Q<VisualElement>("dynImage");
 *      dynImage.RegisterCallback<GeometryChangedEvent>(_ =>
 *      {
 *          if (!dynamicTexture.IsInitialized)
 *          {
 *              dynamicTexture.Init(dynImage);
 *              dynamicTexture.DrawLine(0, 0, 100, 100, Color.red);
 *              dynamicTexture.ApplyTexture();
 *          }
 *      });
 * </pre>
 */
public class DynamicTexture
{
    public Color backgroundColor = new Color(0, 0, 0, 0);

    // Blank color array (background color in every pixel)
    private Color[] blank;
    private Texture2D texture;

    public int TextureWidth { get; private set; } = -1;
    public int TextureHeight { get; private set; } = -1;

    public bool IsInitialized => texture != null;

    public bool initializeTextureDelayed;

    private readonly GameObject gameObject;

    public DynamicTexture(GameObject gameObject, VisualElement visualElement)
    {
        this.gameObject = gameObject;

        if (visualElement.resolvedStyle.width <= 0
            || visualElement.resolvedStyle.height <= 0)
        {
            throw new UnityException("VisualElement has no size. Consider calling Init from GeometryChangedEvent.");
        }

        UIDocument uiDocument = GameObject.FindObjectOfType<UIDocument>();
        Vector2 visualElementSize = new PanelHelper(uiDocument).PanelToScreen(visualElement.worldBound).size;
        Init((int)visualElementSize.x, (int)visualElementSize.y);
        visualElement.style.backgroundImage = new StyleBackground(texture);
    }

    public void Init(int textureWidth, int textureHeight)
    {
        Destroy();

        TextureWidth = textureWidth;
        TextureHeight = textureHeight;
        CreateTexture();
    }

    public void Destroy()
    {
        if (texture != null)
        {
            GameObject.Destroy(texture);
            texture = null;
        }
    }

    private void CreateTexture()
    {
        if (texture != null)
        {
            return;
        }
        if (TextureWidth <= 0
            || TextureHeight <= 0)
        {
            throw new UnityException("Texture size missing. Call Init first and make sure the VisualElement has a size.");
        }

        // create the texture and assign to the rawImage
        texture = new Texture2D(TextureWidth, TextureHeight);

        // release texture when GameObject is destroyed
        gameObject.OnDestroyAsObservable()
            .Subscribe(_ => Destroy());

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
        CreateTexture();
        texture.Apply();
    }

    public void ClearTexture()
    {
        CreateTexture();
        texture.SetPixels(blank);
    }

    public void SetPixel(int x, int y, Color color)
    {
        CreateTexture();
        texture.SetPixel(x, y, color);
    }

    public void DrawRectByWidthAndHeight(int xStart, int yStart, int width, int height, Color color)
    {
        CreateTexture();
        int xEnd = xStart + width;
        int yEnd = yStart + height;
        DrawRectByCorners(xStart, yStart, xEnd, yEnd, color);
    }

    public void DrawRectByCorners(int xStart, int yStart, int xEnd, int yEnd, Color color)
    {
        CreateTexture();
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
        CreateTexture();

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
        CreateTexture();

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
