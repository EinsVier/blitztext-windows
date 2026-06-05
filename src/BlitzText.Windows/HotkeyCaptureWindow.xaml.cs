using System.Windows;
using System.Windows.Input;
using BlitzText.Windows.Models;
using BlitzText.Windows.Services;

namespace BlitzText.Windows;

public partial class HotkeyCaptureWindow : Window
{
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;

    public HotkeyOption? CapturedHotkey { get; private set; }

    public HotkeyCaptureWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Keyboard.Focus(this);
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift)
        {
            return;
        }

        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        if (virtualKey <= 0)
        {
            DetectedText.Text = $"Nicht erkannt: {key}";
            return;
        }

        var modifiers = GetModifiers();
        var known = HotkeyOptions.FindBySignature(modifiers, (uint)virtualKey);
        if (known is null)
        {
            DetectedText.Text = $"Erkannt, aber nicht in der Liste: {FormatHotkey(modifiers, key)}";
            return;
        }

        CapturedHotkey = known;
        DetectedText.Text = $"Erkannt: {known.DisplayName}";
        DialogResult = true;
        Close();
    }

    private static uint GetModifiers()
    {
        uint modifiers = 0;
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            modifiers |= ModControl;
        }

        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            modifiers |= ModShift;
        }

        if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
        {
            modifiers |= ModAlt;
        }

        return modifiers;
    }

    private static string FormatHotkey(uint modifiers, Key key)
    {
        var parts = new List<string>();
        if ((modifiers & ModControl) != 0)
        {
            parts.Add("Ctrl");
        }

        if ((modifiers & ModShift) != 0)
        {
            parts.Add("Shift");
        }

        if ((modifiers & ModAlt) != 0)
        {
            parts.Add("Alt");
        }

        parts.Add(key.ToString());
        return string.Join("+", parts);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
