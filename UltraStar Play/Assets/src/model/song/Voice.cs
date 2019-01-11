using System.Collections.Generic;
using UnityEngine;

public class Voice
{
    // this needs to be switched over to IReadOnlyList
    public List<Sentence> Sentences {get;}
    
    public Voice(List<Sentence> sentences)
    {
        if (sentences == null || sentences.Count < 1)
        {
            throw new UnityException("sentences is null or empty!");
        }
        Sentences = sentences;
    }
}
