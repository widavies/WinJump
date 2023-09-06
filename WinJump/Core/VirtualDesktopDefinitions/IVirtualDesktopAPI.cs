﻿using System;
using Microsoft.Win32;

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
    /// Creates the appropriate virtual desktop API definition for the current Windows version.
    /// </summary>
    /// <returns>A virtual desktop API for the installed Windows version</returns>
    /// <exception cref="Exception">If the particular Windows version is unsupported</exception>
    public static IVirtualDesktopAPI Create() {
        string? releaseId = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
            "CurrentBuildNumber", "")?.ToString();

        string? releaseBuild = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
            "UBR", "")?.ToString();
        
        if(!int.TryParse(releaseId, out int releaseIdNumber)) {
            throw new Exception("Unrecognized Windows release id version");
        }

        if(!int.TryParse(releaseBuild, out int releaseBuildNumber)) {
            throw new Exception("Unrecognized Windows build version");
        }

        return releaseIdNumber switch {
            // Work out the proper desktop wrapper
            >= 22621 => releaseBuildNumber >= 2215
                ? new Windows11_22621_2215.VirtualDesktopApi()
                : new Windows11_22621.VirtualDesktopApi(),
            >= 22000 => new Windows11_22000.VirtualDesktopApi(),
            >= 17763 => new Windows10_17763.VirtualDesktopApi(),
            _ => throw new Exception($"Unsupported Windows version {releaseId}.{releaseBuildNumber}")
        };
    }
}

public delegate void OnDesktopChanged(int desktop);