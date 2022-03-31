using System.IO;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_ANDROID
    using UnityEngine.Android;
#endif

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongLibraryOptionsSceneControl : MonoBehaviour, INeedInjection, ITranslator
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
            .Subscribe(onNext => UpdateSongFolderList())
            .AddTo(gameObject);

        addButton.RegisterCallbackButtonTriggered(() =>
        {
            settings.GameSettings.songDirs.Add("./Songs");
            UpdateSongFolderList();

            RequestExternalStoragePermissionIfNeeded();
        });

        downloadSceneButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.ContentDownloadScene));

        backButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsScene));
        backButton.Focus();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.OptionsScene));
    }

    private static void RequestExternalStoragePermissionIfNeeded()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
        }
#else
        // Nothing to do
#endif
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        downloadSceneButton.text = TranslationManager.GetTranslation(R.Messages.options_downloadSongs_button);
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.options_songLibrary_title);
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
        VisualElement warningContainer = result.Q<VisualElement>(R.UxmlNames.warningContainer);
        Label warningLabel = warningContainer.Q<Label>(R.UxmlNames.warningLabel);

        void CheckFolderExists(string path)
        {
            if (Directory.Exists(path))
            {
                warningContainer.HideByDisplay();
            }
            else if (File.Exists(path))
            {
                warningContainer.ShowByDisplay();
                warningLabel.text = "Not a folder.";
            }
            else
            {
                warningContainer.ShowByDisplay();
                warningLabel.text = "File does not exist.";
            }
        }

        TextField textField = result.Q<TextField>(R.UxmlNames.pathTextField);
        textField.value = songDir;
        PathTextFieldControl pathTextFieldControl = new(textField);
        pathTextFieldControl.ValueChangedEventStream
            .Subscribe(newValueUnescaped =>
            {
                settings.GameSettings.songDirs[indexInList] = newValueUnescaped;
                CheckFolderExists(newValueUnescaped);
            });

        Button deleteButton = result.Q<Button>(R.UxmlNames.deleteButton);
        deleteButton.text = TranslationManager.GetTranslation(R.Messages.delete);
        deleteButton.RegisterCallbackButtonTriggered(() =>
            {
                settings.GameSettings.songDirs.RemoveAt(indexInList);
                UpdateSongFolderList();
                backButton.Focus();
            });

        CheckFolderExists(songDir);

        return result;
    }
}
