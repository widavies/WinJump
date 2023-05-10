using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using WinJump.Core.VirtualDesktopDefinitions;
using WinJump.UI;

namespace WinJump.Core;

public delegate void DesktopChanged(bool lightMode, uint desktopNum);

/// <summary>
/// Ties everything together.
///
/// - Loads config file & registers listeners for the shortcuts
/// - Calls VirtualDesktop API to jump to desktops/get notified when they change
/// - Manages ExplorerManager to make sure that explorer.exe's lifecycle is properly controlled
///   and that color scheme / taskbar created events are handled.
/// </summary>
public class WinJumpManager : IDisposable {
    /*
     * Internal fields
     */

    private readonly STAThread? _thread; // tied exactly to the lifecycle of explorer.exe
    private readonly ExplorerMonitor _explorerMonitor = new();
    private readonly KeyboardHook _keyboardHook = new();
    private bool _lightMode { get; set; }
    private uint _currentDesktop { get; set; }
    private uint? _lastDesktop { get; set; }

    public WinJumpManager(DesktopChanged desktopChanged) {
        // Load config file
        var config = Config.Load();

        // Attempt to register the shortcuts. 

        var registerShortcuts = new List<Func<bool>> {
            () => {
                return config.JumpTo.Select(t => t.Shortcut).All(shortcut =>
                    _keyboardHook.RegisterHotKey(shortcut.ModifierKeys, shortcut.Keys));
            },
            () => {
                return config.ToggleGroups.Select(t => t.Shortcut).All(shortcut =>
                    _keyboardHook.RegisterHotKey(shortcut.ModifierKeys, shortcut.Keys));
            }
        };

        if(!registerShortcuts.All(x => x.Invoke())) {
            // If we failed to register the shortcuts, we need to kill explorer and try again
            // and restart it after we've registered the shortcuts
            _explorerMonitor.Kill();
            if(!registerShortcuts.All(x => x.Invoke())) {
                // Still didn't work, exit out (make sure to restart explorer before doing so)
                _explorerMonitor.EnsureExplorerIsAlive();
                Application.Current.Shutdown();
                return;
            }
        }

        // Add handler for hotkey press events
        _keyboardHook.KeyPressed += (_, args) => {
            Shortcut pressed = new Shortcut {
                ModifierKeys = args.Modifier,
                Keys = args.Key
            };

            // First, scan for jump to shortcuts
            JumpTo? jumpTo = config.JumpTo.FirstOrDefault(x => x.Shortcut.IsEqual(pressed));

            if(jumpTo != null) {
                if(config.JumpCurrentGoesToLast && _lastDesktop != null) {
                    _thread?.JumpTo(jumpTo.Desktop - 1, _lastDesktop.Value);
                } else {
                    _thread?.JumpTo(jumpTo.Desktop - 1);
                }

                return;
            }

            ToggleGroup? toggleGroup = config.ToggleGroups.FirstOrDefault(x => x.Shortcut.IsEqual(pressed));

            if(toggleGroup != null) {
                _thread?.JumpToNext(toggleGroup.Desktops.Select(x => x - 1).ToArray());
            }
        };

        // We MUST wait for explorer to be online in order to register desktop change notification service
        _explorerMonitor.EnsureExplorerIsAlive();

        _thread = new STAThread(desktop => {
            _lastDesktop = _currentDesktop;
            _currentDesktop = desktop;
            desktopChanged(_lightMode, desktop);
        });

        // This particular event fires immediately on initial register
        _explorerMonitor.OnColorSchemeChanged += lightMode => {
            _lightMode = lightMode;
            desktopChanged.Invoke(lightMode, (uint) _thread.GetCurrentDesktop());
        };

        _explorerMonitor.OnExplorerRestarted += () => {
            try {
                App.SINGLE_INSTANCE_MUTEX.ReleaseMutex();
            } catch(Exception) {
                // ignored
            }

            string? currentExecutablePath = Process.GetCurrentProcess().MainModule?.FileName;

            if(currentExecutablePath == null) return;

            Process.Start(currentExecutablePath);
            Application.Current.Shutdown();
        };
    }

