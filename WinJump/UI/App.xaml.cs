using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.FileProviders;
using WinJump.Core;
using MessageBox = System.Windows.MessageBox;

namespace WinJump.UI;

public partial class App {
    public static TaskbarIcon? notifyIcon;
    private WinJumpManager? manager;

    public static readonly Mutex SINGLE_INSTANCE_MUTEX = new(true, "WinJump");

    public static Guid IDENTIFIER = new Guid("0F4D1F4D-0F4D-0F4D-0F4D-0F4D1F4D0F4D");
    public const Int32 WM_MYMESSAGE = 0x8000; //WM_APP
    public const Int32 NOTIFYICON_VERSION_4 = 0x4;

    //messages
    public const Int32 WM_CONTEXTMENU = 0x7B;
    public const Int32 NIN_BALLOONHIDE = 0x403;
    public const Int32 NIN_BALLOONSHOW = 0x402;
    public const Int32 NIN_BALLOONTIMEOUT = 0x404;
    public const Int32 NIN_BALLOONUSERCLICK = 0x405;
    public const Int32 NIN_KEYSELECT = 0x403;
    public const Int32 NIN_SELECT = 0x400;
    public const Int32 NIN_POPUPOPEN = 0x406;
    public const Int32 NIN_POPUPCLOSE = 0x407;

    public const Int32 NIIF_USER = 0x4;
    public const Int32 NIIF_NONE = 0x0;
    public const Int32 NIIF_INFO = 0x1;
    public const Int32 NIIF_WARNING = 0x2;
    public const Int32 NIIF_ERROR = 0x3;
    public const Int32 NIIF_LARGE_ICON = 0x20;

    public enum NotifyFlags {
        NIF_MESSAGE = 0x01,
        NIF_ICON = 0x02,
        NIF_TIP = 0x04,
        NIF_INFO = 0x10,
        NIF_STATE = 0x08,
        NIF_GUID = 0x20,
        NIF_SHOWTIP = 0x80
    }

    public enum NotifyCommand {
        NIM_ADD = 0x0,
        NIM_DELETE = 0x2,
        NIM_MODIFY = 0x1,
        NIM_SETVERSION = 0x4
    }

    // Shell_NotifyIcon
    [StructLayout(LayoutKind.Sequential)]
    public struct NOTIFYICONDATA {
        public Int32 cbSize;
        public IntPtr hWnd;
        public Int32 uID;
        public NotifyFlags uFlags;
        public Int32 uCallbackMessage;
        public IntPtr hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public String szTip;

        public Int32 dwState;
        public Int32 dwStateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public String szInfo;

        public Int32 uVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public String szInfoTitle;

        public Int32 dwInfoFlags;
        public Guid guidItem; //> IE 6
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll")]
    public static extern System.Int32 Shell_NotifyIcon(NotifyCommand cmd, ref NOTIFYICONDATA data);

    private static FieldInfo idField =
        typeof(TaskbarIcon).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance);

    private static int GetId(TaskbarIcon icon) {
        if(idField == null) throw new InvalidOperationException("[Useful error message]");
        return (int) idField.GetValue(icon);
    }

//https://stackoverflow.com/questions/26153810/get-the-applications-notifyicon-rectangle
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


        // FieldInfo windowFieldInfo =
        //     notifyIcon.GetType().GetField("window", BindingFlags.NonPublic | BindingFlags.Instance);
        // System.Windows.Forms.NativeWindow nativeWindow =
        //     (System.Windows.Forms.NativeWindow) windowFieldInfo.GetValue(notifyIcon);
        // IntPtr iconhandle = nativeWindow.Handle;

        // Debug.WriteLine(iconhandle);
        notifyIcon.TrayMouseMove += (e, args) => {
            Debug.WriteLine("here" + notifyIcon.IsMouseOver);
        };
        notifyIcon.PreviewTrayToolTipOpen += (e, args) => {
            Debug.WriteLine("Enter");
        };

        notifyIcon.PreviewTrayToolTipClose += (e, args) => {
            Debug.WriteLine("closeC");
        };
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
    
    private static FieldInfo windowField = typeof(TaskbarIcon).GetField("window", BindingFlags.NonPublic | BindingFlags.Instance);
        private static IntPtr GetHandle(TaskbarIcon icon)
        {
            if (windowField == null) throw new InvalidOperationException("[Useful error message]");
            NativeWindow window = windowField.GetValue(icon) as NativeWindow;
    
            if (window == null) throw new InvalidOperationException("[Useful error message]");  // should not happen?
            return window.Handle;
        }
}