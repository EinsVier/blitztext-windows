namespace BlitzText.Windows.Models;

public sealed record DisplayOption<T>(T Value, string Label)
{
    public override string ToString() => Label;
}
