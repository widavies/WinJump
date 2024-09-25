using System;
using System.Runtime.InteropServices;

namespace WinJump.Core.VirtualDesktopDefinitions {
    namespace Windows11_22631_3085 {
        public class VirtualDesktopApi : IVirtualDesktopAPI {
            public event OnDesktopChanged? OnDesktopChanged;
            private readonly uint cookie;

            public VirtualDesktopApi() {
                cookie = DesktopManager.VirtualDesktopNotificationService.Register(new VirtualDesktopNotification {
                    OnDesktopChanged = desktop => { OnDesktopChanged?.Invoke(desktop); }
                });
            }
            
            public void CreateDesktop()
            {
                DesktopManager.CreateDesktop();
            }

            public int GetCurrentDesktop() {
                return DesktopManager.GetCurrentDesktopNum();
            }

            public int GetDesktopCount() {
                return DesktopManager.GetDesktopCount();
            }

            public void JumpToDesktop(int index) {
                DesktopManager.SwitchDesktop(index);
            }

            public void MoveFocusedWindowToDesktop(int index) {
                DesktopManager.MoveCurrentlyFocusedToDesktop(index);
            }

            public void Dispose() {
                DesktopManager.VirtualDesktopNotificationService.Unregister(cookie);
                GC.SuppressFinalize(this);
            }
        }

        /*
         * Implementation
         */

        internal static class DesktopManager {
            // get handle of active window
            [DllImport("user32.dll")]
            private static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

            private static IVirtualDesktopManagerInternal VirtualDesktopManagerInternal;
            internal static IVirtualDesktopNotificationService VirtualDesktopNotificationService;
            internal static IApplicationViewCollection ApplicationViewCollection;
            internal static IVirtualDesktopManager VirtualDesktopManager;

            static DesktopManager() {
                if(Activator.CreateInstance(Type.GetTypeFromCLSID(Guids.CLSID_ImmersiveShell) ??
                                            throw new Exception("Failed to get shell")) is not IServiceProvider10
                   shell) {
                    throw new Exception("Failed to get shell");
                }

                VirtualDesktopManagerInternal = (IVirtualDesktopManagerInternal) shell.QueryService(
                    Guids.CLSID_VirtualDesktopManagerInternal, typeof(IVirtualDesktopManagerInternal).GUID);
                VirtualDesktopNotificationService = (IVirtualDesktopNotificationService) shell.QueryService(
                    Guids.CLSID_VirtualDesktopNotificationService, typeof(IVirtualDesktopNotificationService).GUID);
                VirtualDesktopManager =
                    (IVirtualDesktopManager) Activator.CreateInstance(
                        Type.GetTypeFromCLSID(Guids.CLSID_VirtualDesktopManager));
                ApplicationViewCollection = (IApplicationViewCollection) shell.QueryService(
                    typeof(IApplicationViewCollection).GUID, typeof(IApplicationViewCollection).GUID);
            }

            // Helpers
            private static IVirtualDesktop? GetDesktop(int index) {
                int count = VirtualDesktopManagerInternal.GetCount();
                if(index < 0 || index >= count) return null;
                IObjectArray desktops;
                VirtualDesktopManagerInternal.GetDesktops(out desktops);
                object objdesktop;
                desktops.GetAt(index, typeof(IVirtualDesktop).GUID, out objdesktop);
                Marshal.ReleaseComObject(desktops);
                return (IVirtualDesktop) objdesktop;
            }

            internal static int GetIndex(IVirtualDesktop ivd) {
                IObjectArray desktops;
                VirtualDesktopManagerInternal.GetDesktops(out desktops);

                int count;
                desktops.GetCount(out count);

                for(int i = 0; i < count; i++) {
                    object objdesktop;
                    desktops.GetAt(i, typeof(IVirtualDesktop).GUID, out objdesktop);
                    if(ReferenceEquals(ivd, objdesktop)) {
                        return i;
                    }
                }

                Marshal.ReleaseComObject(desktops);
                return -1;
            }

            internal static void SwitchDesktop(int index) {
                IVirtualDesktop? desktop = GetDesktop(index);
                if(desktop == null) return;
                VirtualDesktopManagerInternal.SwitchDesktop(desktop);
                Marshal.ReleaseComObject(desktop);
            }

            internal static void CreateDesktop()
            {
                VirtualDesktopManagerInternal.CreateDesktop();
            }

