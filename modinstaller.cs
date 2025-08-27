using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Newtonsoft.Json;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace Deadlock_Mod_Loader2
{
    public class ModInstaller
    {
        private readonly string activeModsPath;
        private readonly string gamePath;
        private readonly string manifestPath;
        private readonly string catalogPath;
        private readonly ModAnalyzer analyzer;
        private readonly ModCatalogManager catalogManager;

        private const string DisabledPrefix = "_";
        private const string ModSep = "__";
        private const string ModDirPrefix = "mod_";

        private readonly Dictionary<string, string> SupportedDirectories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "maps", "citadel/maps" },
            { "cfg", "citadel/cfg" },
            { "addons", "citadel/addons" },
            { "materials", "citadel/materials" },
            { "models", "citadel/models" },
            { "scripts", "citadel/scripts" },
            { "sounds", "citadel/sounds" },
            { "particles", "citadel/particles" }
        };

        public ModInstaller(string activeModsPath, string gamePath, string manifestPath, string catalogPath,
            ModAnalyzer analyzer, ModCatalogManager catalogManager)
        {
            this.activeModsPath = activeModsPath;
            this.gamePath = gamePath;
            this.manifestPath = manifestPath;
            this.catalogPath = catalogPath;
            this.analyzer = analyzer;
            this.catalogManager = catalogManager;
        }

        public bool InstallDroppedFile(string filePath, IWin32Window owner)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "DeadlockModLoader_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tempDir);

                if (!ExtractFileToTemp(filePath, tempDir))
                {
                    return false;
                }

                var structureInfo = analyzer.AnalyzeModStructure(tempDir);
                bool hasValidContent = structureInfo.VpkFiles.Any() || structureInfo.Directories.Any();

                if (!hasValidContent)
                {
                    return false;
                }

                if (ShouldShowCollectionDialog(structureInfo, tempDir, filePath))
                {
                    try
                    {
                        using (var choiceForm = new CollectionInstallForm())
                        {
                            var result = choiceForm.ShowDialog(owner);

                            if (result == DialogResult.Yes) // Install as one collection
                            {
                                return ProcessModContents(tempDir, filePath, false);
                            }
                            else if (result == DialogResult.No) // Install groups independently
                            {
                                return ProcessMultipleModsIndependently(structureInfo, tempDir, filePath);
                            }
                            else
                            {
                                return false; // Cancel
                            }
                        }
                    }
                    catch
                    {
                        return ProcessModContents(tempDir, filePath, false);
                    }
                }
                else
                {
                    return ProcessModContents(tempDir, filePath, false);
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        public bool InstallDroppedFileWithName(string filePath, IWin32Window owner, string modName, string authorName)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "DeadlockModLoader_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tempDir);

                if (!ExtractFileToTemp(filePath, tempDir))
                {
                    return false;
                }

                var structureInfo = analyzer.AnalyzeModStructure(tempDir);
                bool hasValidContent = structureInfo.VpkFiles.Any() || structureInfo.Directories.Any();

                if (!hasValidContent)
                {
                    return false;
                }

                return ProcessModContentsWithName(tempDir, filePath, false, modName, authorName);
            }
            catch
            {
                return false;
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        private bool ExtractFileToTemp(string filePath, string tempDir)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (extension == ".zip" || extension == ".rar" || extension == ".7z")
            {
                return ExtractArchiveFile(filePath, tempDir);
            }
            else if (extension == ".vpk")
            {
                File.Copy(filePath, Path.Combine(tempDir, Path.GetFileName(filePath)));
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ExtractArchiveFile(string archivePath, string extractPath)
        {
            try
            {
                Directory.CreateDirectory(extractPath);

                using (var archive = ArchiveFactory.Open(archivePath))
                {
                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    {
                        entry.WriteToDirectory(extractPath, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool ShouldShowCollectionDialog(ModStructureInfo structureInfo, string tempDir, string originalFilePath)
        {
            if (Path.GetExtension(originalFilePath).ToLowerInvariant() == ".vpk")
            {
                return false;
            }

            var vpkGroups = FindVpkGroups(structureInfo.VpkFiles);
            if (vpkGroups.Count > 1)
            {
                var distinctBaseNames = vpkGroups.Select(g => g.BaseName.ToLowerInvariant()).Distinct().Count();
                if (distinctBaseNames > 1)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ProcessModContents(string tempDir, string originalFilePath, bool showMessages = true)
        {
            var modInfo = LoadOrCreateModInfo(tempDir, originalFilePath);
            return ProcessModInfo(modInfo, tempDir, originalFilePath, showMessages);
        }

        private bool ProcessModContentsWithName(string tempDir, string originalFilePath, bool showMessages, string providedName, string providedAuthor)
        {
            var modInfo = LoadOrCreateModInfoWithName(tempDir, originalFilePath, providedName, providedAuthor);
            return ProcessModInfo(modInfo, tempDir, originalFilePath, showMessages);
        }

        private ModInfo LoadOrCreateModInfo(string tempDir, string originalFilePath)
        {
            var modInfo = new ModInfo();

            string modJsonPath = Directory.GetFiles(tempDir, "modinfo.json", SearchOption.AllDirectories).FirstOrDefault();

            if (!string.IsNullOrEmpty(modJsonPath))
            {
                try
                {
                    var parsed = JsonConvert.DeserializeObject<ModInfo>(File.ReadAllText(modJsonPath));
                    if (parsed != null) modInfo = parsed;
                }
                catch { }
            }

            var structureInfo = analyzer.AnalyzeModStructure(tempDir);
            modInfo.Type = structureInfo.Type;

            if (string.IsNullOrWhiteSpace(modInfo.Name))
            {
                modInfo.Name = DeriveModNameFromFile(originalFilePath);
            }

            if (string.IsNullOrWhiteSpace(modInfo.Author)) modInfo.Author = "Unknown";
            if (string.IsNullOrWhiteSpace(modInfo.Description)) modInfo.Description = "A mod installed without a modinfo file.";
            modInfo.FolderName = MakeSafeIdentifier(modInfo.Name);

            return modInfo;
        }

        private ModInfo LoadOrCreateModInfoWithName(string tempDir, string originalFilePath, string providedName, string providedAuthor)
        {
            var modInfo = LoadOrCreateModInfo(tempDir, originalFilePath);

            if (!string.IsNullOrWhiteSpace(providedName))
            {
                modInfo.Name = providedName;
            }

            if (!string.IsNullOrWhiteSpace(providedAuthor))
            {
                modInfo.Author = providedAuthor;
            }

            if (string.IsNullOrWhiteSpace(modInfo.Description))
                modInfo.Description = "A mod downloaded from GameBanana.";

            modInfo.FolderName = MakeSafeIdentifier(modInfo.Name);

            return modInfo;
        }

        private bool ProcessModInfo(ModInfo modInfo, string tempDir, string originalFilePath, bool showMessages)
        {
            var structureInfo = analyzer.AnalyzeModStructure(tempDir);

            var manifest = catalogManager.LoadManifest();
            if (manifest.Any(m => m.OriginalFolderName.Equals(modInfo.FolderName, StringComparison.OrdinalIgnoreCase)))
            {
                if (showMessages) MessageBox.Show("This mod is currently active. Deactivate it before reinstalling.", "Install Blocked");
                return false;
            }

            DeleteDisabledAddonsForMod(modInfo.FolderName);

            bool processResult = false;

            switch (modInfo.Type)
            {
                case ModType.VpkOnly:
                    processResult = ProcessVpkFiles(structureInfo.VpkFiles, modInfo);
                    break;

                case ModType.DirectoryBased:
                    processResult = ProcessDirectoryStructure(structureInfo, modInfo);
                    break;

                case ModType.Mixed:
                    bool vpkResult = ProcessVpkFiles(structureInfo.VpkFiles, modInfo);
                    bool dirResult = ProcessDirectoryStructure(structureInfo, modInfo);
                    processResult = vpkResult && dirResult;
                    break;
            }

            if (!processResult)
            {
                return false;
            }

            catalogManager.UpdateCatalogEntry(modInfo);
            return true;
        }

        private bool ProcessVpkFiles(List<string> vpkFiles, ModInfo modInfo)
        {
            try
            {
                if (!vpkFiles.Any())
                {
                    return true;
                }

                var vpkGroups = FindVpkGroups(vpkFiles);
                if (!vpkGroups.Any())
                {
                    foreach (var vpkFile in vpkFiles)
                    {
                        var singleGroup = new VpkGroup
                        {
                            BaseName = Path.GetFileNameWithoutExtension(vpkFile),
                            Files = new List<string> { vpkFile }
                        };
                        vpkGroups.Add(singleGroup);
                    }
                }

                var taken = CollectTakenPakPrefixes();

                foreach (var group in vpkGroups)
                {
                    string pakPrefix = GetNextPakPrefix(taken);
                    taken.Add(pakPrefix);

                    string descriptivePart = MakeSafeIdentifier(group.BaseName);
                    descriptivePart = Regex.Replace(descriptivePart, @"^pak\d+_dir_-_", "", RegexOptions.IgnoreCase);
                    descriptivePart = Regex.Replace(descriptivePart, @"_dir$", "", RegexOptions.IgnoreCase);
                    descriptivePart = descriptivePart.Trim();

                    foreach (var srcFile in group.Files)
                    {
                        string srcFileName = Path.GetFileName(srcFile);
                        string newFileName = GenerateVpkFileName(srcFileName, descriptivePart, pakPrefix);

                        modInfo.FileMappings.Add(new FileMapping
                        {
                            OriginalName = srcFileName,
                            CurrentName = newFileName,
                            PakPrefix = pakPrefix,
                            Type = FileType.Vpk,
                            RelativePath = GetRelativePath(Path.GetDirectoryName(srcFile), srcFile)
                        });

                        string disabledName = DisabledName(modInfo.FolderName, newFileName);
                        string dst = Path.Combine(activeModsPath, disabledName);

                        if (File.Exists(dst)) File.Delete(dst);
                        File.Copy(srcFile, dst);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool ProcessDirectoryStructure(ModStructureInfo structureInfo, ModInfo modInfo)
        {
            if (!structureInfo.Directories.Any()) return true;

            string modPrefix = GenerateUniqueModPrefix(modInfo.FolderName);

            foreach (var dirMapping in structureInfo.Directories)
            {
                string dirType = dirMapping.Key;
                string sourcePath = dirMapping.Value;

                if (!SupportedDirectories.ContainsKey(dirType))
                    continue;

                string targetGamePath = SupportedDirectories[dirType];

                var directoryMapping = new DirectoryMapping
                {
                    SourcePath = sourcePath,
                    TargetPath = targetGamePath,
                    ModPrefix = modPrefix
                };

                if (structureInfo.FilesByDirectory.ContainsKey(dirType))
                {
                    foreach (string filePath in structureInfo.FilesByDirectory[dirType])
                    {
                        string fileName = Path.GetFileName(filePath);

                        if (dirType.ToLowerInvariant() == "addons" && fileName.ToLowerInvariant().EndsWith(".vpk"))
                        {
                            continue;
                        }

                        string relativePath = GetRelativePath(sourcePath, filePath);
                        string finalFileName = fileName;
                        FileType fileType = analyzer.DetermineFileType(fileName, dirType);

                        modInfo.FileMappings.Add(new FileMapping
                        {
                            OriginalName = fileName,
                            CurrentName = finalFileName,
                            RelativePath = relativePath,
                            Type = fileType,
                            PakPrefix = modPrefix
                        });

                        directoryMapping.Files.Add(finalFileName);

                        string targetDir = Path.Combine(gamePath, targetGamePath.Replace("/", "\\"));
                        string disabledFileName = DisabledName(modInfo.FolderName, finalFileName);
                        string targetPath = Path.Combine(targetDir, disabledFileName);

                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                        if (File.Exists(targetPath)) File.Delete(targetPath);
                        File.Copy(filePath, targetPath);
                    }
                }

                modInfo.DirectoryMappings.Add(directoryMapping);
            }

            return true;
        }

        private bool ProcessMultipleModsIndependently(ModStructureInfo structureInfo, string tempDir, string originalFilePath)
        {
            bool allSuccess = true;

            var vpkGroups = FindVpkGroups(structureInfo.VpkFiles);
            foreach (var group in vpkGroups)
            {
                string singleModTempDir = Path.Combine(tempDir, "mod_install_temp", group.BaseName);
                Directory.CreateDirectory(singleModTempDir);
                string primaryVpkPath = group.Files.FirstOrDefault(f => f.EndsWith("_dir.vpk", StringComparison.OrdinalIgnoreCase)) ?? group.Files.First();

                foreach (var file in group.Files)
                {
                    File.Copy(file, Path.Combine(singleModTempDir, Path.GetFileName(file)));
                }

                if (!ProcessModContents(singleModTempDir, primaryVpkPath, false))
                {
                    allSuccess = false;
                }
            }

            return allSuccess;
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

        private string DeriveModNameFromFile(string originalFilePath)
        {
            string fileName = Path.GetFileName(originalFilePath);
            string modName = Path.GetFileNameWithoutExtension(fileName);

            int separatorIndex = modName.IndexOf(" - ");
            if (separatorIndex > 0)
            {
                modName = modName.Substring(separatorIndex + 3).Trim();
            }
            else if (Regex.IsMatch(modName, @"_v?\d+(\.\d+)*$", RegexOptions.IgnoreCase))
            {
                modName = Regex.Replace(modName, @"_v?\d+(\.\d+)*$", "", RegexOptions.IgnoreCase);
            }
            else if (modName.Contains(" by "))
            {
                modName = modName.Substring(0, modName.IndexOf(" by "));
            }

            modName = modName.Replace("_", " ").Replace("-", " ");
            modName = Regex.Replace(modName, @"\s+", " ").Trim();
            return modName;
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

        private string GenerateVpkFileName(string srcFileName, string descriptivePart, string pakPrefix)
        {
            if (srcFileName.EndsWith("_dir.vpk", StringComparison.OrdinalIgnoreCase))
            {
                return $"{descriptivePart}_{pakPrefix}_dir.vpk";
            }
            else if (srcFileName.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(srcFileName, @"_(\d{3})\.vpk$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string number = match.Groups[1].Value;
                    return $"{descriptivePart}_{pakPrefix}_{number}.vpk";
                }
                else
                {
                    return $"{descriptivePart}_{pakPrefix}_dir.vpk";
                }
            }
            else
            {
                return $"{descriptivePart}_{pakPrefix}_dir.vpk";
            }
        }

        private string GenerateUniqueModPrefix(string folderName)
        {
            var catalog = catalogManager.LoadCatalog();
            var manifest = catalogManager.LoadManifest();

            var existingPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var mod in catalog)
            {
                foreach (var mapping in mod.DirectoryMappings)
                {
                    existingPrefixes.Add(mapping.ModPrefix);
                }
            }

            foreach (var mod in manifest)
            {
                foreach (var prefix in mod.PakPrefixes)
                {
                    existingPrefixes.Add(prefix);
                }
            }

            string basePrefix = ModDirPrefix + MakeSafeIdentifier(folderName).Substring(0, Math.Min(8, folderName.Length));
            string uniquePrefix = basePrefix;
            int counter = 1;

            while (existingPrefixes.Contains(uniquePrefix))
            {
                uniquePrefix = $"{basePrefix}_{counter:D2}";
                counter++;
            }

            return uniquePrefix;
        }

        private string GetNextPakPrefix(HashSet<string> taken)
        {
            int i = 1;
            while (i <= 99)
            {
                string prefix = $"pak{i:D2}";
                if (!taken.Contains(prefix))
                {
                    return prefix;
                }
                i++;
            }

            throw new InvalidOperationException("All pak prefixes from pak01 to pak99 are taken. Cannot install more mods.");
        }

        private HashSet<string> CollectTakenPakPrefixes()
        {
            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            taken.Add("pak00");

            if (!Directory.Exists(activeModsPath)) return taken;

            var pakRegex = new Regex(@"(pak\d{2})", RegexOptions.IgnoreCase);

            foreach (var vpkFile in Directory.GetFiles(activeModsPath, "*.vpk", SearchOption.AllDirectories))
            {
                var match = pakRegex.Match(Path.GetFileName(vpkFile));
                if (match.Success)
                {
                    taken.Add(match.Groups[1].Value);
                }
            }

            foreach (var mod in catalogManager.LoadManifest())
            {
                foreach (var prefix in mod.PakPrefixes)
                {
                    taken.Add(prefix);
                }
            }

            return taken;
        }

        private static string GetRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath)) return toPath;
            if (string.IsNullOrEmpty(toPath)) return string.Empty;

            Uri fromUri = new Uri(AppendDirectorySeparatorChar(fromPath));
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) return toPath;

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            if (!Path.HasExtension(path) && !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                return path + Path.DirectorySeparatorChar;
            }
            return path;
        }

        private string DisabledName(string folderName, string tail) => DisabledPrefix + folderName + ModSep + tail;

        private int DeleteDisabledAddonsForMod(string folderName)
        {
            if (!Directory.Exists(activeModsPath)) return 0;
            string prefix = DisabledPrefix + folderName + ModSep;
            int deleted = 0;
            foreach (var file in Directory.GetFiles(activeModsPath, prefix + "*.vpk"))
            {
                try { File.Delete(file); deleted++; }
                catch { }
            }
            return deleted;
        }
    }
}