using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
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

            var killExplorer = Process.Start("cmd.exe", "/c taskkill /f /im explorer.exe");

            killExplorer.WaitForExit();

            Process.Start(Environment.SystemDirectory + "\\..\\explorer.exe");

            Application.Current.Shutdown();
        }
    };

    public string Version =>
        $"WinJump {FileVersionInfo.GetVersionInfo(typeof(TrayModel).Assembly.Location).FileVersion}";
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