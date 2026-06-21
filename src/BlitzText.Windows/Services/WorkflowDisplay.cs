using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public static class WorkflowDisplay
{
    public static readonly IReadOnlyList<DisplayOption<WorkflowKind>> Options =
    [
        new(WorkflowKind.Transcribe, "Nur transkribieren"),
        new(WorkflowKind.Improve, "Verbessern"),
        new(WorkflowKind.Calm, "Entschaerfen"),
    ];

    public static IReadOnlyList<DisplayOption<WorkflowKind>> GetOptions(AppLanguage language)
    {
        return language == AppLanguage.English
            ?
            [
                new(WorkflowKind.Transcribe, "Transcribe only"),
                new(WorkflowKind.Improve, "Improve"),
                new(WorkflowKind.Calm, "Calm down"),
            ]
            : Options;
    }

    public static string GetLabel(WorkflowKind workflow)
    {
        return Options.FirstOrDefault(option => option.Value == workflow)?.Label ?? workflow.ToString();
    }

    public static string GetLabel(WorkflowKind workflow, AppLanguage language)
    {
        if (workflow == WorkflowKind.Emojis)
        {
            return language == AppLanguage.English ? "Add emojis (legacy)" : "Emojis ergaenzen (alt)";
        }

        return GetOptions(language).FirstOrDefault(option => option.Value == workflow)?.Label ?? workflow.ToString();
    }

    public static DisplayOption<WorkflowKind> FindOption(WorkflowKind workflow)
    {
        return Options.FirstOrDefault(option => option.Value == workflow) ?? Options[0];
    }

    public static DisplayOption<WorkflowKind> FindOption(WorkflowKind workflow, AppLanguage language)
    {
        var options = GetOptions(language);
        return options.FirstOrDefault(option => option.Value == workflow) ?? options[0];
    }
}
