using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace GrassiNotes;

public static class StartupService
{
    public static void EnsureEnabled()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
            if (key == null) return;
            string? baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string localLauncher = Path.Combine(baseDir, "GrassiNotes.exe");
            string parentLauncher = Path.Combine(Directory.GetParent(baseDir)?.FullName ?? baseDir, "GrassiNotes.exe");
            string? executable = File.Exists(localLauncher) ? localLauncher : File.Exists(parentLauncher) ? parentLauncher : Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(executable)) key.SetValue("GrassiNotes", "\"" + executable + "\" --startup");
        }
        catch { }
    }
}
