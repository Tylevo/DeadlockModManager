using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Text;

namespace Deadlock_Mod_Loader2
{
    public class ModActivator
    {
        private readonly string activeModsPath;
        private readonly string gamePath;
        private readonly ModCatalogManager catalogManager;
        private readonly GameInfoPatcher gameInfoPatcher;

        private const string DisabledPrefix = "_";
        private const string ModSep = "__";

        public ModActivator(string activeModsPath, string gamePath, ModCatalogManager catalogManager, GameInfoPatcher gameInfoPatcher)
        {
            this.activeModsPath = activeModsPath;
            this.gamePath = gamePath;
            this.catalogManager = catalogManager;
            this.gameInfoPatcher = gameInfoPatcher;
        }

        public void ActivateMod(ModInfo modToActivate)
        {
            var manifest = catalogManager.LoadManifest();
            if (manifest.Any(m => m.OriginalFolderName.Equals(modToActivate.FolderName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("This mod is already active.", "Warning");
                return;
            }

            switch (modToActivate.Type)
            {
                case ModType.VpkOnly:
                    ActivateVpkMod(modToActivate);
                    break;
                case ModType.DirectoryBased:
                    ActivateDirectoryMod(modToActivate);
                    break;
                case ModType.Mixed:
                    ActivateVpkMod(modToActivate);
                    ActivateDirectoryMod(modToActivate);
                    break;
            }

            var activeMod = new ActiveModInfo
            {
                ModName = modToActivate.Name,
                OriginalFolderName = modToActivate.FolderName,
                Type = modToActivate.Type
            };

            var prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mapping in modToActivate.FileMappings)
            {
                if (!string.IsNullOrEmpty(mapping.PakPrefix))
                    prefixes.Add(mapping.PakPrefix);
            }
            activeMod.PakPrefixes = prefixes.ToList();

            activeMod.ActiveDirectories = modToActivate.DirectoryMappings.Select(d => d.TargetPath).ToList();

            manifest.Add(activeMod);
            gameInfoPatcher.UpdateModSearchPaths(manifest);
        }

        public void DeactivateMod(ActiveModInfo modToDeactivate)
        {
            var manifest = catalogManager.LoadManifest();
            var modInManifest = manifest.FirstOrDefault(m => m.OriginalFolderName.Equals(modToDeactivate.OriginalFolderName, StringComparison.OrdinalIgnoreCase));
            if (modInManifest == null) return;

            var catalog = catalogManager.LoadCatalog();
            var modInfo = catalog.FirstOrDefault(m => m.FolderName.Equals(modToDeactivate.OriginalFolderName, StringComparison.OrdinalIgnoreCase));
            if (modInfo == null)
            {
                DeactivateVpkMod(modInManifest);
            }
            else
            {
                switch (modInfo.Type)
                {
                    case ModType.VpkOnly:
                        DeactivateVpkMod(modInManifest);
                        break;
                    case ModType.DirectoryBased:
                        DeactivateDirectoryMod(modInfo);
                        break;
                    case ModType.Mixed:
                        DeactivateVpkMod(modInManifest);
                        DeactivateDirectoryMod(modInfo);
                        break;
                }
            }

            manifest.Remove(modInManifest);
            gameInfoPatcher.UpdateModSearchPaths(manifest);
        }

        private void ActivateVpkMod(ModInfo modToActivate)
        {
            string modSubFolderPath = Path.Combine(activeModsPath, modToActivate.FolderName);
            if (!Directory.Exists(modSubFolderPath)) Directory.CreateDirectory(modSubFolderPath);

            var disabledNameMap = LoadDisabledNameMap(modSubFolderPath);

            string searchPattern = DisabledPrefix + modToActivate.FolderName + ModSep + "*.vpk";
            var disabledFiles = Directory.GetFiles(activeModsPath, searchPattern);

            foreach (var df in disabledFiles)
            {
                string fileName = Path.GetFileName(df);
                string expectedPrefix = DisabledPrefix + modToActivate.FolderName + ModSep;
                if (!fileName.StartsWith(expectedPrefix)) continue;

                string tail = fileName.Substring(expectedPrefix.Length);
                string activeName = ExtractActivePakNameFromTail(tail);

                try
                {
                    string dst = Path.Combine(modSubFolderPath, activeName);
                    disabledNameMap[activeName] = tail;

                    if (File.Exists(dst)) File.Delete(dst);
                    File.Move(df, dst);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to activate file '{fileName}': {ex.Message}", "Activation Error");
                    return;
                }
            }

            SaveDisabledNameMap(modSubFolderPath, disabledNameMap);
        }

        private void ActivateDirectoryMod(ModInfo modToActivate)
        {
            foreach (var dirMapping in modToActivate.DirectoryMappings)
            {
                string targetDir = Path.Combine(gamePath, dirMapping.TargetPath.Replace("/", "\\"));

                Console.WriteLine($"[DEBUG] Activating directory: {dirMapping.TargetPath}");
                Console.WriteLine($"[DEBUG] Target directory: {targetDir}");

                foreach (string file in dirMapping.Files)
                {
                    string disabledFileName = DisabledName(modToActivate.FolderName, file);
                    string disabledPath = Path.Combine(targetDir, disabledFileName);
                    string activePath = Path.Combine(targetDir, file);

                    Console.WriteLine($"[DEBUG] Activating file: {file}");
                    Console.WriteLine($"[DEBUG] From: {disabledPath}");
                    Console.WriteLine($"[DEBUG] To: {activePath}");

                    try
                    {
                        if (File.Exists(disabledPath))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(activePath));

                            if (File.Exists(activePath)) File.Delete(activePath);
                            File.Move(disabledPath, activePath);
                            Console.WriteLine($"[DEBUG] Successfully moved: {file}");
                        }
                        else
                        {
                            Console.WriteLine($"[DEBUG] Disabled file not found: {disabledPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG] Failed to activate file '{file}': {ex.Message}");
                        MessageBox.Show($"Failed to activate file '{file}': {ex.Message}", "Activation Error");
                        return;
                    }
                }
            }
        }

