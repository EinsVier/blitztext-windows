using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using BlitzText.Windows.Models;
using BlitzText.Windows.Services;

namespace BlitzText.Windows;

public partial class MainWindow : Window
{
    private readonly SettingsStore settingsStore = new();
    private readonly HistoryStore historyStore = new();
    private readonly CredentialStore credentialStore = new();
    private readonly OllamaConnectionTester ollamaConnectionTester = new();
    private readonly UpdateChecker updateChecker = new(new HttpClient());
    private readonly AppSettings settings;
    private readonly AudioRecorderService audioRecorder = new();
    private readonly BlitzWorkflowRunner workflowRunner;
    private readonly GlobalHotkeyService hotkeyService = new();
    private readonly RecordingIndicatorWindow recordingIndicator = new();
    private readonly TargetWindowService targetWindowService;
    private readonly DispatcherTimer autoSaveTimer;
    private readonly ObservableCollection<HistoryEntry> historyEntries = [];
    private readonly List<HistoryEntry> allHistoryEntries = [];
    private bool isLoading;
    private bool isHotkeyReady;
    private bool isProcessing;
    private bool isRefreshingWhisperModels;
    private string latestUpdateUrl = "";
    private string selectedPromptPresetId = "general";
    private WorkflowKind activeWorkflow;
    private TargetWindow activeTargetWindow = new(IntPtr.Zero, "");
    private CancellationTokenSource? workflowCancellation;

    public AppLanguage CurrentAppLanguage => settings.AppLanguage;

    public MainWindow()
    {
        settings = settingsStore.Load();
        LoadProviderApiKeys();
        workflowRunner = new BlitzWorkflowRunner(new ProviderFactory(settings), settings);
        targetWindowService = new TargetWindowService(() => new WindowInteropHelper(this).Handle);
        autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(700)
        };
        autoSaveTimer.Tick += (_, _) => AutoSaveSettings();

