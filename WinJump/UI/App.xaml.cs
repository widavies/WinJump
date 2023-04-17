using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.FileProviders;
using WinJump.Core;

namespace WinJump.UI;

public partial class App {
    private TaskbarIcon? notifyIcon;
    private WinJumpManager? manager;

    public static readonly Mutex SINGLE_INSTANCE_MUTEX = new(true, "WinJump");

    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);

        if(!SINGLE_INSTANCE_MUTEX.WaitOne(TimeSpan.Zero, true)) {
            MessageBox.Show("WinJump is already running");
            Shutdown();
            return;
        }

        if(FindResource("NotifyIcon") is TaskbarIcon icon) {
            notifyIcon = icon;
        } else {
            throw new Exception("Could not find NotifyIcon");
        }

        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        // Preload icons
        var lightIcons = new Dictionary<uint, Icon>();
        var darkIcons = new Dictionary<uint, Icon>();

        for(uint i = 1; i <= 16; i++) {
            string fileName = i > 15 ? "15+.ico" : $"{i}.ico";

            var lightFileInfo = embeddedProvider.GetFileInfo("UI/Icons/Light/" + fileName);
            var darkFileInfo = embeddedProvider.GetFileInfo("UI/Icons/Dark/" + fileName);

            using var lightStream = lightFileInfo.CreateReadStream();
            using var darkStream = darkFileInfo.CreateReadStream();

            lightIcons.Add(i, new Icon(lightStream));
            darkIcons.Add(i, new Icon(darkStream));
        }

        manager = new WinJumpManager(
            (lightMode, desktopIcon) => {
                notifyIcon.Icon = lightMode ? darkIcons[desktopIcon + 1] : lightIcons[desktopIcon + 1];
            });
    }

    protected override void OnExit(ExitEventArgs e) {
        try {
            notifyIcon?.Dispose();
            manager?.Dispose();
            SINGLE_INSTANCE_MUTEX.ReleaseMutex();
        } catch(Exception) {
            // ignored
        }

        base.OnExit(e);
    }
}