using System.Text.Json.Serialization;

namespace BlitzText.Windows.Models;

public sealed class HistoryEntry
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public WorkflowKind Workflow { get; set; } = WorkflowKind.Improve;
    public string Text { get; set; } = "";

    [JsonIgnore]
    public string Title => $"{CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm} - {WorkflowLabel}";

    [JsonIgnore]
    public string WorkflowLabel => Workflow switch
    {
        WorkflowKind.Transcribe => "Nur transkribieren",
        WorkflowKind.Improve => "Verbessern",
        WorkflowKind.Calm => "Entschaerfen",
        WorkflowKind.Emojis => "Emojis ergaenzen",
        _ => Workflow.ToString()
    };

    [JsonIgnore]
    public string Preview
    {
        get
        {
            var compactText = Text.ReplaceLineEndings(" ").Trim();
            return compactText.Length <= 72 ? compactText : compactText[..72] + "...";
        }
    }
}