        InitializeComponent();
        HistoryList.ItemsSource = historyEntries;
        LoadHistoryEntries();
        LoadUiFromSettings();
    }

    protected override void OnClosed(EventArgs e)
    {
        audioRecorder.Dispose();
        hotkeyService.Dispose();
        autoSaveTimer.Stop();
        recordingIndicator.Close();
        workflowCancellation?.Dispose();
        base.OnClosed(e);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            hotkeyService.RegisterWorkflowHotkeys(handle, GetWorkflowHotkeyOptions());
            isHotkeyReady = true;
            hotkeyService.WorkflowPressed += async (_, args) => await Dispatcher.InvokeAsync(() => ToggleRecordingAsync(args.Workflow));
            _ = WarmOllamaIfEnabledAsync(showStatus: false);
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }

        base.OnStateChanged(e);
    }

    private void LoadUiFromSettings()
    {
        isLoading = true;
        WorkflowCombo.ItemsSource = WorkflowDisplay.GetOptions(settings.AppLanguage);
        PromptPresetCombo.ItemsSource = PromptPresetCatalog.GetOptions(settings.AppLanguage);
        AppLanguageCombo.ItemsSource = LanguageDisplay.AppLanguageOptions(settings.AppLanguage);
        DictationLanguageCombo.ItemsSource = LanguageDisplay.DictationLanguageOptions(settings.AppLanguage);
        TranscriptionProviderCombo.ItemsSource = Enum.GetValues<TranscriptionProviderKind>();
        RewriteProviderCombo.ItemsSource = Enum.GetValues<RewriteProviderKind>();
        TranscribeHotkeyCombo.ItemsSource = HotkeyOptions.KeyboardOnly;
        ImproveHotkeyCombo.ItemsSource = HotkeyOptions.KeyboardOnly;
        CalmHotkeyCombo.ItemsSource = HotkeyOptions.KeyboardOnly;
        EmojisHotkeyCombo.ItemsSource = HotkeyOptions.KeyboardOnly;

        WorkflowCombo.SelectedItem = WorkflowDisplay.FindOption(settings.DefaultWorkflow, settings.AppLanguage);
        SelectPromptPreset(selectedPromptPresetId);
        AppLanguageCombo.SelectedItem = LanguageDisplay.FindAppLanguage(settings.AppLanguage, settings.AppLanguage);
        DictationLanguageCombo.SelectedItem = LanguageDisplay.FindDictationLanguage(settings.DictationLanguage, settings.AppLanguage);
        TranscriptionProviderCombo.SelectedItem = settings.TranscriptionProvider;
        RewriteProviderCombo.SelectedItem = settings.RewriteProvider;
        TranscribeHotkeyCombo.SelectedItem = HotkeyOptions.FindById(settings.TranscribeHotkeyId);
        ImproveHotkeyCombo.SelectedItem = HotkeyOptions.FindById(settings.ImproveHotkeyId);
        CalmHotkeyCombo.SelectedItem = HotkeyOptions.FindById(settings.CalmHotkeyId);
        EmojisHotkeyCombo.SelectedItem = HotkeyOptions.FindById(settings.EmojisHotkeyId);
        OpenAiApiKeyBox.Password = settings.OpenAiApiKey;
        OpenAiTranscriptionModelBox.Text = settings.OpenAiTranscriptionModel;
        OpenAiRewriteModelBox.Text = settings.OpenAiRewriteModel;
        OpenRouterApiKeyBox.Password = settings.OpenRouterApiKey;
        OpenRouterRewriteModelBox.Text = settings.OpenRouterRewriteModel;
        AnthropicApiKeyBox.Password = settings.AnthropicApiKey;
        AnthropicRewriteModelBox.Text = settings.AnthropicRewriteModel;
        LocalWhisperExecutablePathBox.Text = settings.LocalWhisperExecutablePath;
        LocalWhisperModelPathBox.Text = settings.LocalWhisperModelPath;
        LocalWhisperTimeoutBox.Text = settings.LocalWhisperTimeoutSeconds.ToString();
        OllamaBaseUrlBox.Text = settings.OllamaBaseUrl;
        OllamaRewriteModelBox.Text = settings.OllamaRewriteModel;
        CustomNamesBox.Text = settings.CustomNames;
        TranscriptionPromptBox.Text = settings.TranscriptionPrompt;
        ImprovePromptBox.Text = settings.ImprovePrompt;
        CalmPromptBox.Text = settings.CalmPrompt;
        EmojisPromptBox.Text = settings.EmojisPrompt;
        AutoPasteCheckBox.IsChecked = settings.AutoPaste;
        SaveHistoryCheckBox.IsChecked = settings.SaveHistory;
        KeepOllamaWarmCheckBox.IsChecked = settings.KeepOllamaWarm;
        RefreshWhisperModelOptions();
        isLoading = false;
        ApplyLocalization();
        UpdateRecordButton();
    }

    private async void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        await ToggleRecordingAsync();
    }

    public Task ToggleWorkflowAsync(WorkflowKind workflow)
    {
        return ToggleRecordingAsync(workflow);
    }

    private async Task ToggleRecordingAsync(WorkflowKind? workflowOverride = null)
    {
        try
        {
            if (isProcessing)
            {
                workflowCancellation?.Cancel();
                StatusText.Text = "Breche Verarbeitung ab...";
                return;
            }

            if (!audioRecorder.IsRecording)
            {
                activeWorkflow = workflowOverride ?? settings.DefaultWorkflow;
                activeTargetWindow = targetWindowService.CaptureActiveWindow();
                WorkflowCombo.SelectedItem = WorkflowDisplay.FindOption(activeWorkflow, settings.AppLanguage);
                await audioRecorder.StartAsync();
                UpdateRecordButton();
                recordingIndicator.Start();
                StatusText.Text = settings.AppLanguage == AppLanguage.English
                    ? $"Recording: {WorkflowDisplay.GetLabel(activeWorkflow, settings.AppLanguage)}"
                    : $"Aufnahme laeuft: {WorkflowDisplay.GetLabel(activeWorkflow, settings.AppLanguage)}";
                return;
            }

            isProcessing = true;
            RecordButton.IsEnabled = true;
            RecordButton.Content = Localizer.T(settings.AppLanguage, "Cancel");
            StatusText.Text = settings.AppLanguage == AppLanguage.English
                ? "Transcribing and processing..."
                : "Transkribiere und verarbeite...";

            var wavPath = await audioRecorder.StopAsync();
            recordingIndicator.Stop();
            workflowCancellation?.Cancel();
            workflowCancellation = new CancellationTokenSource();

            var result = await workflowRunner.RunAsync(
                wavPath,
                activeWorkflow,
                status => Dispatcher.Invoke(() => StatusText.Text = status),
                workflowCancellation.Token);
            ResultBox.Text = result;
            if (settings.SaveHistory)
            {
                AddHistoryEntry(result, activeWorkflow);
            }
            ClipboardPasteService.Copy(result);

            if (settings.AutoPaste)
            {
                if (targetWindowService.Activate(activeTargetWindow))
                {
                    await ClipboardPasteService.PasteAsync();
                    StatusText.Text = string.IsNullOrWhiteSpace(activeTargetWindow.Title)
                        ? (settings.AppLanguage == AppLanguage.English ? "Done. Result was pasted." : "Fertig. Ergebnis wurde eingefuegt.")
                        : (settings.AppLanguage == AppLanguage.English ? $"Done. Result was pasted into: {activeTargetWindow.Title}" : $"Fertig. Ergebnis wurde eingefuegt in: {activeTargetWindow.Title}");
                    return;
                }

                StatusText.Text = settings.AppLanguage == AppLanguage.English
                    ? "Done. Target window not found; result is in the clipboard."
                    : "Fertig. Ziel-Fenster nicht gefunden; Ergebnis ist in der Zwischenablage.";
                return;
            }

            StatusText.Text = settings.AppLanguage == AppLanguage.English
                ? "Done. Result was copied to the clipboard."
                : "Fertig. Ergebnis wurde in die Zwischenablage kopiert.";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = settings.AppLanguage == AppLanguage.English ? "Processing canceled." : "Verarbeitung abgebrochen.";
        }
        catch (Exception ex)
        {
            if (!audioRecorder.IsRecording)
            {
                recordingIndicator.Stop();
            }

            StatusText.Text = ex.Message;
        }
        finally
        {
            isProcessing = false;
            UpdateRecordButton();
        }
    }

    private void UpdateRecordButton()
    {
        var hotkeyLabel = GetDefaultWorkflowHotkeyLabel();
        RecordButton.IsEnabled = true;
        RecordButton.Content = isProcessing
            ? Localizer.T(settings.AppLanguage, "Cancel")
            : audioRecorder.IsRecording
                ? Localizer.T(settings.AppLanguage, "StopRecording")
                : $"{Localizer.T(settings.AppLanguage, "StartRecording")} ({hotkeyLabel})";
        RecordButton.ToolTip = $"{Localizer.T(settings.AppLanguage, "RecordButtonTip")}: {hotkeyLabel}";
    }

    private string GetDefaultWorkflowHotkeyLabel()
    {
        var hotkeyId = settings.DefaultWorkflow switch
        {
            WorkflowKind.Transcribe => settings.TranscribeHotkeyId,
            WorkflowKind.Improve => settings.ImproveHotkeyId,
            WorkflowKind.Calm => settings.CalmHotkeyId,
            WorkflowKind.Emojis => settings.EmojisHotkeyId,
            _ => settings.ImproveHotkeyId
        };

        return HotkeyOptions.FindById(hotkeyId).DisplayName;
    }

    private void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSettingsFromUi();

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = "blitztext-settings.json",
            Filter = "JSON-Datei (*.json)|*.json|Alle Dateien (*.*)|*.*",
            Title = "BlitzText-Einstellungen exportieren"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        settingsStore.Export(settings, dialog.FileName);
        StatusText.Text = "Einstellungen exportiert.";
        SaveStatusText.Text = $"Exportiert: {DateTime.Now:HH:mm:ss}";
    }

    private void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON-Datei (*.json)|*.json|Alle Dateien (*.*)|*.*",
            Title = "BlitzText-Einstellungen importieren"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var importedSettings = settingsStore.Import(dialog.FileName);
        ApplyImportedSettings(importedSettings);
        LoadUiFromSettings();
        SaveSettingsFromUi();
        RegisterWorkflowHotkeys();
        StatusText.Text = "Einstellungen importiert.";
        SaveStatusText.Text = $"Importiert: {DateTime.Now:HH:mm:ss}";
    }

    private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdatesButton.IsEnabled = false;
        OpenUpdateButton.IsEnabled = false;
        latestUpdateUrl = "";
        UpdateStatusText.Text = Localizer.T(settings.AppLanguage, "CheckingUpdates");

        try
        {
            var result = await updateChecker.CheckAsync(
                settings.UpdateManifestUrl,
                GetAppVersion(),
                CancellationToken.None);

            if (!result.IsUpdateAvailable)
            {
                UpdateStatusText.Text = string.Format(
                    Localizer.T(settings.AppLanguage, "AppIsCurrent"),
                    result.CurrentVersion);
                return;
            }

            latestUpdateUrl = result.DownloadUrl;
            OpenUpdateButton.IsEnabled = true;
            UpdateStatusText.Text = string.Format(
                Localizer.T(settings.AppLanguage, "UpdateAvailable"),
                result.LatestVersion);
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = string.Format(
                Localizer.T(settings.AppLanguage, "UpdateCheckFailed"),
                ex.Message);
        }
        finally
        {
            CheckUpdatesButton.IsEnabled = true;
        }
    }

    private void OpenUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(latestUpdateUrl))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(latestUpdateUrl)
        {
            UseShellExecute = true
        });
    }

    private void ResetPromptsButton_Click(object sender, RoutedEventArgs e)
    {
        ImprovePromptBox.Text = DefaultPrompts.GetImprove(settings.AppLanguage);
        CalmPromptBox.Text = DefaultPrompts.GetCalm(settings.AppLanguage);
        EmojisPromptBox.Text = DefaultPrompts.GetEmojis(settings.AppLanguage);
        SaveSettingsFromUi(saveToDisk: false);
        ScheduleAutoSave();
        StatusText.Text = settings.AppLanguage == AppLanguage.English
            ? "Workflow prompts were reset."
            : "Workflow-Prompts wurden zurueckgesetzt.";
    }

    private void ApplyPromptPresetButton_Click(object sender, RoutedEventArgs e)
    {
        if (PromptPresetCombo.SelectedItem is not DisplayOption<PromptPreset> selectedPreset)
        {
            return;
        }

        selectedPromptPresetId = selectedPreset.Value.Id;
        ImprovePromptBox.Text = selectedPreset.Value.GetPrompt(settings.AppLanguage);
        SaveSettingsFromUi(saveToDisk: false);
        ScheduleAutoSave();
        StatusText.Text = settings.AppLanguage == AppLanguage.English
            ? $"Prompt preset applied: {selectedPreset.Label}"
            : $"Prompt-Vorlage angewendet: {selectedPreset.Label}";
    }

    private void HistoryList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (HistoryList.SelectedItem is HistoryEntry entry)
        {
            ResultBox.Text = entry.Text;
        }
    }

    private void CopyResultButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ResultBox.Text))
        {
            StatusText.Text = settings.AppLanguage == AppLanguage.English ? "No result to copy." : "Kein Ergebnis zum Kopieren.";
            return;
        }

        ClipboardPasteService.Copy(ResultBox.Text);
        StatusText.Text = settings.AppLanguage == AppLanguage.English
            ? "Result was copied to the clipboard."
            : "Ergebnis wurde in die Zwischenablage kopiert.";
    }

    private void DeleteHistoryEntryButton_Click(object sender, RoutedEventArgs e)
    {
        if (HistoryList.SelectedItem is not HistoryEntry entry)
        {
            StatusText.Text = settings.AppLanguage == AppLanguage.English ? "No history entry selected." : "Kein Verlaufseintrag ausgewaehlt.";
            return;
        }

        allHistoryEntries.Remove(entry);
        historyStore.Save(allHistoryEntries);
        ApplyHistoryFilter();
        ResultBox.Clear();
        StatusText.Text = settings.AppLanguage == AppLanguage.English ? "History entry deleted." : "Verlaufseintrag geloescht.";
    }

    private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        allHistoryEntries.Clear();
        historyEntries.Clear();
        historyStore.Save(allHistoryEntries);
        ResultBox.Clear();
        StatusText.Text = settings.AppLanguage == AppLanguage.English ? "History cleared." : "Verlauf geleert.";
    }

    private void HistorySearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!isLoading)
        {
            ApplyHistoryFilter();
        }
    }

    private void OpenAiApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        ProviderApiKeyBox_PasswordChanged(sender, e);
    }

    private void ProviderApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (!isLoading)
        {
            settings.OpenAiApiKey = OpenAiApiKeyBox.Password;
            settings.OpenRouterApiKey = OpenRouterApiKeyBox.Password;
            settings.AnthropicApiKey = AnthropicApiKeyBox.Password;
            ScheduleAutoSave();
        }
    }

    private async void TestOllamaButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSettingsFromUi(saveToDisk: false);

        try
        {
            TestOllamaButton.IsEnabled = false;
            StatusText.Text = "Teste Ollama...";
            var result = await ollamaConnectionTester.TestAsync(
                settings.OllamaBaseUrl,
                settings.OllamaRewriteModel,
                CancellationToken.None);
            StatusText.Text = result;

            if (settings.KeepOllamaWarm && result.StartsWith("Ollama OK.", StringComparison.Ordinal))
            {
                StatusText.Text = "Ollama OK. Halte Modell warm...";
                StatusText.Text = await ollamaConnectionTester.WarmAsync(
                    settings.OllamaBaseUrl,
                    settings.OllamaRewriteModel,
                    CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ollama-Test fehlgeschlagen: {ex.Message}";
        }
        finally
        {
            TestOllamaButton.IsEnabled = true;
        }
    }

    private void BrowseWhisperExeButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*",
            Title = settings.AppLanguage == AppLanguage.English
                ? "Select whisper.cpp executable"
                : "whisper.cpp EXE auswaehlen"
        };

        if (!string.IsNullOrWhiteSpace(LocalWhisperExecutablePathBox.Text))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(LocalWhisperExecutablePathBox.Text);
        }

        if (dialog.ShowDialog(this) == true)
        {
            LocalWhisperExecutablePathBox.Text = dialog.FileName;
            SaveSettingsFromUi(saveToDisk: false);
            ScheduleAutoSave();
        }
    }

    private void BrowseWhisperModelButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Whisper model (*.bin)|*.bin|All files (*.*)|*.*",
            Title = settings.AppLanguage == AppLanguage.English
                ? "Select Whisper model"
                : "Whisper-Modell auswaehlen"
        };

        if (!string.IsNullOrWhiteSpace(LocalWhisperModelPathBox.Text))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(LocalWhisperModelPathBox.Text);
        }

        if (dialog.ShowDialog(this) == true)
        {
            LocalWhisperModelPathBox.Text = dialog.FileName;
            RefreshWhisperModelOptions();
            SaveSettingsFromUi(saveToDisk: false);
            ScheduleAutoSave();
        }
    }

    private async void TestWhisperButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSettingsFromUi(saveToDisk: false);

        if (!File.Exists(settings.LocalWhisperExecutablePath))
        {
            SetStatus(settings.AppLanguage == AppLanguage.English ? "Whisper executable not found." : "Whisper-EXE nicht gefunden.", true);
            return;
        }

        if (!File.Exists(settings.LocalWhisperModelPath))
        {
            SetStatus(settings.AppLanguage == AppLanguage.English ? "Whisper model not found." : "Whisper-Modell nicht gefunden.", true);
            return;
        }

        try
        {
            TestWhisperButton.IsEnabled = false;
            SetStatus(settings.AppLanguage == AppLanguage.English ? "Testing Whisper..." : "Teste Whisper...");
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = settings.LocalWhisperExecutablePath,
                    Arguments = "--help",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();
            SetStatus(settings.AppLanguage == AppLanguage.English
                ? $"Whisper OK. Model: {Path.GetFileName(settings.LocalWhisperModelPath)}"
                : $"Whisper OK. Modell: {Path.GetFileName(settings.LocalWhisperModelPath)}");
        }
        catch (Exception ex)
        {
            SetStatus($"Whisper-Test fehlgeschlagen: {ex.Message}", true);
        }
        finally
        {
            TestWhisperButton.IsEnabled = true;
        }
    }

    private void LocalWhisperModelCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (isLoading || isRefreshingWhisperModels || LocalWhisperModelCombo.SelectedItem is not DisplayOption<string> model)
        {
            return;
        }

        LocalWhisperModelPathBox.Text = model.Value;
        SaveSettingsFromUi(saveToDisk: false);
        ScheduleAutoSave();
    }

    private void Settings_Changed(object sender, RoutedEventArgs e)
    {
        if (!isLoading)
        {
            SaveSettingsFromUi(saveToDisk: false);
            RefreshLocalizedOptionSources();
            ApplyLocalization();
            UpdateRecordButton();
            ScheduleAutoSave();
        }
    }

    private void WorkflowHotkey_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (isLoading)
        {
            return;
        }

        SaveSettingsFromUi(saveToDisk: false);
        RegisterWorkflowHotkeys();
        UpdateRecordButton();
        ScheduleAutoSave();
    }

    private void DetectTranscribeHotkey_Click(object sender, RoutedEventArgs e)
    {
        DetectHotkeyFor(TranscribeHotkeyCombo);
    }

    private void DetectImproveHotkey_Click(object sender, RoutedEventArgs e)
    {
        DetectHotkeyFor(ImproveHotkeyCombo);
    }

    private void DetectCalmHotkey_Click(object sender, RoutedEventArgs e)
    {
        DetectHotkeyFor(CalmHotkeyCombo);
    }

    private void DetectEmojisHotkey_Click(object sender, RoutedEventArgs e)
    {
        DetectHotkeyFor(EmojisHotkeyCombo);
    }

    private void RestoreDefaultHotkeysButton_Click(object sender, RoutedEventArgs e)
    {
        TranscribeHotkeyCombo.SelectedItem = HotkeyOptions.FindById("browser-home");
        ImproveHotkeyCombo.SelectedItem = HotkeyOptions.FindById("shift-browser-home");
        CalmHotkeyCombo.SelectedItem = HotkeyOptions.FindById("ctrl-browser-home");
        EmojisHotkeyCombo.SelectedItem = HotkeyOptions.FindById("alt-browser-home");
        SaveSettingsFromUi(saveToDisk: false);
        RegisterWorkflowHotkeys();
        UpdateRecordButton();
        ScheduleAutoSave();
        StatusText.Text = settings.AppLanguage == AppLanguage.English
            ? "Default hotkeys restored."
            : "Standard-Hotkeys wiederhergestellt.";
    }

    private void DetectHotkeyFor(System.Windows.Controls.ComboBox comboBox)
    {
        hotkeyService.PauseKeyboardHotkeys();

        try
        {
            var dialog = new HotkeyCaptureWindow
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true && dialog.CapturedHotkey is not null)
            {
                comboBox.SelectedItem = dialog.CapturedHotkey;
                SaveSettingsFromUi(saveToDisk: false);
                RegisterWorkflowHotkeys();
                UpdateRecordButton();
                ScheduleAutoSave();
                StatusText.Text = $"Erkannt: {dialog.CapturedHotkey.DisplayName}";
                return;
            }
        }
        finally
        {
            RegisterWorkflowHotkeys();
        }
    }

    private void SaveSettingsFromUi(bool saveToDisk = true)
    {
        settings.DefaultWorkflow = WorkflowCombo.SelectedItem is DisplayOption<WorkflowKind> workflow
            ? workflow.Value
            : settings.DefaultWorkflow;
        settings.AppLanguage = AppLanguageCombo.SelectedItem is DisplayOption<AppLanguage> appLanguage
            ? appLanguage.Value
            : settings.AppLanguage;
        settings.DictationLanguage = DictationLanguageCombo.SelectedItem is DisplayOption<DictationLanguage> dictationLanguage
            ? dictationLanguage.Value
            : settings.DictationLanguage;
        settings.TranscriptionProvider = TranscriptionProviderCombo.SelectedItem is TranscriptionProviderKind transcriptionProvider
            ? transcriptionProvider
            : settings.TranscriptionProvider;
        settings.RewriteProvider = RewriteProviderCombo.SelectedItem is RewriteProviderKind rewriteProvider
            ? rewriteProvider
            : settings.RewriteProvider;
        settings.TranscribeHotkeyId = TranscribeHotkeyCombo.SelectedItem is HotkeyOption transcribeHotkey
            ? transcribeHotkey.Id
            : settings.TranscribeHotkeyId;
        settings.ImproveHotkeyId = ImproveHotkeyCombo.SelectedItem is HotkeyOption improveHotkey
            ? improveHotkey.Id
            : settings.ImproveHotkeyId;
        settings.CalmHotkeyId = CalmHotkeyCombo.SelectedItem is HotkeyOption calmHotkey
            ? calmHotkey.Id
            : settings.CalmHotkeyId;
        settings.EmojisHotkeyId = EmojisHotkeyCombo.SelectedItem is HotkeyOption emojisHotkey
            ? emojisHotkey.Id
            : settings.EmojisHotkeyId;
        settings.OpenAiApiKey = OpenAiApiKeyBox.Password;
        settings.OpenAiTranscriptionModel = OpenAiTranscriptionModelBox.Text.Trim();
        settings.OpenAiRewriteModel = OpenAiRewriteModelBox.Text.Trim();
        settings.OpenRouterApiKey = OpenRouterApiKeyBox.Password;
        settings.OpenRouterRewriteModel = OpenRouterRewriteModelBox.Text.Trim();
        settings.AnthropicApiKey = AnthropicApiKeyBox.Password;
        settings.AnthropicRewriteModel = AnthropicRewriteModelBox.Text.Trim();
        settings.LocalWhisperExecutablePath = LocalWhisperExecutablePathBox.Text.Trim();
        settings.LocalWhisperModelPath = LocalWhisperModelPathBox.Text.Trim();
        settings.LocalWhisperTimeoutSeconds = int.TryParse(LocalWhisperTimeoutBox.Text.Trim(), out var timeoutSeconds)
            ? timeoutSeconds
            : settings.LocalWhisperTimeoutSeconds;
        settings.OllamaBaseUrl = OllamaBaseUrlBox.Text.Trim();
        settings.OllamaRewriteModel = OllamaRewriteModelBox.Text.Trim();
        settings.CustomNames = CustomNamesBox.Text.Trim();
        settings.TranscriptionPrompt = TranscriptionPromptBox.Text.Trim();
        settings.ImprovePrompt = ImprovePromptBox.Text.Trim();
        settings.CalmPrompt = CalmPromptBox.Text.Trim();
        settings.EmojisPrompt = EmojisPromptBox.Text.Trim();
        settings.AutoPaste = AutoPasteCheckBox.IsChecked == true;
        settings.SaveHistory = SaveHistoryCheckBox.IsChecked == true;
        settings.KeepOllamaWarm = KeepOllamaWarmCheckBox.IsChecked == true;

        if (saveToDisk)
        {
            credentialStore.SaveOpenAiApiKey(settings.OpenAiApiKey);
            credentialStore.SaveOpenRouterApiKey(settings.OpenRouterApiKey);
            credentialStore.SaveAnthropicApiKey(settings.AnthropicApiKey);
            settingsStore.Save(settings);
        }
    }

    private void ApplyImportedSettings(AppSettings importedSettings)
    {
        var existingApiKey = settings.OpenAiApiKey;

        settings.DefaultWorkflow = importedSettings.DefaultWorkflow;
        settings.AppLanguage = importedSettings.AppLanguage;
        settings.DictationLanguage = importedSettings.DictationLanguage;
        settings.TranscriptionProvider = importedSettings.TranscriptionProvider;
        settings.RewriteProvider = importedSettings.RewriteProvider;
        settings.TranscribeHotkeyId = importedSettings.TranscribeHotkeyId;
        settings.ImproveHotkeyId = importedSettings.ImproveHotkeyId;
        settings.CalmHotkeyId = importedSettings.CalmHotkeyId;
        settings.EmojisHotkeyId = importedSettings.EmojisHotkeyId;
        settings.OpenAiTranscriptionModel = importedSettings.OpenAiTranscriptionModel;
        settings.OpenAiRewriteModel = importedSettings.OpenAiRewriteModel;
        settings.OpenRouterRewriteModel = importedSettings.OpenRouterRewriteModel;
        settings.AnthropicRewriteModel = importedSettings.AnthropicRewriteModel;
        settings.LocalWhisperExecutablePath = importedSettings.LocalWhisperExecutablePath;
        settings.LocalWhisperModelPath = importedSettings.LocalWhisperModelPath;
        settings.LocalWhisperTimeoutSeconds = importedSettings.LocalWhisperTimeoutSeconds;
        settings.OllamaBaseUrl = importedSettings.OllamaBaseUrl;
        settings.OllamaRewriteModel = importedSettings.OllamaRewriteModel;
        settings.CustomNames = importedSettings.CustomNames;
        settings.TranscriptionPrompt = importedSettings.TranscriptionPrompt;
        settings.ImprovePrompt = importedSettings.ImprovePrompt;
        settings.CalmPrompt = importedSettings.CalmPrompt;
        settings.EmojisPrompt = importedSettings.EmojisPrompt;
        settings.AutoPaste = importedSettings.AutoPaste;
        settings.SaveHistory = importedSettings.SaveHistory;
        settings.KeepOllamaWarm = importedSettings.KeepOllamaWarm;
        settings.UpdateManifestUrl = importedSettings.UpdateManifestUrl;
        settings.OpenAiApiKey = existingApiKey;
    }

    private void LoadHistoryEntries()
    {
        historyEntries.Clear();
        allHistoryEntries.Clear();

        foreach (var entry in historyStore.Load())
        {
            allHistoryEntries.Add(entry);
        }

        ApplyHistoryFilter();
    }

    private void AddHistoryEntry(string text, WorkflowKind workflow)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var entry = new HistoryEntry
        {
            CreatedAt = DateTimeOffset.Now,
            Workflow = workflow,
            Text = text
        };
        allHistoryEntries.Insert(0, entry);

        while (allHistoryEntries.Count > HistoryStore.MaxEntries)
        {
            allHistoryEntries.RemoveAt(allHistoryEntries.Count - 1);
        }

        historyStore.Save(allHistoryEntries);
        ApplyHistoryFilter();
        HistoryList.SelectedIndex = 0;
    }

    private void ApplyHistoryFilter()
    {
        var query = HistorySearchBox.Text.Trim();
        var filteredEntries = string.IsNullOrWhiteSpace(query)
            ? allHistoryEntries
            : allHistoryEntries
                .Where(entry =>
                    entry.Text.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    entry.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    entry.Preview.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

        historyEntries.Clear();
        foreach (var entry in filteredEntries)
        {
            historyEntries.Add(entry);
        }
    }

    private void RefreshLocalizedOptionSources()
    {
        var selectedWorkflow = settings.DefaultWorkflow;
        var selectedAppLanguage = settings.AppLanguage;
        var selectedDictationLanguage = settings.DictationLanguage;
        var selectedPresetId = GetSelectedPromptPresetId();

        isLoading = true;
        WorkflowCombo.ItemsSource = WorkflowDisplay.GetOptions(settings.AppLanguage);
        PromptPresetCombo.ItemsSource = PromptPresetCatalog.GetOptions(settings.AppLanguage);
        AppLanguageCombo.ItemsSource = LanguageDisplay.AppLanguageOptions(settings.AppLanguage);
        DictationLanguageCombo.ItemsSource = LanguageDisplay.DictationLanguageOptions(settings.AppLanguage);
        WorkflowCombo.SelectedItem = WorkflowDisplay.FindOption(selectedWorkflow, settings.AppLanguage);
        SelectPromptPreset(selectedPresetId);
        AppLanguageCombo.SelectedItem = LanguageDisplay.FindAppLanguage(selectedAppLanguage, settings.AppLanguage);
        DictationLanguageCombo.SelectedItem = LanguageDisplay.FindDictationLanguage(selectedDictationLanguage, settings.AppLanguage);
        isLoading = false;
    }

    private string GetSelectedPromptPresetId()
    {
        if (PromptPresetCombo.SelectedItem is DisplayOption<PromptPreset> selectedPreset)
        {
            selectedPromptPresetId = selectedPreset.Value.Id;
        }

        return selectedPromptPresetId;
    }

    private void SelectPromptPreset(string presetId)
    {
        if (PromptPresetCombo.ItemsSource is not IEnumerable<DisplayOption<PromptPreset>> options)
        {
            return;
        }

        var selectedOption = options.FirstOrDefault(option => option.Value.Id == presetId) ?? options.FirstOrDefault();
        if (selectedOption is not null)
        {
            selectedPromptPresetId = selectedOption.Value.Id;
            PromptPresetCombo.SelectedItem = selectedOption;
        }
    }

    private void ApplyLocalization()
    {
        var language = settings.AppLanguage;

        SubtitleText.Text = Localizer.T(language, "Subtitle");
        ControlsTitleText.Text = Localizer.T(language, "Controls");
        DefaultModeText.Text = Localizer.T(language, "DefaultMode");
        WorkflowLabelText.Text = Localizer.T(language, "Workflow");
        TranscriptionLabelText.Text = Localizer.T(language, "Transcription");
        RewriteLabelText.Text = Localizer.T(language, "Rewrite");
        AppLanguageLabelText.Text = Localizer.T(language, "AppLanguage");
        DictationLanguageLabelText.Text = Localizer.T(language, "DictationLanguage");
        AutoPasteCheckBox.Content = Localizer.T(language, "AutoPaste");
        SaveHistoryCheckBox.Content = Localizer.T(language, "SaveHistory");
        KeepOllamaWarmCheckBox.Content = Localizer.T(language, "KeepOllamaWarm");
        StatusLabelText.Text = Localizer.T(language, "Status");
        SaveStatusText.Text = Localizer.T(language, "AutoSaveHint");

        ProviderTab.Header = Localizer.T(language, "Provider");
        HotkeysTab.Header = Localizer.T(language, "Hotkeys");
        PromptsTab.Header = Localizer.T(language, "Prompts");
        BackupTab.Header = Localizer.T(language, "Backup");
        ResultTab.Header = Localizer.T(language, "Results");

        OpenAiKeyHintText.Text = Localizer.T(language, "OpenAiKeyHint");
        OpenAiTranscriptionModelLabelText.Text = Localizer.T(language, "TranscriptionModel");
        OpenAiRewriteModelLabelText.Text = Localizer.T(language, "RewriteModel");
        OpenRouterHintText.Text = Localizer.T(language, "OpenRouterHint");
        OpenRouterRewriteModelLabelText.Text = Localizer.T(language, "RewriteModel");
        AnthropicHintText.Text = Localizer.T(language, "AnthropicHint");
        AnthropicRewriteModelLabelText.Text = Localizer.T(language, "RewriteModel");
        LocalTranscriptionTitleText.Text = Localizer.T(language, "LocalTranscription");
        LocalTranscriptionHintText.Text = Localizer.T(language, "LocalTranscriptionHint");
        WhisperModelLabelText.Text = Localizer.T(language, "WhisperModel");
        WhisperModelPresetLabelText.Text = Localizer.T(language, "FoundWhisperModels");
        WhisperTimeoutLabelText.Text = Localizer.T(language, "WhisperTimeout");
        BrowseWhisperExeButton.Content = Localizer.T(language, "Browse");
        BrowseWhisperModelButton.Content = Localizer.T(language, "Browse");
        TestWhisperButton.Content = Localizer.T(language, "TestWhisper");
        OllamaHintText.Text = Localizer.T(language, "OllamaHint");
        OllamaRewriteModelLabelText.Text = Localizer.T(language, "RewriteModel");
        TestOllamaButton.Content = Localizer.T(language, "TestOllama");

        TranscribeHotkeyLabelText.Text = Localizer.T(language, "TranscribeOnly");
        ImproveHotkeyLabelText.Text = Localizer.T(language, "Improve");
        CalmHotkeyLabelText.Text = Localizer.T(language, "Calm");
        EmojisHotkeyLabelText.Text = Localizer.T(language, "Emojis");
        DetectTranscribeHotkeyButton.Content = Localizer.T(language, "Detect");
        DetectImproveHotkeyButton.Content = Localizer.T(language, "Detect");
        DetectCalmHotkeyButton.Content = Localizer.T(language, "Detect");
        DetectEmojisHotkeyButton.Content = Localizer.T(language, "Detect");
        RestoreDefaultHotkeysButton.Content = Localizer.T(language, "RestoreDefaultHotkeys");
        HotkeyStatusText.Text = Localizer.T(language, "HotkeysActive");

        CustomNamesTitleText.Text = Localizer.T(language, "CustomNames");
        CustomNamesHintText.Text = Localizer.T(language, "CustomNamesHint");
        TranscriptionHintTitleText.Text = Localizer.T(language, "TranscriptionHint");
        TranscriptionHintHelpText.Text = Localizer.T(language, "TranscriptionHintHelp");
        WorkflowPromptsTitleText.Text = Localizer.T(language, "WorkflowPrompts");
        WorkflowPromptsHintText.Text = Localizer.T(language, "WorkflowPromptsHint");
        PromptPresetTitleText.Text = Localizer.T(language, "PromptPresets");
        PromptPresetHintText.Text = Localizer.T(language, "PromptPresetsHint");
        ApplyPromptPresetButton.Content = Localizer.T(language, "Apply");
        ImprovePromptLabelText.Text = Localizer.T(language, "Improve");
        CalmPromptLabelText.Text = Localizer.T(language, "Calm");
        EmojisPromptLabelText.Text = Localizer.T(language, "Emojis");
        ResetPromptsButton.Content = Localizer.T(language, "ResetPrompts");

        ExportSettingsTitleText.Text = Localizer.T(language, "ExportSettings");
        AppVersionTitleText.Text = Localizer.T(language, "Version");
        AppVersionText.Text = $"BlitzText Windows {GetAppVersion()}";
        CheckUpdatesButton.Content = Localizer.T(language, "CheckUpdates");
        OpenUpdateButton.Content = Localizer.T(language, "OpenDownload");
        if (string.IsNullOrWhiteSpace(latestUpdateUrl))
        {
            UpdateStatusText.Text = Localizer.T(language, "UpdateNotChecked");
        }
        ExportSettingsHintText.Text = Localizer.T(language, "ExportSettingsHint");
        ExportSettingsButton.Content = Localizer.T(language, "Export");
        ImportSettingsTitleText.Text = Localizer.T(language, "ImportSettings");
        ImportSettingsHintText.Text = Localizer.T(language, "ImportSettingsHint");
        ImportSettingsButton.Content = Localizer.T(language, "Import");

        CopyResultButton.Content = Localizer.T(language, "Copy");
        DeleteHistoryEntryButton.Content = Localizer.T(language, "Delete");
        ClearHistoryButton.Content = Localizer.T(language, "ClearHistory");
        HistorySearchLabelText.Text = Localizer.T(language, "SearchHistoryLabel");
        HistorySearchBox.ToolTip = Localizer.T(language, "SearchHistory");
        ResultsTitleText.Text = Localizer.T(language, "Results");
        FooterText.Text = Localizer.T(language, "Footer");
    }

    private void ScheduleAutoSave()
    {
        SaveStatusText.Text = Localizer.T(settings.AppLanguage, "AutoSaving");
        autoSaveTimer.Stop();
        autoSaveTimer.Start();
    }

    private void RefreshWhisperModelOptions()
    {
        isRefreshingWhisperModels = true;
        try
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddModelsFromDirectory(@"C:\Apps\whisper.cpp\models", paths);
            if (!string.IsNullOrWhiteSpace(LocalWhisperModelPathBox.Text))
            {
                var directory = Path.GetDirectoryName(LocalWhisperModelPathBox.Text);
                AddModelsFromDirectory(directory, paths);
                if (File.Exists(LocalWhisperModelPathBox.Text))
                {
                    paths.Add(LocalWhisperModelPathBox.Text);
                }
            }

            var options = paths
                .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                .Select(path => new DisplayOption<string>(path, Path.GetFileName(path)))
                .ToList();

            LocalWhisperModelCombo.ItemsSource = options;
            LocalWhisperModelCombo.SelectedItem = options.FirstOrDefault(option =>
                option.Value.Equals(LocalWhisperModelPathBox.Text, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            isRefreshingWhisperModels = false;
        }
    }

    private static void AddModelsFromDirectory(string? directory, ISet<string> paths)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.bin"))
        {
            paths.Add(file);
        }
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusText.Foreground = isError
            ? System.Windows.Media.Brushes.Firebrick
            : System.Windows.Media.Brushes.SeaGreen;
        StatusText.Text = message;
    }

    private void AutoSaveSettings()
    {
        autoSaveTimer.Stop();

        try
        {
            SaveSettingsFromUi();
            SaveStatusText.Text = $"{Localizer.T(settings.AppLanguage, "AutoSaved")}: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            SaveStatusText.Text = $"{Localizer.T(settings.AppLanguage, "AutoSaveFailed")}: {ex.Message}";
        }
    }

    private void LoadProviderApiKeys()
    {
        var apiKey = credentialStore.ReadOpenAiApiKey();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            settings.OpenAiApiKey = apiKey;
        }

        var legacyApiKey = settingsStore.ReadLegacyOpenAiApiKey();
        if (string.IsNullOrWhiteSpace(settings.OpenAiApiKey) && !string.IsNullOrWhiteSpace(legacyApiKey))
        {
            settings.OpenAiApiKey = legacyApiKey;
            credentialStore.SaveOpenAiApiKey(legacyApiKey);
            settingsStore.Save(settings);
        }

        settings.OpenRouterApiKey = credentialStore.ReadOpenRouterApiKey();
        settings.AnthropicApiKey = credentialStore.ReadAnthropicApiKey();
    }

    private async Task WarmOllamaIfEnabledAsync(bool showStatus)
    {
        if (!settings.KeepOllamaWarm || settings.RewriteProvider != RewriteProviderKind.Ollama)
        {
            return;
        }

        try
        {
            if (showStatus)
            {
                StatusText.Text = "Halte Ollama warm...";
            }

            var result = await ollamaConnectionTester.WarmAsync(
                settings.OllamaBaseUrl,
                settings.OllamaRewriteModel,
                CancellationToken.None);

            if (showStatus)
            {
                StatusText.Text = result;
            }
        }
        catch
        {
            if (showStatus)
            {
                StatusText.Text = "Ollama Warmhalten fehlgeschlagen.";
            }
        }
    }

    private void RegisterWorkflowHotkeys()
    {
        if (!isHotkeyReady)
        {
            return;
        }

        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            hotkeyService.RegisterWorkflowHotkeys(handle, GetWorkflowHotkeyOptions());
            var message = Localizer.T(settings.AppLanguage, "HotkeysActive");
            HotkeyStatusText.Text = message;
            StatusText.Text = message;
        }
        catch (Exception ex)
        {
            HotkeyStatusText.Text = ex.Message;
            StatusText.Text = ex.Message;
        }
    }

    private IReadOnlyDictionary<WorkflowKind, HotkeyOption> GetWorkflowHotkeyOptions()
    {
        return new Dictionary<WorkflowKind, HotkeyOption>
        {
            [WorkflowKind.Transcribe] = HotkeyOptions.FindById(settings.TranscribeHotkeyId),
            [WorkflowKind.Improve] = HotkeyOptions.FindById(settings.ImproveHotkeyId),
            [WorkflowKind.Calm] = HotkeyOptions.FindById(settings.CalmHotkeyId),
            [WorkflowKind.Emojis] = HotkeyOptions.FindById(settings.EmojisHotkeyId)
        };
    }

    private static string GetAppVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
    }
}
