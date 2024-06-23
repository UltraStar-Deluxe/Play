using System;
using Responsible;
using static Responsible.Responsibly;

public class ResponsibleSceneUtils
{
    public static ITestWaitCondition<object> WaitForScene(EScene scene)
        => WaitForCondition(
            $"wait for scene {scene}",
            () => SceneNavigator.Instance.CurrentScene == scene);

    public static ITestInstruction<object> ExpectScene(EScene scene, float timeoutInSeconds = 10)
        => WaitForScene(scene)
            .ExpectWithinSeconds(timeoutInSeconds)
            .ContinueWith(WaitForFrames(2));

    public static ITestInstruction<T> GetSceneData<T>() where T : SceneData
        => DoAndReturn(
                $"get scene data of type {typeof(T)}",
                () => SceneNavigator.GetSceneDataOrThrow<T>());

    public static ITestInstruction<object> ExpectSceneDataWhere<T>(
        string predicateDescription,
        Predicate<T> predicate) where T : SceneData
        => GetSceneData<T>()
            .ContinueWith(sceneData => WaitForCondition(
                $"expect scene data of type {typeof(T)} where '{predicateDescription}'",
                () => predicate(sceneData))
                .ExpectWithinSeconds(1));
}