        private void DeactivateVpkMod(ActiveModInfo modToDeactivate)
        {
            string modSubFolderPath = Path.Combine(activeModsPath, modToDeactivate.OriginalFolderName);
            var disabledNameMap = LoadDisabledNameMap(modSubFolderPath);

            if (Directory.Exists(modSubFolderPath))
            {
                foreach (var file in Directory.GetFiles(modSubFolderPath, "*.vpk"))
                {
                    try
                    {
                        string activeName = Path.GetFileName(file);
                        string disabledTail;
                        if (!disabledNameMap.TryGetValue(activeName, out disabledTail))
                        {
                            disabledTail = activeName;
                        }

                        string disabledName = DisabledName(modToDeactivate.OriginalFolderName, disabledTail);
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

                try
                {
                    var mapPath = Path.Combine(modSubFolderPath, ".disabled-names.json");
                    if (File.Exists(mapPath)) File.Delete(mapPath);
                }
                catch { }

                if (!Directory.EnumerateFileSystemEntries(modSubFolderPath).Any())
                {
                    Directory.Delete(modSubFolderPath);
                }
            }
        }

        private void DeactivateDirectoryMod(ModInfo modInfo)
        {
            foreach (var dirMapping in modInfo.DirectoryMappings)
            {
                string targetDir = Path.Combine(gamePath, dirMapping.TargetPath);

                foreach (string file in dirMapping.Files)
                {
                    string activePath = Path.Combine(targetDir, file);
                    string disabledFileName = DisabledName(modInfo.FolderName, file);
                    string disabledPath = Path.Combine(targetDir, disabledFileName);

                    try
                    {
                        if (File.Exists(activePath))
                        {
                            if (File.Exists(disabledPath)) File.Delete(disabledPath);
                            File.Move(activePath, disabledPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to deactivate file '{file}': {ex.Message}", "Deactivation Error");
                        return;
                    }
                }
            }
        }

        private static string ExtractActivePakNameFromTail(string tail)
        {
            var match = Regex.Match(tail, @"(pak\d{2})(_\d{3}\.vpk|_dir\.vpk)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                string pakPrefix = match.Groups[1].Value;
                string suffix = match.Groups[2].Value;
                return $"{pakPrefix}{suffix}";
            }

            var dirMatch = Regex.Match(tail, @"(pak\d{2})", RegexOptions.IgnoreCase);
            return dirMatch.Success ? $"{dirMatch.Groups[1].Value}_dir.vpk" : "pak00_dir.vpk";
        }

        private static Dictionary<string, string> LoadDisabledNameMap(string folderPath)
        {
            var path = Path.Combine(folderPath, ".disabled-names.json");
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var map = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    return map ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
            }
            catch { }
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        private static void SaveDisabledNameMap(string folderPath, Dictionary<string, string> map)
        {
            var path = Path.Combine(folderPath, ".disabled-names.json");
            var json = JsonConvert.SerializeObject(map, Formatting.Indented);
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }

        private string DisabledName(string folderName, string tail) => DisabledPrefix + folderName + ModSep + tail;
    }
}