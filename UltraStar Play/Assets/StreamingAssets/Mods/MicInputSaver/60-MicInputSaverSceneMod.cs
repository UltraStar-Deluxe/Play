using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class MicInputSaverSceneMod : ISceneMod
{
    [Inject]
    private MicInputSaverModSettings modSettings;

    [Inject]
    private ModObjectContext modObjectContext;

    public void OnSceneEntered(SceneEnteredContext sceneEnteredContext)
    {
        if (sceneEnteredContext.Scene == EScene.SingScene)
        {
            CreateMicInputSaverMonoBehaviour(sceneEnteredContext);
        }
        else if (sceneEnteredContext.Scene == EScene.SingingResultsScene)
        {
            AddSaveMicInputButton(sceneEnteredContext);
        }
    }

    private void AddSaveMicInputButton(SceneEnteredContext sceneEnteredContext)
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
        button.text = "Save Mic Input";
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

    private void CreateMicInputSaverMonoBehaviour(SceneEnteredContext sceneEnteredContext)
    {
        GameObject gameObject = new GameObject();
        gameObject.name = nameof(MicInputSaverMonoBehaviour);
        MicInputSaverMonoBehaviour behaviour = gameObject.AddComponent<MicInputSaverMonoBehaviour>();
        sceneEnteredContext.SceneInjector
            .WithBindingForInstance(modSettings)
            .WithBindingForInstance(modObjectContext)
            .Inject(behaviour);
    }
}