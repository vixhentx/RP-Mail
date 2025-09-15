using System;
using System.Diagnostics;

namespace RPMailUI.Services;

public static class PathOpenHelper
{
    private static readonly Action<string> _openFileMethod;
    private static readonly Action<string> _openDirectoryMethod;
    public static void OpenFilePath(string path) => _openFileMethod(path);
    public static void OpenDirectory(string path) => _openDirectoryMethod(path);

    static PathOpenHelper()
    {
        if (OperatingSystem.IsWindows())
        {
            _openFileMethod = path => Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select, \"{path}\"",
                UseShellExecute = true
            });
            _openDirectoryMethod = path => Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = path,
                UseShellExecute = true
            });
        }
        else if (OperatingSystem.IsLinux())
        {
            _openFileMethod = path => 
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{path}\"",
                    UseShellExecute = true
                });
            };

            _openDirectoryMethod = path => Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });
        }
        else if (OperatingSystem.IsMacOS())
        {
            _openFileMethod = path => Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"-R \"{path}\"",
                UseShellExecute = true
            });

            // macOS: 打开目录
            _openDirectoryMethod = path => Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
    }
}