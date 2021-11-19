using PrimeInputActions;
using ProTrans;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongLibraryOptionsUiControl : MonoBehaviour, INeedInjection, ITranslator
{
    [InjectedInInspector]
    public VisualTreeAsset songFolderListEntryAsset;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.downloadSceneButton)]
    private Button downloadSceneButton;

    [Inject(UxmlName = R.UxmlNames.songList)]
    private ScrollView songList;

    [Inject(UxmlName = R.UxmlNames.addButton)]
    private Button addButton;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject]
    private Settings settings;

    private void Start()
    {
        settings.GameSettings.ObserveEveryValueChanged(gameSettings => gameSettings.songDirs)
            .Subscribe(onNext => UpdateSongFolderList());

        addButton.RegisterCallbackButtonTriggered(() =>
        {
            settings.GameSettings.songDirs.Add("./Songs");
            UpdateSongFolderList();
        });

        downloadSceneButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.ContentDownloadScene));

        backButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsScene));
        backButton.Focus();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.OptionsScene));
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        downloadSceneButton.text = TranslationManager.GetTranslation(R.Messages.options_openDownloadSceneButton);
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.songLibraryOptionsScene_title);
    }

    private void UpdateSongFolderList()
    {
        songList.Clear();
        int index = 0;
        settings.GameSettings.songDirs.ForEach(songDir =>
        {
            songList.Add(CreateSongFolderEntry(songDir, index));
            index++;
        });
    }

    private VisualElement CreateSongFolderEntry(string songDir, int indexInList)
    {
        VisualElement result = songFolderListEntryAsset.CloneTree();
        TextField textField = result.Q<TextField>(R.UxmlNames.pathTextField);

        // Workaround for escaped characters
        // (https://forum.unity.com/threads/preventing-escaped-characters-in-textfield.1071425/)
        string backslashReplacement = "＼";
        textField.RegisterValueChangedCallback(evt =>
        {
            string newValueEscaped = evt.newValue
                .Replace("\\", backslashReplacement);
            string newValueUnescaped = newValueEscaped.Replace(backslashReplacement, "\\");
            settings.GameSettings.songDirs[indexInList] = newValueUnescaped;
            textField.SetValueWithoutNotify(newValueEscaped);
        });

        textField.value = songDir.Replace("\\", backslashReplacement);
        Button deleteButton = result.Q<Button>(R.UxmlNames.deleteButton);
        deleteButton.text = TranslationManager.GetTranslation(R.Messages.delete);
        deleteButton.RegisterCallbackButtonTriggered(() =>
            {
                settings.GameSettings.songDirs.RemoveAt(indexInList);
                UpdateSongFolderList();
                backButton.Focus();
            });
        return result;
    }
}
