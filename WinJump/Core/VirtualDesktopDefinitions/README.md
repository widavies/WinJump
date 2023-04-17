# Reverse engineering Windows virtual desktops

## Introduction

Unfortunately Windows does not publish an API for virtual desktops. This means two things:
1. Windows must be reverse engineered to figure out the internal API for virtual desktops.
2. Windows may change the API on a whim for any given release. Every time this happens, step #1 needs to be repeated. 

Much of the reverse engineering has been [done already](https://github.com/MScholtes/VirtualDesktop).
Huge thanks to [MScholtes](https://github.com/MScholtes) and [NyaMisty](https://github.com/NyaMisty).

MScholtes and NyaMisty regularly release reverse-engineered virtual desktop APIs for the latest Windows builds,
so most of the necessary work can be copied from them. However, WinJump does require a few more APIs that they do not
reverse engineer (most notably `IVirtualDesktopNotificationService`). For this reason, WinJump maintains its own
reverse-engineered API definitions.

Because Windows internal API changes are a regular occurence (and I don't have a lot of extra time), 
I've decided to document the process here to make it easier for you to get WinJump working on your specific version of Windows.

`WinJump/Core/VirtualDesktopDefinitions/` contains a list of _virtual desktop API definitions_. They have the format
like:
```
Windows10_17763.cs
Windows11_22000.cs
Windows11_22621.cs
WindowsXX_<BUILD>.cs
...
```

Each of these provides an implementation for `IVirtualDesktopAPI` for a specific Windows build. Every time the virtual
desktop API changes, a new `WindowsXX_<BUILD>.cs` file needs to be added. Please abide by the naming scheme: the build
number in each file name indicates the first Windows build number for which the implementation applies. This
implementation will apply to all subsequent Windows build numbers until the API changes, at which point a new file
is created. If you take a look at `Create()` in the `IVirtualDesktopAPI` file, the Windows build version
is checked and the appropriate implementation is used.

## Down to business
To begin, download a copy of the following [Python script](https://github.com/widavies/GetVirtualDesktopAPI_DIA).

Make sure Visual Studio is installed. You may have to update [this line](https://github.com/widavies/GetVirtualDesktopAPI_DIA/blob/fcb6a6ed4ecbd5cfac6518853e79581596125ba6/DiaGetVDInfo.py#L12)
in the script to point to a `msdiaXXX.dll` file within your Visual Studio installation folder. It doesn't seem like the
particular number after this file matters. Run the script and it will produce an output like so:
```
IID_IVirtualDesktopManager: A5CD92FF-29BE-454C-8D04-D82879FB3F1B
IID_IVirtualDesktopAccessibility: 9975B71D-0A84-4909-BDDE-B455BBFA55C6
IID_IVirtualDesktopManagerInternal: B2F925B9-5A0F-4D2E-9F4D-2B1507593C10
IID_IVirtualDesktopHotkeyHandler: 71DB071A-44AE-4271-B9F6-01CFB6A12DEE
IID_IVirtualDesktopSwitcherInvoker: 7A25165A-86F1-4B4A-B1D2-E89650CD9589
IID_IVirtualDesktopNotificationService: 0CD45E71-D927-4F15-8B0A-8FEF525337BF
IID_IVirtualDesktopPinnedApps: 4CE81583-1E4C-4632-A621-07A53543148F
IID_IVirtualDesktopAnimationSyncNotification: 6CAFD3F1-05D1-4D26-A32A-9907A72C920B
IID_IVirtualDesktop: 536D3495-B208-4CC9-AE26-DE8111275BF8
IID_IVirtualDesktopTabletModePolicyService: 56B32065-0BB3-42E2-975D-A559DE1316E8
IID_IVirtualDesktopNotification: CD403E52-DEED-4C13-B437-B98380F2B1E8
IID_IVirtualDesktopAnimationSyncNotificationService: 0DDAF2D8-C38F-4638-95FC-FB9C6DDAE52F
IID_IVirtualDesktopSwitcherHost: 1BE71764-E771-4442-B78F-EDA2C7F067F3


Dumping vftable: const CVirtualDesktopComponent::`vftable'{for `Microsoft::WRL::Details::Selector<class CImmersiveShellComponentWithGITSite,struct Microsoft::WRL::Details::ImplementsHelper<struct Microsoft::WRL::RuntimeClassFlags<2>,0,struct Microsoft::WRL::Details::ImplementsMarker<class CImmersiveShellComponentWithGITSite>,class Microsoft::WRL::FtmBase> >'}
    Method  0: public: virtual long __cdecl Microsoft::WRL::Details::RuntimeClassImpl<struct Microsoft::WRL::RuntimeClassFlags<2>,1,0,0,class CImmersiveShellComponentWithGITSite,class Microsoft::WRL::FtmBase>::QueryInterface(struct _GUID const & __ptr64,void * __ptr64 * __ptr64) __ptr64 (?QueryInterface@?$RuntimeClassImpl@U?$RuntimeClassFlags@$01@WRL@Microsoft@@$00$0A@$0A@VCImmersiveShellComponentWithGITSite@@VFtmBase@23@@Details@WRL@Microsoft@@UEAAJAEBU_GUID@@PEAPEAX@Z)
    Method  1: public: virtual unsigned long __cdecl Microsoft::WRL::Details::RuntimeClassImpl<struct Microsoft::WRL::RuntimeClassFlags<2>,1,0,0,class CImmersiveShellComponentWithSite,struct IShellPositionerManager,struct IApplicationViewChangeListener,struct ITabletModeChangeListener,struct IShellPositionerActivationHandler,struct IShellPositionerFrameworkViewTypeChangedHandler,struct IShellPositionerPresentationRequestedHandler>::AddRef(void) __ptr64 (?AddRef@?$RuntimeClassImpl@U?$RuntimeClassFlags@$01@WRL@Microsoft@@$00$0A@$0A@VCImmersiveShellComponentWithSite@@UIShellPositionerManager@@UIApplicationViewChangeListener@@UITabletModeChangeListener@@UIShellPositionerActivationHandler@@UIShellPositionerFrameworkViewTypeChangedHandler@@UIShellPositionerPresentationRequestedHandler@@@Details@WRL@Microsoft@@UEAAKXZ)
    Method  2: public: virtual unsigned long __cdecl Microsoft::WRL::Details::RuntimeClassImpl<struct Microsoft::WRL::RuntimeClassFlags<2>,1,0,0,class CImmersiveShellComponentWithGITSite,class Microsoft::WRL::FtmBase>::Release(void) __ptr64 (?Release@?$RuntimeClassImpl@U?$RuntimeClassFlags@$01@WRL@Microsoft@@$00$0A@$0A@VCImmersiveShellComponentWithGITSite@@VFtmBase@23@@Details@WRL@Microsoft@@UEAAKXZ)
    Method  3: public: virtual long __cdecl CWRLObjectWithGITSite::SetSite(struct IUnknown * __ptr64) __ptr64 (?SetSite@CWRLObjectWithGITSite@@UEAAJPEAUIUnknown@@@Z)
    Method  4: public: virtual long __cdecl CWRLObjectWithGITSite::GetSite(struct _GUID const & __ptr64,void * __ptr64 * __ptr64) __ptr64 (?GetSite@CWRLObjectWithGITSite@@UEAAJAEBU_GUID@@PEAPEAX@Z)


Dumping vftable: const CVirtualDesktopHolographicViewTransitionNotification::`vftable'{for `IHolographicViewTransitionNotification'}
    Method  0: public: virtual long __cdecl Microsoft::WRL::Details::RuntimeClassImpl<struct Microsoft::WRL::RuntimeClassFlags<2>,1,0,0,struct IHolographicViewTransitionNotification,class Microsoft::WRL::FtmBase>::QueryInterface(struct _GUID const & __ptr64,void * __ptr64 * __ptr64) __ptr64 (?QueryInterface@?$RuntimeClassImpl@U?$RuntimeClassFlags@$01@WRL@Microsoft@@$00$0A@$0A@UIHolographicViewTransitionNotification@@VFtmBase@23@@Details@WRL@Microsoft@@UEAAJAEBU_GUID@@PEAPEAX@Z)
    Method  1: public: virtual unsigned long __cdecl Microsoft::WRL::Details::RuntimeClassImpl<struct Microsoft::WRL::RuntimeClassFlags<2>,1,0,0,struct IImmersiveShellHookNotification,class Microsoft::WRL::FtmBase>::AddRef(void) __ptr64 (?AddRef@?$RuntimeClassImpl@U?$RuntimeClassFlags@$01@WRL@Microsoft@@$00$0A@$0A@UIImmersiveShellHookNotification@@VFtmBase@23@@Details@WRL@Microsoft@@UEAAKXZ)
    Method  2: public: virtual unsigned long __cdecl Microsoft::WRL::Details::RuntimeClassImpl<struct Microsoft::WRL::RuntimeClassFlags<2>,1,0,0,class CWRLObjectWithSite,struct IVisibilityOverride,struct IImmersiveSessionIdleNotification>::Release(void) __ptr64 (?Release@?$RuntimeClassImpl@U?$RuntimeClassFlags@$01@WRL@Microsoft@@$00$0A@$0A@VCWRLObjectWithSite@@UIVisibilityOverride@@UIImmersiveSessionIdleNotification@@@Details@WRL@Microsoft@@UEAAKXZ)
    Method  3: public: virtual long __cdecl CVirtualDesktopHolographicViewTransitionNotification::ViewTransitionedToHolographic(struct IApplicationView * __ptr64) __ptr64 (?ViewTransitionedToHolographic@CVirtualDesktopHolographicViewTransitionNotification@@UEAAJPEAUIApplicationView@@@Z)
    Method  4: public: virtual long __cdecl CVirtualDesktopHolographicViewTransitionNotification::ViewTransitionedFromHolographic(struct IApplicationView * __ptr64) __ptr64 (?ViewTransitionedFromHolographic@CVirtualDesktopHolographicViewTransitionNotification@@UEAAJPEAUIApplicationView@@@Z)
    Method  5: public: virtual void * __ptr64 __cdecl CVirtualDesktopHolographicViewTransitionNotification::`vector deleting destructor'(unsigned int) __ptr64 (??_ECVirtualDesktopHolographicViewTransitionNotification@@UEAAPEAXI@Z)

...
```

To get WinJump working, you have to provide definitions for the three functions in `IVirtualDesktopAPI`:
```c#
/// <summary>
/// An event that notifies subscribers when the virtual desktop changes.
/// </summary>
event OnDesktopChanged OnDesktopChanged;

/// <summary>
/// Returns the current virtual desktop that the user is on.
/// </summary>
/// <returns>0-indexed, where '0' is the first desktop</returns>
int GetCurrentDesktop();

/// <summary>
/// Jumps to the virtual desktop.
/// </summary>
/// <param name="index">0-indexed desktop number. If it is invalid it will be ignored.</param>
void JumpToDesktop(int index);
```

Under the hood, this usually requires reverse engineering and accessing the `IVirtualDesktopManagerInternal` and
`IVirtualDesktopNotificationService` internal services. For these services, you MUST figure out three items:
1. The `GUID` of the service
2. The signature of the service: the functions it contains with their signatures.
3. The `ComImport` attribute `GUID` 

### Item 1
The `GUID` class contains the COM service `GUID`s:
```c#
public static readonly Guid CLSID_ImmersiveShell = new("C2F03A33-21F5-47FA-B4BB-156362A2F239");

public static readonly Guid CLSID_VirtualDesktopManagerInternal = new("C5E0CDCA-7B6E-41B2-9FC4-D93975CC467B");

public static readonly Guid CLSID_VirtualDesktopNotificationService =
    new("A501FDEC-4A09-464C-AE4E-1B9C21B84918");
```

These `GUID`s are fixed. It is unlikely that they will change in any future Windows release. They can be a bit hard
to track down, but using [magnumdb.com](https://www.magnumdb.com/search?q=CLSID_ImmersiveShell) and querying
by the name (e.g. "CLSID_ImmersiveShell") seems to work well.

### Item 2
Starting with the `IVirtualDesktopNotificationService` and `IVirtualDesktopManagerInternal`, all internal services
must be translated into C# interfaces. For example, the Python script will dump:

```
Dumping vftable: const CVirtualDesktopManager::`vftable'{for `IVirtualDesktopManagerInternal'}
    Method  0: [thunk]:public: virtual long __cdecl Microsoft::WRL::Details::RuntimeClassImpl<struct Microsoft::WRL::RuntimeClassFlags<3>,1,1,0,struct Microsoft::WRL::ChainInterfaces<struct IVirtualDesktopManagerPrivate,struct IVirtualDesktopManagerInternal,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil>,struct IVirtualDesktopManagerInternal,struct ISuspendableVirtualDesktopManager,struct IImmersiveWindowMessageNotification,class Microsoft::WRL::FtmBase>::QueryInterface`adjustor{24}' (struct _GUID const & __ptr64,void * __ptr64 * __ptr64) __ptr64 (?QueryInterface@?$RuntimeClassImpl@U?$RuntimeClassFlags@$02@WRL@Microsoft@@$00$00$0A@U?$ChainInterfaces@UIVirtualDesktopManagerPrivate@@UIVirtualDesktopManagerInternal@@VNil@Details@WRL@Microsoft@@V3456@V3456@V3456@V3456@V3456@V3456@V3456@@23@UIVirtualDesktopManagerInternal@@UISuspendableVirtualDesktopManager@@UIImmersiveWindowMessageNotification@@VFtmBase@23@@Details@WRL@Microsoft@@WBI@EAAJAEBU_GUID@@PEAPEAX@Z)
    Method  1: [thunk]:public: virtual unsigned long __cdecl Microsoft::WRL::Details::RuntimeClassImpl<struct Microsoft::WRL::RuntimeClassFlags<3>,1,1,0,struct Microsoft::WRL::ChainInterfaces<struct IVirtualDesktopManagerPrivate,struct IVirtualDesktopManagerInternal,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil>,struct IVirtualDesktopManagerInternal,struct ISuspendableVirtualDesktopManager,struct IImmersiveWindowMessageNotification,class Microsoft::WRL::FtmBase>::AddRef`adjustor{24}' (void) __ptr64 (?AddRef@?$RuntimeClassImpl@U?$RuntimeClassFlags@$02@WRL@Microsoft@@$00$00$0A@U?$ChainInterfaces@UIVirtualDesktopManagerPrivate@@UIVirtualDesktopManagerInternal@@VNil@Details@WRL@Microsoft@@V3456@V3456@V3456@V3456@V3456@V3456@V3456@@23@UIVirtualDesktopManagerInternal@@UISuspendableVirtualDesktopManager@@UIImmersiveWindowMessageNotification@@VFtmBase@23@@Details@WRL@Microsoft@@WBI@EAAKXZ)
    Method  2: [thunk]:public: virtual unsigned long __cdecl Microsoft::WRL::Details::RuntimeClassImpl<struct Microsoft::WRL::RuntimeClassFlags<3>,1,1,0,struct Microsoft::WRL::ChainInterfaces<struct IVirtualDesktopManagerPrivate,struct IVirtualDesktopManagerInternal,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil,class Microsoft::WRL::Details::Nil>,struct IVirtualDesktopManagerInternal,struct ISuspendableVirtualDesktopManager,struct IImmersiveWindowMessageNotification,class Microsoft::WRL::FtmBase>::Release`adjustor{24}' (void) __ptr64 (?Release@?$RuntimeClassImpl@U?$RuntimeClassFlags@$02@WRL@Microsoft@@$00$00$0A@U?$ChainInterfaces@UIVirtualDesktopManagerPrivate@@UIVirtualDesktopManagerInternal@@VNil@Details@WRL@Microsoft@@V3456@V3456@V3456@V3456@V3456@V3456@V3456@@23@UIVirtualDesktopManagerInternal@@UISuspendableVirtualDesktopManager@@UIImmersiveWindowMessageNotification@@VFtmBase@23@@Details@WRL@Microsoft@@WBI@EAAKXZ)
    Method  3: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::GetCount`adjustor{16}' (struct HMONITOR__ * __ptr64,unsigned int * __ptr64) __ptr64 (?GetCount@CVirtualDesktopManager@@WBA@EAAJPEAUHMONITOR__@@PEAI@Z)
    Method  4: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::MoveViewToDesktop`adjustor{16}' (struct IApplicationView * __ptr64,struct IVirtualDesktop * __ptr64) __ptr64 (?MoveViewToDesktop@CVirtualDesktopManager@@WBA@EAAJPEAUIApplicationView@@PEAUIVirtualDesktop@@@Z)
    Method  5: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::CanViewMoveDesktops`adjustor{16}' (struct IApplicationView * __ptr64,int * __ptr64) __ptr64 (?CanViewMoveDesktops@CVirtualDesktopManager@@WBA@EAAJPEAUIApplicationView@@PEAH@Z)
    Method  6: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::GetCurrentDesktop`adjustor{16}' (struct HMONITOR__ * __ptr64,struct IVirtualDesktop * __ptr64 * __ptr64) __ptr64 (?GetCurrentDesktop@CVirtualDesktopManager@@WBA@EAAJPEAUHMONITOR__@@PEAPEAUIVirtualDesktop@@@Z)
    Method  7: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::GetAllCurrentDesktops`adjustor{16}' (struct IObjectArray * __ptr64 * __ptr64) __ptr64 (?GetAllCurrentDesktops@CVirtualDesktopManager@@WBA@EAAJPEAPEAUIObjectArray@@@Z)
    Method  8: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::GetDesktops`adjustor{16}' (struct HMONITOR__ * __ptr64,struct IObjectArray * __ptr64 * __ptr64) __ptr64 (?GetDesktops@CVirtualDesktopManager@@WBA@EAAJPEAUHMONITOR__@@PEAPEAUIObjectArray@@@Z)
    Method  9: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::GetAdjacentDesktop`adjustor{16}' (struct IVirtualDesktop * __ptr64,unsigned int,struct IVirtualDesktop * __ptr64 * __ptr64) __ptr64 (?GetAdjacentDesktop@CVirtualDesktopManager@@WBA@EAAJPEAUIVirtualDesktop@@IPEAPEAU2@@Z)
    Method 10: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::SwitchDesktop`adjustor{16}' (struct HMONITOR__ * __ptr64,struct IVirtualDesktop * __ptr64) __ptr64 (?SwitchDesktop@CVirtualDesktopManager@@WBA@EAAJPEAUHMONITOR__@@PEAUIVirtualDesktop@@@Z)
    Method 11: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::CreateDesktopW`adjustor{16}' (struct HMONITOR__ * __ptr64,struct IVirtualDesktop * __ptr64 * __ptr64) __ptr64 (?CreateDesktopW@CVirtualDesktopManager@@WBA@EAAJPEAUHMONITOR__@@PEAPEAUIVirtualDesktop@@@Z)
    Method 12: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::MoveDesktop`adjustor{16}' (struct IVirtualDesktop * __ptr64,struct HMONITOR__ * __ptr64,unsigned int) __ptr64 (?MoveDesktop@CVirtualDesktopManager@@WBA@EAAJPEAUIVirtualDesktop@@PEAUHMONITOR__@@I@Z)
    Method 13: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::RemoveDesktop`adjustor{16}' (struct IVirtualDesktop * __ptr64,struct IVirtualDesktop * __ptr64) __ptr64 (?RemoveDesktop@CVirtualDesktopManager@@WBA@EAAJPEAUIVirtualDesktop@@0@Z)
    Method 14: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::FindDesktop`adjustor{16}' (struct _GUID const & __ptr64,struct IVirtualDesktop * __ptr64 * __ptr64) __ptr64 (?FindDesktop@CVirtualDesktopManager@@WBA@EAAJAEBU_GUID@@PEAPEAUIVirtualDesktop@@@Z)
    Method 15: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::GetDesktopSwitchIncludeExcludeViews`adjustor{16}' (struct IVirtualDesktop * __ptr64,struct IObjectArray * __ptr64 * __ptr64,struct IObjectArray * __ptr64 * __ptr64) __ptr64 (?GetDesktopSwitchIncludeExcludeViews@CVirtualDesktopManager@@WBA@EAAJPEAUIVirtualDesktop@@PEAPEAUIObjectArray@@1@Z)
    Method 16: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::SetDesktopName`adjustor{16}' (struct IVirtualDesktop * __ptr64,struct HSTRING__ * __ptr64) __ptr64 (?SetDesktopName@CVirtualDesktopManager@@WBA@EAAJPEAUIVirtualDesktop@@PEAUHSTRING__@@@Z)
    Method 17: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::SetDesktopWallpaper`adjustor{16}' (struct IVirtualDesktop * __ptr64,struct HSTRING__ * __ptr64) __ptr64 (?SetDesktopWallpaper@CVirtualDesktopManager@@WBA@EAAJPEAUIVirtualDesktop@@PEAUHSTRING__@@@Z)
    Method 18: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::UpdateWallpaperPathForAllDesktops`adjustor{16}' (struct HSTRING__ * __ptr64) __ptr64 (?UpdateWallpaperPathForAllDesktops@CVirtualDesktopManager@@WBA@EAAJPEAUHSTRING__@@@Z)
    Method 19: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::CopyDesktopState`adjustor{16}' (struct IApplicationView * __ptr64,struct IApplicationView * __ptr64) __ptr64 (?CopyDesktopState@CVirtualDesktopManager@@WBA@EAAJPEAUIApplicationView@@0@Z)
    Method 20: [thunk]:public: virtual long __cdecl CVirtualDesktopManager::GetDesktopIsPerMonitor`adjustor{16}' (int * __ptr64) __ptr64 (?GetDesktopIsPerMonitor@CVirtualDesktopManager@@WBA@EAAJPEAH@Z)
```

Create a matching `IVirtualDesktopManagerInternal` C# interface that matches the _exact_ signature of the function listing above.
In other words, you must translate Method 3 through Method 20 into a C# interface functions. You will need to create
an additional C# interface for each type that is referenced in the method listing above. For example, `MoveViewToDesktop`
references an `IApplicationView` type which also needs to be translated to a C# interface. Each of these interfaces
must be labeled with a `[GUID]` attribute. You can determine the `GUID` to place in this attribute by following [Item 3](#item-3)
below.

### Item 3
The `ComImport` can be found at the beginning of the Python script dump:
```
IID_IVirtualDesktopNotification: CD403E52-DEED-4C13-B437-B98380F2B1E8
IID_IVirtualDesktopNotificationService: 0CD45E71-D927-4F15-8B0A-8FEF525337BF
IID_IVirtualDesktopManagerInternal: B2F925B9-5A0F-4D2E-9F4D-2B1507593C10
...
```

These `GUID`s should be copied above the corresponding C# interface:
```c#
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("0CD45E71-D927-4F15-8B0A-8FEF525337BF")] // copy to here
internal interface IVirtualDesktopNotificationService {
```

You'll need to make sure to copy over all `[Guid]` attribute tags to match the Python dump.

While WinJump only needs the `IVirtualDesktopNotificationService` and `IVirtualDesktopManagerInternal` services,
their function signatures reference other interface types. This means that you'll have to consult the GUID
for every referenced type as explained in [Item #2](#Item-3).

