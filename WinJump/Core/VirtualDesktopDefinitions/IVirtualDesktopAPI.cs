using System;
using WinJump.Core.VirtualDesktopDefinitions.Windows11_22621_2215;

namespace WinJump.Core.VirtualDesktopDefinitions;

/// <summary>
/// This interface represents the essential functions that must be provided by a virtual desktop API for WinJump
/// to function. When reverse engineering the Windows virtual desktop API, you must provide definitions
/// for these functions.
/// </summary>
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

    /// <summary>
    /// Moves the currently focused window to the specified desktop.
    /// </summary>
    /// <param name="index"></param>
    void MoveFocusedWindowToDesktop(int index);

    /// <summary>
    /// Creates the appropriate virtual desktop API definition for the current Windows version.
    /// </summary>
    /// <returns>A virtual desktop API for the installed Windows version</returns>
    /// <exception cref="Exception">If the particular Windows version is unsupported</exception>
    public static IVirtualDesktopAPI Create() {
        WinVersion version = WinVersion.Determine();

        return version.Build switch {
            >= 22631 => version.ReleaseBuild >= 3085
                ? new Windows11_22631_3085.VirtualDesktopApi()
                : new Windows11_22621.VirtualDesktopApi(),
            // Work out the proper desktop wrapper
            >= 22621 => version.ReleaseBuild >= 2215
                ? new VirtualDesktopApi()
                : new Windows11_22621.VirtualDesktopApi(),
            >= 22000 => new Windows11_22000.VirtualDesktopApi(),
            >= 17763 => new Windows10_17763.VirtualDesktopApi(),
            // Just try the most recent as a last ditch effort
            _ => new VirtualDesktopApi()
        };
    }
}

public delegate void OnDesktopChanged(int desktop);
