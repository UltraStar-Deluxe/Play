using System;

public class SpeechRecognitionWordResult
{
    /**
     * The recognized word
     */
    public string Text { get;private set; }

    /**
     * Start time of the word in the audio.
     */
    public TimeSpan Start { get;private set; }

    /**
     * End time of the word in the audio.
     */
    public TimeSpan End { get;private set; }
    
    /**
     * Confidence of the result from 0 (probably wrong) to 1 (probably correct).
     */
    public double Conf { get;private set; }
    
    public SpeechRecognitionWordResult(string text, TimeSpan start, TimeSpan end, double conf = 1)
    {
        Text = text;
        Start = start;
        End = end;
        Conf = conf;
    }
}
