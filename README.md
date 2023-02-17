# Overview
Ever wanted a way to jump to the `Nth` desktop on Windows 10 or 11 with a keyboard shortcut? WinJump maps `WINDOWS_KEY + <NUMBER>` as a shortcut for the first 10 desktops in Windows.

WinJump uses the excellent [VirtualDesktop](https://github.com/MScholtes/VirtualDesktop) library. Most other solutions use an [AutoHotKey](https://www.autohotkey.com/) based solution which automates pressing the Windows built-in `WINDOWS_KEY + CTRL + [LEFT ARROW OR RIGHT ARROW]` shortcut a number of times in a row. 
This often results in glitchly visuals and lagging while jumping to the desktop you want. VirtualDesktop instead immediately jumps to the desired desktop.

# Installation
## Supported versions
Currently, the following versions of Windows are supported:
| Windows Edition      | Version |
| ----------- | ----------- |
| Windows 10      | 1607-1709, 1803, 1809 - 21H2       |
| Windows 11   | 21H2, 22H2       |
## How to install
1) [Download](https://github.com/widavies/WinJump/releases/download/1.3.0/Release_1_3_0.zip)
2) Extract and run `setup.exe`
3) You're done! WinJump will start automatically and will also register itself to start when your computer boots.

## Config file
You can optionally include a configuration file named `.winjump` in your home directory to change default behavior.

### Syntax
There are two blocks:

- `toggle-groups` let you group desktops together and cycle through them with a keyboard shortcut
- `jump-to` lets you define shortcuts that jump directly to a desktop

Both blocks contain a list of items, each item has a `shortcut` keyword. This shortcut keyword must be a combination of:
`win`, `alt`, `shift`, and `ctrl`, it must be terminated by a key listed [here](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.keys?view=windowsdesktop-7.0),
and each token must be separated by `+`.


### Example
```
C:\Users\<UserName>\.winjump

{
  "toggle-groups": [
    {
      "shortcut": "win+w",
      "desktops": [1, 3, 4]
    }
  ],
  "jump-to": [
     {
       shortcut: "win+d1",
       desktop: 1
     },
     {
       shortcut: "alt+d2,
       desktop: 2
     },
     {
       shortcut: "alt+d3",
       desktop: 3
     },
     {
       shortcut: "alt+d4",
       desktop: 4
     },
     {
       shortcut: "alt+d5",
       desktop: 5
     },
     {
       shortcut: "alt+d6",
       desktop: 6
     },
     {
       shortcut: "alt+d7",
       desktop: 7
     },
     {
       shortcut: "alt+d8",
       desktop: 8
     },
     {
       shortcut: "alt+d9",
       desktop: 9
     },
     {
       shortcut: "alt+d0",
       desktop: 10
    }
  ]
}
```
