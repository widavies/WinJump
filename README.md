![image](https://github.com/widavies/WinJump/assets/11671115/6e370296-cd73-4def-a256-de0b6df3bfb8)

Configure a custom keyboard shortcut to jump between virtual desktops in Windows. 

Features:
- Configure a custom keyboard shortcut (like `alt + N` or `win + N`) to jump directly to a virtual desktop
- See what virtual desktop you're currently on in the system tray:

![image](https://user-images.githubusercontent.com/11671115/232614847-1f8ccd7f-d5b8-429b-a67c-7f94cc5e18d9.png)

- Group virtual desktops together and cycle through them with a custom shortcut key
- Configure a shortcut to jump back and forth between two virtual desktops

## Installation
1. [Download](https://github.com/widavies/WinJump/releases/download/2.0.3/WinJump.exe)
> Note, you may receive a Windows smartscreen warning when you try to run WinJump. I do not feel like spending hundreds of dollars on a certificate, so just click "More options" and click "Run anyway"
2. Press Ctrl+R and type `shell:startup`
3. Drag the `WinJump.exe` to the shell startup folder

### Config file

To configure WinJump, right click the system tray icon and select "open config file":

![image](https://github.com/widavies/WinJump/assets/11671115/ee7c6b1d-0b33-4c45-a965-99c523564125)

#### Syntax

There are three blocks:

- `toggle-groups` let you group desktops together and cycle through them with a keyboard shortcut
- `jump-current-goes-to-last` lets you decide whether jumping to the desktop you're already on does A) nothing or B) goes to your previous desktop
- `jump-to` lets you define shortcuts that jump directly to a desktop

The `toggle-groups` and `jump-to` blocks contain a list of items, each item has a `shortcut` property. This shortcut must be a combination of:
`win`, `alt`, `shift`, and `ctrl`, it must be terminated by a key listed [here](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.keys?view=windowsdesktop-7.0),
and each token must be separated by `+`.

Each `toggle-groups` item has the `desktops` property, which should be a list of positive integers, with `1` representing the first desktop.

Each `jump-to` item has the `desktop` property, which should be a single positive integer, with `1` representing the first desktop.

> ⚠️ If no *.winjump* config file is found or a syntax error exists within it, WinJump will use default key mappings.

> ⚠️ WinJump does not auto-reload your configuration file. To apply changes, right click the system-tray icon and select `Reload configuration`.

#### Example

Below is an example configuration file that changes the shortcut to `alt+N` to jump to a desktop and adds a toggle group that is triggered by `alt+w` that will cycle between desktops `1`, `5`, and `6`:

```json
// C:\Users\<UserName>\.winjump
{
  "toggle-groups": [
    {
      "shortcut": "alt+w",
      "desktops": [ 1, 5, 6 ]
    }
  ],
  "jump-current-goes-to-last": false,
  "jump-to": [
    {
      "shortcut": "alt+d1",
      "desktop": 1
    },
    {
      "shortcut": "alt+d2",
      "desktop": 2
    },
    {
      "shortcut": "alt+d3",
      "desktop": 3
    },
    {
      "shortcut": "alt+d4",
      "desktop": 4
    },
    {
      "shortcut": "alt+d5",
      "desktop": 5
    },
    {
      "shortcut": "alt+d6",
      "desktop": 6
    },
    {
      "shortcut": "alt+d7",
      "desktop": 7
    },
    {
      "shortcut": "alt+d8",
      "desktop": 8
    },
    {
      "shortcut": "alt+d9",
      "desktop": 9
    },
    {
      "shortcut": "alt+d0",
      "desktop": 10
    }
  ]
}
```

# Advanced
> WinJump uses the reverse engineered Windows virtual desktop API. This means that the API often changes between Windows releases. Please see the [reverse engineering guide](https://github.com/widavies/WinJump/blob/main/WinJump/Core/VirtualDesktopDefinitions/README.md) if you're interested in contributing reverse-engineering definitions for new Windows releases.

# Uninstall
1. Press Ctrl+R and type `shell:startup`
2. Delete `WinJump.exe`
