using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;

namespace WinJump.Core;

public delegate void ColorSchemeChanged(bool lightMode);

public delegate void ExplorerRestarted();

/// <summary>
/// ExplorerMonitor is responsible for:
/// - Starting/stopping explorer.exe (if necessary)
/// - Listening to system color scheme changes
/// - Listening for taskbar restart events (several notification registrations require explorer to be running)
///
/// Takes some inspiration from https://github.com/mntone/VirtualDesktop/tree/develop
/// </summary>
public class ExplorerMonitor : IDisposable {
    public event ColorSchemeChanged OnColorSchemeChanged {
        add {
            _onColorSchemeChanged += value;
            // Immediately notify the caller of the current light/dark mode
            value.Invoke(IsLightMode());
        }
        remove => _onColorSchemeChanged -= value;
    }

    public event ExplorerRestarted? OnExplorerRestarted;

    /*
     * Internal fields
     */

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string lpProcName);

    [DllImport("user32.dll")]
    private static extern bool CloseWindow(IntPtr hWnd);

    private event ColorSchemeChanged? _onColorSchemeChanged;
    private HwndSource _source { get; }
    private readonly uint _explorerRestartedMessage;

    private readonly AutoResetEvent taskbarCreatedEvent = new(false);

    
    public ExplorerMonitor() {
        _source = new HwndSource(new HwndSourceParameters("ExplorerMonitorWinJump") {
            Width = 1,
            Height = 1,
            WindowStyle = 0x800000
        });
        _source.AddHook(WndProc);
        _explorerRestartedMessage = RegisterWindowMessage("TaskbarCreated");
    }

    private static bool IsLightMode() {
        RegistryKey? startupApp = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false);

        object? val = startupApp?.GetValue("SystemUseLightTheme");

        bool lightMode = (int) (val ?? 0) == 1;
        return lightMode;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
        if(msg == _explorerRestartedMessage) {
            taskbarCreatedEvent.Set();

            OnExplorerRestarted?.Invoke();

            return IntPtr.Zero;
        }

        if(msg == 0x001A) {
            // https://stackoverflow.com/questions/51334674/how-to-detect-windows-10-light-dark-mode-in-win32-application
            _onColorSchemeChanged?.Invoke(IsLightMode());

            return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    public void Kill() {
        var killExplorer = Process.Start("cmd.exe", "/c taskkill /f /im explorer.exe");

        killExplorer.WaitForExit();
    }

    public void EnsureExplorerIsAlive() {
        var processes = Process.GetProcessesByName("explorer");

        if(processes.Length > 0 && !processes.Any(x => x.HasExited)) {
            return;
        }
        
        taskbarCreatedEvent.Reset();
        
        // If it's not, start it
        Process.Start(Environment.SystemDirectory + "\\..\\explorer.exe");

        taskbarCreatedEvent.WaitOne();
    }

    public void Dispose() {
        taskbarCreatedEvent.Dispose();
        _source.RemoveHook(WndProc);
        // Source could have been created on a different thread, which means we 
        // have to Dispose of it on the UI thread or it will crash.
        _source.Dispatcher?.BeginInvoke(DispatcherPriority.Send, (Action) (() => _source.Dispose()));
        CloseWindow(_source.Handle);
        GC.SuppressFinalize(this);
    }
}