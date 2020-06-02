using UnityEngine;

public interface ISingSceneNoteDisplayer
{
    void DisplaySentence(Sentence currentSentence, Sentence nextSentence);

    void CreatePerfectSentenceEffect();

    void CreatePerfectNoteEffect(Note perfectNote);

    void Init(int lineCount);

    GameObject GetGameObject();
}
