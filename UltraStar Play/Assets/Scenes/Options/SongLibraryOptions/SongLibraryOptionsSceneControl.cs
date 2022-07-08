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

    [Inject(UxmlName = R.UxmlNames.androidSongFolderHintContainer)]
    private VisualElement androidSongFolderHintContainer;

    [Inject(UxmlName = R.UxmlNames.androidSongFolderHintLabel)]
    private Label androidSongFolderHintLabel;

    [Inject(UxmlName = R.UxmlNames.addAndroidSdCardSongFolderButton)]
    private Button addAndroidSdCardSongFolderButton;

    [Inject(UxmlName = R.UxmlNames.addAndroidInternalSongFolderButton)]
    private Button addAndroidInternalSongFolderButton;

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

#if UNITY_ANDROID
        if (AndroidUtils.GetAppSpecificStorageAbsolutePath(false).IsNullOrEmpty()
            && AndroidUtils.GetAppSpecificStorageAbsolutePath(true).IsNullOrEmpty())
        {
            // No storage folders found. Do not show any hint.
            androidSongFolderHintContainer.HideByDisplay();
        }
        else
        {
            androidSongFolderHintContainer.ShowByDisplay();
            addAndroidInternalSongFolderButton.RegisterCallbackButtonTriggered(() =>
                AddSongFolderIfNotContains(AndroidUtils.GetAppSpecificStorageAbsolutePath(false)));
            addAndroidSdCardSongFolderButton.RegisterCallbackButtonTriggered(() =>
                AddSongFolderIfNotContains(AndroidUtils.GetAppSpecificStorageAbsolutePath(true)));
        }
        UpdateAddAndroidSongFoldersButtons();
#else
        androidSongFolderHintContainer.HideByDisplay();
#endif
    }

    private void AddSongFolderIfNotContains(string basePath)
    {
        settings.GameSettings.songDirs.AddIfNotContains(basePath + "/Songs");
        UpdateSongFolderList();
    }

    private void UpdateAddAndroidSongFoldersButtons()
    {
        string sdCardSongFolder = AndroidUtils.GetAppSpecificStorageAbsolutePath(true);
        string internalSongFolder = AndroidUtils.GetAppSpecificStorageAbsolutePath(false);
        bool anySongDirInSdCardSongFolder = settings.GameSettings.songDirs
            .AnyMatch(songDir => songDir.StartsWith(sdCardSongFolder));
        bool anySongDirInInternalSongFolder = settings.GameSettings.songDirs
            .AnyMatch(songDir => songDir.StartsWith(internalSongFolder));
        addAndroidSdCardSongFolderButton.SetVisibleByDisplay(!sdCardSongFolder.IsNullOrEmpty() && !anySongDirInSdCardSongFolder);
        addAndroidInternalSongFolderButton.SetVisibleByDisplay(!internalSongFolder.IsNullOrEmpty() && !anySongDirInInternalSongFolder);
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
        androidSongFolderHintLabel.text = TranslationManager.GetTranslation(R.Messages.options_songLibrary_androidFolderHint,
            // AppSpecificStorageRelativePath is the same for internal memory and sd card.
            "androidAppSpecificStorageRelativePath", AndroidUtils.GetAppSpecificStorageRelativePath(false));
        addAndroidSdCardSongFolderButton.text = TranslationManager.GetTranslation(R.Messages.options_songLibrary_addSdCardSongFolder);
        addAndroidInternalSongFolderButton.text = TranslationManager.GetTranslation(R.Messages.options_songLibrary_addInternalSongFolder);
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
        UpdateAddAndroidSongFoldersButtons();
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
                UpdateAddAndroidSongFoldersButtons();
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
