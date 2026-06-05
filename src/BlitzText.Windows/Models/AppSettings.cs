using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace BlitzText.Windows.Models;

public sealed class AppSettings : INotifyPropertyChanged
{
    private AppLanguage appLanguage = AppLanguage.German;
    private DictationLanguage dictationLanguage = DictationLanguage.Auto;
    private TranscriptionProviderKind transcriptionProvider = TranscriptionProviderKind.OpenAI;
    private RewriteProviderKind rewriteProvider = RewriteProviderKind.OpenAI;
    private string openAiApiKey = "";
    private string openAiTranscriptionModel = "whisper-1";
    private string openAiRewriteModel = "gpt-4o-mini";
    private string localWhisperExecutablePath = "";
    private string localWhisperModelPath = "";
    private int localWhisperTimeoutSeconds = 180;
    private string ollamaBaseUrl = "http://localhost:11434";
    private string ollamaRewriteModel = "llama3.1";
    private string customNames = "";
    private string transcriptionPrompt = "";
    private string improvePrompt = DefaultPrompts.Improve;
    private string calmPrompt = DefaultPrompts.Calm;
    private string emojisPrompt = DefaultPrompts.Emojis;
    private string hotkeyId = "ctrl-alt-space";
    private string transcribeHotkeyId = "ctrl-shift-f8";
    private string improveHotkeyId = "ctrl-shift-f9";
    private string calmHotkeyId = "ctrl-shift-f10";
    private string emojisHotkeyId = "ctrl-shift-f12";
    private bool keepOllamaWarm = true;
    private bool saveHistory = true;
    private bool autoPaste;
    private WorkflowKind defaultWorkflow = WorkflowKind.Improve;

    public event PropertyChangedEventHandler? PropertyChanged;

    public AppLanguage AppLanguage
    {
        get => appLanguage;
        set => SetField(ref appLanguage, value);
    }

    public DictationLanguage DictationLanguage
    {
        get => dictationLanguage;
        set => SetField(ref dictationLanguage, value);
    }

    public TranscriptionProviderKind TranscriptionProvider
    {
        get => transcriptionProvider;
        set => SetField(ref transcriptionProvider, value);
    }

    public RewriteProviderKind RewriteProvider
    {
        get => rewriteProvider;
        set => SetField(ref rewriteProvider, value);
    }

    [JsonIgnore]
    public string OpenAiApiKey
    {
        get => openAiApiKey;
        set => SetField(ref openAiApiKey, value);
    }

    public string OpenAiTranscriptionModel
    {
        get => openAiTranscriptionModel;
        set => SetField(ref openAiTranscriptionModel, value);
    }

    public string OpenAiRewriteModel
    {
        get => openAiRewriteModel;
        set => SetField(ref openAiRewriteModel, value);
    }

    public string LocalWhisperExecutablePath
    {
        get => localWhisperExecutablePath;
        set => SetField(ref localWhisperExecutablePath, value);
    }

    public string LocalWhisperModelPath
    {
        get => localWhisperModelPath;
        set => SetField(ref localWhisperModelPath, value);
    }

    public int LocalWhisperTimeoutSeconds
    {
        get => localWhisperTimeoutSeconds;
        set => SetField(ref localWhisperTimeoutSeconds, Math.Clamp(value, 30, 900));
    }

    public string OllamaBaseUrl
    {
        get => ollamaBaseUrl;
        set => SetField(ref ollamaBaseUrl, value.TrimEnd('/'));
    }

    public string OllamaRewriteModel
    {
        get => ollamaRewriteModel;
        set => SetField(ref ollamaRewriteModel, value);
    }

    public string CustomNames
    {
        get => customNames;
        set => SetField(ref customNames, value);
    }

    public string TranscriptionPrompt
    {
        get => transcriptionPrompt;
        set => SetField(ref transcriptionPrompt, value);
    }

    public string ImprovePrompt
    {
        get => improvePrompt;
        set => SetField(ref improvePrompt, value);
    }

    public string CalmPrompt
    {
        get => calmPrompt;
        set => SetField(ref calmPrompt, value);
    }

    public string EmojisPrompt
    {
        get => emojisPrompt;
        set => SetField(ref emojisPrompt, value);
    }

    public bool AutoPaste
    {
        get => autoPaste;
        set => SetField(ref autoPaste, value);
    }

    public bool KeepOllamaWarm
    {
        get => keepOllamaWarm;
        set => SetField(ref keepOllamaWarm, value);
    }

    public bool SaveHistory
    {
        get => saveHistory;
        set => SetField(ref saveHistory, value);
    }

    public string HotkeyId
    {
        get => hotkeyId;
        set => SetField(ref hotkeyId, value);
    }

    public string TranscribeHotkeyId
    {
        get => transcribeHotkeyId;
        set => SetField(ref transcribeHotkeyId, value);
    }

    public string ImproveHotkeyId
    {
        get => improveHotkeyId;
        set => SetField(ref improveHotkeyId, value);
    }

    public string CalmHotkeyId
    {
        get => calmHotkeyId;
        set => SetField(ref calmHotkeyId, value);
    }

    public string EmojisHotkeyId
    {
        get => emojisHotkeyId;
        set => SetField(ref emojisHotkeyId, value);
    }

    public WorkflowKind DefaultWorkflow
    {
        get => defaultWorkflow;
        set => SetField(ref defaultWorkflow, value);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
