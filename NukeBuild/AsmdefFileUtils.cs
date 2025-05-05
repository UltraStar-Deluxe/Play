using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nuke.Common.IO;

namespace DefaultNamespace;

public class AsmdefFileUtils
{
    public static void Create(AbsolutePath targetPath)
    {
        Console.WriteLine($"Writing asmdef file: '{targetPath}'");

        Dictionary<string, object> asmdefContent = new()
        {
            ["name"] = Path.GetFileNameWithoutExtension(targetPath),
            ["references"] = new List<string>(),
            ["includePlatforms"] = new List<string>(),
        };

        File.WriteAllText(
            targetPath,
            JObject.FromObject(asmdefContent).ToString(Formatting.Indented));
    }
}
