public struct SpeechRecognizerConfig
{
    public string ModelPath { get; private set; }
    public string SpeechRecognitionLanguage { get; private set; }
    public string Prompt { get; private set; }

    public SpeechRecognizerConfig(string modelPath, string speechRecognitionLanguage, string prompt)
    {
        ModelPath = modelPath;
        SpeechRecognitionLanguage = speechRecognitionLanguage;
        Prompt = prompt;
    }
}
