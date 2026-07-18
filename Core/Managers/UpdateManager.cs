using System.Text.Json;
using System.IO.Compression;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using Core.Models;
using Core.Logging;
using System.IO;

namespace Core.Managers
{
    public sealed class UpdateManager
    {
        private static readonly Lazy<UpdateManager> _instance = new(() => new UpdateManager());
        public static UpdateManager Instance => _instance.Value;

        private readonly string _repositoryOwner = "Scuttle-ZapAccess";
        private readonly string _repositoryName = "StreamDropCollector";

        public event EventHandler<ProgressEventArgs>? DownloadProgress;

        private UpdateManager()
        { }

        /// <summary>
        /// Downloads the latest self-contained release build from GitHub and restarts the application to apply it.
        /// </summary>
        /// <remarks>Reads the target version from <c>updateInfo.sdc</c> on the repository's default
        /// branch, downloads the matching self-contained release asset (no .NET runtime required on the user's
        /// machine), extracts it, and relaunches. Any errors encountered during the update process are logged and
        /// surfaced as a notification. This method should typically be called from the UI thread, as it may cause
        /// the application to exit and restart.</remarks>
        public async Task DownloadUpdate()
        {
            string basePath = Path.Combine(Environment.ExpandEnvironmentVariables("%APPDATA%"), "Stream Drop Collector");
            string updatePath = Path.Combine(basePath, "Update");
            string zipPath = Path.Combine(basePath, "update.zip");

            try
            {
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("StreamDropCollector-Updater");

                string updateInfoJson = await client.GetStringAsync($"https://raw.githubusercontent.com/{_repositoryOwner}/{_repositoryName}/master/updateInfo.sdc");
                UpdateInfo? updateInfo = JsonSerializer.Deserialize<UpdateInfo>(updateInfoJson);

                if (string.IsNullOrWhiteSpace(updateInfo?.Version))
                    throw new InvalidOperationException("Could not determine the latest version to download.");

                string downloadUrl = $"https://github.com/{_repositoryOwner}/{_repositoryName}/releases/download/v{updateInfo.Version}/StreamDropCollector-v{updateInfo.Version}-self-contained.zip";
                AppLogger.Info("UpdateManager", $"Downloading update from {downloadUrl}");

                if (Directory.Exists(updatePath))
                    Directory.Delete(updatePath, true);
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                using (HttpResponseMessage response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long? totalBytes = response.Content.Headers.ContentLength;
                    await using FileStream fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await using Stream downloadStream = await response.Content.ReadAsStreamAsync();

                    byte[] buffer = new byte[81920];
                    long totalRead = 0;
                    int read;
                    while ((read = await downloadStream.ReadAsync(buffer)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, read));
                        totalRead += read;

                        if (totalBytes.HasValue)
                            OnProgressChanged(new ProgressEventArgs((int)(totalRead * 100 / totalBytes.Value)));
                    }
                }

                // Verify integrity before extracting/running anything from the downloaded archive. HTTPS already
                // protects the transport, but this adds defense-in-depth against a compromised release asset or a
                // corrupted download, and it's checked against a hash published alongside the version manifest
                // rather than trusting the download alone.
                if (!string.IsNullOrWhiteSpace(updateInfo.Sha256))
                {
                    string actualHash;
                    await using (FileStream verifyStream = File.OpenRead(zipPath))
                        actualHash = Convert.ToHexString(await SHA256.HashDataAsync(verifyStream)).ToLowerInvariant();

                    if (!string.Equals(actualHash, updateInfo.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(zipPath);
                        throw new InvalidOperationException($"Downloaded update failed integrity check (expected sha256={updateInfo.Sha256}, got {actualHash}). Aborting update.");
                    }

                    AppLogger.Info("UpdateManager", "Update package passed SHA256 integrity check.");
                }
                else
                {
                    AppLogger.Warn("UpdateManager", "No sha256 published for this version; skipping integrity check.");
                }

                Directory.CreateDirectory(updatePath);
                ZipFile.ExtractToDirectory(zipPath, updatePath, overwriteFiles: true);
                File.Delete(zipPath);

                AppLogger.Info("UpdateManager", $"Update v{updateInfo.Version} downloaded and extracted successfully.");

                Process.Start(Path.Combine(updatePath, "Stream Drop Collector"), "--updating");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                NotificationManager.ShowNotification("Update Error", $"An error occurred while updating the application.\n{ex.Message}\n\nTry again after this time", 300);
                AppLogger.Error("UpdateManager", "DownloadUpdate failed.", ex);
            }
        }
        /// <summary>
        /// Raises the event that reports progress updates during an operation.
        /// </summary>
        /// <param name="e">A ProgressEventArgs object that contains the progress data, such as the percentage completed.</param>
        private void OnProgressChanged(ProgressEventArgs e)
        {
            DownloadProgress?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Provides data for events that report progress updates, including the current progress percentage.
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        public int Progress { get; set; }

        public ProgressEventArgs(int progress)
        {
            Progress = progress;
        }
    }
}
