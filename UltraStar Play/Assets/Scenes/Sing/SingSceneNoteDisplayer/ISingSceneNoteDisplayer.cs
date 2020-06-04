using UnityEngine;

public interface ISingSceneNoteDisplayer
{
    void CreatePerfectSentenceEffect();

    void CreatePerfectNoteEffect(Note perfectNote);

    void Init(int lineCount);

    GameObject GetGameObject();
}
