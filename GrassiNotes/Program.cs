using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace GrassiNotes;

public static class Program
{
    private const string MutexName = "Local\\GrassiNotes.SingleInstance.v2";
    private const string ShowEventName = "Local\\GrassiNotes.Show.v2";

    [STAThread]
    public static void Main(string[] args)
    {
        using var mutex = new Mutex(true, MutexName, out bool isFirstInstance);
        using var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        if (!isFirstInstance)
        {
            showEvent.Set();
            return;
        }

        bool startHidden = Array.Exists(args, a => string.Equals(a, "--startup", StringComparison.OrdinalIgnoreCase));
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        FontFamily appFont = FontLoader.Load();
        app.Resources["AppFont"] = appFont;
        app.Resources[SystemFonts.MessageFontFamilyKey] = appFont;

        var window = new MainWindow(startHidden, appFont);
        app.MainWindow = window;
        window.Initialize();

        ThreadPool.RegisterWaitForSingleObject(showEvent, (_, _) =>
        {
            try { app.Dispatcher.BeginInvoke(new Action(window.ShowFromTray)); }
            catch { }
        }, null, Timeout.Infinite, false);

        window.Show();
        if (startHidden) window.HideToTray(false);
        app.Run();
    }
}
