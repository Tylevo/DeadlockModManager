using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Deadlock_Mod_Loader2
{
    public class ModInfo
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string FolderName { get; set; }
    }

    public class ActiveModInfo
    {
        public string ModName { get; set; }
        public List<string> PakPrefixes { get; set; } = new List<string>();
        public string OriginalFolderName { get; set; }
    }

    public class ModLoader
    {
        private string gamePath;
        private string activeModsPath;
        private string manifestPath;
        private string catalogPath;
        private string gameInfoPath;

        private const string DisabledPrefix = "_";
        private const string ModSep = "__";

        public string GetAddonsPath() => activeModsPath;

        public ModLoader() { }

        public bool SetGamePath(string path)
        {
            if (Directory.Exists(path) && Directory.Exists(Path.Combine(path, "citadel")))
            {
                gamePath = path;
                activeModsPath = Path.Combine(gamePath, "citadel", "addons");
                manifestPath = Path.Combine(activeModsPath, "active_mods.json");
                catalogPath = Path.Combine(activeModsPath, "mods_catalog.json");
                gameInfoPath = Path.Combine(gamePath, "citadel", "gameinfo.gi");

                if (Directory.Exists(activeModsPath))
                {
                    if (!File.Exists(manifestPath)) SaveManifest(new List<ActiveModInfo>());
                    if (!File.Exists(catalogPath)) SaveCatalog(new List<ModInfo>());
                }

                return true;
            }
            return false;
        }

        // ++ CHANGED ++ This method is now much more robust and handles loose VPK files.
        public void ImportUnmanagedMods()
        {
            if (string.IsNullOrEmpty(activeModsPath) || !Directory.Exists(activeModsPath))
            {
                MessageBox.Show("Addons path is not set or does not exist.", "Error");
                return;
            }

            int importedCount = 0;

            // --- Part 1: Find and organize any loose VPK files ---
            var looseVpks = Directory.GetFiles(activeModsPath, "*.vpk", SearchOption.TopDirectoryOnly).ToList();
            if (looseVpks.Any())
            {
                var vpkGroups = FindVpkGroups(looseVpks);
                foreach (var group in vpkGroups)
                {
                    // Generate a name and folder for this group of loose files
                    string modName = group.BaseName.Replace("_", " ").Replace("-", " ");
                    modName = Regex.Replace(modName, @"\s+", " ").Trim();
                    string folderName = MakeSafeIdentifier(modName);

                    string newModSubFolder = Path.Combine(activeModsPath, folderName);

                    // In case a folder with this name already exists, add a suffix
                    int suffix = 1;
                    while (Directory.Exists(newModSubFolder))
                    {
                        newModSubFolder = Path.Combine(activeModsPath, $"{folderName}_{suffix++}");
                    }
                    Directory.CreateDirectory(newModSubFolder);

                    // Move the loose files into their new home
                    foreach (var vpkFile in group.Files)
                    {
                        try
                        {
                            string destFile = Path.Combine(newModSubFolder, Path.GetFileName(vpkFile));
                            File.Move(vpkFile, destFile);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Could not move loose file '{Path.GetFileName(vpkFile)}'. It may be in use.\n\nError: {ex.Message}", "Import Warning");
                        }
                    }
                }
            }

            // --- Part 2: Discover and process all unmanaged folders (including newly created ones) ---
            var manifest = LoadManifest();
            var catalog = LoadCatalog();
            var managedFolders = manifest.Select(m => m.OriginalFolderName)
                                        .Concat(catalog.Select(c => c.FolderName))
                                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var allDirectories = Directory.GetDirectories(activeModsPath);
            var unmanagedDirs = allDirectories.Where(dir => !managedFolders.Contains(Path.GetFileName(dir))).ToList();

            foreach (var dir in unmanagedDirs)
            {
                if (ProcessModContents(dir, dir))
                {
                    importedCount++;
                }
            }

            if (importedCount > 0)
            {
                MessageBox.Show($"{importedCount} pre-existing mod(s) were successfully imported into the library.", "Import Complete");
            }
            else
            {
                MessageBox.Show("No unmanaged mods found to import.", "Import");
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
                else
                {
                    MessageBox.Show("gameinfo.gi is already configured correctly. No changes were made.", "All Good!");
                }

                UpdateModSearchPaths(LoadManifest());

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

                SaveManifest(orderedMods);

                string content = File.ReadAllText(gameInfoPath);

                var modPaths = new StringBuilder();
                foreach (var mod in orderedMods)
                {
                    modPaths.AppendLine($"\t\tGame\t\t\tcitadel/addons/{mod.OriginalFolderName}");
                }

                string modPathPattern = @"^\s*Game\s+""?citadel/addons/.*""?\s*$";

                var searchPathRegex = new Regex(@"(^\s*""?SearchPaths""?\s*\{)([\s\S]*?)(\s*\})", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                var searchPathMatch = searchPathRegex.Match(content);

                if (searchPathMatch.Success)
                {
                    string existingPaths = searchPathMatch.Groups[2].Value;
                    string cleanedPaths = Regex.Replace(existingPaths, modPathPattern, "", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                    cleanedPaths = string.Join("\n", cleanedPaths.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)));

                    string newPathsBlock = modPaths.ToString() + cleanedPaths;
                    string newSearchPathsBlock = searchPathMatch.Groups[1].Value + "\n" + newPathsBlock + "\n" + searchPathMatch.Groups[3].Value;
                    string newContent = content.Replace(searchPathMatch.Value, newSearchPathsBlock);

                    File.WriteAllText(gameInfoPath, newContent, new UTF8Encoding(false));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update mod load order: {ex.Message}", "Error");
            }
        }

        public void ActivateMod(ModInfo modToActivate)
        {
            var manifest = LoadManifest();
            if (manifest.Any(m => m.OriginalFolderName.Equals(modToActivate.FolderName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("This mod is already active.", "Warning");
                return;
            }

            string modSubFolderPath = Path.Combine(activeModsPath, modToActivate.FolderName);
            if (!Directory.Exists(modSubFolderPath)) Directory.CreateDirectory(modSubFolderPath);

            string searchPattern = DisabledPrefix + modToActivate.FolderName + ModSep + "*.vpk";
            var disabledFiles = Directory.GetFiles(activeModsPath, searchPattern);

            if (disabledFiles.Length == 0)
            {
                MessageBox.Show("No disabled files found for this mod. Try reinstalling it.", "Activate Warning");
                return;
            }

            var prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var df in disabledFiles)
            {
                string fileName = Path.GetFileName(df);
                string expectedPrefix = DisabledPrefix + modToActivate.FolderName + ModSep;
                if (fileName.StartsWith(expectedPrefix))
                {
                    string tail = fileName.Substring(expectedPrefix.Length);
                    string prefix = ExtractPakPrefixFromTail(tail);
                    if (!string.IsNullOrEmpty(prefix)) prefixes.Add(prefix);

                    try
                    {
                        string activeName = tail;
                        string dst = Path.Combine(modSubFolderPath, activeName);

                        if (File.Exists(dst)) File.Delete(dst);
                        File.Move(df, dst);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to activate file '{fileName}': {ex.Message}", "Activation Error");
                        return;
                    }
                }
            }

            var entry = new ActiveModInfo
            {
                ModName = modToActivate.Name,
                OriginalFolderName = modToActivate.FolderName,
                PakPrefixes = prefixes.ToList()
            };

            manifest.Add(entry);
            UpdateModSearchPaths(manifest);
        }

        public void DeactivateMod(ActiveModInfo modToDeactivate)
        {
            var manifest = LoadManifest();
            var modInManifest = manifest.FirstOrDefault(m => m.OriginalFolderName.Equals(modToDeactivate.OriginalFolderName, StringComparison.OrdinalIgnoreCase));
            if (modInManifest == null) return;

            string modSubFolderPath = Path.Combine(activeModsPath, modToDeactivate.OriginalFolderName);
            if (Directory.Exists(modSubFolderPath))
            {
                foreach (var file in Directory.GetFiles(modSubFolderPath, "*.vpk"))
                {
                    try
                    {
                        string activeName = Path.GetFileName(file);
                        string disabledName = DisabledName(modInManifest.OriginalFolderName, activeName);
                        string dst = Path.Combine(activeModsPath, disabledName);

                        if (File.Exists(dst)) File.Delete(dst);
                        File.Move(file, dst);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to deactivate file '{Path.GetFileName(file)}': {ex.Message}", "Deactivation Error");
                        return;
                    }
                }
                if (!Directory.EnumerateFileSystemEntries(modSubFolderPath).Any())
                {
                    Directory.Delete(modSubFolderPath);
                }
            }

            manifest.Remove(modInManifest);
            UpdateModSearchPaths(manifest);
        }

        #region Helper and Management Methods
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

        private List<ActiveModInfo> LoadManifest()
        {
            if (!File.Exists(manifestPath)) return new List<ActiveModInfo>();
            string json = File.ReadAllText(manifestPath);
            return JsonConvert.DeserializeObject<List<ActiveModInfo>>(json) ?? new List<ActiveModInfo>();
        }

        private void SaveManifest(List<ActiveModInfo> manifest)
        {
            string json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
            File.WriteAllText(manifestPath, json);
        }

        public List<ActiveModInfo> GetActiveMods() => LoadManifest();

        private List<ModInfo> LoadCatalog()
        {
            if (!File.Exists(catalogPath)) return new List<ModInfo>();
            string json = File.ReadAllText(catalogPath);
            return JsonConvert.DeserializeObject<List<ModInfo>>(json) ?? new List<ModInfo>();
        }

        private void SaveCatalog(List<ModInfo> catalog)
        {
            string json = JsonConvert.SerializeObject(catalog, Formatting.Indented);
            File.WriteAllText(catalogPath, json);
        }

        public List<ModInfo> GetAvailableMods()
        {
            var catalog = LoadCatalog();
            var active = LoadManifest().Select(m => m.OriginalFolderName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return catalog.Where(m => !active.Contains(m.FolderName))
                          .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                          .ToList();
        }

        public bool InstallDroppedFile(string filePath)
        {
            bool isDirectory = File.GetAttributes(filePath).HasFlag(FileAttributes.Directory);
            string tempDir = isDirectory ? filePath : Path.Combine(Path.GetTempPath(), "DeadlockModLoader_" + Guid.NewGuid().ToString("N"));

            try
            {
                if (!isDirectory)
                {
                    Directory.CreateDirectory(tempDir);
                    string extension = Path.GetExtension(filePath).ToLowerInvariant();

                    if (extension == ".zip")
                    {
                        ZipFile.ExtractToDirectory(filePath, tempDir);
                    }
                    else if (extension == ".rar")
                    {
                        if (!ExtractRarFile(filePath, tempDir))
                        {
                            MessageBox.Show("RAR extraction failed. Please install WinRAR or 7-Zip, or convert to ZIP format.", "RAR Extraction Error");
                            return false;
                        }
                    }
                    else if (extension == ".7z")
                    {
                        if (!Extract7ZipFile(filePath, tempDir))
                        {
                            MessageBox.Show("7Z extraction failed. Please install 7-Zip or convert to ZIP format.", "7Z Extraction Error");
                            return false;
                        }
                    }
                    else if (extension == ".vpk")
                    {
                        File.Copy(filePath, Path.Combine(tempDir, Path.GetFileName(filePath)));
                    }
                    else
                    {
                        MessageBox.Show($"Unsupported file format: {extension}\nSupported formats: .zip, .rar, .7z, .vpk", "Unsupported Format");
                        return false;
                    }
                }

                return ProcessModContents(tempDir, filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to install mod: {ex.Message}", "Error");
                return false;
            }
            finally
            {
                if (!isDirectory && Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        private bool ExtractRarFile(string rarPath, string extractPath)
        {
            try
            {
                string winrarPath = @"C:\Program Files\WinRAR\WinRAR.exe";
                if (!File.Exists(winrarPath))
                    winrarPath = @"C:\Program Files (x86)\WinRAR\WinRAR.exe";

                if (File.Exists(winrarPath))
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = winrarPath,
                        Arguments = $"x \"{rarPath}\" \"{extractPath}\\\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (var process = System.Diagnostics.Process.Start(startInfo))
                    {
                        process.WaitForExit();
                        return process.ExitCode == 0;
                    }
                }

                string sevenZipPath = @"C:\Program Files\7-Zip\7z.exe";
                if (!File.Exists(sevenZipPath))
                    sevenZipPath = @"C:\Program Files (x86)\7-Zip\7z.exe";

                if (File.Exists(sevenZipPath))
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = sevenZipPath,
                        Arguments = $"x \"{rarPath}\" -o\"{extractPath}\" -y",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (var process = System.Diagnostics.Process.Start(startInfo))
                    {
                        process.WaitForExit();
                        return process.ExitCode == 0;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool Extract7ZipFile(string archivePath, string extractPath)
        {
            try
            {
                string sevenZipPath = @"C:\Program Files\7-Zip\7z.exe";
                if (!File.Exists(sevenZipPath))
                    sevenZipPath = @"C:\Program Files (x86)\7-Zip\7z.exe";

                if (File.Exists(sevenZipPath))
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = sevenZipPath,
                        Arguments = $"x \"{archivePath}\" -o\"{extractPath}\" -y",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (var process = System.Diagnostics.Process.Start(startInfo))
                    {
                        process.WaitForExit();
                        return process.ExitCode == 0;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool ProcessModContents(string tempDir, string originalFilePath)
        {
            var modInfo = new ModInfo();

            string modJsonPath = Directory.GetFiles(tempDir, "modinfo.json", SearchOption.AllDirectories).FirstOrDefault();

            if (string.IsNullOrEmpty(modJsonPath))
            {
                var commonNames = new[] { "mod.json", "info.json", "addon.json" };
                foreach (var name in commonNames)
                {
                    modJsonPath = Directory.GetFiles(tempDir, name, SearchOption.AllDirectories).FirstOrDefault();
                    if (!string.IsNullOrEmpty(modJsonPath)) break;
                }
            }

            if (!string.IsNullOrEmpty(modJsonPath))
            {
                try
                {
                    var parsed = JsonConvert.DeserializeObject<ModInfo>(File.ReadAllText(modJsonPath));
                    if (parsed != null) modInfo = parsed;
                }
                catch { }
            }

            var allVpks = Directory.GetFiles(tempDir, "*.vpk", SearchOption.AllDirectories).ToList();
            if (!allVpks.Any())
            {
                MessageBox.Show("No .vpk files found.", "Install Failed");
                return false;
            }

            if (string.IsNullOrWhiteSpace(modInfo.Name))
            {
                string fileName = Path.GetFileName(originalFilePath);
                string modName = Path.GetFileNameWithoutExtension(fileName);

                int separatorIndex = modName.IndexOf(" - ");
                if (separatorIndex > 0)
                {
                    modName = modName.Substring(separatorIndex + 3).Trim();
                }
                else if (Regex.IsMatch(modName, @"_v?\d+(\.\d+)*$"))
                {
                    modName = Regex.Replace(modName, @"_v?\d+(\.\d+)*$", "");
                }
                else if (modName.Contains(" by "))
                {
                    modName = modName.Substring(0, modName.IndexOf(" by "));
                }

                modName = modName.Replace("_", " ").Replace("-", " ");
                modName = Regex.Replace(modName, @"\s+", " ").Trim();
                modInfo.Name = modName;
            }

            if (string.IsNullOrWhiteSpace(modInfo.Author)) modInfo.Author = "Unknown";
            if (string.IsNullOrWhiteSpace(modInfo.Description)) modInfo.Description = "A mod installed without a modinfo file.";
            modInfo.FolderName = MakeSafeIdentifier(modInfo.Name);

            var manifest = LoadManifest();
            if (manifest.Any(m => m.OriginalFolderName.Equals(modInfo.FolderName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("This mod is currently active. Deactivate it before reinstalling.", "Install Blocked");
                return false;
            }

            DeleteDisabledAddonsForMod(modInfo.FolderName);

            var vpkGroups = FindVpkGroups(allVpks);
            if (!vpkGroups.Any())
            {
                MessageBox.Show("No valid VPK groups found.", "Install Failed");
                return false;
            }

            var taken = CollectTakenPakPrefixes();
            foreach (var group in vpkGroups)
            {
                string pakPrefix = GetNextPakPrefix(taken);
                taken.Add(pakPrefix);

                Action<string, string> fileOperation = (src, dest) =>
                {
                    if (File.Exists(dest)) File.Delete(dest);
                    File.Move(src, dest);
                };
                if (tempDir != originalFilePath)
                {
                    fileOperation = (src, dest) =>
                    {
                        if (File.Exists(dest)) File.Delete(dest);
                        File.Copy(src, dest);
                    };
                }


                if (group.Files.Count == 1)
                {
                    string newFileName = pakPrefix + "_dir.vpk";
                    string disabledName = DisabledName(modInfo.FolderName, newFileName);
                    string dst = Path.Combine(activeModsPath, disabledName);
                    fileOperation(group.Files.First(), dst);
                }
                else
                {
                    var mainFile = group.Files.FirstOrDefault(f => f.EndsWith("_dir.vpk", StringComparison.OrdinalIgnoreCase)) ?? group.Files.First();
                    foreach (var srcFile in group.Files)
                    {
                        string srcFileName = Path.GetFileName(srcFile);
                        string newFileName;

                        if (srcFile == mainFile)
                        {
                            newFileName = pakPrefix + "_dir.vpk";
                        }
                        else
                        {
                            int lastUnderscore = srcFileName.LastIndexOf('_');
                            string suffix = srcFileName.Substring(lastUnderscore);
                            newFileName = pakPrefix + suffix;
                        }

                        string disabledName = DisabledName(modInfo.FolderName, newFileName);
                        string dst = Path.Combine(activeModsPath, disabledName);
                        fileOperation(srcFile, dst);
                    }
                }
            }

            var catalog = LoadCatalog();
            var existing = catalog.FirstOrDefault(m => m.FolderName.Equals(modInfo.FolderName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Name = modInfo.Name;
                existing.Author = modInfo.Author;
                existing.Description = modInfo.Description;
            }
            else
            {
                catalog.Add(modInfo);
            }
            SaveCatalog(catalog);

            if (tempDir == originalFilePath && !Directory.EnumerateFileSystemEntries(tempDir).Any())
            {
                Directory.Delete(tempDir);
            }

            return true;
        }

        private sealed class VpkGroup
        {
            public string BaseName;
            public List<string> Files = new List<string>();
        }

        private List<VpkGroup> FindVpkGroups(IEnumerable<string> vpkPaths)
        {
            var groups = new Dictionary<string, VpkGroup>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in vpkPaths)
            {
                string file = Path.GetFileName(path);
                string baseName = null;

                if (file.EndsWith("_dir.vpk", StringComparison.OrdinalIgnoreCase))
                {
                    baseName = file.Substring(0, file.Length - "_dir.vpk".Length);
                }
                else if (file.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase))
                {
                    int us = file.LastIndexOf('_');
                    if (us > 0)
                    {
                        string suffix = file.Substring(us + 1);
                        if (suffix.Length == 7 &&
                            char.IsDigit(suffix[0]) && char.IsDigit(suffix[1]) && char.IsDigit(suffix[2]) &&
                            suffix[3] == '.' &&
                            suffix.EndsWith("vpk", StringComparison.OrdinalIgnoreCase))
                        {
                            baseName = file.Substring(0, us);
                        }
                    }
                }

                if (baseName == null && file.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase))
                {
                    baseName = Path.GetFileNameWithoutExtension(file);
                }

                if (baseName == null) continue;

                if (!groups.TryGetValue(baseName, out var g))
                {
                    g = new VpkGroup { BaseName = baseName };
                    groups[baseName] = g;
                }
                g.Files.Add(path);
            }

            return groups.Values.ToList();
        }

        private static string MakeSafeIdentifier(string name)
        {
            var sanitized = Regex.Replace(name, @"[^\w\-_\s]", "");
            var chars = sanitized.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray();
            var id = new string(chars).Trim('_');

            id = Regex.Replace(id, @"_+", "_");

            if (string.IsNullOrWhiteSpace(id))
                id = "mod_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            return id;
        }

        private string GetNextPakPrefix(HashSet<string> taken)
        {
            int i = 0;
            while (true)
            {
                string prefix = $"pak{i:D2}";
                if (!taken.Contains(prefix) && !File.Exists(Path.Combine(activeModsPath, prefix + "_dir.vpk")))
                {
                    return prefix;
                }
                i++;
            }
        }

        private HashSet<string> CollectTakenPakPrefixes()
        {
            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(activeModsPath))
            {
                foreach (var f in Directory.GetFiles(activeModsPath, "*.vpk"))
                {
                    var fn = Path.GetFileName(f);
                    if (fn.StartsWith(DisabledPrefix, StringComparison.Ordinal))
                    {
                        int sepIdx = fn.IndexOf(ModSep, StringComparison.Ordinal);
                        if (sepIdx > 1)
                        {
                            string tail = fn.Substring(sepIdx + ModSep.Length);
                            string prefix = ExtractPakPrefixFromTail(tail);
                            if (!string.IsNullOrEmpty(prefix)) taken.Add(prefix);
                        }
                    }
                    else
                    {
                        string prefix = ExtractPakPrefixFromTail(fn);
                        if (!string.IsNullOrEmpty(prefix)) taken.Add(prefix);
                    }
                }
            }
            foreach (var m in LoadManifest())
                foreach (var p in m.PakPrefixes)
                    taken.Add(p);
            return taken;
        }


        private static string ExtractPakPrefixFromTail(string fileName)
        {
            if (!fileName.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase)) return null;
            int underscore = fileName.IndexOf('_');
            if (underscore <= 0) return null;
            string prefix = fileName.Substring(0, underscore);
            if (prefix.StartsWith("pak", StringComparison.OrdinalIgnoreCase)) return prefix;
            return null;
        }

        private string DisabledName(string folderName, string tail) => DisabledPrefix + folderName + ModSep + tail;

        private IEnumerable<string> EnumGroupFiles(string pakPrefix, bool disabled, string folderName)
        {
            if (!Directory.Exists(activeModsPath)) yield break;
            if (disabled)
            {
                string searchPattern = DisabledPrefix + folderName + ModSep + pakPrefix + "*.vpk";
                foreach (var f in Directory.GetFiles(activeModsPath, searchPattern)) yield return f;
            }
            else
            {
                string searchPattern = pakPrefix + "*.vpk";
                foreach (var f in Directory.GetFiles(activeModsPath, searchPattern))
                {
                    string fileName = Path.GetFileName(f);
                    if (!fileName.StartsWith(DisabledPrefix))
                        yield return f;
                }
            }
        }

        private int DeleteDisabledAddonsForMod(string folderName)
        {
            if (!Directory.Exists(activeModsPath)) return 0;
            string prefix = DisabledPrefix + folderName + ModSep;
            int deleted = 0;
            foreach (var file in Directory.GetFiles(activeModsPath, prefix + "*.vpk"))
            {
                try { File.Delete(file); deleted++; }
                catch (Exception ex) { MessageBox.Show($"Failed to delete disabled file '{Path.GetFileName(file)}': {ex.Message}", "Delete Warning"); }
            }
            return deleted;
        }

        public bool DeleteModFromLibrary(ModInfo modToDelete)
        {
            if (modToDelete == null) return false;
            var manifest = LoadManifest();
            if (manifest.Any(m => m.OriginalFolderName.Equals(modToDelete.FolderName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Cannot delete a mod that is currently active. Please deactivate it first.", "Action Required");
                return false;
            }
            try
            {
                int removed = DeleteDisabledAddonsForMod(modToDelete.FolderName);
                var catalog = LoadCatalog();
                var idx = catalog.FindIndex(m => m.FolderName.Equals(modToDelete.FolderName, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    catalog.RemoveAt(idx);
                    SaveCatalog(catalog);
                }
                if (removed > 0)
                    MessageBox.Show($"'{modToDelete.Name}' deleted. Removed {removed} disabled file(s) from addons.", "Success");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete mod: {ex.Message}", "Error");
                return false;
            }
        }
        #endregion
    }
}
