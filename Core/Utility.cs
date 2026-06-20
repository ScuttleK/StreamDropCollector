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
                Process.Start(url);
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Utility", $"Process.Start direct launch failed for url '{url}'. Falling back by platform. {ex.Message}");
                // Hack for running the above line in DOTNET Core...
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
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
                AppLogger.Debug("Utility", $"Registry Key Check: {key.GetValue(keyName)}");
                AppLogger.Debug("Utility", $"Registry Key Write: \"{keyValue}\" {string.Join(" ", arguments ?? [])}");

                if (arguments != null)
                    key.SetValue(keyName, $"\"{keyValue}\" {string.Join(" ", arguments)}");
                else
                    key.SetValue(keyName, $"\"{keyValue}\"");

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