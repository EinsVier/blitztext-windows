using System.Windows;
using Forms = System.Windows.Forms;

namespace BlitzText.Windows.Services;

public static class ClipboardPasteService
{
    public static void Copy(string text)
    {
        System.Windows.Clipboard.SetText(text);
    }

    public static async Task PasteAsync()
    {
        await Task.Delay(180);
        Forms.SendKeys.SendWait("^v");
    }
}
