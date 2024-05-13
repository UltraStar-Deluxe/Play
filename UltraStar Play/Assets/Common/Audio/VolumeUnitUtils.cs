using UnityEngine;

public static class VolumeUnitUtils
{
    /**
     * Convert linear value between 0 and 1 to decibels
     */
    public static float GetDecibelValue(float linearValue)
    {
        // commonly used for linear to decibel conversion
        float conversionFactor = 20f;

        float decibelValue = linearValue != 0 ? conversionFactor * Mathf.Log10(linearValue) : -144f;
        return decibelValue;
    }

    /**
     * Convert decibel value to a range between 0 and 1
     */
    public static float GetLinearValue(float decibelValue)
    {
        float conversionFactor = 20f;

        return Mathf.Pow(10f, decibelValue / conversionFactor);

    }
}
