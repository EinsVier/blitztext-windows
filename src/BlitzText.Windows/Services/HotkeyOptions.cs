using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public static class HotkeyOptions
{
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint VkSpace = 0x20;
    private const uint VkA = 0x41;
    private const uint VkB = 0x42;
    private const uint VkC = 0x43;
    private const uint VkD = 0x44;
    private const uint VkM = 0x4D;
    private const uint VkR = 0x52;
    private const uint VkT = 0x54;
    private const uint VkF6 = 0x75;
    private const uint VkF7 = 0x76;
    private const uint VkF8 = 0x77;
    private const uint VkF9 = 0x78;
    private const uint VkF10 = 0x79;
    private const uint VkF11 = 0x7A;
    private const uint VkF12 = 0x7B;
    private const uint VkBrowserHome = 0xAC;

    public static readonly IReadOnlyList<HotkeyOption> All =
    [
        new("ctrl-alt-space", "Ctrl+Alt+Space", ModControl | ModAlt, VkSpace),
        new("ctrl-shift-space", "Ctrl+Shift+Space", ModControl | ModShift, VkSpace),
        new("ctrl-alt-a", "Ctrl+Alt+A", ModControl | ModAlt, VkA),
        new("ctrl-alt-b", "Ctrl+Alt+B", ModControl | ModAlt, VkB),
        new("ctrl-alt-c", "Ctrl+Alt+C", ModControl | ModAlt, VkC),
        new("ctrl-alt-d", "Ctrl+Alt+D", ModControl | ModAlt, VkD),
        new("ctrl-alt-m", "Ctrl+Alt+M", ModControl | ModAlt, VkM),
        new("ctrl-alt-r", "Ctrl+Alt+R", ModControl | ModAlt, VkR),
        new("ctrl-alt-t", "Ctrl+Alt+T", ModControl | ModAlt, VkT),
        new("ctrl-shift-a", "Ctrl+Shift+A", ModControl | ModShift, VkA),
        new("ctrl-shift-b", "Ctrl+Shift+B", ModControl | ModShift, VkB),
        new("ctrl-shift-d", "Ctrl+Shift+D", ModControl | ModShift, VkD),
        new("ctrl-shift-m", "Ctrl+Shift+M", ModControl | ModShift, VkM),
        new("ctrl-shift-r", "Ctrl+Shift+R", ModControl | ModShift, VkR),
        new("ctrl-shift-t", "Ctrl+Shift+T", ModControl | ModShift, VkT),
        new("ctrl-alt-shift-space", "Ctrl+Alt+Shift+Space", ModControl | ModAlt | ModShift, VkSpace),
        new("ctrl-alt-shift-b", "Ctrl+Alt+Shift+B", ModControl | ModAlt | ModShift, VkB),
        new("ctrl-alt-shift-d", "Ctrl+Alt+Shift+D", ModControl | ModAlt | ModShift, VkD),
        new("ctrl-alt-shift-r", "Ctrl+Alt+Shift+R", ModControl | ModAlt | ModShift, VkR),
        new("ctrl-f6", "Ctrl+F6", ModControl, VkF6),
        new("ctrl-f7", "Ctrl+F7", ModControl, VkF7),
        new("ctrl-f8", "Ctrl+F8", ModControl, VkF8),
        new("ctrl-f9", "Ctrl+F9", ModControl, VkF9),
        new("ctrl-f10", "Ctrl+F10", ModControl, VkF10),
        new("ctrl-f11", "Ctrl+F11", ModControl, VkF11),
        new("ctrl-f12", "Ctrl+F12", ModControl, VkF12),
        new("ctrl-shift-f6", "Ctrl+Shift+F6", ModControl | ModShift, VkF6),
        new("ctrl-shift-f7", "Ctrl+Shift+F7", ModControl | ModShift, VkF7),
        new("ctrl-shift-f8", "Ctrl+Shift+F8", ModControl | ModShift, VkF8),
        new("ctrl-shift-f9", "Ctrl+Shift+F9", ModControl | ModShift, VkF9),
        new("ctrl-shift-f10", "Ctrl+Shift+F10", ModControl | ModShift, VkF10),
        new("ctrl-shift-f11", "Ctrl+Shift+F11", ModControl | ModShift, VkF11),
        new("ctrl-shift-f12", "Ctrl+Shift+F12", ModControl | ModShift, VkF12),
        new("ctrl-alt-shift-f8", "Ctrl+Alt+Shift+F8", ModControl | ModAlt | ModShift, VkF8),
        new("ctrl-alt-shift-f9", "Ctrl+Alt+Shift+F9", ModControl | ModAlt | ModShift, VkF9),
        new("ctrl-alt-shift-f10", "Ctrl+Alt+Shift+F10", ModControl | ModAlt | ModShift, VkF10),
        new("ctrl-alt-shift-f12", "Ctrl+Alt+Shift+F12", ModControl | ModAlt | ModShift, VkF12),
        new("browser-home", "Browser Home", 0, VkBrowserHome),
        new("shift-browser-home", "Shift+Browser Home", ModShift, VkBrowserHome),
        new("alt-browser-home", "Alt+Browser Home", ModAlt, VkBrowserHome),
        new("ctrl-browser-home", "Ctrl+Browser Home", ModControl, VkBrowserHome),
        new("ctrl-shift-browser-home", "Ctrl+Shift+Browser Home", ModControl | ModShift, VkBrowserHome),
        new("ctrl-alt-browser-home", "Ctrl+Alt+Browser Home", ModControl | ModAlt, VkBrowserHome),
        new("middle-mouse", "Middle Mouse Button (experimental)", 0, 0, true),
    ];

    public static HotkeyOption Default => All[0];

    public static IReadOnlyList<HotkeyOption> KeyboardOnly => All.Where(option => !option.IsMiddleMouse).ToArray();

    public static HotkeyOption FindById(string? id)
    {
        return All.FirstOrDefault(option => option.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) ?? Default;
    }

    public static HotkeyOption? FindBySignature(uint modifiers, uint key)
    {
        return KeyboardOnly.FirstOrDefault(option => option.Modifiers == modifiers && option.Key == key);
    }
}
