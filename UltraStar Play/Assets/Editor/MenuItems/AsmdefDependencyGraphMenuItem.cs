using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AsmdefDependencyGraphMenuItem
{
    private class AsmdefData
    {
        public FileInfo fileInfo;

        public string name;
        public string[] references;
        public string[] precompiledReferences;
    }

    [MenuItem("Generate/asmdef Dependency Graph (PlantUML)")]
    public static void GeneratePlantUml()
    {
        string[] foldersToSearch = {
            Application.dataPath, // Assets folder
        };
        string outputPath = new FileInfo($"{Application.dataPath}/../../Build/asmdef-dependency-graph.puml").FullName;

        List<AsmdefData> asmdefs = new();
        foreach (string folder in foldersToSearch)
        {
            asmdefs.AddRange(LoadAsmdefData(folder));
        }

        List<string> plantUmlLines = BuildPlantUml(asmdefs);
        File.WriteAllLines(outputPath, plantUmlLines);

        Debug.Log($"PlantUML dependency graph written to '{outputPath}'");
    }

    private static List<AsmdefData> LoadAsmdefData(string folder)
    {
        List<AsmdefData> asmdefs = new();
        string[] asmdefFiles = Directory.GetFiles(folder, "*.asmdef", SearchOption.AllDirectories);
        foreach (string file in asmdefFiles)
        {
            string json = File.ReadAllText(file);
            AsmdefData data = JsonConverter.FromJson<AsmdefData>(json);
            if (IsValid(data)
                && !IsIgnoredNode(data.name))
            {
                data.fileInfo = new FileInfo(file);
                asmdefs.Add(data);
            }
        }

        return asmdefs;
    }

    private static bool IsValid(AsmdefData data)
    {
        return data != null && !data.name.IsNullOrEmpty();
    }

    private static Dictionary<string, List<string>> GroupAsmdefsByFolder(List<AsmdefData> asmdefs)
    {
        Dictionary<string,string> asmdefToFolder = asmdefs.ToDictionary(asmdef => asmdef.name, asmdef => asmdef.fileInfo.Directory.FullName);
        return asmdefToFolder
            .GroupBy(kvp => kvp.Value)
            .ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Key).ToList());
    }

    private static List<string> BuildPlantUml(List<AsmdefData> asmdefs)
    {
        Dictionary<string, List<string>> groupedAsmdefs = GroupAsmdefsByFolder(asmdefs);

        var lines = new List<string>
        {
            "@startuml \"assembly-dependencies\"",
            "!pragma layout smetana",
            ""
        };

        AddGroupedComponents(lines, groupedAsmdefs);
        AddDependencies(lines, asmdefs);

        lines.Add("");
        lines.Add("@enduml");
        return lines;
    }

    private static void AddGroupedComponents(List<string> lines, Dictionary<string, List<string>> groupedAsmdefs)
    {
        foreach (var group in groupedAsmdefs)
        {
            lines.Add("' ---------------------------");
            lines.Add($"' {group.Key}");

            if (group.Value.Count > 1)
            {
                lines.Add($"component [{Path.GetFileName(group.Key)}] {{");
                foreach (string asmdef in group.Value)
                    lines.Add($"    [{asmdef}]");
                lines.Add("}");
            }
            else
            {
                lines.Add($"[{group.Value[0]}]");
            }

            lines.Add("");
        }
    }

    private static void AddDependencies(List<string> lines, List<AsmdefData> asmdefs)
    {
        lines.Add("' ---------------------------");
        lines.Add("' Dependencies");
        lines.Add("");

        foreach (AsmdefData asmdef in asmdefs)
        {
            AddAsmdefReferences(lines, asmdef, asmdefs);
            AddPrecompiledDllReferences(lines, asmdef, asmdefs);
        }
    }

    private static void AddAsmdefReferences(List<string> lines, AsmdefData asmdef, List<AsmdefData> asmdefs)
    {
        if (asmdef.references.IsNullOrEmpty())
        {
            return;
        }

        foreach (string reference in asmdef.references)
        {
            if (IsIgnoredReference(reference))
            {
                continue;
            }

            lines.Add($"[{asmdef.name}] --> [{reference}]");
        }
    }

    private static bool IsIgnoredNode(string node)
    {
        return node.EndsWith("Tests")
            || node.EndsWith("Editor");
    }

    private static bool IsIgnoredReference(string reference)
    {
        return reference.StartsWith("GUID:")
               || reference.StartsWith("Unity.")
               || reference.EndsWith("Tests")
               || reference.EndsWith("Editor");
    }

    private static void AddPrecompiledDllReferences(List<string> lines, AsmdefData asmdef, List<AsmdefData> asmdefs)
    {
        if (asmdef.precompiledReferences.IsNullOrEmpty())
        {
            return;
        }

        foreach (string reference in asmdef.precompiledReferences)
        {
            lines.Add($"[{asmdef.name}] --> [{reference}]");
        }
    }
}
