using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Deadlock_Mod_Loader2
{
    public class GameInfoPatcher
    {
        private readonly string gameInfoPath;
        private readonly string activeModsPath;
        private readonly ModCatalogManager catalogManager;

        public GameInfoPatcher(string gameInfoPath, string activeModsPath, ModCatalogManager catalogManager)
        {
            this.gameInfoPath = gameInfoPath;
            this.activeModsPath = activeModsPath;
            this.catalogManager = catalogManager;
        }

        public bool PatchGameInfoFile()
        {
            try
            {
                if (!Directory.Exists(activeModsPath))
                {
                    Directory.CreateDirectory(activeModsPath);
                }

                if (string.IsNullOrWhiteSpace(gameInfoPath) || !File.Exists(gameInfoPath))
                {
                    MessageBox.Show("gameinfo.gi not found. Please ensure the game path is correct.", "Error");
                    return false;
                }

                string backupPath = gameInfoPath + ".bak";
                if (!File.Exists(backupPath))
                {
                    File.Copy(gameInfoPath, backupPath);
                }

                string content = File.ReadAllText(gameInfoPath);
                bool needsUpdate = false;

                var requiredSearchPaths = new List<string>
                {
                    "Game\t\t\tcitadel/addons",
                    "Game\t\t\tcitadel/maps",
                    "Game\t\t\tcitadel/cfg",
                    "Game\t\t\tcitadel/materials",
                    "Game\t\t\tcitadel/models",
                    "Game\t\t\tcitadel/scripts",
                    "Game\t\t\tcitadel/sounds",
                    "Game\t\t\tcitadel/particles",
                    "Game\t\t\tcitadel",
                    "Write\t\t\tcitadel",
                    "Mod\t\t\tcitadel",
                    "Game\t\t\tcore",
                    "Write\t\t\tcore",
                    "Mod\t\t\tcore"
                };

                var searchPathRegex = new Regex(@"(^\s*""?SearchPaths""?\s*\{)([\s\S]*?)(\s*\})", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                var searchPathMatch = searchPathRegex.Match(content);

                if (searchPathMatch.Success)
                {
                    string existingPaths = searchPathMatch.Groups[2].Value;
                    var pathsToAdd = new StringBuilder();

                    foreach (var requiredPath in requiredSearchPaths)
                    {
                        var pathParts = requiredPath.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        var key = pathParts[0].Trim();
                        var value = pathParts[1].Trim();

                        var pathPattern = new Regex($@"^\s*{Regex.Escape(key)}\s+""?{Regex.Escape(value)}""?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                        if (!pathPattern.IsMatch(existingPaths))
                        {
                            pathsToAdd.AppendLine($"\t\t{key}\t\t\t{value}");
                            needsUpdate = true;
                        }
                    }

                    if (pathsToAdd.Length > 0)
                    {
                        content = content.Insert(searchPathMatch.Groups[1].Index + searchPathMatch.Groups[1].Length, "\n" + pathsToAdd.ToString());
                    }
                }
                else
                {
                    MessageBox.Show("Could not find 'SearchPaths' block in gameinfo.gi. Please verify game files in Steam.", "Error");
                    return false;
                }

                var addonConfigRegex = new Regex(@"^\s*""?AddonConfig""?\s*\{[\s\S]*?\}", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                if (!addonConfigRegex.IsMatch(content))
                {
                    string addonConfigBlock = "\n\tAddonConfig\n\t{\n\t\t\"UseOfficialAddons\" \"1\"\n\t}\n";
                    var fileSystemRegex = new Regex(@"(^\s*""?FileSystem""?\s*\{[\s\S]*?\s*\})", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    var fsMatch = fileSystemRegex.Match(content);
                    if (fsMatch.Success)
                    {
                        content = content.Insert(fsMatch.Index + fsMatch.Length, addonConfigBlock);
                        needsUpdate = true;
                    }
                }

                if (needsUpdate)
                {
                    File.WriteAllText(gameInfoPath, content, new UTF8Encoding(false));
                    MessageBox.Show("gameinfo.gi has been successfully updated for modding!", "Success");
                }

                UpdateModSearchPaths(catalogManager.LoadManifest());

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while patching gameinfo.gi. Please try running as administrator.\n\nError: {ex.Message}", "Patch Failed");
                return false;
            }
        }

        public void UpdateModSearchPaths(List<ActiveModInfo> orderedMods)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(gameInfoPath) || !File.Exists(gameInfoPath)) return;

                catalogManager.SaveManifest(orderedMods);
                string content = File.ReadAllText(gameInfoPath);

                var modPaths = new StringBuilder();
                foreach (var mod in orderedMods)
                {
                    if (mod.Type == ModType.VpkOnly || mod.Type == ModType.Mixed)
                    {
                        modPaths.AppendLine($"\t\tGame\t\t\tcitadel/addons/{mod.OriginalFolderName}");
                    }
                }

                var searchPathRegex = new Regex(@"(SearchPaths\s*\{)([\s\S]*?)(\})",
                    RegexOptions.Multiline | RegexOptions.IgnoreCase);
                var searchPathMatch = searchPathRegex.Match(content);

                if (searchPathMatch.Success)
                {
                    string existingPaths = searchPathMatch.Groups[2].Value;
                    var lines = existingPaths.Split('\n');
                    var cleanedLines = new List<string>();

                    foreach (string line in lines)
                    {
                        string trimmedLine = line.Trim();
                        bool isModSpecificPath = trimmedLine.StartsWith("Game") &&
                                                trimmedLine.Contains("citadel/addons/") &&
                                                !trimmedLine.EndsWith("citadel/addons");

                        if (!isModSpecificPath && !string.IsNullOrWhiteSpace(trimmedLine))
                        {
                            cleanedLines.Add(line);
                        }
                    }
                    string rebuiltPaths = "\n" + modPaths.ToString() + string.Join("\n", cleanedLines) + "\n\t";

                    string newSearchPathsBlock = searchPathMatch.Groups[1].Value + rebuiltPaths + searchPathMatch.Groups[3].Value;
                    content = content.Replace(searchPathMatch.Value, newSearchPathsBlock);

                    File.WriteAllText(gameInfoPath, content, new UTF8Encoding(false));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update mod load order:\n{ex.Message}", "Error");
            }
        }

        public void RestoreGameInfoBackup()
        {
            try
            {
                if (string.IsNullOrEmpty(gameInfoPath))
                {
                    MessageBox.Show("Game path is not set.", "Error");
                    return;
                }

                string backupPath = gameInfoPath + ".bak";
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, gameInfoPath, true);
                    MessageBox.Show("Successfully restored gameinfo.gi from backup. Any modding changes have been removed.", "Restore Successful");
                }
                else
                {
                    MessageBox.Show("No backup file (gameinfo.gi.bak) was found. Please verify game files in Steam to restore.", "Backup Not Found");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to restore backup: {ex.Message}", "Error");
            }
        }

        public string ReadFileWithoutBOM(string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            if (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
            {
                fileBytes = fileBytes.Skip(3).ToArray();
            }
            return Encoding.UTF8.GetString(fileBytes);
        }

        public void WriteFileWithoutBOM(string filePath, string content)
        {
            File.WriteAllText(filePath, content, new UTF8Encoding(false));
        }
    }
}