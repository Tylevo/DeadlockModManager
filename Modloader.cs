using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SharpCompress.Archives;
using SharpCompress.Common;
using System.Text.RegularExpressions;

namespace Deadlock_Mod_Loader2
{
    public class ModLoader
    {
        private string gamePath;
        private string citadelPath;
        private string activeModsPath;
        private string manifestPath;
        private string catalogPath;
        private string gameInfoPath;

        private ProfileManager profileManager;
        private ModAnalyzer modAnalyzer;
        private ModInstaller modInstaller;
        private ModActivator modActivator;
        private ModCatalogManager catalogManager;
        private GameInfoPatcher gameInfoPatcher;

        public string GetAddonsPath() => activeModsPath;

        public ModLoader()
        {
        }

        public bool SetGamePath(string path)
        {
            if (Directory.Exists(path) && Directory.Exists(Path.Combine(path, "citadel")))
            {
                gamePath = path;
                citadelPath = Path.Combine(gamePath, "citadel");
                activeModsPath = Path.Combine(citadelPath, "addons");
                manifestPath = Path.Combine(activeModsPath, "active_mods.json");
                catalogPath = Path.Combine(activeModsPath, "mods_catalog.json");
                gameInfoPath = Path.Combine(citadelPath, "gameinfo.gi");

                InitializeDependencies();

                if (Directory.Exists(activeModsPath))
                {
                    if (!File.Exists(manifestPath)) catalogManager.SaveManifest(new List<ActiveModInfo>());
                    if (!File.Exists(catalogPath)) catalogManager.SaveCatalog(new List<ModInfo>());
                }

                return true;
            }
            return false;
        }

        private void InitializeDependencies()
        {
            profileManager = new ProfileManager(gamePath);
            modAnalyzer = new ModAnalyzer();
            catalogManager = new ModCatalogManager(manifestPath, catalogPath, activeModsPath, gamePath);
            gameInfoPatcher = new GameInfoPatcher(gameInfoPath, activeModsPath, catalogManager);
            modInstaller = new ModInstaller(activeModsPath, gamePath, manifestPath, catalogPath, modAnalyzer, catalogManager);
            modActivator = new ModActivator(activeModsPath, gamePath, catalogManager, gameInfoPatcher);
        }

        #region Profile Management
        public List<ModProfile> GetProfiles()
        {
            return profileManager?.LoadProfiles() ?? new List<ModProfile>();
        }

        public void SaveProfile(string profileName, string description = "")
        {
            if (profileManager == null) throw new InvalidOperationException("Game path not set");

            var profiles = profileManager.LoadProfiles();
            var activeMods = GetActiveMods();

            var existingProfile = profiles.FirstOrDefault(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));

            if (existingProfile != null)
            {
                existingProfile.Description = description;
                existingProfile.LastUsed = DateTime.Now;
                existingProfile.ActiveModFolderNames = activeMods.Select(m => m.OriginalFolderName).ToList();
                existingProfile.ModLoadOrder = activeMods.Select((mod, index) => new { mod.OriginalFolderName, Index = index })
                                                        .ToDictionary(x => x.OriginalFolderName, x => x.Index);
            }
            else
            {
                var newProfile = new ModProfile
                {
                    Name = profileName,
                    Description = description,
                    Created = DateTime.Now,
                    LastUsed = DateTime.Now,
                    ActiveModFolderNames = activeMods.Select(m => m.OriginalFolderName).ToList(),
                    ModLoadOrder = activeMods.Select((mod, index) => new { mod.OriginalFolderName, Index = index })
                                            .ToDictionary(x => x.OriginalFolderName, x => x.Index)
                };
                profiles.Add(newProfile);
            }

            profileManager.SaveProfiles(profiles);
        }

        public bool LoadProfile(string profileName)
        {
            if (profileManager == null) return false;

            var profiles = profileManager.LoadProfiles();
            var profile = profiles.FirstOrDefault(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));

            if (profile == null) return false;

            try
            {
                var currentActiveMods = GetActiveMods().ToList();
                foreach (var mod in currentActiveMods)
                {
                    DeactivateMod(mod);
                }

                var availableMods = catalogManager.LoadCatalog();

                var modsToActivate = new List<ModInfo>();

                foreach (var folderName in profile.ActiveModFolderNames)
                {
                    var mod = availableMods.FirstOrDefault(m => m.FolderName.Equals(folderName, StringComparison.OrdinalIgnoreCase));
                    if (mod != null)
                    {
                        modsToActivate.Add(mod);
                    }
                }

                if (profile.ModLoadOrder.Any())
                {
                    modsToActivate = modsToActivate.OrderBy(mod =>
                        profile.ModLoadOrder.ContainsKey(mod.FolderName) ?
                        profile.ModLoadOrder[mod.FolderName] : int.MaxValue).ToList();
                }

                foreach (var mod in modsToActivate)
                {
                    ActivateMod(mod);
                }

                profile.LastUsed = DateTime.Now;
                profileManager.SaveProfiles(profiles);
                profileManager.CurrentProfile = profileName;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool DeleteProfile(string profileName)
        {
            if (profileManager == null || profileName.Equals("Default", StringComparison.OrdinalIgnoreCase))
                return false;

            var profiles = profileManager.LoadProfiles();
            var profile = profiles.FirstOrDefault(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));

            if (profile != null)
            {
                profiles.Remove(profile);
                profileManager.SaveProfiles(profiles);
                return true;
            }

            return false;
        }

