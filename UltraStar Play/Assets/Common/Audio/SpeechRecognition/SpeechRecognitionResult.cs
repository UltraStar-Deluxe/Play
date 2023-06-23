using System.Collections.Generic;

public class SpeechRecognitionResult
{
    public string Text { get; set; } 
    public List<SpeechRecognitionWordResult> Words { get; set; }
    
    public SpeechRecognitionResult(string text, List<SpeechRecognitionWordResult> words)
    {
        Text = text;
        Words = words;
    }
}
