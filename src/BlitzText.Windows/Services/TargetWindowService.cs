using System.Runtime.InteropServices;
using System.Text;

namespace BlitzText.Windows.Services;

public sealed class TargetWindowService
{
    private readonly Func<IntPtr> ownWindowHandleProvider;

    public TargetWindowService(Func<IntPtr> ownWindowHandleProvider)
    {
        this.ownWindowHandleProvider = ownWindowHandleProvider;
    }

    public TargetWindow CaptureActiveWindow()
    {
        var handle = GetForegroundWindow();
        if (handle == IntPtr.Zero || handle == ownWindowHandleProvider())
        {
            return new TargetWindow(IntPtr.Zero, "");
        }

        return new TargetWindow(handle, GetWindowTitle(handle));
    }

    public bool Activate(TargetWindow target)
    {
        if (!target.IsValid || !IsWindow(target.Handle))
        {
            return false;
        }

        if (IsIconic(target.Handle))
        {
            ShowWindow(target.Handle, ShowWindowRestore);
        }

        return SetForegroundWindow(target.Handle);
    }

    private static string GetWindowTitle(IntPtr handle)
    {
        var length = GetWindowTextLength(handle);
        if (length <= 0)
        {
            return "";
        }

        var builder = new StringBuilder(length + 1);
        GetWindowText(handle, builder, builder.Capacity);
        return builder.ToString();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    private const int ShowWindowRestore = 9;
}
