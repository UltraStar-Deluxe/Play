public struct SpeechRecognitionParameters
{
    public string ModelPath { get; private set; }
    public string SpeechRecognitionLanguage { get; private set; }
    public string Prompt { get; private set; }

    public SpeechRecognitionParameters(string modelPath, string speechRecognitionLanguage, string prompt)
    {
        ModelPath = modelPath;
        SpeechRecognitionLanguage = speechRecognitionLanguage;
        Prompt = prompt;
    }
}
