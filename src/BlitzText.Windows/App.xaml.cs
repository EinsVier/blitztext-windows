using System.Windows;
using BlitzText.Windows.Services;
using Forms = System.Windows.Forms;

namespace BlitzText.Windows;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? notifyIcon;
    private MainWindow? mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        mainWindow = new MainWindow();
        notifyIcon = new Forms.NotifyIcon
        {
            Text = "BlitzText",
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? "") ?? System.Drawing.SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
        notifyIcon.DoubleClick += (_, _) => ShowMainWindow();

        ShowMainWindow();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DisposeTrayIcon();
        base.OnExit(e);
    }

    private Forms.ContextMenuStrip BuildTrayMenu()
    {
        var language = mainWindow?.CurrentAppLanguage ?? Models.AppLanguage.German;
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(language == Models.AppLanguage.English ? "Open BlitzText" : "BlitzText oeffnen", null, (_, _) => ShowMainWindow());
        menu.Items.Add(new Forms.ToolStripSeparator());

        foreach (var workflow in WorkflowDisplay.GetOptions(language))
        {
            var label = language == Models.AppLanguage.English ? $"Record: {workflow.Label}" : $"Aufnahme: {workflow.Label}";
            menu.Items.Add(label, null, async (_, _) =>
            {
                ShowMainWindow();

                if (mainWindow is not null)
                {
                    await mainWindow.ToggleWorkflowAsync(workflow.Value);
                }
            });
        }

        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(language == Models.AppLanguage.English ? "Exit" : "Beenden", null, (_, _) => ExitApplication());
        return menu;
    }

    private void ExitApplication()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(ExitApplication);
            return;
        }

        DisposeTrayIcon();
        mainWindow?.Close();
        mainWindow = null;
        Shutdown();
    }

    private void DisposeTrayIcon()
    {
        if (notifyIcon is null)
        {
            return;
        }

        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        notifyIcon = null;
    }

    private void ShowMainWindow()
    {
        if (mainWindow is null)
        {
            return;
        }

        mainWindow.Show();
        mainWindow.WindowState = WindowState.Normal;
        mainWindow.Activate();
    }
}
