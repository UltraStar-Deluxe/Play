using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class ReflectionUtils
{
    public static List<Type> GetTypeInAppDomain<T>(bool logExceptions)
    {
        Type parent = typeof(T);
        Debug.Log($"Searching implementations of {parent} in app domain.");

        using DisposableStopwatch d = new($"Searching implementations of {parent} in app domain took <ms> ms");

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        List<ReflectionTypeLoadException> exceptions = new();

        List<Type> types = assemblies.SelectMany(assembly =>
        {
            Type[] typesOfAssembly;
            try
            {
                typesOfAssembly = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                exceptions.Add(ex);

                // Careful: types that could not be loaded are null in the array.
                typesOfAssembly = ex.Types;
            }

            return typesOfAssembly.Where(type => type != null
                                                 && !type.IsAbstract
                                                 && !type.IsInterface
                                                 && parent.IsAssignableFrom(type));
        }).ToList();

        if (logExceptions)
        {
            // Only log duplicate messages once.
            HashSet<string> loggedErrorMessages = new();
            exceptions
                .Distinct()
                .ForEach(ex =>
                {
                    if (loggedErrorMessages.Contains(ex.Message))
                    {
                        return;
                    }
                    Debug.LogException(ex);
                    loggedErrorMessages.Add(ex.Message);
                });
        }

        return types;
    }
}
