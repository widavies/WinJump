Ever wanted a way to jump to the `Nth` desktop on Windows 10 or 11 with a keyboard shortcut? WinJump maps `WINDOWS_KEY + <NUMBER>` as a shortcut for the first 10 desktops in Windows.

- WinJump uses the excellent [VirtualDesktop](https://github.com/MScholtes/VirtualDesktop) library. Most other solutions use an [AutoHotKey](https://www.autohotkey.com/) based solution which automates pressing the Windows built-in `WINDOWS_KEY + CTRL + [LEFT ARROW OR RIGHT ARROW]` shortcut a number of times in a row. 
This often results in glitchly visuals and lagging while jumping to the desktop you want. VirtualDesktop instead immediately jumps to the desired desktop.

Currently, the following versions of Windows are supported:
| Windows Edition      | Version |
| ----------- | ----------- |
| Windows 10      | 1607-1709, 1803, 1809 - 21H2       |
| Windows 11   | 21H2, 22H2       |

# Installation
1) [Download](https://github.com/widavies/WinJump/releases/download/1.0.0/Release_1_0_0.zip)
2) Extract and run `setup.exe`
3) Run WinJump
4) To make WinJump run at startup:
  1) Press the windows key and search for `"WinJump"`
  2) Right click > Open file location
  3) Copy the file named "WinJump"
  4) Press `WINDOWS_KEY + R` and type `shell:startup`
  5) Paste the file you copied into this directory.
