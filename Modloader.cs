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
                if (!Directory.Exists(activeModsPath))
                    Directory.CreateDirectory(activeModsPath);

                manifestPath = Path.Combine(activeModsPath, "active_mods.json");
                catalogPath = Path.Combine(activeModsPath, "mods_catalog.json");
                gameInfoPath = Path.Combine(gamePath, "citadel", "gameinfo.gi");

                if (!File.Exists(manifestPath)) SaveManifest(new List<ActiveModInfo>());
                if (!File.Exists(catalogPath)) SaveCatalog(new List<ModInfo>());

                return true;
            }
            return false;
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
            string tempDir = Path.Combine(Path.GetTempPath(), "DeadlockModLoader_" + Guid.NewGuid().ToString("N"));
            try
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

                return ProcessModContents(tempDir, filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to install mod: {ex.Message}", "Error");
                return false;
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        private bool ExtractRarFile(string rarPath, string extractPath)
        {
            try
            {
                // Try WinRAR first
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

                // Try 7-Zip as fallback
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

            // Look for modinfo.json (most flexible naming)
            string modJsonPath = Directory.GetFiles(tempDir, "modinfo.json", SearchOption.AllDirectories).FirstOrDefault();

            // Also look for other common mod info file names
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

            // Enhanced filename parsing for mod name generation
            if (string.IsNullOrWhiteSpace(modInfo.Name))
            {
                string fileName = Path.GetFileNameWithoutExtension(originalFilePath);
                string modName = fileName;

                // Pattern 1: "pak04_dir - Health Bar (Vertical)" 
                int separatorIndex = fileName.IndexOf(" - ");
                if (separatorIndex > 0)
                {
                    modName = fileName.Substring(separatorIndex + 3).Trim();
                }
                // Pattern 2: "ModName_v1.2.3" (remove version info)
                else if (Regex.IsMatch(fileName, @"_v?\d+(\.\d+)*$"))
                {
                    modName = Regex.Replace(fileName, @"_v?\d+(\.\d+)*$", "");
                }
                // Pattern 3: "ModName by Author" 
                else if (fileName.Contains(" by "))
                {
                    modName = fileName.Substring(0, fileName.IndexOf(" by "));
                }

                modName = modName.Replace("_", " ").Replace("-", " ");
                modName = Regex.Replace(modName, @"\s+", " ").Trim(); // Remove extra spaces
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

                if (group.Files.Count == 1)
                {
                    string newFileName = pakPrefix + "_dir.vpk";
                    string disabledName = DisabledName(modInfo.FolderName, newFileName);
                    string dst = Path.Combine(activeModsPath, disabledName);
                    File.Copy(group.Files.First(), dst, overwrite: true);
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
                        File.Copy(srcFile, dst, overwrite: true);
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

            return true;
        }

        private static string MakeSafeIdentifier(string name)
        {
            var sanitized = Regex.Replace(name, @"[^\w\-_\s]", ""); // Keep only word chars, hyphens, underscores, and spaces
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

        public void ActivateMod(ModInfo modToActivate)
        {
            var manifest = LoadManifest();
            if (manifest.Any(m => m.OriginalFolderName.Equals(modToActivate.FolderName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("This mod is already active.", "Warning");
                return;
            }

            string searchPattern = DisabledPrefix + modToActivate.FolderName + ModSep + "*.vpk";
            var disabledFiles = Directory.GetFiles(activeModsPath, searchPattern);

            if (disabledFiles.Length == 0)
            {
                MessageBox.Show("No disabled files found in addons for this mod. Try reinstalling.", "Activate Warning");
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
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        prefixes.Add(prefix);
                    }
                }
            }

            if (prefixes.Count == 0)
            {
                MessageBox.Show($"Unable to determine VPK groups for this mod. Files found: {string.Join(", ", disabledFiles.Select(Path.GetFileName))}", "Activate Warning");
                return;
            }

            var entry = new ActiveModInfo
            {
                ModName = modToActivate.Name,
                OriginalFolderName = modToActivate.FolderName,
                PakPrefixes = prefixes.ToList()
            };

            foreach (var prefix in entry.PakPrefixes)
            {
                foreach (var file in EnumGroupFiles(prefix, disabled: true, folderName: modToActivate.FolderName))
                {
                    try
                    {
                        string disabledFileName = Path.GetFileName(file);
                        string expectedPrefix = DisabledPrefix + modToActivate.FolderName + ModSep;
                        string activeName = disabledFileName.Substring(expectedPrefix.Length);
                        string dst = Path.Combine(activeModsPath, activeName);

                        if (File.Exists(dst)) File.Delete(dst);
                        File.Move(file, dst);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to activate file '{Path.GetFileName(file)}': {ex.Message}", "Activation Error");
                        return;
                    }
                }
            }

            manifest.Add(entry);
            // UPDATED: Call new method to handle manifest saving and gameinfo.gi update
            UpdateActiveModsOrder(manifest);
        }

        public void DeactivateMod(ActiveModInfo modToDeactivate)
        {
            var manifest = LoadManifest();
            var modInManifest = manifest.FirstOrDefault(m => m.OriginalFolderName.Equals(modToDeactivate.OriginalFolderName, StringComparison.OrdinalIgnoreCase));
            if (modInManifest == null) return;

            foreach (var prefix in modInManifest.PakPrefixes)
            {
                foreach (var file in EnumGroupFiles(prefix, disabled: false, folderName: null))
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
            }

            manifest.Remove(modInManifest);
            // UPDATED: Call new method to handle manifest saving and gameinfo.gi update
            UpdateActiveModsOrder(manifest);
        }

        // NEW METHOD: Updates the manifest and gameinfo.gi based on a new mod order.
        public void UpdateActiveModsOrder(List<ActiveModInfo> orderedMods)
        {
            SaveManifest(orderedMods);
            UpdateGameInfoSearchPaths();
        }

        // NEW METHOD: Rewrites the SearchPaths block in gameinfo.gi based on the active mods list.
        public void UpdateGameInfoSearchPaths()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(gameInfoPath) || !File.Exists(gameInfoPath)) return;

                string content = ReadFileWithoutBOM(gameInfoPath);
                var activeMods = LoadManifest();

                // The last mod in the UI list should have the highest priority.
                // In Source Engine, the FIRST path in the list wins.
                // So, we do NOT reverse the list. The top of the UI list is the first path written.
                // To give something higher priority, the user moves it UP the list.

                var newSearchPathsBlock = new StringBuilder();
                newSearchPathsBlock.AppendLine("\tSearchPaths");
                newSearchPathsBlock.AppendLine("\t{");

                // Add each active mod's path in the user-defined order
                foreach (var mod in activeMods)
                {
                    // Assuming OriginalFolderName is a safe, single-directory name for the path
                    newSearchPathsBlock.AppendLine($"\t\tGame\t\t\tcitadel/addons/{mod.OriginalFolderName}");
                }

                // Add the default game paths AFTER the mods
                newSearchPathsBlock.AppendLine("\t\tGame\t\t\tcitadel/addons");
                newSearchPathsBlock.AppendLine("\t\tGame\t\t\tcitadel");
                newSearchPathsBlock.AppendLine("\t\tWrite\t\t\tcitadel");
                newSearchPathsBlock.AppendLine("\t\tGame\t\t\tcore");
                newSearchPathsBlock.AppendLine("\t\tWrite\t\t\tcore");
                newSearchPathsBlock.AppendLine("\t\tMod\t\t\tcore");
                newSearchPathsBlock.AppendLine("\t}");

                string pattern = @"(SearchPaths\s*\{[\s\S]*?\})";
                string newContent;

                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    newContent = Regex.Replace(content, pattern, newSearchPathsBlock.ToString(), RegexOptions.IgnoreCase);
                }
                else
                {
                    // Fallback if SearchPaths doesn't exist at all, use the robust initial setup logic
                    newContent = ModifyGameInfoContent(content);
                }

                WriteFileWithoutBOM(gameInfoPath, newContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update gameinfo.gi: {ex.Message}", "Error");
            }
        }

        public void InitialSetup()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(gameInfoPath) || !File.Exists(gameInfoPath))
                {
                    MessageBox.Show("gameinfo.gi file not found at expected location.", "Setup Error");
                    return;
                }
                string originalContent = ReadFileWithoutBOM(gameInfoPath);

                // We now always ensure the search paths are up-to-date on setup.
                UpdateGameInfoSearchPaths();

                // Check for AddonConfig separately
                if (!originalContent.Contains("AddonConfig"))
                {
                    string currentContent = ReadFileWithoutBOM(gameInfoPath);
                    string addonConfig = "\nAddonConfig\n{\n\t\"UseOfficialAddons\" \"1\"\n}\n";

                    // Find the end of the FileSystem block to append AddonConfig after it
                    var fsMatch = Regex.Match(currentContent, @"(FileSystem\s*\{[\s\S]*?\})", RegexOptions.IgnoreCase);
                    if (fsMatch.Success)
                    {
                        string fsBlock = fsMatch.Groups[1].Value;
                        currentContent = currentContent.Replace(fsBlock, fsBlock + addonConfig);
                        WriteFileWithoutBOM(gameInfoPath, currentContent);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to setup gameinfo.gi: {ex.Message}", "Setup Error");
            }
        }

        // This method is now primarily a fallback for the very first run
        private string ModifyGameInfoContent(string content)
        {
            var activeMods = LoadManifest();
            // activeMods.Reverse(); // Reverse for priority

            var newSearchPathsBlock = new StringBuilder();
            newSearchPathsBlock.AppendLine("\tSearchPaths");
            newSearchPathsBlock.AppendLine("\t{");
            foreach (var mod in activeMods)
            {
                newSearchPathsBlock.AppendLine($"\t\tGame\t\t\tcitadel/addons/{mod.OriginalFolderName}");
            }
            newSearchPathsBlock.AppendLine("\t\tGame\t\t\tcitadel/addons");
            newSearchPathsBlock.AppendLine("\t\tGame\t\t\tcitadel");
            newSearchPathsBlock.AppendLine("\t\tWrite\t\t\tcitadel");
            newSearchPathsBlock.AppendLine("\t\tGame\t\t\tcore");
            newSearchPathsBlock.AppendLine("\t\tWrite\t\t\tcore");
            newSearchPathsBlock.AppendLine("\t\tMod\t\t\tcore");
            newSearchPathsBlock.AppendLine("\t}");

            string pattern = @"(SearchPaths\s*\{[\s\S]*?\})";
            if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
            {
                return Regex.Replace(content, pattern, newSearchPathsBlock.ToString(), RegexOptions.IgnoreCase);
            }
            else // If no SearchPaths block exists, insert it
            {
                var fsMatch = Regex.Match(content, @"(FileSystem\s*\{)", RegexOptions.IgnoreCase);
                if (fsMatch.Success)
                {
                    return content.Insert(fsMatch.Index + fsMatch.Length, "\n" + newSearchPathsBlock.ToString() + "\n");
                }
            }
            return content; // Return original if FileSystem block not found
        }

        #endregion
    }
}