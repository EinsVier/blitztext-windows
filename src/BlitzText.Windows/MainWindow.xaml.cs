using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
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
    private readonly DispatcherTimer statusHighlightTimer;
    private readonly ObservableCollection<HistoryEntry> historyEntries = [];
    private readonly ObservableCollection<string> ollamaRewriteModels = [];
    private readonly List<HistoryEntry> allHistoryEntries = [];
    private bool isLoading;
    private bool isHotkeyReady;
    private bool isProcessing;
    private bool isRefreshingWhisperModels;
    private bool isRefreshingOllamaModels;
    private bool isResultEditorRecording;
    private bool isDisposed;
    private string latestUpdateUrl = "";
    private string selectedPromptPresetId = "general";
    private string? previousImprovePrompt;
    private WorkflowKind activeWorkflow;
    private TargetWindow activeTargetWindow = new(IntPtr.Zero, "");
    private CancellationTokenSource? workflowCancellation;

    public AppLanguage CurrentAppLanguage => settings.AppLanguage;

    public MainWindow()
    {
        settings = settingsStore.Load();
        MigrateLegacyEmojiWorkflow(settings);
        LoadProviderApiKeys();
        workflowRunner = new BlitzWorkflowRunner(new ProviderFactory(settings), settings);
        targetWindowService = new TargetWindowService(() => new WindowInteropHelper(this).Handle);
        autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(700)
        };
        autoSaveTimer.Tick += (_, _) => AutoSaveSettings();
        statusHighlightTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        statusHighlightTimer.Tick += (_, _) => ResetStatusHighlight();

        InitializeComponent();
        DependencyPropertyDescriptor.FromProperty(System.Windows.Controls.TextBlock.TextProperty, typeof(System.Windows.Controls.TextBlock))
            .AddValueChanged(StatusText, (_, _) => HighlightStatusChange(StatusText.Text));
        HistoryList.ItemsSource = historyEntries;
        OllamaRewriteModelCombo.ItemsSource = ollamaRewriteModels;
        OllamaRewriteModelCombo.AddHandler(
            System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
            new System.Windows.Controls.TextChangedEventHandler(OllamaRewriteModelCombo_TextChanged));
        LoadHistoryEntries();
        LoadUiFromSettings();
    }

    protected override void OnClosed(EventArgs e)
    {
        DisposeResources();
        base.OnClosed(e);
    }

    private void DisposeResources()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        workflowCancellation?.Cancel();
        audioRecorder.Dispose();
        hotkeyService.Dispose();
        autoSaveTimer.Stop();
        statusHighlightTimer.Stop();
        recordingIndicator.Close();
        workflowCancellation?.Dispose();
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
            StatusText.Text = UserErrorFormatter.Format(ex, settings.AppLanguage);
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
        ReprocessWorkflowCombo.ItemsSource = WorkflowDisplay.GetOptions(settings.AppLanguage);
        PromptPresetCombo.ItemsSource = PromptPresetCatalog.GetOptions(settings.AppLanguage);
        AppLanguageCombo.ItemsSource = LanguageDisplay.AppLanguageOptions(settings.AppLanguage);
        AppThemeCombo.ItemsSource = LanguageDisplay.AppThemeOptions(settings.AppLanguage);
        DictationLanguageCombo.ItemsSource = LanguageDisplay.DictationLanguageOptions(settings.AppLanguage);
        TranscriptionProviderCombo.ItemsSource = Enum.GetValues<TranscriptionProviderKind>();
        RewriteProviderCombo.ItemsSource = Enum.GetValues<RewriteProviderKind>();
        TranscribeHotkeyCombo.ItemsSource = HotkeyOptions.KeyboardOnly;
        ImproveHotkeyCombo.ItemsSource = HotkeyOptions.KeyboardOnly;
        CalmHotkeyCombo.ItemsSource = HotkeyOptions.KeyboardOnly;

        WorkflowCombo.SelectedItem = WorkflowDisplay.FindOption(settings.DefaultWorkflow, settings.AppLanguage);
        ReprocessWorkflowCombo.SelectedItem = WorkflowDisplay.FindOption(WorkflowKind.Improve, settings.AppLanguage);
        SelectPromptPreset(selectedPromptPresetId);
        AppLanguageCombo.SelectedItem = LanguageDisplay.FindAppLanguage(settings.AppLanguage, settings.AppLanguage);
        AppThemeCombo.SelectedItem = LanguageDisplay.FindAppTheme(settings.AppTheme, settings.AppLanguage);
        DictationLanguageCombo.SelectedItem = LanguageDisplay.FindDictationLanguage(settings.DictationLanguage, settings.AppLanguage);
        TranscriptionProviderCombo.SelectedItem = settings.TranscriptionProvider;
        RewriteProviderCombo.SelectedItem = settings.RewriteProvider;
        TranscribeHotkeyCombo.SelectedItem = HotkeyOptions.FindById(settings.TranscribeHotkeyId);
        ImproveHotkeyCombo.SelectedItem = HotkeyOptions.FindById(settings.ImproveHotkeyId);
        CalmHotkeyCombo.SelectedItem = HotkeyOptions.FindById(settings.CalmHotkeyId);
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
        RefreshOllamaModelOptions([settings.OllamaRewriteModel]);
        OllamaRewriteModelCombo.Text = settings.OllamaRewriteModel;
        CustomNamesBox.Text = settings.CustomNames;
        TranscriptionPromptBox.Text = settings.TranscriptionPrompt;
        ImprovePromptBox.Text = settings.ImprovePrompt;
        CalmPromptBox.Text = settings.CalmPrompt;
        EmojisPromptBox.Text = settings.EmojisPrompt;
        AddEmojisToRewriteCheckBox.IsChecked = settings.AddEmojisToRewrite;
        ReprocessAddEmojisCheckBox.IsChecked = settings.AddEmojisToRewrite;
        AutoPasteCheckBox.IsChecked = settings.AutoPaste;
        SaveHistoryCheckBox.IsChecked = settings.SaveHistory;
        KeepOllamaWarmCheckBox.IsChecked = settings.KeepOllamaWarm;
        RefreshWhisperModelOptions();
        isLoading = false;
        ApplyTheme();
        ApplyLocalization();
        UpdateRecordButton();
        UpdateEmojiOptionAvailability();
        UpdateActiveProviderBadges();
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
        var ownsProcessingState = false;

        try
        {
            if (isResultEditorRecording)
            {
                await ToggleResultEditorRecordingAsync();
                return;
            }

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
            ownsProcessingState = true;
            RecordButton.IsEnabled = true;
            RecordButton.Content = Localizer.T(settings.AppLanguage, "Cancel");
            StatusText.Text = settings.AppLanguage == AppLanguage.English
                ? "Transcribing and processing..."
                : "Transkribiere und verarbeite...";

            var wavPath = await audioRecorder.StopAsync();
            recordingIndicator.Stop();
            workflowCancellation?.Cancel();
            workflowCancellation = new CancellationTokenSource();

            var result = await workflowRunner.RunWithTranscriptAsync(
                wavPath,
                activeWorkflow,
                status => Dispatcher.Invoke(() => StatusText.Text = status),
                workflowCancellation.Token);
            SourceTextBox.Text = result.Transcript;
            ResultBox.Text = result.Text;
            if (settings.SaveHistory)
            {
                AddHistoryEntry(result.Text, activeWorkflow, result.Transcript);
            }

            if (settings.AutoPaste)
            {
                if (targetWindowService.Activate(activeTargetWindow))
                {
                    var clipboardRestored = await ClipboardPasteService.PasteTextPreservingClipboardAsync(result.Text, activeTargetWindow.Handle);
                    StatusText.Text = string.IsNullOrWhiteSpace(activeTargetWindow.Title)
                        ? (settings.AppLanguage == AppLanguage.English ? "Done. Result was pasted." : "Fertig. Ergebnis wurde eingefuegt.")
                        : (settings.AppLanguage == AppLanguage.English ? $"Done. Result was pasted into: {activeTargetWindow.Title}" : $"Fertig. Ergebnis wurde eingefuegt in: {activeTargetWindow.Title}");

                    if (!clipboardRestored)
                    {
                        StatusText.Text += settings.AppLanguage == AppLanguage.English
                            ? " Clipboard restore failed."
                            : " Zwischenablage konnte nicht wiederhergestellt werden.";
                    }

                    return;
                }

                StatusText.Text = settings.AppLanguage == AppLanguage.English
                    ? "Done. Target window not found; clipboard was left unchanged."
                    : "Fertig. Ziel-Fenster nicht gefunden; Zwischenablage blieb unveraendert.";
                return;
            }

            ClipboardPasteService.Copy(result.Text);
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

            StatusText.Text = UserErrorFormatter.Format(ex, settings.AppLanguage);
        }
        finally
        {
            if (ownsProcessingState)
            {
                isProcessing = false;
                UpdateRecordButton();
            }
        }
    }

    private async void ResultRecordButton_Click(object sender, RoutedEventArgs e)
    {
        await ToggleResultEditorRecordingAsync();
    }

    private void OpenSoundSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:sound")
            {
                UseShellExecute = true
            });
            StatusText.Text = settings.AppLanguage == AppLanguage.English
                ? "Windows sound settings opened."
                : "Windows-Soundeinstellungen wurden geoeffnet.";
        }
        catch (Exception ex)
        {
            StatusText.Text = UserErrorFormatter.Format(ex, settings.AppLanguage);
        }
    }

    private async Task ToggleResultEditorRecordingAsync()
    {
        var ownsProcessingState = false;

        try
        {
            if (isProcessing)
            {
                workflowCancellation?.Cancel();
                StatusText.Text = settings.AppLanguage == AppLanguage.English
                    ? "Canceling processing..."
                    : "Breche Verarbeitung ab...";
                return;
            }

            if (!audioRecorder.IsRecording)
            {
                isResultEditorRecording = true;
                await audioRecorder.StartAsync();
                recordingIndicator.Start();
                UpdateRecordButton();
                StatusText.Text = settings.AppLanguage == AppLanguage.English
                    ? "Recording spoken text..."
                    : "Gesprochenen Text aufnehmen...";
                return;
            }

            if (!isResultEditorRecording)
            {
                StatusText.Text = settings.AppLanguage == AppLanguage.English
                    ? "Another recording is already running."
                    : "Es laeuft bereits eine andere Aufnahme.";
                return;
            }

            isProcessing = true;
            ownsProcessingState = true;
            UpdateRecordButton();
            StatusText.Text = settings.AppLanguage == AppLanguage.English
                ? "Transcribing spoken text..."
                : "Transkribiere gesprochenen Text...";

            var wavPath = await audioRecorder.StopAsync();
            isResultEditorRecording = false;
            recordingIndicator.Stop();
            workflowCancellation?.Cancel();
            workflowCancellation = new CancellationTokenSource();

            var result = await workflowRunner.RunWithTranscriptAsync(
                wavPath,
                WorkflowKind.Transcribe,
                status => Dispatcher.Invoke(() => StatusText.Text = status),
                workflowCancellation.Token);

            var transcript = result.Transcript.Trim();
            var existingText = SourceTextBox.Text;
            SourceTextBox.Text = string.IsNullOrWhiteSpace(existingText)
                ? transcript
                : $"{existingText.TrimEnd()} {transcript}";
            SourceTextBox.CaretIndex = SourceTextBox.Text.Length;
            SourceTextBox.ScrollToEnd();
            ResultBox.Clear();
            StatusText.Text = settings.AppLanguage == AppLanguage.English
                ? "The recording was appended to the spoken text."
                : "Die Aufnahme wurde an den gesprochenen Text angefuegt.";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = settings.AppLanguage == AppLanguage.English
                ? "Processing canceled."
                : "Verarbeitung abgebrochen.";
        }
        catch (Exception ex)
        {
            if (!audioRecorder.IsRecording)
            {
                isResultEditorRecording = false;
                recordingIndicator.Stop();
            }

            StatusText.Text = UserErrorFormatter.Format(ex, settings.AppLanguage);
        }
        finally
        {
            if (ownsProcessingState)
            {
                isProcessing = false;
                UpdateRecordButton();
            }
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

        if (ResultRecordButton is not null)
        {
            ResultRecordButton.IsEnabled = !isProcessing || isResultEditorRecording;
            ResultRecordIcon.Data = Geometry.Parse(isResultEditorRecording
                ? "M5,5 L15,5 L15,15 L5,15 Z"
                : "M10,2 C8.35,2 7,3.35 7,5 L7,10 C7,11.65 8.35,13 10,13 C11.65,13 13,11.65 13,10 L13,5 C13,3.35 11.65,2 10,2 Z M4.5,9.5 L4.5,10 C4.5,13.04 6.96,15.5 10,15.5 C13.04,15.5 15.5,13.04 15.5,10 L15.5,9.5 M10,15.5 L10,18 M7.5,18 L12.5,18");
            ResultRecordIcon.Fill = isResultEditorRecording
                ? FindResource("StatusErrorBrush") as System.Windows.Media.Brush
                : System.Windows.Media.Brushes.Transparent;
            ResultRecordIcon.Stroke = isResultEditorRecording
                ? FindResource("StatusErrorBrush") as System.Windows.Media.Brush
                : FindResource("StatusInfoBrush") as System.Windows.Media.Brush;
            ResultRecordButton.ToolTip = Localizer.T(
                settings.AppLanguage,
                isResultEditorRecording ? "StopSpokenTextRecording" : "RecordSpokenText");
        }
    }

    private string GetDefaultWorkflowHotkeyLabel()
    {
        var hotkeyId = settings.DefaultWorkflow switch
        {
            WorkflowKind.Transcribe => settings.TranscribeHotkeyId,
            WorkflowKind.Improve => settings.ImproveHotkeyId,
            WorkflowKind.Calm => settings.CalmHotkeyId,
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
                UserErrorFormatter.Format(ex, settings.AppLanguage));
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

    private void OpenGitHubButton_Click(object sender, RoutedEventArgs e)
    {
        OpenExternalUrl("https://github.com/EinsVier/blitztext-windows");
    }

    private void ReportIssueButton_Click(object sender, RoutedEventArgs e)
    {
        OpenExternalUrl("https://github.com/EinsVier/blitztext-windows/issues/new/choose");
    }

    private void OpenLicenseButton_Click(object sender, RoutedEventArgs e)
    {
        OpenExternalUrl("https://github.com/EinsVier/blitztext-windows/blob/master/LICENSE");
    }

    private static void OpenExternalUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    private void ResetPromptsButton_Click(object sender, RoutedEventArgs e)
    {
        var confirmation = System.Windows.MessageBox.Show(
            Localizer.T(settings.AppLanguage, "ConfirmResetPrompts"),
            Localizer.T(settings.AppLanguage, "ConfirmResetPromptsTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmation != MessageBoxResult.Yes)
        {
            StatusText.Text = settings.AppLanguage == AppLanguage.English
                ? "The existing workflow prompts were kept."
                : "Die vorhandenen Workflow-Prompts wurden beibehalten.";
            return;
        }

        ImprovePromptBox.Text = DefaultPrompts.GetImprove(settings.AppLanguage);
        CalmPromptBox.Text = DefaultPrompts.GetCalm(settings.AppLanguage);
        EmojisPromptBox.Text = DefaultPrompts.GetEmojis(settings.AppLanguage);
        previousImprovePrompt = null;
        RestorePreviousPromptButton.Visibility = Visibility.Collapsed;
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

        var presetPrompt = selectedPreset.Value.GetPrompt(settings.AppLanguage);
        var currentPrompt = ImprovePromptBox.Text;
        if (string.Equals(currentPrompt.Trim(), presetPrompt.Trim(), StringComparison.Ordinal))
        {
            StatusText.Text = settings.AppLanguage == AppLanguage.English
                ? "This prompt preset is already active."
                : "Diese Prompt-Vorlage ist bereits aktiv.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(currentPrompt))
        {
            var confirmation = System.Windows.MessageBox.Show(
                Localizer.T(settings.AppLanguage, "ConfirmPresetReplacement"),
                Localizer.T(settings.AppLanguage, "ConfirmPresetReplacementTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (confirmation != MessageBoxResult.Yes)
            {
                StatusText.Text = settings.AppLanguage == AppLanguage.English
                    ? "The existing prompt was kept."
                    : "Der vorhandene Prompt wurde beibehalten.";
                return;
            }
        }

        previousImprovePrompt ??= currentPrompt;
        RestorePreviousPromptButton.Visibility = Visibility.Visible;
        selectedPromptPresetId = selectedPreset.Value.Id;
        ImprovePromptBox.Text = presetPrompt;
        SaveSettingsFromUi(saveToDisk: false);
        ScheduleAutoSave();
        StatusText.Text = settings.AppLanguage == AppLanguage.English
            ? $"Prompt preset applied: {selectedPreset.Label}. The previous prompt can be restored."
            : $"Prompt-Vorlage angewendet: {selectedPreset.Label}. Der vorherige Prompt kann wiederhergestellt werden.";
    }

    private void RestorePreviousPromptButton_Click(object sender, RoutedEventArgs e)
    {
        if (previousImprovePrompt is null)
        {
            return;
        }

        ImprovePromptBox.Text = previousImprovePrompt;
        previousImprovePrompt = null;
        RestorePreviousPromptButton.Visibility = Visibility.Collapsed;
        SaveSettingsFromUi(saveToDisk: false);
        ScheduleAutoSave();
        StatusText.Text = settings.AppLanguage == AppLanguage.English
            ? "The previous prompt was restored."
            : "Der vorherige Prompt wurde wiederhergestellt.";
    }

    private void HistoryList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (HistoryList.SelectedItem is HistoryEntry entry)
        {
            SourceTextBox.Text = entry.SourceForRewrite;
            ResultBox.Text = entry.Text;
            var workflow = entry.Workflow == WorkflowKind.Emojis ? WorkflowKind.Improve : entry.Workflow;
            ReprocessWorkflowCombo.SelectedItem = WorkflowDisplay.FindOption(workflow, settings.AppLanguage);
            if (entry.Workflow == WorkflowKind.Emojis)
            {
                ReprocessAddEmojisCheckBox.IsChecked = true;
            }
            RefreshPromptDetails();
        }
    }

    private void SourceTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        RefreshPromptDetails();
    }

    private void ReprocessWorkflowCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateEmojiOptionAvailability();
        RefreshPromptDetails();
    }

    private void RefreshPromptDetails()
    {
        if (PromptDetailsBox is null || SourceTextBox is null || ReprocessWorkflowCombo is null)
        {
            return;
        }

        var workflow = ReprocessWorkflowCombo.SelectedItem is DisplayOption<WorkflowKind> selectedWorkflow
            ? selectedWorkflow.Value
            : settings.DefaultWorkflow;
        var prompt = WorkflowPromptFactory.CreateRewritePrompt(workflow, SourceTextBox.Text, settings);

        PromptDetailsBox.Text = prompt ?? (settings.AppLanguage == AppLanguage.English
            ? "This workflow does not send a rewrite prompt. The edited transcription is used unchanged."
            : "Dieser Workflow sendet keinen Rewrite-Prompt. Die bearbeitete Transkription wird unveraendert verwendet.");
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

    private async void ReprocessResultButton_Click(object sender, RoutedEventArgs e)
    {
        if (isProcessing || audioRecorder.IsRecording)
        {
            StatusText.Text = settings.AppLanguage == AppLanguage.English
                ? "Finish or cancel the current recording or processing first."
                : "Bitte zuerst die laufende Aufnahme oder Verarbeitung beenden beziehungsweise abbrechen.";
            return;
        }

        var sourceText = SourceTextBox.Text;

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            StatusText.Text = settings.AppLanguage == AppLanguage.English
                ? "No text to reprocess."
                : "Kein Text zum erneuten Verarbeiten.";
            return;
        }

        var workflow = ReprocessWorkflowCombo.SelectedItem is DisplayOption<WorkflowKind> selectedWorkflow
            ? selectedWorkflow.Value
            : settings.DefaultWorkflow;

        try
        {
            isProcessing = true;
            ReprocessResultButton.IsEnabled = false;
            UpdateRecordButton();
            StatusText.Text = settings.AppLanguage == AppLanguage.English
                ? $"Reprocessing: {WorkflowDisplay.GetLabel(workflow, settings.AppLanguage)}..."
                : $"Verarbeite erneut: {WorkflowDisplay.GetLabel(workflow, settings.AppLanguage)}...";

            workflowCancellation?.Cancel();
            workflowCancellation = new CancellationTokenSource();
            var result = await workflowRunner.RunTextAsync(
                sourceText,
                workflow,
                status => Dispatcher.Invoke(() => StatusText.Text = status),
                workflowCancellation.Token);

            ResultBox.Text = result;
            if (settings.SaveHistory)
            {
                AddHistoryEntry(result, workflow, sourceText);
            }

            ClipboardPasteService.Copy(result);
            StatusText.Text = settings.AppLanguage == AppLanguage.English
                ? "Reprocessed result was copied to the clipboard."
                : "Erneut verarbeitetes Ergebnis wurde in die Zwischenablage kopiert.";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = settings.AppLanguage == AppLanguage.English ? "Processing canceled." : "Verarbeitung abgebrochen.";
        }
        catch (Exception ex)
        {
            StatusText.Text = UserErrorFormatter.Format(ex, settings.AppLanguage);
        }
        finally
        {
            isProcessing = false;
            ReprocessResultButton.IsEnabled = true;
            UpdateRecordButton();
        }
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
        SourceTextBox.Clear();
        ResultBox.Clear();
        PromptDetailsBox.Clear();
        StatusText.Text = settings.AppLanguage == AppLanguage.English ? "History entry deleted." : "Verlaufseintrag geloescht.";
    }

    private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        allHistoryEntries.Clear();
        historyEntries.Clear();
        historyStore.Save(allHistoryEntries);
        SourceTextBox.Clear();
        ResultBox.Clear();
        PromptDetailsBox.Clear();
        StatusText.Text = settings.AppLanguage == AppLanguage.English ? "History cleared." : "Verlauf geleert.";
    }

    private void HistorySearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!isLoading)
        {
            ApplyHistoryFilter();
        }
    }

    private async void CheckSetupButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSettingsFromUi(saveToDisk: false);
        CheckSetupButton.IsEnabled = false;
        SetupCheckResultBox.Text = settings.AppLanguage == AppLanguage.English ? "Checking setup..." : "Pruefe Setup...";

        try
        {
            SetupCheckResultBox.Text = await BuildSetupCheckReportAsync();
            StatusText.Text = settings.AppLanguage == AppLanguage.English ? "Setup check completed." : "Setup-Pruefung abgeschlossen.";
        }
        finally
        {
            CheckSetupButton.IsEnabled = true;
        }
    }

    private async Task<string> BuildSetupCheckReportAsync()
    {
        var lines = new List<string>();
        var english = settings.AppLanguage == AppLanguage.English;
        void Add(bool ok, string label, string detail)
        {
            lines.Add($"{(ok ? "OK" : "!")} {label}: {detail}");
        }

        Add(isHotkeyReady,
            english ? "Hotkeys" : "Hotkeys",
            isHotkeyReady
                ? (english ? "registered" : "registriert")
                : (english ? "not registered yet" : "noch nicht registriert"));

        var openAiRequired = settings.TranscriptionProvider == TranscriptionProviderKind.OpenAI ||
                             settings.RewriteProvider == RewriteProviderKind.OpenAI;
        Add(!openAiRequired || !string.IsNullOrWhiteSpace(settings.OpenAiApiKey),
            "OpenAI",
            !openAiRequired
                ? (english ? "not selected" : "nicht ausgewaehlt")
                : !string.IsNullOrWhiteSpace(settings.OpenAiApiKey)
                ? (english ? "API key available" : "API-Key vorhanden")
                : (english ? "API key missing" : "API-Key fehlt"));

        Add(settings.RewriteProvider != RewriteProviderKind.OpenRouter || !string.IsNullOrWhiteSpace(settings.OpenRouterApiKey),
            "OpenRouter",
            settings.RewriteProvider != RewriteProviderKind.OpenRouter
                ? (english ? "not selected" : "nicht ausgewaehlt")
                : string.IsNullOrWhiteSpace(settings.OpenRouterApiKey)
                ? (english ? "API key missing" : "API-Key fehlt")
                : (english ? "API key available" : "API-Key vorhanden"));

        Add(settings.RewriteProvider != RewriteProviderKind.Anthropic || !string.IsNullOrWhiteSpace(settings.AnthropicApiKey),
            "Anthropic",
            settings.RewriteProvider != RewriteProviderKind.Anthropic
                ? (english ? "not selected" : "nicht ausgewaehlt")
                : string.IsNullOrWhiteSpace(settings.AnthropicApiKey)
                ? (english ? "API key missing" : "API-Key fehlt")
                : (english ? "API key available" : "API-Key vorhanden"));

        if (settings.RewriteProvider == RewriteProviderKind.Ollama || settings.KeepOllamaWarm)
        {
            try
            {
                var result = await ollamaConnectionTester.TestAsync(settings.OllamaBaseUrl, settings.OllamaRewriteModel, CancellationToken.None);
                Add(result.ModelFound, "Ollama", result.Message);
            }
            catch (Exception ex)
            {
                Add(false, "Ollama", UserErrorFormatter.Format(ex, settings.AppLanguage));
            }
        }
        else
        {
            Add(true, "Ollama", english ? "not selected" : "nicht ausgewaehlt");
        }

        var whisperExeOk = File.Exists(settings.LocalWhisperExecutablePath);
        var whisperModelOk = File.Exists(settings.LocalWhisperModelPath);
        var localWhisperSelected = settings.TranscriptionProvider == TranscriptionProviderKind.LocalWhisper;
        Add(!localWhisperSelected || whisperExeOk,
            english ? "Whisper executable" : "Whisper-EXE",
            !localWhisperSelected
                ? (english ? "not selected" : "nicht ausgewaehlt")
                : whisperExeOk ? settings.LocalWhisperExecutablePath : (english ? "missing" : "fehlt"));
        Add(!localWhisperSelected || whisperModelOk,
            english ? "Whisper model" : "Whisper-Modell",
            !localWhisperSelected
                ? (english ? "not selected" : "nicht ausgewaehlt")
                : whisperModelOk ? settings.LocalWhisperModelPath : (english ? "missing" : "fehlt"));

        return string.Join(Environment.NewLine, lines);
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
            RefreshOllamaModelOptions(result.Models);
            OllamaRewriteModelCombo.Text = settings.OllamaRewriteModel;
            StatusText.Text = result.Message;

            if (settings.KeepOllamaWarm && result.ModelFound)
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
            StatusText.Text = settings.AppLanguage == AppLanguage.English
                ? $"Ollama test failed: {UserErrorFormatter.Format(ex, settings.AppLanguage)}"
                : $"Ollama-Test fehlgeschlagen: {UserErrorFormatter.Format(ex, settings.AppLanguage)}";
        }
        finally
        {
            TestOllamaButton.IsEnabled = true;
        }
    }

    private void OllamaRewriteModelCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (isLoading || isRefreshingOllamaModels || OllamaRewriteModelCombo.SelectedItem is not string model)
        {
            return;
        }

        OllamaRewriteModelCombo.Text = model;
        SaveSettingsFromUi(saveToDisk: false);
        ScheduleAutoSave();
    }

    private void OllamaRewriteModelCombo_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (isLoading || isRefreshingOllamaModels)
        {
            return;
        }

        SaveSettingsFromUi(saveToDisk: false);
        ScheduleAutoSave();
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
            SetStatus(settings.AppLanguage == AppLanguage.English
                ? $"Whisper test failed: {UserErrorFormatter.Format(ex, settings.AppLanguage)}"
                : $"Whisper-Test fehlgeschlagen: {UserErrorFormatter.Format(ex, settings.AppLanguage)}", true);
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
            ApplyTheme();
            ApplyLocalization();
            UpdateRecordButton();
            UpdateEmojiOptionAvailability();
            UpdateActiveProviderBadges();
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

    private void UpdateEmojiOptionAvailability()
    {
        if (AddEmojisToRewriteCheckBox is null || WorkflowCombo is null)
        {
            return;
        }

        var workflow = WorkflowCombo.SelectedItem is DisplayOption<WorkflowKind> selectedWorkflow
            ? selectedWorkflow.Value
            : settings.DefaultWorkflow;
        AddEmojisToRewriteCheckBox.IsEnabled = workflow is WorkflowKind.Improve or WorkflowKind.Calm;
        AddEmojisToRewriteHelpText.IsEnabled = AddEmojisToRewriteCheckBox.IsEnabled;

        if (ReprocessAddEmojisCheckBox is not null && ReprocessWorkflowCombo is not null)
        {
            var reprocessWorkflow = ReprocessWorkflowCombo.SelectedItem is DisplayOption<WorkflowKind> selectedReprocessWorkflow
                ? selectedReprocessWorkflow.Value
                : WorkflowKind.Improve;
            ReprocessAddEmojisCheckBox.IsEnabled = reprocessWorkflow is WorkflowKind.Improve or WorkflowKind.Calm;
        }
    }

    private void EmojiOption_Changed(object sender, RoutedEventArgs e)
    {
        if (isLoading)
        {
            return;
        }

        var isChecked = sender is System.Windows.Controls.CheckBox checkBox && checkBox.IsChecked == true;
        isLoading = true;
        AddEmojisToRewriteCheckBox.IsChecked = isChecked;
        ReprocessAddEmojisCheckBox.IsChecked = isChecked;
        isLoading = false;
        SaveSettingsFromUi(saveToDisk: false);
        RefreshPromptDetails();
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

    private void RestoreDefaultHotkeysButton_Click(object sender, RoutedEventArgs e)
    {
        TranscribeHotkeyCombo.SelectedItem = HotkeyOptions.FindById("browser-home");
        ImproveHotkeyCombo.SelectedItem = HotkeyOptions.FindById("shift-browser-home");
        CalmHotkeyCombo.SelectedItem = HotkeyOptions.FindById("ctrl-browser-home");
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
        settings.AppTheme = AppThemeCombo.SelectedItem is DisplayOption<AppTheme> appTheme
            ? appTheme.Value
            : settings.AppTheme;
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
        settings.OllamaRewriteModel = OllamaRewriteModelCombo.Text.Trim();
        settings.CustomNames = CustomNamesBox.Text.Trim();
        settings.TranscriptionPrompt = TranscriptionPromptBox.Text.Trim();
        settings.ImprovePrompt = ImprovePromptBox.Text.Trim();
        settings.CalmPrompt = CalmPromptBox.Text.Trim();
        settings.EmojisPrompt = EmojisPromptBox.Text.Trim();
        settings.AddEmojisToRewrite = AddEmojisToRewriteCheckBox.IsChecked == true;
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

        RefreshPromptDetails();
    }

    private void ApplyImportedSettings(AppSettings importedSettings)
    {
        MigrateLegacyEmojiWorkflow(importedSettings);
        var existingApiKey = settings.OpenAiApiKey;

        settings.DefaultWorkflow = importedSettings.DefaultWorkflow;
        settings.AppLanguage = importedSettings.AppLanguage;
        settings.AppTheme = importedSettings.AppTheme;
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
        settings.AddEmojisToRewrite = importedSettings.AddEmojisToRewrite;
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
            entry.WorkflowLabel = WorkflowDisplay.GetLabel(entry.Workflow, settings.AppLanguage);
            allHistoryEntries.Add(entry);
        }

        ApplyHistoryFilter();
    }

    private void AddHistoryEntry(string text, WorkflowKind workflow, string sourceText)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var entry = new HistoryEntry
        {
            CreatedAt = DateTimeOffset.Now,
            Workflow = workflow,
            Text = text,
            SourceText = sourceText,
            WorkflowLabel = WorkflowDisplay.GetLabel(workflow, settings.AppLanguage)
        };
        allHistoryEntries.Insert(0, entry);

        while (allHistoryEntries.Count > HistoryStore.MaxEntries)
        {
            allHistoryEntries.RemoveAt(allHistoryEntries.Count - 1);
        }

        historyStore.Save(allHistoryEntries);
        ApplyHistoryFilter();
        if (historyEntries.Contains(entry))
        {
            HistoryList.SelectedItem = entry;
        }
    }

    private void ApplyHistoryFilter()
    {
        var query = HistorySearchBox.Text.Trim();
        var filteredEntries = string.IsNullOrWhiteSpace(query)
            ? allHistoryEntries
            : allHistoryEntries
                .Where(entry =>
                    entry.Text.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    entry.SourceText.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    entry.WorkflowLabel.Contains(query, StringComparison.OrdinalIgnoreCase) ||
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
        var selectedTheme = settings.AppTheme;
        var selectedDictationLanguage = settings.DictationLanguage;
        var selectedReprocessWorkflow = ReprocessWorkflowCombo.SelectedItem is DisplayOption<WorkflowKind> reprocessWorkflow
            ? reprocessWorkflow.Value
            : WorkflowKind.Improve;
        var selectedPresetId = GetSelectedPromptPresetId();

        isLoading = true;
        WorkflowCombo.ItemsSource = WorkflowDisplay.GetOptions(settings.AppLanguage);
        ReprocessWorkflowCombo.ItemsSource = WorkflowDisplay.GetOptions(settings.AppLanguage);
        PromptPresetCombo.ItemsSource = PromptPresetCatalog.GetOptions(settings.AppLanguage);
        AppLanguageCombo.ItemsSource = LanguageDisplay.AppLanguageOptions(settings.AppLanguage);
        AppThemeCombo.ItemsSource = LanguageDisplay.AppThemeOptions(settings.AppLanguage);
        DictationLanguageCombo.ItemsSource = LanguageDisplay.DictationLanguageOptions(settings.AppLanguage);
        WorkflowCombo.SelectedItem = WorkflowDisplay.FindOption(selectedWorkflow, settings.AppLanguage);
        ReprocessWorkflowCombo.SelectedItem = WorkflowDisplay.FindOption(selectedReprocessWorkflow, settings.AppLanguage);
        SelectPromptPreset(selectedPresetId);
        AppLanguageCombo.SelectedItem = LanguageDisplay.FindAppLanguage(selectedAppLanguage, settings.AppLanguage);
        AppThemeCombo.SelectedItem = LanguageDisplay.FindAppTheme(selectedTheme, settings.AppLanguage);
        DictationLanguageCombo.SelectedItem = LanguageDisplay.FindDictationLanguage(selectedDictationLanguage, settings.AppLanguage);
        isLoading = false;

        foreach (var entry in allHistoryEntries)
        {
            entry.WorkflowLabel = WorkflowDisplay.GetLabel(entry.Workflow, settings.AppLanguage);
        }
        ApplyHistoryFilter();
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
        AppThemeLabelText.Text = Localizer.T(language, "AppTheme");
        DictationLanguageLabelText.Text = Localizer.T(language, "DictationLanguage");
        AutoPasteCheckBox.Content = Localizer.T(language, "AutoPaste");
        AddEmojisToRewriteText.Text = Localizer.T(language, "AddEmojisToRewrite");
        SaveHistoryCheckBox.Content = Localizer.T(language, "SaveHistory");
        KeepOllamaWarmCheckBox.Content = Localizer.T(language, "KeepOllamaWarm");
        StatusLabelText.Text = Localizer.T(language, "Status");
        SaveStatusText.Text = Localizer.T(language, "AutoSaveHint");

        ProviderTab.Header = Localizer.T(language, "Provider");
        HotkeysTab.Header = Localizer.T(language, "Hotkeys");
        PromptsTab.Header = Localizer.T(language, "Prompts");
        BackupTab.Header = Localizer.T(language, "Info");
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
        SetActiveBadgeText(OpenAiActiveBadge, language);
        SetActiveBadgeText(OpenRouterActiveBadge, language);
        SetActiveBadgeText(OllamaActiveBadge, language);
        SetActiveBadgeText(AnthropicActiveBadge, language);

        TranscribeHotkeyLabelText.Text = Localizer.T(language, "TranscribeOnly");
        ImproveHotkeyLabelText.Text = Localizer.T(language, "Improve");
        CalmHotkeyLabelText.Text = Localizer.T(language, "Calm");
        DetectTranscribeHotkeyButton.Content = Localizer.T(language, "Detect");
        DetectImproveHotkeyButton.Content = Localizer.T(language, "Detect");
        DetectCalmHotkeyButton.Content = Localizer.T(language, "Detect");
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
        RestorePreviousPromptButton.Content = Localizer.T(language, "RestorePreviousPrompt");
        ImprovePromptLabelText.Text = Localizer.T(language, "Improve");
        CalmPromptLabelText.Text = Localizer.T(language, "Calm");
        EmojiOptionTitleText.Text = Localizer.T(language, "EmojiOption");
        EmojiOptionHintText.Text = Localizer.T(language, "EmojiOptionHint");
        ReprocessAddEmojisText.Text = Localizer.T(language, "AddEmojisToRewrite");
        ResetPromptsButton.Content = Localizer.T(language, "ResetPrompts");

        AppVersionTitleText.Text = Localizer.T(language, "AboutBlitzText");
        AppVersionText.Text = $"BlitzText Windows {GetAppVersion()}";
        AboutBlitzTextHintText.Text = Localizer.T(language, "AboutBlitzTextHint");
        OpenGitHubButton.Content = Localizer.T(language, "OpenGitHub");
        ReportIssueButton.Content = Localizer.T(language, "ReportIssue");
        OpenLicenseButton.Content = Localizer.T(language, "OpenLicense");
        UpdatesTitleText.Text = Localizer.T(language, "Updates");
        CheckUpdatesButton.Content = Localizer.T(language, "CheckUpdates");
        OpenUpdateButton.Content = Localizer.T(language, "OpenDownload");
        SetupCheckTitleText.Text = Localizer.T(language, "SetupCheck");
        SetupCheckHintText.Text = Localizer.T(language, "SetupCheckHint");
        CheckSetupButton.Content = Localizer.T(language, "CheckSetup");
        if (string.IsNullOrWhiteSpace(latestUpdateUrl))
        {
            UpdateStatusText.Text = Localizer.T(language, "UpdateNotChecked");
        }
        SettingsBackupTitleText.Text = Localizer.T(language, "SettingsBackup");
        SettingsBackupHintText.Text = Localizer.T(language, "SettingsBackupHint");
        ExportSettingsTitleText.Text = Localizer.T(language, "Export");
        ExportSettingsHintText.Text = Localizer.T(language, "ExportSettingsHint");
        ExportSettingsButton.Content = Localizer.T(language, "ExportSettings");
        ImportSettingsTitleText.Text = Localizer.T(language, "ImportSettings");
        ImportSettingsHintText.Text = Localizer.T(language, "ImportSettingsHint");
        ImportSettingsButton.Content = Localizer.T(language, "ImportSettings");

        CopyResultButton.Content = Localizer.T(language, "Copy");
        ReprocessResultButton.Content = Localizer.T(language, "Reprocess");
        SpokenTextTitleText.Text = Localizer.T(language, "SpokenText");
        SpokenTextHintText.Text = Localizer.T(language, "SpokenTextHint");
        OpenSoundSettingsButton.ToolTip = Localizer.T(language, "OpenSoundSettings");
        ReprocessWorkflowLabelText.Text = Localizer.T(language, "ReprocessWorkflow");
        FinalTextTitleText.Text = Localizer.T(language, "FinalText");
        PromptDetailsHeaderText.Text = Localizer.T(language, "PromptDetails");
        PromptDetailsHintText.Text = Localizer.T(language, "PromptDetailsHint");
        DeleteHistoryEntryButton.Content = Localizer.T(language, "Delete");
        ClearHistoryButton.Content = Localizer.T(language, "ClearHistory");
        HistorySearchLabelText.Text = Localizer.T(language, "SearchHistoryLabel");
        HistorySearchBox.ToolTip = Localizer.T(language, "SearchHistory");
        ResultsTitleText.Text = Localizer.T(language, "Results");
        FooterText.Text = Localizer.T(language, "Footer");

        ControlsHelpText.ToolTip = Localizer.T(language, "HelpControls");
        AddEmojisToRewriteHelpText.ToolTip = Localizer.T(language, "HelpAddEmojisToRewrite");
        AutoPasteHelpText.ToolTip = Localizer.T(language, "HelpAutoPaste");
        KeepOllamaWarmHelpText.ToolTip = Localizer.T(language, "HelpKeepOllamaWarm");
        OpenAiHelpText.ToolTip = Localizer.T(language, "HelpOpenAi");
        OpenRouterHelpText.ToolTip = Localizer.T(language, "HelpOpenRouter");
        LocalTranscriptionHelpText.ToolTip = Localizer.T(language, "HelpLocalTranscription");
        OllamaHelpText.ToolTip = Localizer.T(language, "HelpOllama");
        AnthropicHelpText.ToolTip = Localizer.T(language, "HelpAnthropic");
        HotkeysHelpText.ToolTip = Localizer.T(language, "HelpHotkeys");
        PromptPresetsHelpText.ToolTip = Localizer.T(language, "HelpPromptPresets");
        CustomNamesHelpText.ToolTip = Localizer.T(language, "HelpCustomNames");
        WorkflowPromptsHelpText.ToolTip = Localizer.T(language, "HelpWorkflowPrompts");
        EmojiOptionHelpText.ToolTip = Localizer.T(language, "HelpEmojiOption");
        UpdatesHelpText.ToolTip = Localizer.T(language, "HelpUpdates");
        BackupHelpText.ToolTip = Localizer.T(language, "HelpBackup");
        ImportSettingsHelpText.ToolTip = Localizer.T(language, "HelpImportSettings");
        ResultsHelpText.ToolTip = Localizer.T(language, "HelpResults");
        SpokenTextHelpText.ToolTip = Localizer.T(language, "HelpSpokenText");
        RefreshPromptDetails();
    }

    private void SetActiveBadgeText(System.Windows.Controls.Label badge, AppLanguage language)
    {
        badge.Content = Localizer.T(language, "ActiveProvider");
        badge.ToolTip = Localizer.T(language, "ActiveProviderTooltip");
    }

    private void UpdateActiveProviderBadges()
    {
        OpenAiActiveBadge.Visibility = settings.RewriteProvider == RewriteProviderKind.OpenAI ? Visibility.Visible : Visibility.Collapsed;
        OpenRouterActiveBadge.Visibility = settings.RewriteProvider == RewriteProviderKind.OpenRouter ? Visibility.Visible : Visibility.Collapsed;
        OllamaActiveBadge.Visibility = settings.RewriteProvider == RewriteProviderKind.Ollama ? Visibility.Visible : Visibility.Collapsed;
        AnthropicActiveBadge.Visibility = settings.RewriteProvider == RewriteProviderKind.Anthropic ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyTheme()
    {
        var useDarkTheme = settings.AppTheme switch
        {
            AppTheme.Dark => true,
            AppTheme.Light => false,
            _ => IsSystemDarkTheme()
        };

        if (useDarkTheme)
        {
            SetThemeBrush("AppBackgroundBrush", "#0F172A");
            SetThemeBrush("CardBackgroundBrush", "#111827");
            SetThemeBrush("BorderBrushColor", "#334155");
            SetThemeBrush("PrimaryTextBrush", "#F8FAFC");
            SetThemeBrush("BodyTextBrush", "#CBD5E1");
            SetThemeBrush("SecondaryTextBrush", "#94A3B8");
            SetThemeBrush("MutedTextBrush", "#94A3B8");
            SetThemeBrush("InputBackgroundBrush", "#0B1220");
            SetThemeBrush("InputBorderBrush", "#475569");
            SetThemeBrush("NeutralButtonBrush", "#1F2937");
            SetThemeBrush("NeutralButtonHoverBrush", "#334155");
            SetThemeBrush("NativeControlTextBrush", "#111827");
            SetThemeBrush("StatusInfoBrush", "#60A5FA");
            SetThemeBrush("StatusSuccessBrush", "#22C55E");
            SetThemeBrush("StatusErrorBrush", "#F87171");
            SetThemeBrush("ActiveBadgeBackgroundBrush", "#14532D");
            SetThemeBrush("ActiveBadgeForegroundBrush", "#DCFCE7");
            SetThemeBrush("TooltipBackgroundBrush", "#E5E7EB");
            SetThemeBrush("TooltipForegroundBrush", "#111827");
            ResetStatusHighlight();
            return;
        }

        SetThemeBrush("AppBackgroundBrush", "#F5F7FB");
        SetThemeBrush("CardBackgroundBrush", "#FFFFFF");
        SetThemeBrush("BorderBrushColor", "#D9DEE8");
        SetThemeBrush("PrimaryTextBrush", "#14171F");
        SetThemeBrush("BodyTextBrush", "#3F4654");
        SetThemeBrush("SecondaryTextBrush", "#5A6170");
        SetThemeBrush("MutedTextBrush", "#6B7280");
        SetThemeBrush("InputBackgroundBrush", "#FFFFFF");
        SetThemeBrush("InputBorderBrush", "#AEB4BF");
        SetThemeBrush("NeutralButtonBrush", "#E5E7EB");
        SetThemeBrush("NeutralButtonHoverBrush", "#D1D5DB");
        SetThemeBrush("NativeControlTextBrush", "#14171F");
        SetThemeBrush("StatusInfoBrush", "#2563EB");
        SetThemeBrush("StatusSuccessBrush", "#16A34A");
        SetThemeBrush("StatusErrorBrush", "#DC2626");
        SetThemeBrush("ActiveBadgeBackgroundBrush", "#DCFCE7");
        SetThemeBrush("ActiveBadgeForegroundBrush", "#166534");
        SetThemeBrush("TooltipBackgroundBrush", "#111827");
        SetThemeBrush("TooltipForegroundBrush", "#FFFFFF");
        ResetStatusHighlight();
    }

    private void SetThemeBrush(string key, string color)
    {
        Resources[key] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
    }

    private static bool IsSystemDarkTheme()
    {
        try
        {
            using var personalizeKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return personalizeKey?.GetValue("AppsUseLightTheme") is int appsUseLightTheme && appsUseLightTheme == 0;
        }
        catch
        {
            return false;
        }
    }

    private void ScheduleAutoSave()
    {
        SaveStatusText.Text = Localizer.T(settings.AppLanguage, "AutoSaving");
        autoSaveTimer.Stop();
        autoSaveTimer.Start();
    }

    private void HighlightStatusChange(string message)
    {
        if (isLoading || StatusHighlightBorder is null)
        {
            return;
        }

        StatusHighlightBorder.BorderThickness = new Thickness(2);
        StatusHighlightBorder.BorderBrush = (System.Windows.Media.Brush)FindResource(GetStatusHighlightBrushKey(message));
        statusHighlightTimer.Stop();
        statusHighlightTimer.Start();
    }

    private void ResetStatusHighlight()
    {
        statusHighlightTimer.Stop();

        if (StatusHighlightBorder is null)
        {
            return;
        }

        StatusHighlightBorder.BorderThickness = new Thickness(1);
        StatusHighlightBorder.BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrushColor");
    }

    private static string GetStatusHighlightBrushKey(string message)
    {
        var normalized = message.ToLowerInvariant();
        if (normalized.Contains("fehl", StringComparison.Ordinal)
            || normalized.Contains("error", StringComparison.Ordinal)
            || normalized.Contains("nicht gefunden", StringComparison.Ordinal)
            || normalized.Contains("not found", StringComparison.Ordinal)
            || normalized.Contains("abgebrochen", StringComparison.Ordinal)
            || normalized.Contains("canceled", StringComparison.Ordinal)
            || normalized.Contains("failed", StringComparison.Ordinal))
        {
            return "StatusErrorBrush";
        }

        if (normalized.Contains(" ok", StringComparison.Ordinal)
            || normalized.StartsWith("ok", StringComparison.Ordinal)
            || normalized.Contains("gespeichert", StringComparison.Ordinal)
            || normalized.Contains("exportiert", StringComparison.Ordinal)
            || normalized.Contains("importiert", StringComparison.Ordinal)
            || normalized.Contains("kopiert", StringComparison.Ordinal)
            || normalized.Contains("eingefuegt", StringComparison.Ordinal)
            || normalized.Contains("saved", StringComparison.Ordinal)
            || normalized.Contains("copied", StringComparison.Ordinal)
            || normalized.Contains("inserted", StringComparison.Ordinal))
        {
            return "StatusSuccessBrush";
        }

        return "StatusInfoBrush";
    }

    private void RefreshOllamaModelOptions(IEnumerable<string> models)
    {
        isRefreshingOllamaModels = true;
        try
        {
            var selectedModel = OllamaRewriteModelCombo.Text.Trim();
            var options = models
                .Where(model => !string.IsNullOrWhiteSpace(model))
                .Append(selectedModel)
                .Where(model => !string.IsNullOrWhiteSpace(model))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(model => model, StringComparer.OrdinalIgnoreCase)
                .ToList();

            ollamaRewriteModels.Clear();
            foreach (var model in options)
            {
                ollamaRewriteModels.Add(model);
            }

            OllamaRewriteModelCombo.SelectedItem = ollamaRewriteModels.FirstOrDefault(model =>
                model.Equals(selectedModel, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            isRefreshingOllamaModels = false;
        }
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
        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            hotkeyService.RegisterWorkflowHotkeys(handle, GetWorkflowHotkeyOptions());
            isHotkeyReady = true;
            var message = Localizer.T(settings.AppLanguage, "HotkeysActive");
            HotkeyStatusText.Text = message;
            StatusText.Text = message;
        }
        catch (Exception ex)
        {
            isHotkeyReady = false;
            var message = UserErrorFormatter.Format(ex, settings.AppLanguage);
            HotkeyStatusText.Text = message;
            StatusText.Text = message;
        }
    }

    private IReadOnlyDictionary<WorkflowKind, HotkeyOption> GetWorkflowHotkeyOptions()
    {
        return new Dictionary<WorkflowKind, HotkeyOption>
        {
            [WorkflowKind.Transcribe] = HotkeyOptions.FindById(settings.TranscribeHotkeyId),
            [WorkflowKind.Improve] = HotkeyOptions.FindById(settings.ImproveHotkeyId),
            [WorkflowKind.Calm] = HotkeyOptions.FindById(settings.CalmHotkeyId)
        };
    }

    private static void MigrateLegacyEmojiWorkflow(AppSettings appSettings)
    {
        if (appSettings.DefaultWorkflow != WorkflowKind.Emojis)
        {
            return;
        }

        appSettings.DefaultWorkflow = WorkflowKind.Improve;
        appSettings.AddEmojisToRewrite = true;
    }

    private static string GetAppVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
    }
}
