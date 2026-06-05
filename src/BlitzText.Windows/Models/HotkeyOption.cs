namespace BlitzText.Windows.Models;

public sealed record HotkeyOption(string Id, string DisplayName, uint Modifiers, uint Key, bool IsMiddleMouse = false)
{
    public override string ToString() => DisplayName;
}
