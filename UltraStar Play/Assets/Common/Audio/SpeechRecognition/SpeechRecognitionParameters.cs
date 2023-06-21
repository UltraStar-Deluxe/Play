public struct SpeechRecognitionParameters
{
    public string ModelPath { get; private set; }
    public string SpeechRecognitionLanguage { get; private set; }

    public SpeechRecognitionParameters(string modelPath, string speechRecognitionLanguage)
    {
        ModelPath = modelPath;
        SpeechRecognitionLanguage = speechRecognitionLanguage;
    }
}
