using System.Collections.Specialized;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;
using WpfClipboard = System.Windows.Clipboard;
using WpfDataObject = System.Windows.DataObject;
using WpfTextDataFormat = System.Windows.TextDataFormat;

namespace BlitzText.Windows.Services;

public static class ClipboardPasteService
{
    private static readonly TimeSpan ClipboardRetryDelay = TimeSpan.FromMilliseconds(40);
    private const int ClipboardRetryCount = 8;

    public static void Copy(string text)
    {
        SetText(text);
    }

    public static async Task<bool> PasteTextPreservingClipboardAsync(string text, IntPtr targetWindowHandle)
    {
        var previousClipboard = CaptureClipboard();

        try
        {
            SetText(text);
            await PasteAsync(targetWindowHandle);
        }
        finally
        {
            await Task.Delay(850);
        }

        return await RestoreClipboardAsync(previousClipboard);
    }

    public static async Task PasteAsync(IntPtr targetWindowHandle)
    {
        await Task.Delay(250);

        if (TryPasteToFocusedEditorControl(targetWindowHandle))
        {
            return;
        }

        if (!SendCtrlV())
        {
            Forms.SendKeys.SendWait("^v");
        }
    }

    private static bool TryPasteToFocusedEditorControl(IntPtr targetWindowHandle)
    {
        if (targetWindowHandle == IntPtr.Zero)
        {
            return false;
        }

        var targetThreadId = GetWindowThreadProcessId(targetWindowHandle, out _);
        var currentThreadId = GetCurrentThreadId();
        var attached = false;

        try
        {
            if (targetThreadId != 0 && targetThreadId != currentThreadId)
            {
                attached = AttachThreadInput(currentThreadId, targetThreadId, attach: true);
            }

            var focusedHandle = GetFocus();
            if (focusedHandle == IntPtr.Zero || !IsChild(targetWindowHandle, focusedHandle))
            {
                return false;
            }

            if (!IsKnownTextEditorControl(focusedHandle))
            {
                return false;
            }

            SendMessage(focusedHandle, WindowMessagePaste, IntPtr.Zero, IntPtr.Zero);
            return true;
        }
        finally
        {
            if (attached)
            {
                AttachThreadInput(currentThreadId, targetThreadId, attach: false);
            }
        }
    }

