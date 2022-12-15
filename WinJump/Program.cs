using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using VirtualDesktop.VirtualDesktop;

namespace WinJump {
    internal static class Program {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);
        
        [STAThread]
        public static void Main() {
            // Because windows_key + number is already a bulit in Windows shortcut, we need to kill explorer
            // (explorer is the process that registers the shortcut) so that it releases the shortcut.
            // Then, we can register it and restart explorer.
            var killExplorer = Process.Start("cmd.exe", "/c taskkill /f /im explorer.exe");

            killExplorer?.WaitForExit();
            
            var thread = new STAThread();

            // Start a thread that can handle UI requests
            KeyboardHook hook = new KeyboardHook();
            
            hook.KeyPressed += (sender, args) => {
                if (args.Key < Keys.D0 || args.Key > Keys.D9 || args.Modifier != ModifierKeys.Win) return;
                
                int index = args.Key == Keys.D0 ? 10 : (args.Key - Keys.D1);
                thread.JumpTo(index);
            };
            
            for(var key = Keys.D0; key <= Keys.D9; key++) {
                hook.RegisterHotKey(ModifierKeys.Win, key);
            }
            
            Process.Start(Environment.SystemDirectory + "\\..\\explorer.exe");
            
            AppDomain.CurrentDomain.ProcessExit += (s, e) => 
            {
                hook.Dispose();
            };
            
            Application.Run();
        }
        
        // Credit https://stackoverflow.com/a/21684059/4779937
        private sealed class STAThread : IDisposable {
            private IntPtr[] LastActiveWindow = new IntPtr[10];
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
                ctx.Post((_) => dlg.DynamicInvoke(args), null);
            }
            public void JumpTo(int index) {
                if (ctx == null) throw new ObjectDisposedException("STAThread");
                
                ctx.Send((_) => {
                    // Before we go to a new Window, save the foreground Window
                    LastActiveWindow[vdw.GetDesktop()] = GetForegroundWindow();
                    
                    vdw.JumpTo(index);
                    // Give it just a little time to let the desktop settle
                    Thread.Sleep(50);
                    
                    if(LastActiveWindow[index] != IntPtr.Zero) {
                        // Check if the window still exists (it might have been closed)
                        if (IsWindow(LastActiveWindow[index])) {
                            SetForegroundWindow(LastActiveWindow[index]);    
                        } else {
                            LastActiveWindow[index] = IntPtr.Zero;
                        }
                    }
                }, null);
            }

            private void Initialize(object sender, EventArgs e) {
                ctx = SynchronizationContext.Current;
                mre.Set();
                Application.Idle -= Initialize;
            }
            public void Dispose() {
                if (ctx == null) return;
                
                ctx.Send((_) => Application.ExitThread(), null);
                ctx = null;
            }

            private SynchronizationContext ctx;
            private readonly ManualResetEvent mre;
        }

    }
}
