using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AlbionDpsMeter.Startup;

/// <summary>
/// Runs at the very top of <see cref="App.Main"/>, before any WinUI / WinRT
/// call. If a required dependency is missing it shows a native Win32 dialog
/// and terminates the process. If everything checks out, returns silently
/// and lets the normal startup continue.
/// </summary>
public static class PreLaunchValidator
{
    private const string DialogCaption = "Albion DPS Meter — Missing Dependency";
    private const string LogPath = "logs/prelaunch.log";

    /// <summary>
    /// Runs all dependency checks. On the first failure, surfaces a Yes/No
    /// dialog ("YES" opens the download URL in the default browser) and then
    /// calls <see cref="Environment.Exit(int)"/> with code 1. Returns
    /// normally when every dependency is present.
    /// </summary>
    public static void ValidateOrExit()
    {
        StreamWriter? log = null;
        try
        {
            log = OpenLog();
            log.WriteLine();
            log.WriteLine($"=== Pre-launch validation @ {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz} ===");

            var checks = new List<DependencyResult>
            {
                DependencyChecks.CheckWindowsAppRuntime17(log),
                DependencyChecks.CheckNpcap(log),
                DependencyChecks.CheckWpcapShim(log),
            };

            foreach (var check in checks)
            {
                if (!check.IsInstalled)
                {
                    log.WriteLine($"FAIL: {check.Name} missing — surfacing dialog");
                    log.Flush();
                    HandleMissingDependency(check, log);
                    // HandleMissingDependency always exits the process; this
                    // line is only reached if Environment.Exit was somehow
                    // suppressed, in which case we still abort startup.
                    return;
                }
            }

            log.WriteLine("PASS: all dependencies present");
        }
        catch (Exception ex)
        {
            // Validation itself must never crash the app silently. Log what we
            // can and let the normal Main() flow continue — the existing
            // reactive error handling will take over if anything is actually
            // broken.
            try { log?.WriteLine($"ERROR: validator threw {ex.GetType().Name}: {ex.Message}"); }
            catch { /* nothing we can do */ }
        }
        finally
        {
            log?.Flush();
            log?.Dispose();
        }
    }

    private static void HandleMissingDependency(DependencyResult result, StreamWriter log)
    {
        string text =
            $"{result.Name} is required but was not detected.\n\n" +
            $"{result.MissingDetail}\n\n" +
            $"{result.FixInstructions}\n\n" +
            $"Download URL: {result.DownloadUrl}";

        bool yes = NativeMessageBox.ShowYesNo(text, DialogCaption);

        if (yes)
        {
            log.WriteLine($"User chose YES — opening {result.DownloadUrl}");
            try
            {
                Process.Start(new ProcessStartInfo(result.DownloadUrl)
                {
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                log.WriteLine($"Process.Start failed: {ex.GetType().Name}: {ex.Message}");
            }
        }
        else
        {
            log.WriteLine("User dismissed dialog — exiting");
        }

        log.Flush();
        Environment.Exit(1);
    }

    private static StreamWriter OpenLog()
    {
        try
        {
            Directory.CreateDirectory("logs");
        }
        catch
        {
            // If logs dir cannot be created, fall back to writing next to the
            // exe so we always leave a paper trail.
        }

        var stream = new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        return new StreamWriter(stream) { AutoFlush = true };
    }
}
