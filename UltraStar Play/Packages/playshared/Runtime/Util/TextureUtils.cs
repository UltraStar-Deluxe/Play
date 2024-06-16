using UnityEngine;

namespace Util
{
    public static class TextureUtils
    {
        public static void FlipTextureVertically(Texture2D original)
        {
            var originalPixels = original.GetPixels();

            var newPixels = new Color[originalPixels.Length];

            var width = original.width;
            var rows = original.height;

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < rows; y++)
                {
                    newPixels[x + y * width] = originalPixels[x + (rows - y -1) * width];
                }
            }

            original.SetPixels(newPixels);
            original.Apply();
        }

        public static void FlipTextureHorizontally(Texture2D original)
        {
            var originalPixels = original.GetPixels();

            var newPixels = new Color[originalPixels.Length];

            var width = original.width;
            var rows = original.height;

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < rows; y++)
                {
                    newPixels[x + y * width] = originalPixels[(width - x - 1) + y * width];
                }
            }

            original.SetPixels(newPixels);
            original.Apply();
        }
    }
}