            internal static int GetCurrentDesktopNum() {
                var vd = VirtualDesktopManagerInternal.GetCurrentDesktop();

                return GetIndex(vd);
            }

            internal static int GetDesktopCount() {
                return VirtualDesktopManagerInternal.GetCount();
            }

            internal static void MoveCurrentlyFocusedToDesktop(int index) {
                int processId;
                IntPtr hWnd = GetForegroundWindow();
                if(hWnd == IntPtr.Zero) throw new ArgumentNullException();
                GetWindowThreadProcessId(hWnd, out processId);

                var desktop = GetDesktop(index);
                if (desktop == null) return;

                if(Environment.ProcessId == processId) {
                    // window of process
                    try // the easy way (if we are owner)
                    {
                        VirtualDesktopManager.MoveWindowToDesktop(hWnd, desktop.GetId());
                    } catch // window of process, but we are not the owner
                    {
                        IApplicationView view;
                        ApplicationViewCollection.GetViewForHwnd(hWnd, out view);
                        VirtualDesktopManagerInternal.MoveViewToDesktop(view, desktop);
                    }
                } else {
                    // window of other process
                    ApplicationViewCollection.GetViewForHwnd(hWnd, out var view);
                    try {
                        VirtualDesktopManagerInternal.MoveViewToDesktop(view, desktop);
                    } catch {
                        // could not move active window, try main window (or whatever windows thinks is the main window)
                        ApplicationViewCollection.GetViewForHwnd(
                            System.Diagnostics.Process.GetProcessById(processId).MainWindowHandle, out view);
                        VirtualDesktopManagerInternal.MoveViewToDesktop(view, desktop);
                    }
                }
            }
        }

        internal class VirtualDesktopNotification : IVirtualDesktopNotification {
            public required Action<int> OnDesktopChanged;

            public void VirtualDesktopCreated(IVirtualDesktop pDesktop) {
            }

            public void VirtualDesktopDestroyBegin(IVirtualDesktop pDesktopDestroyed,
                IVirtualDesktop pDesktopFallback) {
            }

            public void VirtualDesktopDestroyFailed(IVirtualDesktop pDesktopDestroyed,
                IVirtualDesktop pDesktopFallback) {
            }

            public void VirtualDesktopDestroyed(IVirtualDesktop pDesktopDestroyed, IVirtualDesktop pDesktopFallback) {
            }

            public void VirtualDesktopMoved(IVirtualDesktop pDesktop, int nIndexFrom, int nIndexTo) {
            }

            public void VirtualDesktopNameChanged(IVirtualDesktop pDesktop, string path) {
            }

            public void ViewVirtualDesktopChanged(IApplicationView pView) {
            }

            public void CurrentVirtualDesktopChanged(IVirtualDesktop pDesktopOld, IVirtualDesktop pDesktopNew) {
                OnDesktopChanged.Invoke(DesktopManager.GetIndex(pDesktopNew));
            }

            public void VirtualDesktopWallpaperChanged(IVirtualDesktop pDesktop, string path) {
            }

            public void VirtualDesktopSwitched(IVirtualDesktop pDesktop) {
            }

            public void RemoteVirtualDesktopConnected(IVirtualDesktop pDesktop) {
            }
        }

        #region COM Interfaces

        internal static class Guids {
            public static readonly Guid CLSID_ImmersiveShell = new("C2F03A33-21F5-47FA-B4BB-156362A2F239");

            public static readonly Guid CLSID_VirtualDesktopManager = new("AA509086-5CA9-4C25-8F95-589D3C07B48A");

            public static readonly Guid CLSID_VirtualDesktopManagerInternal =
                new("C5E0CDCA-7B6E-41B2-9FC4-D93975CC467B");

