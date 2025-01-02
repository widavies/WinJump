using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinJump.Core;

public sealed class MouseHook : IDisposable {
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private static readonly LowLevelMouseProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    private static event EventHandler<MouseWheelScrolledEventArgs>? _mouseEvents;

    public event EventHandler<MouseWheelScrolledEventArgs>? MouseScrolled;

    public MouseHook() {
        if(_hookID != IntPtr.Zero) {
            throw new ArgumentException("Singleton only");
        }

        _mouseEvents += onMouseEvent;
    }

    private void onMouseEvent(object? sender, MouseWheelScrolledEventArgs args) {
        MouseScrolled?.Invoke(this, args);
    }

    public void Register() {
        _hookID = SetHook(_proc);
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr SetHook(LowLevelMouseProc proc) {
        using Process curProcess = Process.GetCurrentProcess();
        return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle("user32"), 0);
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
        if(nCode >= 0 && MouseMessages.WM_MOUSEWHEEL == (MouseMessages) wParam) {
            MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            int delta = (short) ((hookStruct.mouseData & 0xFFFF0000) >> 16);

            _mouseEvents?.Invoke(null, new MouseWheelScrolledEventArgs {
                x = hookStruct.pt.x,
                y = hookStruct.pt.y,
                up = delta > 0
            });
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private const int WH_MOUSE_LL = 14;

    private enum MouseMessages {
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_MOUSEMOVE = 0x0200,
        WM_MOUSEWHEEL = 0x020A,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT {
        public POINT pt;
        public uint mouseData, flags, time;
        public IntPtr dwExtraInfo;
    }

    public void Dispose() {
        if(_hookID != IntPtr.Zero) {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }

        _mouseEvents -= onMouseEvent;
    }
}

/// <summary>
/// Event ARgs for the event that is fired after the mouse wheel is scrolled.
/// </summary>
public class MouseWheelScrolledEventArgs : EventArgs {
    public required int x, y;
    public required bool up;
}