using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class ProfileMarkerUtils
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        nameToProfilerMarker.Clear();
    }

    private static readonly Dictionary<string, ProfilerMarker> nameToProfilerMarker = new();

    public static ProfilerMarker.AutoScope Auto(string name)
    {
        return GetProfilerMarker(name).Auto();
    }
    
    public static ProfilerMarker GetProfilerMarker(string name)
    {
        if (nameToProfilerMarker.TryGetValue(name, out ProfilerMarker profilerMarker))
        {
            return profilerMarker;
        }

        profilerMarker = new(name);
        nameToProfilerMarker[name] = profilerMarker;
        return profilerMarker;
    }
}
