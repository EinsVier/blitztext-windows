using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int HotkeyBaseId = 9001;
    private const int WmHotkey = 0x0312;
    private const int WhMouseLl = 14;
    private const int WmMButtonUp = 0x0208;

    private HwndSource? source;
    private IntPtr windowHandle;
    private IntPtr mouseHookHandle;
    private readonly LowLevelMouseProc mouseProc;
    private readonly Dictionary<int, WorkflowKind> registeredWorkflowHotkeys = [];
    private DateTimeOffset lastMouseTrigger = DateTimeOffset.MinValue;
    private bool isHookAttached;

    public event EventHandler? Pressed;
    public event EventHandler<WorkflowHotkeyPressedEventArgs>? WorkflowPressed;

    public GlobalHotkeyService()
    {
        mouseProc = MouseHookCallback;
    }

    public void Register(IntPtr handle, HotkeyOption option)
    {
        windowHandle = handle;
        source = HwndSource.FromHwnd(handle);
        if (!isHookAttached)
        {
            source.AddHook(WndProc);
            isHookAttached = true;
        }

        UnregisterAllKeyboardHotkeys();
        UnregisterMouseHook();

        if (option.IsMiddleMouse)
        {
            RegisterMouseHook(option);
            return;
        }

        if (!RegisterHotKey(windowHandle, HotkeyBaseId, option.Modifiers, option.Key))
        {
            throw new InvalidOperationException($"Could not register global hotkey {option.DisplayName}.");
        }
    }

    public void RegisterWorkflowHotkeys(IntPtr handle, IReadOnlyDictionary<WorkflowKind, HotkeyOption> options)
    {
        windowHandle = handle;
        source = HwndSource.FromHwnd(handle);
        if (!isHookAttached)
        {
            source.AddHook(WndProc);
            isHookAttached = true;
        }

        UnregisterAllKeyboardHotkeys();
        UnregisterMouseHook();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;
        foreach (var pair in options)
        {
            var option = pair.Value;
            if (option.IsMiddleMouse)
            {
                throw new InvalidOperationException("Middle mouse cannot be used for workflow-specific hotkeys.");
            }

            if (!seen.Add(option.Id))
            {
                throw new InvalidOperationException($"Duplicate hotkey selected: {option.DisplayName}.");
            }

            var hotkeyId = HotkeyBaseId + index;
            if (!RegisterHotKey(windowHandle, hotkeyId, option.Modifiers, option.Key))
            {
                throw new InvalidOperationException($"Could not register global hotkey {option.DisplayName}.");
            }

            registeredWorkflowHotkeys[hotkeyId] = pair.Key;
            index++;
        }
    }

    public void PauseKeyboardHotkeys()
    {
        UnregisterAllKeyboardHotkeys();
    }

    public void Dispose()
    {
        if (windowHandle != IntPtr.Zero)
        {
            UnregisterAllKeyboardHotkeys();
        }

        UnregisterMouseHook();

        if (isHookAttached)
        {
            source?.RemoveHook(WndProc);
            isHookAttached = false;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && registeredWorkflowHotkeys.TryGetValue(wParam.ToInt32(), out var workflow))
        {
            WorkflowPressed?.Invoke(this, new WorkflowHotkeyPressedEventArgs(workflow));
            handled = true;
        }
        else if (msg == WmHotkey && wParam.ToInt32() == HotkeyBaseId)
        {
            Pressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void RegisterMouseHook(HotkeyOption option)
    {
        using var currentProcess = Process.GetCurrentProcess();
        using var currentModule = currentProcess.MainModule;
        var moduleHandle = currentModule?.ModuleName is { Length: > 0 } moduleName
            ? GetModuleHandle(moduleName)
            : IntPtr.Zero;

        mouseHookHandle = SetWindowsHookEx(WhMouseLl, mouseProc, moduleHandle, 0);
        if (mouseHookHandle == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Could not register global mouse trigger {option.DisplayName}. Windows error: {error}.");
        }
    }

    private void UnregisterMouseHook()
    {
        if (mouseHookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(mouseHookHandle);
            mouseHookHandle = IntPtr.Zero;
        }
    }

    private void UnregisterAllKeyboardHotkeys()
    {
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        UnregisterHotKey(windowHandle, HotkeyBaseId);
        foreach (var hotkeyId in registeredWorkflowHotkeys.Keys.ToArray())
        {
            UnregisterHotKey(windowHandle, hotkeyId);
        }

        registeredWorkflowHotkeys.Clear();
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam.ToInt32() == WmMButtonUp && IsMouseTriggerReady())
        {
            Pressed?.Invoke(this, EventArgs.Empty);
        }

        return CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);
    }

    private bool IsMouseTriggerReady()
    {
        var now = DateTimeOffset.UtcNow;
        if (now - lastMouseTrigger < TimeSpan.FromMilliseconds(350))
        {
            return false;
        }

        lastMouseTrigger = now;
        return true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
}
