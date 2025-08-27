using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Deadlock_Mod_Loader2
{
    public class UpdateChecker
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string CURRENT_VERSION = "1.4"; // Update this with each release

        private const string GITHUB_API_URL = "https://api.github.com/repos/Tylevo/DeadlockModManager/releases/latest";
        private const string GITHUB_RELEASES_URL = "https://github.com/Tylevo/DeadlockModManager/releases";

        private const string GAMEBANANA_PAGE_URL = "https://gamebanana.com/tools/20525"; // Update when you have the actual URL

        private const string BACKUP_CHECK_URL = "https://raw.githubusercontent.com/Tylevo/DeadlockModManager/main/version.txt";

        static UpdateChecker()
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DeadlockModManager/1.4");
            httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public static async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                var githubResult = await CheckGitHubUpdatesAsync();
                if (githubResult != null) return githubResult;

                var simpleResult = await CheckSimpleVersionAsync();
                if (simpleResult != null) return simpleResult;
                return new UpdateInfo
                {
                    IsUpdateAvailable = false,
                    CurrentVersion = CURRENT_VERSION,
                    Message = "Could not check for updates automatically. Click 'Visit Page' to check manually.",
                    DownloadUrl = GAMEBANANA_PAGE_URL
                };
            }
            catch (Exception ex)
            {
                return new UpdateInfo
                {
                    IsUpdateAvailable = false,
                    CurrentVersion = CURRENT_VERSION,
                    Message = $"Update check failed: {ex.Message}",
                    DownloadUrl = GAMEBANANA_PAGE_URL
                };
            }
        }

        private static async Task<UpdateInfo> CheckGitHubUpdatesAsync()
        {
            try
            {
                var response = await httpClient.GetStringAsync(GITHUB_API_URL);
                var release = JsonConvert.DeserializeObject<GitHubRelease>(response);

                if (release != null && !string.IsNullOrEmpty(release.TagName))
                {
                    string latestVersion = release.TagName.TrimStart('v'); // Remove 'v' prefix if present
                    bool isNewer = IsVersionNewer(latestVersion, CURRENT_VERSION);

                    return new UpdateInfo
                    {
                        IsUpdateAvailable = isNewer,
                        CurrentVersion = CURRENT_VERSION,
                        LatestVersion = latestVersion,
                        ReleaseNotes = release.Body,
                        DownloadUrl = release.HtmlUrl,
                        Message = isNewer
                            ? $"Version {latestVersion} is available!"
                            : "You have the latest version."
                    };
                }
            }
            catch
            {
            }
            return null;
        }

        private static async Task<UpdateInfo> CheckSimpleVersionAsync()
        {
            try
            {
                var response = await httpClient.GetStringAsync(BACKUP_CHECK_URL);
                var lines = response.Split('\n');

                if (lines.Length > 0)
                {
                    string latestVersion = lines[0].Trim();
                    string releaseNotes = lines.Length > 1 ? string.Join("\n", lines, 1, lines.Length - 1) : "";

                    bool isNewer = IsVersionNewer(latestVersion, CURRENT_VERSION);

                    return new UpdateInfo
                    {
                        IsUpdateAvailable = isNewer,
                        CurrentVersion = CURRENT_VERSION,
                        LatestVersion = latestVersion,
                        ReleaseNotes = releaseNotes,
                        DownloadUrl = GAMEBANANA_PAGE_URL,
                        Message = isNewer
                            ? $"Version {latestVersion} is available!"
                            : "You have the latest version."
                    };
                }
            }
            catch
            {
            }
            return null;
        }

        private static bool IsVersionNewer(string latestVersion, string currentVersion)
        {
            try
            {
                var latest = new Version(NormalizeVersion(latestVersion));
                var current = new Version(NormalizeVersion(currentVersion));
                return latest > current;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeVersion(string version)
        {
            var parts = version.Split('.');
            if (parts.Length == 1)
                return version + ".0";
            return version;
        }

        public static void ShowUpdateDialog(UpdateInfo updateInfo, IWin32Window owner = null)
        {
            if (updateInfo.IsUpdateAvailable)
            {
                var result = MessageBox.Show(
                    $"{updateInfo.Message}\n\n" +
                    $"Current Version: {updateInfo.CurrentVersion}\n" +
                    $"Latest Version: {updateInfo.LatestVersion}\n\n" +
                    $"Would you like to visit GitHub to download the update?",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(GITHUB_RELEASES_URL);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not open browser: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            else
            {
                MessageBox.Show(updateInfo.Message, "Update Check",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    public class UpdateInfo
    {
        public bool IsUpdateAvailable { get; set; }
        public string CurrentVersion { get; set; }
        public string LatestVersion { get; set; }
        public string ReleaseNotes { get; set; }
        public string DownloadUrl { get; set; }
        public string Message { get; set; }
    }

    public class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }

        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonProperty("prerelease")]
        public bool IsPrerelease { get; set; }
    }
}