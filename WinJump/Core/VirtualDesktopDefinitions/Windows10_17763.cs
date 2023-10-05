using System;
using System.Runtime.InteropServices;

namespace WinJump.Core.VirtualDesktopDefinitions {
    namespace Windows10_17763 {
        public class VirtualDesktopApi : IVirtualDesktopAPI {
            public event OnDesktopChanged? OnDesktopChanged;
            private readonly uint cookie;

            public VirtualDesktopApi() {
                cookie = DesktopManager.VirtualDesktopNotificationService.Register(new VirtualDesktopNotification {
                    OnDesktopChanged = desktop => { OnDesktopChanged?.Invoke(desktop); }
                });
            }

            public int GetCurrentDesktop() {
                return DesktopManager.GetCurrentDesktopNum();
            }

            public void JumpToDesktop(int index) {
                DesktopManager.SwitchDesktop(index);
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
            private static IVirtualDesktopManagerInternal VirtualDesktopManagerInternal;
            internal static IVirtualDesktopNotificationService VirtualDesktopNotificationService;

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

            internal static int GetCurrentDesktopNum() {
                var vd = VirtualDesktopManagerInternal.GetCurrentDesktop();

                return GetIndex(vd);
            }
        }

        internal class VirtualDesktopNotification : IVirtualDesktopNotification {
            public required Action<int> OnDesktopChanged;

            public void VirtualDesktopCreated(IObjectArray array, IVirtualDesktop pDesktop) {
            }

            public void VirtualDesktopDestroyBegin(IObjectArray array, IVirtualDesktop pDesktopDestroyed,
                IVirtualDesktop pDesktopFallback) {
            }

            public void VirtualDesktopDestroyFailed(IObjectArray array, IVirtualDesktop pDesktopDestroyed,
                IVirtualDesktop pDesktopFallback) {
            }

            public void VirtualDesktopDestroyed(IObjectArray array, IVirtualDesktop pDesktopDestroyed,
                IVirtualDesktop pDesktopFallback) {
            }

            public void VirtualDesktopIsPerMonitorChanged(int value) {
            }

            public void VirtualDesktopMoved(IObjectArray array, IVirtualDesktop pDesktop, int nIndexFrom,
                int nIndexTo) {
            }

            public void VirtualDesktopNameChanged(IVirtualDesktop pDesktop, string path) {
            }

            public void ViewVirtualDesktopChanged(IApplicationView pView) {
            }

            public void CurrentVirtualDesktopChanged(IObjectArray array, IVirtualDesktop pDesktopOld,
                IVirtualDesktop pDesktopNew) {
                OnDesktopChanged.Invoke(DesktopManager.GetIndex(pDesktopNew));
            }

            public void VirtualDesktopWallpaperChanged(IObjectArray array, IVirtualDesktop pDesktop, string path) {
            }
        }

        #region COM Interfaces

        internal static class Guids {
            public static readonly Guid CLSID_ImmersiveShell = new("C2F03A33-21F5-47FA-B4BB-156362A2F239");

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
        [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
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
        [Guid("FF72FFDD-BE7E-43FC-9C03-AD81681E88E4")]
        internal interface IVirtualDesktop {
            bool IsViewVisible(IApplicationView view);
            Guid GetId();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("F31574D6-B682-4CDC-BD56-1827860ABEC6")]
        internal interface IVirtualDesktopManagerInternal {
            int GetCount();
            void MoveViewToDesktop(IApplicationView view, IVirtualDesktop desktop);
            bool CanViewMoveDesktops(IApplicationView view);
            IVirtualDesktop GetCurrentDesktop();
            void GetDesktops(out IObjectArray desktops);

            [PreserveSig]
            int GetAdjacentDesktop(IVirtualDesktop from, int direction, out IVirtualDesktop desktop);

            void SwitchDesktop(IVirtualDesktop desktop);
            IVirtualDesktop CreateDesktop();
            void RemoveDesktop(IVirtualDesktop desktop, IVirtualDesktop fallback);
            IVirtualDesktop FindDesktop(ref Guid desktopid);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0CD45E71-D927-4F15-8B0A-8FEF525337BF")]
        internal interface IVirtualDesktopNotificationService {
            uint Register(IVirtualDesktopNotification notification);

            void Unregister(uint cookie);
        }

        [ComImport]
        [Guid("CD403E52-DEED-4C13-B437-B98380F2B1E8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IVirtualDesktopNotification {
            void VirtualDesktopCreated(IObjectArray array, IVirtualDesktop pDesktop);

            void VirtualDesktopDestroyBegin(IObjectArray array, IVirtualDesktop pDesktopDestroyed,
                IVirtualDesktop pDesktopFallback);

            void VirtualDesktopDestroyFailed(IObjectArray array, IVirtualDesktop pDesktopDestroyed,
                IVirtualDesktop pDesktopFallback);

            void VirtualDesktopDestroyed(IObjectArray array, IVirtualDesktop pDesktopDestroyed,
                IVirtualDesktop pDesktopFallback);

            void VirtualDesktopIsPerMonitorChanged(int value);

            void VirtualDesktopMoved(IObjectArray array, IVirtualDesktop pDesktop, int nIndexFrom, int nIndexTo);

            void VirtualDesktopNameChanged(IVirtualDesktop pDesktop, [MarshalAs(UnmanagedType.HString)] string path);

            void ViewVirtualDesktopChanged(IApplicationView pView);

            void CurrentVirtualDesktopChanged(IObjectArray array, IVirtualDesktop pDesktopOld,
                IVirtualDesktop pDesktopNew);

            void VirtualDesktopWallpaperChanged(IObjectArray array, IVirtualDesktop pDesktop,
                [MarshalAs(UnmanagedType.HString)]
                string path);
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