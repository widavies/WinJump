using System;
using Microsoft.Win32;

namespace WinJump.Core;

public class WinVersion {
    public required int Build;
    public required int ReleaseBuild;

    private WinVersion() {
    }

    public static WinVersion Determine() {
        OperatingSystem osInfo = Environment.OSVersion;

        string? releaseBuild;
        
        // https://stackoverflow.com/a/13729137/4779937
        if(Environment.Is64BitOperatingSystem) {
            using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            releaseBuild = key?.GetValue("UBR")?.ToString();
        } else {
            releaseBuild = Registry.LocalMachine.OpenSubKey("SOFTWARE")?.OpenSubKey("Microsoft")?
                .OpenSubKey("Windows NT")?.OpenSubKey("CurrentVersion")?.GetValue("UBR")?.ToString();
        }

        if(!int.TryParse(releaseBuild, out int releaseBuildNumber)) {
            throw new Exception($"Unrecognized Windows build version {osInfo.Version.Build}.{releaseBuild}");
        }

        return new WinVersion {
            Build = osInfo.Version.Build,
            ReleaseBuild = releaseBuildNumber
        };
    }
}