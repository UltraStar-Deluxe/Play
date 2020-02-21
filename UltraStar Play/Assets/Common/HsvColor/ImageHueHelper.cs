using UnityEngine;
using UnityEngine.UI;

// Helper class to set the hue of images that use the HSVRangeShader.
[RequireComponent(typeof(Image))]
[ExecuteInEditMode]
public class ImageHueHelper : MonoBehaviour
{
    private Image image;

    public float hue;
    public Color hueAsColor;

    private float lastHue;
    private Color lastHueAsColor;

    void Awake()
    {
        image = GetComponent<Image>();
    }

    void OnEnable()
    {
        // Check that the image has the proper shader
        if (Application.isEditor && !image.material.HasProperty("_UseColorRedAsHue"))
        {
            throw new UnityException("The image must use a Material with a HSVRangeShader to set its hue.");
        }

        hueAsColor = Color.HSVToRGB(hue, 1, 1);
        lastHueAsColor = hueAsColor;

        lastHue = hue;

        ApplyHueToImage(image, hue);
    }

    void Update()
    {
        // Sync hue with the color, so changing the color the inspector changes the hue.
        if (lastHue != hue)
        {
            SetHue(hue);
        }
        else if (lastHueAsColor != hueAsColor)
        {
            SetHueByColor(hueAsColor);
        }
    }

    public void SetHue(float newHue)
    {
        hue = newHue;
        lastHue = hue;

        hueAsColor = Color.HSVToRGB(hue, 1, 1);
        lastHueAsColor = hueAsColor;

        ApplyHueToImage(image, hue);
    }

    public void SetHueByColor(Color c)
    {
        hueAsColor = c;
        lastHueAsColor = hueAsColor;

        Color.RGBToHSV(hueAsColor, out float newHue, out float _, out float _);
        hue = newHue;
        lastHue = hue;

        ApplyHueToImage(image, hue);
    }

    public static void ApplyHueToImage(Image theImage, float theHue)
    {
        // The red channel of the image's color is interpreted as hue by the HSVRangeShader.
        theImage.color = new Color(theHue, 0, 0);
    }
}
