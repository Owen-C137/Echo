using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using System.IO;
using Echo.Models;

namespace Echo.Services
{
    public class UpdateChecker
    {
        private const string GITHUB_API_URL = "https://api.github.com/repos/Owen-C137/Echo/releases/latest";
        private static readonly HttpClient _httpClient = new HttpClient();

        static UpdateChecker()
        {
            // GitHub API requires User-Agent header
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Echo-UpdateChecker");
            
            // Try to load GitHub token from .env file for higher rate limits (5000/hour vs 60/hour)
            TryLoadGitHubToken();
        }

        private static void TryLoadGitHubToken()
        {
            try
            {
                var envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
                if (File.Exists(envPath))
                {
                    var lines = File.ReadAllLines(envPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("GITHUB_TOKEN=") && !line.StartsWith("#"))
                        {
                            var token = line.Substring("GITHUB_TOKEN=".Length).Trim();
                            if (!string.IsNullOrWhiteSpace(token) && token != "your_github_token_here")
                            {
                                _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {token}");
                                Logger.Info("GitHub token loaded from .env file - using higher rate limits");
                                return;
                            }
                        }
                    }
                }
                Logger.Info("No GitHub token configured - using standard rate limits (60/hour)");
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load GitHub token from .env: {ex.Message}");
            }
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                // Get current version
                var currentVersion = GetCurrentVersion();
                Logger.Info($"Current version: {currentVersion}");

                // Call GitHub API
                var response = await _httpClient.GetStringAsync(GITHUB_API_URL);
                var release = JsonSerializer.Deserialize<GitHubRelease>(response);

                if (release == null)
                {
                    Logger.Warning("Failed to deserialize GitHub release response");
                    return null;
                }

                // Parse version from tag (e.g., "v1.1.0" -> "1.1.0")
                var latestVersionString = release.TagName.TrimStart('v');
                var latestVersion = Version.Parse(latestVersionString);

                Logger.Info($"Latest version: {latestVersion}");

                // Compare versions
                if (latestVersion > currentVersion)
                {
                    // Find the ZIP asset
                    var asset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip"));
                    if (asset == null)
                    {
                        Logger.Warning("No ZIP asset found in release");
                        return null;
                    }

                    Logger.Info($"Update available: {currentVersion} -> {latestVersion}");

                    return new UpdateInfo
                    {
                        Version = latestVersion,
                        DownloadUrl = asset.BrowserDownloadUrl,
                        Changelog = release.Body,
                        ReleaseDate = release.PublishedAt,
                        FileSize = asset.Size,
                        FileName = asset.Name
                    };
                }

                Logger.Info("Application is up to date");
                return null; // Up to date
            }
            catch (HttpRequestException ex)
            {
                Logger.Error("Failed to check for updates (network error)", ex);
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to check for updates", ex);
                return null;
            }
        }

        private Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version 
                   ?? new Version(1, 0, 0);
        }
    }

    // GitHub API response models
    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("body")]
        public string Body { get; set; } = "";

        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = new();

        [JsonPropertyName("prerelease")]
        public bool PreRelease { get; set; }
    }

    public class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = "";

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }
}
