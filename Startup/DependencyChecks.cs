using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace AlbionDpsMeter.Startup;

/// <summary>
/// Outcome of a single dependency probe. <see cref="IsInstalled"/> drives the
/// pass/fail decision; the other fields are formatted into the user-facing
/// dialog when a check fails.
/// </summary>
internal sealed record DependencyResult(
    bool IsInstalled,
    string Name,
    string DownloadUrl,
    string MissingDetail,
    string FixInstructions);

/// <summary>
/// Individual dependency probes. Each method swallows IO/registry errors and
/// logs them through the supplied writer so locked-down machines still get
/// past the check via the file-probe fallback rather than crashing.
/// </summary>
internal static class DependencyChecks
{
    private const string AppRuntimeName = "Windows App Runtime 1.7";
    private const string NpcapName = "Npcap";
    private const string WpcapShimName = "Npcap WinPcap-compatible mode";

    public static DependencyResult CheckWindowsAppRuntime17(StreamWriter log)
    {
        const string regPath = @"SOFTWARE\Microsoft\WindowsAppRuntime\Deployment\Packages";
        const string namePrefix = "Microsoft.WindowsAppRuntime.1.7";

        bool foundInRegistry = false;
        try
        {
            using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = hklm.OpenSubKey(regPath);
            if (key is not null)
            {
                foundInRegistry = key.GetSubKeyNames()
                    .Any(n => n.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase));
                log.WriteLine($"[AppRuntime] registry probe HKLM\\{regPath}: {(foundInRegistry ? "match" : "no match")}");
            }
            else
            {
                log.WriteLine($"[AppRuntime] registry key HKLM\\{regPath} not present");
            }
        }
        catch (Exception ex)
        {
            log.WriteLine($"[AppRuntime] registry probe threw: {ex.GetType().Name}: {ex.Message}");
        }

        bool foundOnDisk = false;
        try
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "WindowsAppRuntime", "1.7", "Microsoft.WindowsAppRuntime.dll"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "WindowsAppRuntime", "1.7", "Microsoft.WindowsAppRuntime.dll"),
            };
            foreach (var c in candidates)
            {
                if (File.Exists(c))
                {
                    foundOnDisk = true;
                    log.WriteLine($"[AppRuntime] file probe found: {c}");
                    break;
                }
            }
            if (!foundOnDisk)
                log.WriteLine("[AppRuntime] file probe: no candidate found");
        }
        catch (Exception ex)
        {
            log.WriteLine($"[AppRuntime] file probe threw: {ex.GetType().Name}: {ex.Message}");
        }

        bool installed = foundInRegistry || foundOnDisk;
        log.WriteLine($"[AppRuntime] result: {(installed ? "installed" : "MISSING")}");

        return new DependencyResult(
            IsInstalled: installed,
            Name: AppRuntimeName,
            DownloadUrl: GetAppRuntimeDownloadUrl(),
            MissingDetail:
                "This app is built on WinUI 3 (unpackaged) and requires the Windows App Runtime 1.7 to be installed system-wide.",
            FixInstructions:
                "Click YES to download the official installer from Microsoft, then run it and relaunch this app.");
    }

    public static DependencyResult CheckNpcap(StreamWriter log)
    {
        bool foundInRegistry = false;
        try
        {
            using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            string[] regPaths = { @"SOFTWARE\WOW6432Node\Npcap", @"SOFTWARE\Npcap" };
            foreach (var p in regPaths)
            {
                using var key = hklm.OpenSubKey(p);
                if (key is not null)
                {
                    foundInRegistry = true;
                    log.WriteLine($"[Npcap] registry probe HKLM\\{p}: present");
                    break;
                }
            }
            if (!foundInRegistry)
                log.WriteLine("[Npcap] registry probe: no key found");
        }
        catch (Exception ex)
        {
            log.WriteLine($"[Npcap] registry probe threw: {ex.GetType().Name}: {ex.Message}");
        }

        bool foundOnDisk = false;
        try
        {
            string candidate = Path.Combine(Environment.SystemDirectory, "Npcap", "wpcap.dll");
            if (File.Exists(candidate))
            {
                foundOnDisk = true;
                log.WriteLine($"[Npcap] file probe found: {candidate}");
            }
            else
            {
                log.WriteLine($"[Npcap] file probe: {candidate} not present");
            }
        }
        catch (Exception ex)
        {
            log.WriteLine($"[Npcap] file probe threw: {ex.GetType().Name}: {ex.Message}");
        }

        bool installed = foundInRegistry || foundOnDisk;
        log.WriteLine($"[Npcap] result: {(installed ? "installed" : "MISSING")}");

        return new DependencyResult(
            IsInstalled: installed,
            Name: NpcapName,
            DownloadUrl: "https://npcap.com/#download",
            MissingDetail:
                "Npcap is required for packet capture. It does not appear to be installed on this machine.",
            FixInstructions:
                "Click YES to open the Npcap download page. During installation, make sure to check \"Install Npcap in WinPcap API-compatible Mode\".");
    }

    public static DependencyResult CheckWpcapShim(StreamWriter log)
    {
        bool found = false;
        try
        {
            string candidate = Path.Combine(Environment.SystemDirectory, "wpcap.dll");
            if (File.Exists(candidate))
            {
                found = true;
                log.WriteLine($"[WpcapShim] found: {candidate}");
            }
            else
            {
                log.WriteLine($"[WpcapShim] not present: {candidate}");
            }
        }
        catch (Exception ex)
        {
            log.WriteLine($"[WpcapShim] file probe threw: {ex.GetType().Name}: {ex.Message}");
        }

        log.WriteLine($"[WpcapShim] result: {(found ? "installed" : "MISSING")}");

        return new DependencyResult(
            IsInstalled: found,
            Name: WpcapShimName,
            DownloadUrl: "https://npcap.com/#download",
            MissingDetail:
                "Npcap is installed, but it is missing the WinPcap-compatible mode shim (C:\\Windows\\System32\\wpcap.dll). " +
                "The packet capture library used by this app needs that shim to function.",
            FixInstructions:
                "Click YES to reopen the Npcap download page. Reinstall Npcap and check \"Install Npcap in WinPcap API-compatible Mode\" during setup.");
    }

    private static string GetAppRuntimeDownloadUrl()
    {
        // Microsoft's aka.ms redirector resolves to the latest stable 1.7 installer
        // for the requested architecture.
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "https://aka.ms/windowsappsdk/1.7/latest/windowsappruntimeinstall-arm64.exe",
            Architecture.X86 => "https://aka.ms/windowsappsdk/1.7/latest/windowsappruntimeinstall-x86.exe",
            _ => "https://aka.ms/windowsappsdk/1.7/latest/windowsappruntimeinstall-x64.exe",
        };
    }
}