    public void Dispose() {
        _thread?.Dispose();
        _explorerMonitor.Dispose();
        _keyboardHook.Dispose();
        GC.SuppressFinalize(this);
    }
}

// Credit https://stackoverflow.com/a/21684059/4779937
/// <summary>
/// This is a small wrapper utility that calls the IVirtualDesktop API.
/// Any calls to this API must be from an STAThread for it to work.
/// </summary>
internal sealed class STAThread : IDisposable {
    private SynchronizationContext? ctx;
    private readonly ManualResetEvent mre;
    private readonly IVirtualDesktopAPI api = IVirtualDesktopAPI.Create();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetTopWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    public STAThread(Action<uint> DesktopChanged) {
        using(mre = new ManualResetEvent(false)) {
            var thread = new Thread(() => {
                System.Windows.Forms.Application.Idle += Initialize;
                System.Windows.Forms.Application.Run();
            }) {
                IsBackground = true
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            mre.WaitOne();

            api.OnDesktopChanged += desktop => { DesktopChanged((uint) desktop); };
        }
    }

    // public void BeginInvoke(Delegate dlg, params Object[] args) {
    //     if (ctx == null) throw new ObjectDisposedException("STAThread");
    //     ctx.Post(_ => dlg.DynamicInvoke(args), null);
    // }

    public int GetCurrentDesktop() {
        if(ctx == null) throw new ObjectDisposedException("STAThread");

        var blocks = new BlockingCollection<int>();

        return WrapCall(() => { blocks.Add(api.GetCurrentDesktop()); }) ? blocks.Take() : 0;
    }

    // index must be 0-indexed
    public void JumpTo(uint index, uint fallback) {
        WrapCall(() => {
            if(api.GetCurrentDesktop() == index) {
                // Go to last desktop instead
                index = fallback;
            }

            api.JumpToDesktop((int) index);
            
            // Hackish way to fix kind of annoying problem where
            // focus doesn't always come along with a desktop change
            IntPtr t = GetForegroundWindow();
            
            if(t != IntPtr.Zero && IsWindowVisible(t)) {
                SetForegroundWindow(t);
            }
        });
    }

    public void JumpTo(uint index) {
        WrapCall(() => {
            api.JumpToDesktop((int) index);
            
            // Hackish way to fix kind of annoying problem where
            // focus doesn't always come along with a desktop change
            IntPtr t = GetForegroundWindow();
            
            if(t != IntPtr.Zero && IsWindowVisible(t)) {
                SetForegroundWindow(t);
            }
        });
    }

    // desktops must be 0-indexed
    public void JumpToNext(int[] desktops) {
        if(ctx == null) throw new ObjectDisposedException("STAThread");

        WrapCall(() => {
            int index = Array.FindIndex(desktops, x => x == api.GetCurrentDesktop());
            if(index < 0) index = 0;
            int next = desktops[(index + 1) % desktops.Length];

            api.JumpToDesktop(next);
            
            // Hackish way to fix kind of annoying problem where
            // focus doesn't always come along with a desktop change
            IntPtr t = GetForegroundWindow();
            
            if(t != IntPtr.Zero && IsWindowVisible(t)) {
                SetForegroundWindow(t);
            }
        });
    }

    private void Initialize(object? sender, EventArgs e) {
        ctx = SynchronizationContext.Current;
        mre.Set();
        System.Windows.Forms.Application.Idle -= Initialize;
    }

    private bool WrapCall(Action action) {
        if(ctx == null) throw new ObjectDisposedException("STAThread");

        var result = new BlockingCollection<bool>();

        ctx.Send(_ => {
            try {
                action.Invoke();
                result.Add(true);
            } catch(COMException) {
                // Specifically ignored. If explorer.exe dies these calls will start failing.
                // This application can't work when explorer.exe is dead. It will just sit around in a zombie
                // state until it detects that explorer.exe is back up and running at which point it will
                // restart itself.
                result.Add(false);
            }
        }, null);

        return result.TryTake(out bool ret, TimeSpan.FromSeconds(5)) && ret;
    }

    public void Dispose() {
        if(ctx == null) return;

        WrapCall(() => {
            api.Dispose();
            System.Windows.Forms.Application.ExitThread();
        });
        ctx = null;
    }
}