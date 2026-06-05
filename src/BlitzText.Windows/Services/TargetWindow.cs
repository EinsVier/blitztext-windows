namespace BlitzText.Windows.Services;

public sealed record TargetWindow(IntPtr Handle, string Title)
{
    public bool IsValid => Handle != IntPtr.Zero;
}
