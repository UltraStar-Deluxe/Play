using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

public interface ISingSceneNoteDisplayer
{
    void DisplaySentence(Sentence currentSentence, Sentence nextSentence);

    void CreatePerfectSentenceEffect();

    void CreatePerfectNoteEffect(Note perfectNote);

    void SetNoteRowCount(int noteRowCount);
}
