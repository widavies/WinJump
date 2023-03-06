# WinJump

Ever wanted to jump directly to your `Nth` desktop on Windows 10 or 11 with a keyboard shortcut of your choice? WinJump enables you to create custom shortcuts to jump to any desktop and cycle between groups of desktops.

Most other solutions use an [AutoHotKey](https://www.autohotkey.com/) based solution which automates pressing the Windows default shortuct <kbd>Win</kbd> + <kbd>Ctrl</kbd> + <kbd>Left Arrow</kbd> or <kbd>Right Arrow</kbd> multiple times.
This often results in glitchly visuals and lagging while jumping to the desktop you want.
WinJump uses the excellent [VirtualDesktop](https://github.com/MScholtes/VirtualDesktop) library which jumps directly to the desired desktop.

## Features

### Jump To

Jump directly to a desktop with <kbd>Win</kbd> + [ <kbd>0</kbd> - <kbd>9</kbd> ] *(default)*.

### Toggle Groups

Cycle through a group of desktops with a single shortcut *(there are no groups by default)*.

### Back and Forth

Pressing the shortcut for the desktop you are currently on will jump back to the last desktop you were on.

## Installation

### Supported versions

Currently, the following versions of Windows are supported:
| Windows Edition      | Version |
| ----------- | ----------- |
| Windows 10      | 1607-1709, 1803, 1809 - 21H2       |
| Windows 11   | 21H2, 22H2       |

### How to install

1. [Download](https://github.com/widavies/WinJump/releases/download/1.4.0/Release_1_4_0.zip)
2. Extract and run *setup.exe*
3. You're done! WinJump will start automatically and will register itself to start when your computer boots.

### Config file

You can optionally include a configuration file named *.winjump* in your home directory to change the default behavior.

#### Syntax

There are two blocks:

- `toggle-groups` let you group desktops together and cycle through them with a keyboard shortcut
- `jump-to` lets you define shortcuts that jump directly to a desktop

Both blocks contain a list of items, each item has a `shortcut` property. This shortcut must be a combination of:
`win`, `alt`, `shift`, and `ctrl`, it must be terminated by a key listed [here](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.keys?view=windowsdesktop-7.0),
and each token must be separated by `+`.

Each `toggle-groups` item has the `desktops` property, which should be a list of positive integers, with `1` representing the first desktop.

Each `jump-to` item has the `desktop` property, which should be a single positive integer, with `1` representing the first desktop.

> ⚠️ If no *.winjump* config file is found or a syntax error exists within it, WinJump will use default key mappings.

> ⚠️ WinJump does not auto-reload your configuration file. To apply changes, restart WinJump via one of the following methods:
>
> - launch task manager, kill WinJump, launch it again from the start menu
> - log out and back in
> - reboot

#### Example

Below is an example configuration file that changes the shortcut to `alt+N` to jump to a desktop and adds a toggle group that is triggered by `alt+w` that will cycle between desktops `1`, `5`, and `6`:

```jsonc
// C:\Users\<UserName>\.winjump
{
  "toggle-groups": [
    {
      "shortcut": "alt+w",
      "desktops": [ 1, 5, 6 ]
    }
  ],
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

## Uninstallation

WinJump can be uninstalled via the windows application manager

1. Press the start button
2. Search for "Add or remove programs"
3. Find WinJump
4. Uninstall it

## Known issues

- Launching WinJump while it is already running will hang Windows explorer. To fix this you have to use `ctrl+shift+esc` to open task manager, kill all WinJump instances, use `Run new task` and type `explorer`, then start WinJump again