        public string GetCurrentProfileName()
        {
            return profileManager?.CurrentProfile ?? "Default";
        }
        #endregion

        #region Mod Installation
        public bool InstallDroppedFile(string filePath, IWin32Window owner)
        {
            return modInstaller?.InstallDroppedFile(filePath, owner) ?? false;
        }

        public bool InstallDroppedFileWithName(string filePath, IWin32Window owner, string modName, string authorName)
        {
            return modInstaller?.InstallDroppedFileWithName(filePath, owner, modName, authorName) ?? false;
        }
        #endregion

        #region Mod Activation/Deactivation
        public void ActivateMod(ModInfo modToActivate)
        {
            modActivator?.ActivateMod(modToActivate);
        }

        public void DeactivateMod(ActiveModInfo modToDeactivate)
        {
            modActivator?.DeactivateMod(modToDeactivate);
        }

        public void UpdateModSearchPaths(List<ActiveModInfo> orderedMods)
        {
            gameInfoPatcher?.UpdateModSearchPaths(orderedMods);
        }
        #endregion

        #region Catalog Management
        public List<ActiveModInfo> GetActiveMods() => catalogManager?.GetActiveMods() ?? new List<ActiveModInfo>();

        public List<ModInfo> GetAvailableMods() => catalogManager?.GetAvailableMods() ?? new List<ModInfo>();

        public List<ModInfo> GetAllCatalogMods() => catalogManager?.GetAllCatalogMods() ?? new List<ModInfo>();

        public bool DeleteModFromLibrary(ModInfo modToDelete)
        {
            return catalogManager?.DeleteModFromLibrary(modToDelete) ?? false;
        }
        #endregion

        #region Game Configuration
        public bool PatchGameInfoFile()
        {
            return gameInfoPatcher?.PatchGameInfoFile() ?? false;
        }

        public void RestoreGameInfoBackup()
        {
            gameInfoPatcher?.RestoreGameInfoBackup();
        }
        #endregion

        #region Import and Utilities
        public void ImportUnmanagedMods()
        {
            if (string.IsNullOrEmpty(activeModsPath) || !Directory.Exists(activeModsPath))
            {
                MessageBox.Show("Addons path is not set or does not exist.", "Error");
                return;
            }

            int importedCount = 0;

            var looseVpks = Directory.GetFiles(activeModsPath, "*.vpk", SearchOption.TopDirectoryOnly).ToList();
            if (looseVpks.Any())
            {
                var vpkGroups = FindVpkGroups(looseVpks);
                foreach (var group in vpkGroups)
                {
                    string modName = group.BaseName.Replace("_", " ").Replace("-", " ");
                    modName = Regex.Replace(modName, @"\s+", " ").Trim();
                    string folderName = MakeSafeIdentifier(modName);

                    string newModSubFolder = Path.Combine(activeModsPath, folderName);

                    int suffix = 1;
                    while (Directory.Exists(newModSubFolder))
                    {
                        newModSubFolder = Path.Combine(activeModsPath, $"{folderName}_{suffix++}");
                    }
                    Directory.CreateDirectory(newModSubFolder);

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

            var manifest = catalogManager.LoadManifest();
            var catalog = catalogManager.LoadCatalog();
            var managedFolders = manifest.Select(m => m.OriginalFolderName)
                                        .Concat(catalog.Select(c => c.FolderName))
                                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var allDirectories = Directory.GetDirectories(activeModsPath);
            var unmanagedDirs = allDirectories.Where(dir => !managedFolders.Contains(Path.GetFileName(dir))).ToList();

            foreach (var dir in unmanagedDirs)
            {
                if (modInstaller.InstallDroppedFile(dir, null))
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

        private List<VpkGroup> FindVpkGroups(List<string> vpkPaths)
        {
            var groups = new List<VpkGroup>();
            if (vpkPaths == null || !vpkPaths.Any())
            {
                return groups;
            }

            var groupedVpks = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var vpkPath in vpkPaths)
            {
                string fileName = Path.GetFileName(vpkPath);
                string baseName;

                if (fileName.EndsWith("_dir.vpk", StringComparison.OrdinalIgnoreCase))
                {
                    baseName = fileName.Substring(0, fileName.Length - "_dir.vpk".Length);
                }
                else
                {
                    var match = Regex.Match(fileName, @"^(.+?)(_\d{3})?\.vpk$", RegexOptions.IgnoreCase);
                    baseName = match.Success ? match.Groups[1].Value : Path.GetFileNameWithoutExtension(fileName);
                }

                if (!groupedVpks.ContainsKey(baseName))
                {
                    groupedVpks[baseName] = new List<string>();
                }
                groupedVpks[baseName].Add(vpkPath);
            }

            foreach (var kvp in groupedVpks)
            {
                groups.Add(new VpkGroup
                {
                    BaseName = kvp.Key,
                    Files = kvp.Value
                });
            }

            return groups;
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
        #endregion
    }
}