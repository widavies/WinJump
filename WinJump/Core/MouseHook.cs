using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using WinJump.UI;

// https://stackoverflow.com/questions/4366449/determining-location-of-tray-icon
public static class NativeMethods {
    // Windows message constants
    public const int WM_MOUSEWHEEL = 0x020A;

    // Mouse hook type
    public const int WH_MOUSE_LL = 14;

    // Mouse event handler delegate
    public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    // Windows API functions
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    // Shell_NotifyIconGetRect
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern int Shell_NotifyIconGetRect([In] ref NOTIFYICONIDENTIFIER identifier, out RECT iconLocation);

    // Rect
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    // NOTIFYICONIDENTIFIER
    [StructLayout(LayoutKind.Sequential)]
    public struct NOTIFYICONIDENTIFIER {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public Guid guidItem;
    }
}


public class MouseHook {
    public MouseHook() {
        // Set the mouse hook
        IntPtr hook = SetMouseHook();

        // Keep the application running until a key is pressed

        // Unhook the mouse hook
        // UnhookWindowsHookEx(hook);
    }

    private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
        if(nCode >= 0 && wParam == (IntPtr) NativeMethods.WM_MOUSEWHEEL) {
            // Extract the scroll information from the lParam
            MSLLHOOKSTRUCT structer = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);


            // Process the scroll event
            Debug.WriteLine("Scroll Delta: " + structer.mouseData);
        }

        return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    private static IntPtr SetMouseHook() {
        using(Process curProcess = Process.GetCurrentProcess())
        using(ProcessModule curModule = curProcess.MainModule) {
            IntPtr moduleHandle = NativeMethods.GetModuleHandle(curModule.ModuleName);
            return NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, MouseHookCallback, moduleHandle, 0);
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct MSLLHOOKSTRUCT {
    public POINT pt; // The x and y coordinates in screen coordinates. 
    public int mouseData; // The mouse wheel and button info.
    public int flags;
    public int time; // Specifies the time stamp for this message. 
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal class POINT {
    public int x;
    public int y;
}