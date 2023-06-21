using System;
using System.Collections.Generic;

public class SpeechRecognitionWordResult
{
    /**
     * The recognized word
     */
    public string Text { get; set; }

    /**
     * Start time of the word in the audio.
     */
    public TimeSpan Start { get; set; }

    /**
     * End time of the word in the audio.
     */
    public TimeSpan End { get; set; }
    
    /**
     * Confidence of the result from 0 (probably wrong) to 1 (probably correct).
     */
    public double Conf { get; set; }
    
    public SpeechRecognitionWordResult(string text, TimeSpan start, TimeSpan end, double conf = 1)
    {
        Text = text;
        Start = start;
        End = end;
        Conf = conf;
    }

    public static void NormalizeText(List<SpeechRecognitionWordResult> wordResults)
    {
        void MoveCharacterToEndOfLastWord(char c,
            SpeechRecognitionWordResult lastWordResult,
            SpeechRecognitionWordResult currentWordResult)
        {
            if (currentWordResult != null
                && currentWordResult.Text.StartsWith(c))
            {
                currentWordResult.Text = currentWordResult.Text.TrimStart(c);
                
                if (lastWordResult != null
                    && !lastWordResult.Text.EndsWith(c))
                {
                    lastWordResult.Text += c;
                }
            }
        }
        
        SpeechRecognitionWordResult lastWordResult = null;
        foreach (SpeechRecognitionWordResult currentWordResult in wordResults)
        {
            MoveCharacterToEndOfLastWord(' ', lastWordResult, currentWordResult);
            MoveCharacterToEndOfLastWord('.', lastWordResult, currentWordResult);
            MoveCharacterToEndOfLastWord('!', lastWordResult, currentWordResult);
            MoveCharacterToEndOfLastWord('?', lastWordResult, currentWordResult);
            MoveCharacterToEndOfLastWord(',', lastWordResult, currentWordResult);

            lastWordResult = currentWordResult;
        }
    }
}
