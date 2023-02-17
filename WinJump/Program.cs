using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using VirtualDesktop.VirtualDesktop;

namespace WinJump {
    internal static class Program {
        [STAThread]
        public static void Main() {
            // Register win jump to launch at startup
            // The path to the key where Windows looks for startup applications
            RegistryKey startupApp = Registry.CurrentUser.OpenSubKey(
                                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            
            //Path to launch shortcut
            string startPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs) 
                               + @"\WinJump\WinJump.appref-ms";
            
            startupApp?.SetValue("WinJump", startPath);
            
            // Load config file
            var config = Config.FromFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".winjump"));

            // Because windows_key + number is already a bulit in Windows shortcut, we need to kill explorer
            // (explorer is the process that registers the shortcut) so that it releases the shortcut.
            // Then, we can register it and restart explorer.
            var killExplorer = Process.Start("cmd.exe", "/c taskkill /f /im explorer.exe");

            killExplorer?.WaitForExit();

            var thread = new STAThread();

            // Start a thread that can handle UI requests
            KeyboardHook hook = new KeyboardHook();

            hook.KeyPressed += (sender, args) => {
                Shortcut pressed = new Shortcut {
                    ModifierKeys = args.Modifier,
                    Keys = args.Key
                };

                // First, scan for jump to shortcuts
                JumpTo jumpTo = config.JumpTo.FirstOrDefault(x => x.Shortcut.IsEqual(pressed));

                if (jumpTo != null) {
                    thread.JumpTo(jumpTo.Desktop - 1);
                    return;
                }
                
                ToggleGroup toggleGroup = config.ToggleGroups.FirstOrDefault(x => x.Shortcut.IsEqual(pressed));

                if (toggleGroup != null) {
                    thread.JumpToNext(toggleGroup.Desktops.Select(x => x - 1).ToArray());
                }
            };

            // Register the shortcuts
            foreach (var shortcut in config.JumpTo.Select(t => t.Shortcut)) {
                hook.RegisterHotKey(shortcut.ModifierKeys, shortcut.Keys);
            }
            
            // Register the toggle groups
            foreach (var shortcut in config.ToggleGroups.Select(t => t.Shortcut)) {
                hook.RegisterHotKey(shortcut.ModifierKeys, shortcut.Keys);
            }
            
            Process.Start(Environment.SystemDirectory + "\\..\\explorer.exe");

            AppDomain.CurrentDomain.ProcessExit += (s, e) => { hook.Dispose(); };

            Application.Run();
        }

        // Credit https://stackoverflow.com/a/21684059/4779937
        private sealed class STAThread : IDisposable {
            private readonly VirtualDesktopWrapper vdw = VirtualDesktopManager.Create();

            public STAThread() {
                using (mre = new ManualResetEvent(false)) {
                    var thread = new Thread(() => {
                        Application.Idle += Initialize;
                        Application.Run();
                    }) {
                        IsBackground = true
                    };
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    mre.WaitOne();
                }
            }

            public void BeginInvoke(Delegate dlg, params Object[] args) {
                if (ctx == null) throw new ObjectDisposedException("STAThread");
                ctx.Post(_ => dlg.DynamicInvoke(args), null);
            }

            // index must be 0-indexed
            public void JumpTo(int index) {
                if (ctx == null) throw new ObjectDisposedException("STAThread");

                ctx.Send((_) => {
                    vdw.JumpTo(index);
                }, null);
            }

            // desktops must be 0-indexed
            public void JumpToNext(int[] desktops) {
                
                if (ctx == null) throw new ObjectDisposedException("STAThread");

                ctx.Send(_ => {
                    int index = Array.FindIndex(desktops, x => x == vdw.GetDesktop());
                    if (index < 0) index = 0;
                    int next = desktops[(index + 1) % desktops.Length];
                    
                    vdw.JumpTo(next);

                }, null);

            }
            
            private void Initialize(object sender, EventArgs e) {
                ctx = SynchronizationContext.Current;
                mre.Set();
                Application.Idle -= Initialize;
            }

            public void Dispose() {
                if (ctx == null) return;

                ctx.Send(_ => Application.ExitThread(), null);
                ctx = null;
            }

            private SynchronizationContext ctx;
            private readonly ManualResetEvent mre;
        }
    }
}