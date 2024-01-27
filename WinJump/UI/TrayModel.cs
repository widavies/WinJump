using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using WinJump.Core;

namespace WinJump.UI;

public class TrayModel {
    public ICommand OnOpenConfig => new DelegateCommand {
        CanExecuteFunc = () => true,
        CommandAction = () => {
            Config.EnsureCreated();

            ProcessStartInfo processStartInfo = new ProcessStartInfo {
                FileName = "notepad",
                Arguments = Config.LOCATION
            };

            Process.Start(processStartInfo);
        }
    };

    public ICommand ReloadConfig => new DelegateCommand {
        CanExecuteFunc = () => true,
        CommandAction = () => {
            try {
                App.SINGLE_INSTANCE_MUTEX.ReleaseMutex();
            } catch(Exception) {
                // ignored
            }

            string? currentExecutablePath = Process.GetCurrentProcess().MainModule?.FileName;

            if(currentExecutablePath == null) return;

            Process.Start(currentExecutablePath);
            Application.Current.Shutdown();
        }
    };

    public ICommand ViewDocumentation => new DelegateCommand {
        CanExecuteFunc = () => true,
        CommandAction = () => {
            // Open website
            Process.Start(new ProcessStartInfo {
                FileName = "https://github.com/widavies/WinJump",
                UseShellExecute = true
            });
        }
    };

    public ICommand Exit => new DelegateCommand {
        CanExecuteFunc = () => true,
        CommandAction = () => {
            // Restart explorer to clean out any registrations that were present
            // Only need to kill Explorer if we registered "win" shortcuts
            if(WinJumpManager.LastLoadRequiredExplorerRestart) {
                var killExplorer = Process.Start("cmd.exe", "/c taskkill /f /im explorer.exe");

                killExplorer.WaitForExit();

                Process.Start(Environment.SystemDirectory + "\\..\\explorer.exe");
            }

            Application.Current.Shutdown();
        }
    };

    public string Version =>
        $"WinJump {Assembly.GetEntryAssembly()?.GetName().Version?.ToString()}";

    public string WinVersion {
        get {
            try {
                WinVersion winVersion = Core.WinVersion.Determine();
                return $"Windows build: {winVersion.Build}.{winVersion.ReleaseBuild}";
            } catch {
                return "Windows build: Unknown";
            }
        }
    }
}

public class DelegateCommand : ICommand {
    public required Action CommandAction { get; init; }
    public required Func<bool> CanExecuteFunc { get; init; }

    public void Execute(object? parameter) {
        CommandAction();
    }

    public bool CanExecute(object? parameter) {
        return CanExecuteFunc();
    }

    public event EventHandler? CanExecuteChanged {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}