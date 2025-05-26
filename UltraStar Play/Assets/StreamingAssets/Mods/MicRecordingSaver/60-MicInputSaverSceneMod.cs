using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class MicRecordingSaverSceneMod : ISceneMod
{
    [Inject]
    private MicRecordingSaverModSettings modSettings;

    [Inject]
    private ModObjectContext modObjectContext;

    public void OnSceneEntered(SceneEnteredContext sceneEnteredContext)
    {
        if (sceneEnteredContext.Scene == EScene.SingScene)
        {
            CreateMicRecordingSaverMonoBehaviour(sceneEnteredContext);
        }
        else if (sceneEnteredContext.Scene == EScene.SingingResultsScene)
        {
            AddSaveButton(sceneEnteredContext);
        }
    }

    private void AddSaveButton(SceneEnteredContext sceneEnteredContext)
    {
        if (MicRecordingData.PlayerProfileToMicRecording.IsNullOrEmpty())
        {
            return;
        }

        // Get song
        SingingResultsSceneData sceneData = SceneNavigator.GetSceneData(new SingingResultsSceneData());
        SongMeta songMeta = sceneData.SongMetas.FirstOrDefault();

        // Create button
        Button button = new Button();
        button.text = "Save Mic Recording";
        button.AddToClassList("mx-3");
        button.RegisterCallbackButtonTriggered(_ =>
        {
            MicRecordingFileWriter micRecordingFileWriter = sceneEnteredContext.SceneInjector
                .WithBindingForInstance(modSettings)
                .WithBindingForInstance(modObjectContext)
                .WithBindingForInstance(songMeta)
                .CreateAndInject<MicRecordingFileWriter>();
            micRecordingFileWriter.SaveAll();
        });

        // Add button to UI, next to the restart button
        UIDocumentUtils.FindUIDocumentOrThrow().rootVisualElement
            .Q(R.UxmlNames.restartButton)
            .parent
            .Insert(1, button);
    }

    private void CreateMicRecordingSaverMonoBehaviour(SceneEnteredContext sceneEnteredContext)
    {
        GameObject gameObject = new GameObject();
        gameObject.name = nameof(MicRecorderMonoBehaviour);
        MicRecorderMonoBehaviour behaviour = gameObject.AddComponent<MicRecorderMonoBehaviour>();
        sceneEnteredContext.SceneInjector
            .WithBindingForInstance(modSettings)
            .WithBindingForInstance(modObjectContext)
            .Inject(behaviour);
    }
}