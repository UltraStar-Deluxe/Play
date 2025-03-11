using System;
using UnityEngine;

public class WhisperSupportChecker
{
    public bool IsWhisperSupportedOnHardware()
    {
        string avxCheckPath = ApplicationUtils.GetStreamingAssetsPath("AvxCheck/avx-check.exe");
        if (!FileUtils.Exists(avxCheckPath))
        {
            return true;
        }

        // Run the avx-check.exe to check if the hardware supports AVX instructions.
        // This is necessary because the Whisper library uses AVX instructions.
        try
        {
            if (ProcessUtils.RunProcess(avxCheckPath, "--json", out string processOutput, out string processError))
            {
                AvxCheckResultJson avxCheckResultJson = JsonConverter.FromJson<AvxCheckResultJson>(processOutput);
                bool isAvxSupported = avxCheckResultJson.avx > 0
                                      && avxCheckResultJson.avx2 > 0;
                Debug.Log($"isAvxSupported: {isAvxSupported}");
                return isAvxSupported;
            }
            else
            {
                Debug.Log($"Failed to run avx-check. Assuming AVX instructions are supported. Error Message: {processError}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("Failed to run avx-check.exe");
            return true;
        }
    }

    private class AvxCheckResultJson
    {
        public int avx;
        public int avx2;
    }
}