            public static readonly Guid CLSID_VirtualDesktopNotificationService =
                new("A501FDEC-4A09-464C-AE4E-1B9C21B84918");
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Size {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        internal enum APPLICATION_VIEW_CLOAK_TYPE {
            AVCT_NONE = 0,
            AVCT_DEFAULT = 1,
            AVCT_VIRTUAL_DESKTOP = 2
        }

        internal enum APPLICATION_VIEW_COMPATIBILITY_POLICY {
            AVCP_NONE = 0,
            AVCP_SMALL_SCREEN = 1,
            AVCP_TABLET_SMALL_SCREEN = 2,
            AVCP_VERY_SMALL_SCREEN = 3,
            AVCP_HIGH_SCALE_FACTOR = 4
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("372E1D3B-38D3-42E4-A15B-8AB2B178F513")]
        internal interface IApplicationView {
            int SetFocus();
            int SwitchTo();
            int TryInvokeBack(IntPtr /* IAsyncCallback* */ callback);
            int GetThumbnailWindow(out IntPtr hwnd);
            int GetMonitor(out IntPtr /* IImmersiveMonitor */ immersiveMonitor);
            int GetVisibility(out int visibility);
            int SetCloak(APPLICATION_VIEW_CLOAK_TYPE cloakType, int unknown);

            int GetPosition(ref Guid guid /* GUID for IApplicationViewPosition */,
                out IntPtr /* IApplicationViewPosition** */ position);

            int SetPosition(ref IntPtr /* IApplicationViewPosition* */ position);
            int InsertAfterWindow(IntPtr hwnd);
            int GetExtendedFramePosition(out Rect rect);
            int GetAppUserModelId([MarshalAs(UnmanagedType.LPWStr)] out string id);
            int SetAppUserModelId(string id);
            int IsEqualByAppUserModelId(string id, out int result);
            int GetViewState(out uint state);
            int SetViewState(uint state);
            int GetNeediness(out int neediness);
            int GetLastActivationTimestamp(out ulong timestamp);
            int SetLastActivationTimestamp(ulong timestamp);
            int GetVirtualDesktopId(out Guid guid);
            int SetVirtualDesktopId(ref Guid guid);
            int GetShowInSwitchers(out int flag);
            int SetShowInSwitchers(int flag);
            int GetScaleFactor(out int factor);
            int CanReceiveInput(out bool canReceiveInput);
            int GetCompatibilityPolicyType(out APPLICATION_VIEW_COMPATIBILITY_POLICY flags);
            int SetCompatibilityPolicyType(APPLICATION_VIEW_COMPATIBILITY_POLICY flags);
            int GetSizeConstraints(IntPtr /* IImmersiveMonitor* */ monitor, out Size size1, out Size size2);
            int GetSizeConstraintsForDpi(uint uint1, out Size size1, out Size size2);
            int SetSizeConstraintsForDpi(ref uint uint1, ref Size size1, ref Size size2);
            int OnMinSizePreferencesUpdated(IntPtr hwnd);
            int ApplyOperation(IntPtr /* IApplicationViewOperation* */ operation);
            int IsTray(out bool isTray);
            int IsInHighZOrderBand(out bool isInHighZOrderBand);
            int IsSplashScreenPresented(out bool isSplashScreenPresented);
            int Flash();
            int GetRootSwitchableOwner(out IApplicationView rootSwitchableOwner);
            int EnumerateOwnershipTree(out IObjectArray ownershipTree);
            int GetEnterpriseId([MarshalAs(UnmanagedType.LPWStr)] out string enterpriseId);
            int IsMirrored(out bool isMirrored);
            int Unknown1(out int unknown);
            int Unknown2(out int unknown);
            int Unknown3(out int unknown);
            int Unknown4(out int unknown);
            int Unknown5(out int unknown);
            int Unknown6(int unknown);
            int Unknown7();
            int Unknown8(out int unknown);
            int Unknown9(int unknown);
            int Unknown10(int unknownX, int unknownY);
            int Unknown11(int unknown);
            int Unknown12(out Size size1);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("3F07F4BE-B107-441A-AF0F-39D82529072C")]
        internal interface IVirtualDesktop {
            bool IsViewVisible(IApplicationView view);
            Guid GetId();

            [return: MarshalAs(UnmanagedType.HString)]
            string GetName();

            [return: MarshalAs(UnmanagedType.HString)]
            string GetWallpaperPath();

            bool IsRemote();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("53F5CA0B-158F-4124-900C-057158060B27")]
        internal interface IVirtualDesktopManagerInternal {
            int GetCount();
            void MoveViewToDesktop(IApplicationView view, IVirtualDesktop desktop);
            bool CanViewMoveDesktops(IApplicationView view);
            IVirtualDesktop GetCurrentDesktop();
            void GetDesktops(out IObjectArray desktops);

            [PreserveSig]
            int GetAdjacentDesktop(IVirtualDesktop from, int direction, out IVirtualDesktop desktop);

            void SwitchDesktop(IVirtualDesktop desktop);

            //		void SwitchDesktopAndMoveForegroundView(IVirtualDesktop desktop);
            IVirtualDesktop CreateDesktop();
            void MoveDesktop(IVirtualDesktop desktop, int nIndex);
            void RemoveDesktop(IVirtualDesktop desktop, IVirtualDesktop fallback);
            IVirtualDesktop FindDesktop(ref Guid desktopid);

            void GetDesktopSwitchIncludeExcludeViews(IVirtualDesktop desktop, out IObjectArray unknown1,
                out IObjectArray unknown2);

            void SetDesktopName(IVirtualDesktop desktop, [MarshalAs(UnmanagedType.HString)] string name);
            void SetDesktopWallpaper(IVirtualDesktop desktop, [MarshalAs(UnmanagedType.HString)] string path);
            void UpdateWallpaperPathForAllDesktops([MarshalAs(UnmanagedType.HString)] string path);
            void CopyDesktopState(IApplicationView pView0, IApplicationView pView1);
            void CreateRemoteDesktop([MarshalAs(UnmanagedType.HString)] string path, out IVirtualDesktop desktop);
            void SwitchRemoteDesktop(IVirtualDesktop desktop);
            void SwitchDesktopWithAnimation(IVirtualDesktop desktop);
            void GetLastActiveDesktop(out IVirtualDesktop desktop);
            void WaitForAnimationToComplete();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("1841C6D7-4F9D-42C0-AF41-8747538F10E5")]
        internal interface IApplicationViewCollection {
            int GetViews(out IObjectArray array);
            int GetViewsByZOrder(out IObjectArray array);
            int GetViewsByAppUserModelId(string id, out IObjectArray array);
            int GetViewForHwnd(IntPtr hwnd, out IApplicationView view);
            int GetViewForApplication(object application, out IApplicationView view);
            int GetViewForAppUserModelId(string id, out IApplicationView view);
            int GetViewInFocus(out IntPtr view);
            int Unknown1(out IntPtr view);
            void RefreshCollection();
            int RegisterForApplicationViewChanges(object listener, out int cookie);
            int UnregisterForApplicationViewChanges(int cookie);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("A5CD92FF-29BE-454C-8D04-D82879FB3F1B")]
        internal interface IVirtualDesktopManager {
            bool IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow);
            Guid GetWindowDesktopId(IntPtr topLevelWindow);
            void MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0CD45E71-D927-4F15-8B0A-8FEF525337BF")]
        internal interface IVirtualDesktopNotificationService {
            uint Register(IVirtualDesktopNotification notification);

            void Unregister(uint cookie);
        }

        [ComImport]
        [Guid("B9E5E94D-233E-49AB-AF5C-2B4541C3AADE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IVirtualDesktopNotification {
            void VirtualDesktopCreated(IVirtualDesktop pDesktop);

            void VirtualDesktopDestroyBegin(IVirtualDesktop pDesktopDestroyed,
                IVirtualDesktop pDesktopFallback);

            void VirtualDesktopDestroyFailed(IVirtualDesktop pDesktopDestroyed, IVirtualDesktop pDesktopFallback);

            void VirtualDesktopDestroyed(IVirtualDesktop pDesktopDestroyed, IVirtualDesktop pDesktopFallback);

            void VirtualDesktopMoved(IVirtualDesktop pDesktop, int nIndexFrom, int nIndexTo);

            void VirtualDesktopNameChanged(IVirtualDesktop pDesktop, [MarshalAs(UnmanagedType.HString)] string path);

            void ViewVirtualDesktopChanged(IApplicationView pView);

            void CurrentVirtualDesktopChanged(IVirtualDesktop pDesktopOld,
                IVirtualDesktop pDesktopNew);

            void VirtualDesktopWallpaperChanged(IVirtualDesktop pDesktop,
                [MarshalAs(UnmanagedType.HString)]
                string path);

            void VirtualDesktopSwitched(IVirtualDesktop pDesktop);

            void RemoteVirtualDesktopConnected(IVirtualDesktop pDesktop);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9")]
        internal interface IObjectArray {
            void GetCount(out int count);
            void GetAt(int index, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object obj);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
        internal interface IServiceProvider10 {
            [return: MarshalAs(UnmanagedType.IUnknown)]
            object QueryService(ref Guid service, ref Guid riid);
        }

        #endregion
    }
}
