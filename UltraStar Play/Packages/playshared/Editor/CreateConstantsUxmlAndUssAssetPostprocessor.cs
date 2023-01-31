using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProTrans;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using UnityEditor;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CreateConstantsUxmlAndUssAssetPostprocessor : AssetPostprocessor
{
    private static readonly bool createConstansOnFileChange = true;

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (!createConstansOnFileChange)
        {
            return;
        }

        HashSet<string> changedUxmlFiles = new();
        HashSet<string> changedUssFiles = new();
        List<string> paths = importedAssets.Union(movedAssets).ToList();
        foreach (string path in paths)
        {
            string pathToLower = path.ToLowerInvariant();
            if (pathToLower.EndsWith(".uxml"))
            {
                changedUxmlFiles.Add(path);
            }
            else if (pathToLower.EndsWith(".uss"))
            {
                changedUssFiles.Add(path);
            }
        }

        if (changedUxmlFiles.Count > 0
            || changedUssFiles.Count > 0)
        {
            List<string> changedFiles = changedUxmlFiles
                .Union(changedUssFiles)
                .ToList();
            string changedFileNamesCsv = changedFiles.Select(path => Path.GetFileName(path)).ToCsv();
            Debug.Log($"Creating UXML and USS constants because of changed files: {changedFileNamesCsv}");
            CreateConstantsMenuItems.CreateConstantsForUxmlNames();
            CreateConstantsMenuItems.CreateConstantsForUssClasses();
        }
    }
}
