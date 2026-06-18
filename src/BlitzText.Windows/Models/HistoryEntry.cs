using System.Text.Json.Serialization;

namespace BlitzText.Windows.Models;

public sealed class HistoryEntry
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public WorkflowKind Workflow { get; set; } = WorkflowKind.Improve;
    public string Text { get; set; } = "";
    public string SourceText { get; set; } = "";

    [JsonIgnore]
    public string Title => $"{CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}";

    [JsonIgnore]
    public string WorkflowLabel { get; set; } = "";

    [JsonIgnore]
    public string SourceForRewrite => string.IsNullOrWhiteSpace(SourceText) ? Text : SourceText;

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
