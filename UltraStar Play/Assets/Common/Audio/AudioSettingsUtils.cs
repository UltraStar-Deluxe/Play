using UnityEngine;

// Change Unity AudioSettings, see https://docs.unity3d.com/ScriptReference/AudioSettings.Reset.html
public static class AudioSettingsUtils
{
    // Values experimentally found by using AudioSettings.GetDSPBufferSize on Windows 11.
    // NOTE: These values might depend on the specific hardware.
    // Default: 1024, BestLatency: 256, GoodLatency: 512, BestPerformance: 1024 
    public static int[] validDSPBufferSizes =
    {
        32, 64, 128, 256, 340, 480, 512, 1024, 2048, 4096, 8192
    };
    
    private static bool hasChangedDspBufferSize;
    
    public static void UpdateConfiguration(EDspBufferSize dspBufferSize)
    {
        if (!hasChangedDspBufferSize && dspBufferSize is EDspBufferSize.Default)
        {
            // Try to keep the Unity default value
            Debug.Log("Not changing DSP buffer size, keeping default value");
            return;
        }
        hasChangedDspBufferSize = true;
        
        AudioConfiguration config = AudioSettings.GetConfiguration();
        config.dspBufferSize = GetDspBufferSize(dspBufferSize);
        Debug.Log($"Setting DSP buffer size to {config.dspBufferSize}");
        AudioSettings.Reset(config);
    }

    private static int GetDspBufferSize(EDspBufferSize dspBufferSize)
    {
        switch (dspBufferSize)
        {
            case EDspBufferSize.LowLatency:
                return 256;
            case EDspBufferSize.GoodLatency:
                return 512;
            case EDspBufferSize.BalancedLatency:
                return 1024;
            case EDspBufferSize.HighLatency:
                return 2048;
            default:
                return 0;
        }
    }
}
