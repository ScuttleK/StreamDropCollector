using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Diagnostics;
using Microsoft.Win32;
using System.Windows;
using Core.Logging;
using System.IO;

namespace Core
{
    public static class Utility
    {
        /// <summary>
        /// Launch a web URL on Windows, Linux and OSX
        /// </summary>
        /// <param Name="url">The URL to open in the standard browser</param>
        public static void LaunchWeb(string url)
        {
            try
            {
                // UseShellExecute launches the URL via the OS shell association directly (no cmd.exe/shell
                // string parsing involved), which is both the fix for .NET Core no longer shell-executing
                // Process.Start(string) by default and safer than the previous cmd.exe fallback below (which
                // only escaped '&', not other shell metacharacters, in a hand-built command line).
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Utility", $"UseShellExecute launch failed for url '{url}'. Falling back by platform. {ex.Message}");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw new Exception("Could not open the browser on this machine");
                }
            }
        }

        internal static void WriteToRegistry(string keyName, string keyValue, string[]? arguments = null)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");

                // Quote each argument individually - this registry value runs at every Windows login, so an
                // unquoted argument containing a space would silently split into two arguments instead of one.
                string quotedArgs = arguments != null
                    ? string.Join(" ", arguments.Select(a => $"\"{a}\""))
                    : string.Empty;
                string command = string.IsNullOrEmpty(quotedArgs) ? $"\"{keyValue}\"" : $"\"{keyValue}\" {quotedArgs}";

                AppLogger.Debug("Utility", $"Registry Key Check: {key.GetValue(keyName)}");
                AppLogger.Debug("Utility", $"Registry Key Write: {command}");

                key.SetValue(keyName, command);

                key.Close();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Utility", $"Failed to write startup registry key '{keyName}'.", ex);
                MessageBox.Show(ex.Message, "Stream Drop Collector", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        internal static void RemoveFromRegistry(string keyName)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");

                AppLogger.Debug("Utility", $"{keyName}");
                AppLogger.Debug("Utility", $"Registry Key Before Delete: {key.GetValue(keyName)}");

                if (key.GetValue(keyName) != null)
                    key.DeleteValue(keyName);

                key.Close();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Utility", $"Failed to remove startup registry key '{keyName}'.", ex);
                MessageBox.Show(ex.Message, "Stream Drop Collector", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static string GetExePath()
        {
            string? exeLocation = Process.GetCurrentProcess().MainModule?.FileName;

            string executingDir = AppDomain.CurrentDomain.BaseDirectory;
            string executingName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

            return exeLocation ?? $"{Path.Combine(executingDir, executingName)}.exe";
        }

        public class RelayCommand<T>(Func<T?, Task> executeAsync) : ICommand
        {
            public event EventHandler? CanExecuteChanged;
            public bool CanExecute(object? parameter) => true;
            public async void Execute(object? parameter) => await executeAsync(parameter is T t ? t : default);
        }
    }
}