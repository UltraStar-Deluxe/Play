public static class AudioWaveFormUtils
{
    public static void FindMinAndMaxValues(float[] samples, int offset, int length, out float min, out float max)
    {
        min = float.MaxValue;
        max = float.MinValue;

        for (int i = 0; i < length; i++)
        {
            int index = offset + i;
            if (index >= samples.Length)
            {
                min = 0;
                max = 0;
                break;
            }

            float f = samples[index];
            if (f < min)
            {
                min = f;
            }
            if (f > max)
            {
                max = f;
            }
        }
    }
}
