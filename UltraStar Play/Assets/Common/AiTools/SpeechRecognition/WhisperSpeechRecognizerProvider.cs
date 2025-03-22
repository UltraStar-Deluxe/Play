using UniInject;
using UnityEngine;
using Whisper;

public class WhisperSpeechRecognizerProvider : AbstractSingletonBehaviour, INeedInjection
{
    public static WhisperSpeechRecognizerProvider Instance => DontDestroyOnLoadManager.FindComponentOrThrow<WhisperSpeechRecognizerProvider>();

    [InjectedInInspector]
    public WhisperManager whisperManagerPrefab;

    protected override object GetInstance()
    {
        return Instance;
    }

    public SpeechRecognizer CreateSpeechRecognizer(SpeechRecognizerConfig config)
    {
        WhisperManager whisperManager = CreateWhisperManager(
            config.ModelPath,
            config.SpeechRecognitionLanguage,
            config.Prompt);
        SpeechRecognizer speechRecognizer = new WhisperSpeechRecognizer(config, whisperManager);
        return speechRecognizer;
    }

    private WhisperManager CreateWhisperManager(string modelPath, string language, string prompt)
    {
        language = language.ToLowerInvariant();
        Debug.Log($"Creating WhisperManager with model '{modelPath}', modelPath: {modelPath}, prompt: {prompt}");

        WhisperManager whisperManager = GameObject.Instantiate<WhisperManager>(whisperManagerPrefab, transform);
        whisperManager.name = $"WhisperManager language: {language}, modelPath: {modelPath}, prompt: {prompt}";
        whisperManager.IsModelPathInStreamingAssets = false;
        whisperManager.ModelPath = modelPath;
        whisperManager.language = language;
        whisperManager.initialPrompt = prompt;
        whisperManager.enableTokens = true;
        whisperManager.tokensTimestamps = true;
        whisperManager.translateToEnglish = false;
        whisperManager.singleSegment = false;
        return whisperManager;
    }
}
