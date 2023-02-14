using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UniInject;
using UnityEngine;

public class RuntimeLoadedScriptDemo : MonoBehaviour, INeedInjection
{
    private void Start()
    {
        CompilerWrapper compilerWrapper = CreateCompilerWrapper();
        
        // load text files and run them
        string[] csFiles = Directory.GetFiles(Application.streamingAssetsPath + "/Mods", "*.cs");
        foreach (var file in csFiles)
        {
            compilerWrapper.Execute(file);

            string fileName = Path.GetFileName(file);
            Debug.Log($"Executed file {fileName}");
            string report = compilerWrapper.GetReport();
            if (!report.IsNullOrEmpty())
            {
                string logMessage = $"{fileName}  {report}";
                if (compilerWrapper.ErrorsCount > 0)
                {
                    Debug.LogError(logMessage);
                }
                else
                {
                    Debug.Log(logMessage);
                }
            }
        }

        // See what we got! this includes built-ins as well as loaded ones
        if (compilerWrapper.ErrorsCount > 0)
        {
            return;
        }
        
        IEnumerable<IHighscoreProvider> highscoreProviders = compilerWrapper.CreateInstancesOf<IHighscoreProvider>();

        SongMetaManager.Instance.ScanFilesIfNotDoneYet();
        SongMetaManager.Instance.WaitUntilSongScanFinished();
        foreach (IHighscoreProvider highscoreProvider in highscoreProviders)
        {
            Debug.Log($"HighscoreProvider {highscoreProvider.GetType()}, score: {highscoreProvider.GetScore()}, note count: {highscoreProvider.GetNoteCount(SongMetaManager.Instance.GetFirstSongMeta())}");
        }
    }

    private CompilerWrapper CreateCompilerWrapper()
    {
        CompilerWrapper result = new();
        result.ReferenceCurrentAssembly();

        Assembly commonAssembly = Assembly.GetAssembly(typeof(SongMeta));
        result.ReferenceAssembly(commonAssembly);
        
        return result;
    }
}
