using System;
using Microsoft.Win32;

namespace WinJump.Core.VirtualDesktopDefinitions;

public interface IVirtualDesktopAPI : IDisposable {
    
    /// <summary>
    /// An event that notifies subscribers when the virtual desktop changes.
    /// </summary>
    event OnDesktopChanged OnDesktopChanged;

    /// <summary>
    /// Returns the current virtual desktop that the user is on.
    /// </summary>
    /// <returns>0-indexed, where '0' is the first desktop</returns>
    int GetCurrentDesktop();

    /// <summary>
    /// Jumps to the virtual desktop.
    /// </summary>
    /// <param name="index">0-indexed desktop number. If it is invalid it will be ignored.</param>
    void JumpToDesktop(int index);

    public static IVirtualDesktopAPI Create() {
        string? releaseId = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
            "CurrentBuildNumber", "")?.ToString();

        if(!int.TryParse(releaseId, out int buildNumber)) {
            throw new Exception("Unrecognized Windows version");
        }

        return buildNumber switch {
            // Work out the proper desktop wrapper
            >= 22621 => new Windows11_22621.VirtualDesktopApi(),
            >= 22000 => new Windows11_22000.VirtualDesktopApi(),
            >= 17763 => new Windows10_17763.VirtualDesktopApi(),
            _ => throw new Exception("Unsupported Windows version")
        };
    }
}

public delegate void OnDesktopChanged(int desktop);