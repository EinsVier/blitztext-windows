using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public sealed class WorkflowHotkeyPressedEventArgs(WorkflowKind workflow) : EventArgs
{
    public WorkflowKind Workflow { get; } = workflow;
}