    private static bool IsKnownTextEditorControl(IntPtr handle)
    {
        var className = GetClassName(handle);
        return className.Contains("Scintilla", StringComparison.OrdinalIgnoreCase)
            || className.Equals("Edit", StringComparison.OrdinalIgnoreCase)
            || className.Contains("RichEdit", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetClassName(IntPtr handle)
    {
        var builder = new System.Text.StringBuilder(256);
        var length = GetClassName(handle, builder, builder.Capacity);
        return length > 0 ? builder.ToString() : "";
    }

    private static bool SendCtrlV()
    {
        var inputs = new[]
        {
            CreateKeyboardInput(VirtualKeyControl, keyUp: false),
            CreateKeyboardInput(VirtualKeyV, keyUp: false),
            CreateKeyboardInput(VirtualKeyV, keyUp: true),
            CreateKeyboardInput(VirtualKeyControl, keyUp: true)
        };

        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == inputs.Length;
    }

    private static Input CreateKeyboardInput(ushort virtualKey, bool keyUp)
    {
        return new Input
        {
            type = InputKeyboard,
            union = new InputUnion
            {
                keyboard = new KeyboardInput
                {
                    wVk = virtualKey,
                    dwFlags = keyUp ? KeyEventKeyUp : 0
                }
            }
        };
    }

    private static ClipboardSnapshot CaptureClipboard()
    {
        try
        {
            var dataObject = CloneClipboardData();
            var hasData = dataObject.GetFormats(autoConvert: true).Length > 0;
            return new ClipboardSnapshot(dataObject, hasData, CanRestore: true);
        }
        catch
        {
            return new ClipboardSnapshot(null, HasData: false, CanRestore: false);
        }
    }

    private static WpfDataObject CloneClipboardData()
    {
        var clone = new WpfDataObject();
        var source = WpfClipboard.GetDataObject();
        if (source is null)
        {
            return clone;
        }

        CopyWellKnownClipboardFormats(clone);

        foreach (var format in source.GetFormats(autoConvert: false).Concat(source.GetFormats(autoConvert: true)).Distinct())
        {
            if (clone.GetDataPresent(format, autoConvert: false))
            {
                continue;
            }

            try
            {
                var data = source.GetData(format, autoConvert: false) ?? source.GetData(format, autoConvert: true);
                if (data is not null)
                {
                    clone.SetData(format, data);
                }
            }
            catch
            {
                // Some clipboard providers expose delayed or private formats that cannot be copied.
            }
        }

        return clone;
    }

    private static void CopyWellKnownClipboardFormats(WpfDataObject clone)
    {
        CopyTextFormat(clone, WpfTextDataFormat.Text);
        CopyTextFormat(clone, WpfTextDataFormat.UnicodeText);
        CopyTextFormat(clone, WpfTextDataFormat.Rtf);
        CopyTextFormat(clone, WpfTextDataFormat.Html);
        CopyTextFormat(clone, WpfTextDataFormat.CommaSeparatedValue);
        CopyImage(clone);
        CopyFileDropList(clone);
    }

    private static void CopyTextFormat(WpfDataObject clone, WpfTextDataFormat format)
    {
        try
        {
            if (WpfClipboard.ContainsText(format))
            {
                clone.SetText(WpfClipboard.GetText(format), format);
            }
        }
        catch
        {
            // Leave the rest of the clipboard snapshot intact.
        }
    }

    private static void CopyImage(WpfDataObject clone)
    {
        try
        {
            if (!WpfClipboard.ContainsImage())
            {
                return;
            }

            var image = WpfClipboard.GetImage();
            image?.Freeze();
            if (image is not null)
            {
                clone.SetImage(image);
            }
        }
        catch
        {
            // Leave the rest of the clipboard snapshot intact.
        }
    }

    private static void CopyFileDropList(WpfDataObject clone)
    {
        try
        {
            if (!WpfClipboard.ContainsFileDropList())
            {
                return;
            }

            var files = new StringCollection();
            foreach (var file in WpfClipboard.GetFileDropList().Cast<string>())
            {
                files.Add(file);
            }

            clone.SetFileDropList(files);
        }
        catch
        {
            // Leave the rest of the clipboard snapshot intact.
        }
    }

    private static async Task<bool> RestoreClipboardAsync(ClipboardSnapshot snapshot)
    {
        if (!snapshot.CanRestore)
        {
            return false;
        }

        try
        {
            await RetryClipboardOperationAsync(() =>
            {
                if (snapshot.HasData && snapshot.DataObject is not null)
                {
                    WpfClipboard.SetDataObject(snapshot.DataObject, copy: true);
                    return;
                }

                WpfClipboard.Clear();
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void SetText(string text)
    {
        RetryClipboardOperation(() => WpfClipboard.SetText(text));
    }

    private static void RetryClipboardOperation(Action action)
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt < ClipboardRetryCount; attempt++)
        {
            try
            {
                action();
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Thread.Sleep(ClipboardRetryDelay);
            }
        }

        throw lastException ?? new InvalidOperationException("Clipboard operation failed.");
    }

    private static async Task RetryClipboardOperationAsync(Action action)
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt < ClipboardRetryCount; attempt++)
        {
            try
            {
                action();
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                await Task.Delay(ClipboardRetryDelay);
            }
        }

        throw lastException ?? new InvalidOperationException("Clipboard operation failed.");
    }

    private sealed record ClipboardSnapshot(System.Windows.IDataObject? DataObject, bool HasData, bool CanRestore);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint numberOfInputs, Input[] inputs, int sizeOfInputStructure);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr windowHandle, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint currentThreadId, uint targetThreadId, bool attach);

    [DllImport("user32.dll")]
    private static extern IntPtr GetFocus();

    [DllImport("user32.dll")]
    private static extern bool IsChild(IntPtr parentHandle, IntPtr childHandle);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr windowHandle, System.Text.StringBuilder className, int maxCount);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint type;
        public InputUnion union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInput keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint WindowMessagePaste = 0x0302;
    private const ushort VirtualKeyControl = 0x11;
    private const ushort VirtualKeyV = 0x56;
}
