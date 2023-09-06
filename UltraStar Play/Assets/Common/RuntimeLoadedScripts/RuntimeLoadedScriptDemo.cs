using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UniInject;
using UnityEngine;

public class RuntimeLoadedScriptDemo : MonoBehaviour, INeedInjection
{
    [Inject]
    private Injector injector;

    private void Start()
    {
        CompilerWrapper compilerWrapper = CreateCompilerWrapper();

        // load text files and run them
        string runtimeLoadedScriptsFolder = ApplicationUtils.GetStreamingAssetsPath("RuntimeLoadedScripts");
        string[] csFiles = Directory.GetFiles(runtimeLoadedScriptsFolder, "*.cs");
        foreach (var file in csFiles)
        {
            compilerWrapper.Execute(file);

            string fileName = Path.GetFileName(file);
            string report = compilerWrapper.GetReport();
            if (!report.IsNullOrEmpty())
            {
                string logMessage = $"{fileName}  {report}";
                if (compilerWrapper.ErrorsCount > 0)
                {
                    Debug.LogError($"Error in '{fileName}': {logMessage}");
                }
                else
                {
                    Debug.Log($"Successfully compiled file '{fileName}'. {logMessage}");
                }
            }
        }

        // See what we got! this includes built-ins as well as loaded ones
        if (compilerWrapper.ErrorsCount > 0)
        {
            return;
        }

        List<IHighscoreProvider> highscoreProviders = compilerWrapper.CreateInstancesOf<IHighscoreProvider>().ToList();
        highscoreProviders = GetNewestImplementations(highscoreProviders);
        IHighscoreProvider highscoreProvider = highscoreProviders.LastOrDefault();
        injector.Inject(highscoreProvider);

        SongMetaManager.Instance.ScanFilesIfNotDoneYet();
        SongMetaManager.Instance.WaitUntilSongScanFinished();
        Debug.Log($"HighscoreProvider {highscoreProvider.GetType()}, score: {highscoreProvider.GetScore()}, note count: {highscoreProvider.GetNoteCount(SongMetaManager.Instance.GetFirstSongMeta())}");
    }

    private CompilerWrapper CreateCompilerWrapper()
    {
        CompilerWrapper result = new();

        // UniInject
        result.ReferenceAssembly(Assembly.GetAssembly(typeof(InjectAttribute)));
        // PlayShared
        result.ReferenceAssembly(Assembly.GetAssembly(typeof(SongMeta)));
        // PlayShared.UI
        result.ReferenceAssembly(Assembly.GetAssembly(typeof(VisualElementUtils)));
        // Common
        result.ReferenceAssembly(Assembly.GetAssembly(typeof(ApplicationManager)));

        return result;
    }

    public static List<T> GetNewestImplementations<T>(List<T> instances)
    {
        // Multiple implementations of the same class can be loaded, e.g. during development.
        // These implementations cannot be unloaded without unloading the whole AppDomain.
        // Thus, if there are multiple instances with the same class name then we only take the last implementation, which is the most up-to-date version.
        return instances.GroupBy(highscoreProvider => highscoreProvider.GetType().Name)
            .Select(group => group.Last())
            .ToList();
    }
}
