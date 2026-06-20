using Core.Interfaces;
using Core.Enums;
using Core.Logging;

namespace Core.Services
{
    public class TwitchLoginService : LoginServiceBase
    {
        public override async Task ValidateCredentialsAsync(IWebViewHost host)
        {
            AppLogger.Debug("TwitchLogin", "Validating credentials...");
            AppLogger.Info("TwitchLogin", "Credential validation started.");
            UpdateStatus(ConnectionStatus.Connecting);

            if (host == null)
            {
                AppLogger.Warn("TwitchLogin", "Validation aborted: host is null.");
                UpdateStatus(ConnectionStatus.NotConnected);
                return;
            }

            try
            {
                await host.EnsureInitializedAsync();
            }
            catch (Exception ex)
            {
                AppLogger.Warn("TwitchLogin", $"EnsureInitializedAsync failed but continuing: {ex.Message}");
            }

            await host.NavigateAsync("https://twitch.tv/");

            UpdateStatus(ConnectionStatus.Validating);

            string html = await GetPageHtmlAsync(host);
            bool isLoggedIn = !html.Contains("data-a-target=\"login-button\"", StringComparison.OrdinalIgnoreCase);

            AppLogger.Info("TwitchLogin", $"Credential validation completed. isLoggedIn={isLoggedIn}");

            UpdateStatus(isLoggedIn ? ConnectionStatus.Connected : ConnectionStatus.NotConnected);
        }
    }
}