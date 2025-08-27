using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Deadlock_Mod_Loader2
{
    public class ModCatalogManager
    {
        private readonly string manifestPath;
        private readonly string catalogPath;
        private readonly string activeModsPath;
        private readonly string gamePath;

        private const string DisabledPrefix = "_";
        private const string ModSep = "__";

        public ModCatalogManager(string manifestPath, string catalogPath, string activeModsPath, string gamePath)
        {
            this.manifestPath = manifestPath;
            this.catalogPath = catalogPath;
            this.activeModsPath = activeModsPath;
            this.gamePath = gamePath;
        }

        public List<ActiveModInfo> LoadManifest()
        {
            if (!File.Exists(manifestPath)) return new List<ActiveModInfo>();
            string json = File.ReadAllText(manifestPath);
            return JsonConvert.DeserializeObject<List<ActiveModInfo>>(json) ?? new List<ActiveModInfo>();
        }

        public void SaveManifest(List<ActiveModInfo> manifest)
        {
            string json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
            File.WriteAllText(manifestPath, json);
        }

        public List<ModInfo> LoadCatalog()
        {
            if (!File.Exists(catalogPath)) return new List<ModInfo>();
            string json = File.ReadAllText(catalogPath);
            return JsonConvert.DeserializeObject<List<ModInfo>>(json) ?? new List<ModInfo>();
        }

        public void SaveCatalog(List<ModInfo> catalog)
        {
            string json = JsonConvert.SerializeObject(catalog, Formatting.Indented);
            File.WriteAllText(catalogPath, json);
        }

        public void UpdateCatalogEntry(ModInfo modInfo)
        {
            var catalog = LoadCatalog();
            var existing = catalog.FirstOrDefault(m => m.FolderName.Equals(modInfo.FolderName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.Name = modInfo.Name;
                existing.Author = modInfo.Author;
                existing.Description = modInfo.Description;
                existing.FileMappings = modInfo.FileMappings;
                existing.DirectoryMappings = modInfo.DirectoryMappings;
                existing.Type = modInfo.Type;
            }
            else
            {
                catalog.Add(modInfo);
            }

            SaveCatalog(catalog);
        }

        public List<ActiveModInfo> GetActiveMods() => LoadManifest();

        public List<ModInfo> GetAvailableMods()
        {
            var catalog = LoadCatalog();
            var active = LoadManifest().Select(m => m.OriginalFolderName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return catalog.Where(m => !active.Contains(m.FolderName))
                          .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                          .ToList();
        }

        public List<ModInfo> GetAllCatalogMods()
        {
            return LoadCatalog();
        }

        public bool DeleteModFromLibrary(ModInfo modToDelete)
        {
            if (modToDelete == null || string.IsNullOrEmpty(modToDelete.FolderName)) return false;

            var manifest = LoadManifest();
            if (manifest.Any(m => m.OriginalFolderName.Equals(modToDelete.FolderName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Cannot delete a mod that is currently active. Please deactivate it first.", "Action Required");
                return false;
            }

            try
            {
                int removedVpks = DeleteDisabledAddonsForMod(modToDelete.FolderName);

                int removedDirFiles = 0;
                foreach (var dirMapping in modToDelete.DirectoryMappings)
                {
                    string targetDir = Path.Combine(gamePath, dirMapping.TargetPath);

                    foreach (string file in dirMapping.Files)
                    {
                        string disabledFileName = DisabledName(modToDelete.FolderName, file);
                        string disabledPath = Path.Combine(targetDir, disabledFileName);

                        try
                        {
                            if (File.Exists(disabledPath))
                            {
                                File.Delete(disabledPath);
                                removedDirFiles++;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to delete file '{disabledFileName}': {ex.Message}", "Delete Warning");
                        }
                    }
                }

                var catalog = LoadCatalog();
                var idx = catalog.FindIndex(m => m != null && !string.IsNullOrEmpty(m.FolderName) &&
                                                m.FolderName.Equals(modToDelete.FolderName, StringComparison.OrdinalIgnoreCase));

                if (idx >= 0)
                {
                    catalog.RemoveAt(idx);
                    SaveCatalog(catalog);
                }

                string modName = modToDelete.Name ?? modToDelete.FolderName;
                string message = $"'{modName}' and its associated files have been deleted.";
                if (removedVpks > 0 || removedDirFiles > 0)
                {
                    message += $" ({removedVpks} VPK files and {removedDirFiles} directory files removed)";
                }

                MessageBox.Show(message, "Success");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete mod: {ex.Message}", "Error");
                return false;
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

        private string DisabledName(string folderName, string tail) => DisabledPrefix + folderName + ModSep + tail;
    }
}