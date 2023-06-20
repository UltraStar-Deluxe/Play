using System.Collections.Generic;

public class SpeechRecognitionResult
{
    public string Text { get; private set; } 
    public List<SpeechRecognitionWordResult> Words { get; private set; }
    
    public SpeechRecognitionResult(string text, List<SpeechRecognitionWordResult> words)
    {
        Text = text;
        Words = words;
    }
}
