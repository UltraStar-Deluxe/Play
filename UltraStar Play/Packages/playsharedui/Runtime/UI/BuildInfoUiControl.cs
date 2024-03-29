using System.Globalization;
using ProTrans;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class BuildInfoUiControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private TextAsset versionPropertiesTextAsset;

    [Inject(UxmlName = "semanticVersionLabel")]
    private Label semanticVersionLabel;

    [Inject(UxmlName = "buildTimeStampLabel")]
    private Label buildTimeStampLabel;

    [Inject(UxmlName = "commitHashLabel")]
    private Label commitHashLabel;

    [Inject(UxmlName = "unityVersionLabel")]
    private Label unityVersionLabel;

    public void OnInjectionFinished()
    {
        PropertiesFile versionProperties = PropertiesFileParser.ParseText(versionPropertiesTextAsset.text, CultureInfo.InvariantCulture);

        // Show the release number (e.g. release date, or some version number)
        versionProperties.TryGetValue("release", out string release);
        semanticVersionLabel.text = Translation.Get("version", "value", release);

        // Show the commit hash of the build
        versionProperties.TryGetValue("commit_hash", out string commitHash);
        commitHashLabel.text = Translation.Get("commit", "value", commitHash);

        // Show the build time stamp
        versionProperties.TryGetValue("build_timestamp", out string buildTimeStamp);
        buildTimeStampLabel.text = Translation.Get("buildTimeStamp", "value", buildTimeStamp);

        // Show the Unity version
        versionProperties.TryGetValue("unity_version", out string unityVersion);
        if (!Application.isEditor
            && unityVersion != Application.unityVersion)
        {
            Debug.LogWarning("Unity version in VERSION.txt info file does not match the current Unity version. " +
                             $"VERSION.txt: '{unityVersion}', Application.unityVersion: '{Application.unityVersion}'");
        }
        unityVersionLabel.text = $"Unity version: {Application.unityVersion}";
    }
}
