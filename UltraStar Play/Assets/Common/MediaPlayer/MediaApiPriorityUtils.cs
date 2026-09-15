using System.Collections.Generic;
using System.Linq;

public static class MediaApiPriorityUtils
{
    public static List<EMediaApi> GetMediaApisOrderedByPriority(Settings settings)
    {
        // Default priority: Unity by default, then AVPro, then VLC
        Dictionary<EMediaApi, int> apiPriority = new Dictionary<EMediaApi, int>
        {
            { EMediaApi.Unity, 2 },
            { EMediaApi.Avpro, 1 },
            { EMediaApi.Vlc, 0 },
        };

        // Remove disabled
        if (settings.UnityMediaApiUsage == EApiUsage.Disabled)
        {
            apiPriority.Remove(EMediaApi.Unity);
        }
        if (settings.AvProApiUsage == EApiUsage.Disabled)
        {
            apiPriority.Remove(EMediaApi.Avpro);
        }
        if (settings.VlcApiUsage == EApiUsage.Disabled)
        {
            apiPriority.Remove(EMediaApi.Vlc);
        }
        
        // Increase preferred
        int increase = 10; // Must be higher than any default priority.
        if (settings.UnityMediaApiUsage == EApiUsage.Preferred)
        {
            apiPriority[EMediaApi.Unity] += increase;
        }
        if (settings.AvProApiUsage == EApiUsage.Preferred)
        {
            apiPriority[EMediaApi.Avpro] += increase;
        }
        if (settings.VlcApiUsage == EApiUsage.Preferred)
        {
            apiPriority[EMediaApi.Vlc] += increase;
        }
        
        return apiPriority
            .OrderByDescending(x => x.Value)
            .Select(x => x.Key)
            .ToList();
    }
}

