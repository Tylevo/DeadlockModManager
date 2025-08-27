using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Deadlock_Mod_Loader2
{
    public class ModAnalyzer
    {
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

        public ModStructureInfo AnalyzeModStructure(string tempDir)
        {
            Console.WriteLine($"[DEBUG] AnalyzeModStructure called with: {tempDir}");
            var info = new ModStructureInfo();

            string extractedCitadelPath = FindCitadelPath(tempDir);

            info.VpkFiles = new List<string>();

            if (!string.IsNullOrEmpty(extractedCitadelPath) && Directory.Exists(extractedCitadelPath))
            {
                info.HasCitadelStructure = true;
                info.CitadelPath = extractedCitadelPath;
                AnalyzeCitadelStructure(extractedCitadelPath, info);
            }
            else
            {
                AnalyzeStandaloneStructure(tempDir, info);
            }
            FindUnhandledVpks(tempDir, info);

            DetermineModType(info);

            Console.WriteLine($"[DEBUG] Mod type determined: {info.Type}");
            return info;
        }

        private string FindCitadelPath(string tempDir)
        {
            string extractedCitadelPath = Path.Combine(tempDir, "citadel");

            if (!Directory.Exists(extractedCitadelPath))
            {
                var gameDir = Directory.GetDirectories(tempDir, "*", SearchOption.AllDirectories)
                                       .FirstOrDefault(d => Path.GetFileName(d).Equals("game", StringComparison.OrdinalIgnoreCase));
                if (gameDir != null)
                {
                    extractedCitadelPath = Path.Combine(gameDir, "citadel");
                }
                if (!Directory.Exists(extractedCitadelPath))
                {
                    extractedCitadelPath = Directory.GetDirectories(tempDir, "*", SearchOption.AllDirectories)
                                                  .FirstOrDefault(d => Path.GetFileName(d).Equals("citadel", StringComparison.OrdinalIgnoreCase));
                }
            }

            return extractedCitadelPath;
        }

        private void AnalyzeCitadelStructure(string extractedCitadelPath, ModStructureInfo info)
        {
            foreach (var kvp in SupportedDirectories)
            {
                string dirPath = Path.Combine(extractedCitadelPath, kvp.Key);
                if (Directory.Exists(dirPath))
                {
                    info.Directories.Add(kvp.Key, dirPath);

                    var allFiles = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories).ToList();
                    var nonVpkFiles = allFiles.Where(f => !f.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase)).ToList();
                    var vpkFiles = allFiles.Where(f => f.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase)).ToList();

                    info.FilesByDirectory[kvp.Key] = nonVpkFiles;

                    if (kvp.Key.Equals("addons", StringComparison.OrdinalIgnoreCase))
                    {
                        info.VpkFiles.AddRange(vpkFiles);
                        Console.WriteLine($"[DEBUG] Found {vpkFiles.Count} VPK files in addons directory");
                    }
                    else if (vpkFiles.Any())
                    {
                        Console.WriteLine($"[DEBUG] Found {vpkFiles.Count} VPK files in {kvp.Key} directory - treating as regular files");
                        var combinedFiles = new List<string>(nonVpkFiles);
                        combinedFiles.AddRange(vpkFiles);
                        info.FilesByDirectory[kvp.Key] = combinedFiles;
                    }
                }
            }
        }

        private void AnalyzeStandaloneStructure(string tempDir, ModStructureInfo info)
        {
            var standaloneDirectories = new[] { "cfg", "maps", "addons", "materials", "models", "scripts", "sounds", "particles" };

            foreach (var dirName in standaloneDirectories)
            {
                var foundDirs = Directory.GetDirectories(tempDir, dirName, SearchOption.AllDirectories);

                foreach (var dirPath in foundDirs)
                {
                    if (!info.Directories.ContainsKey(dirName))
                    {
                        info.Directories.Add(dirName, dirPath);

                        var allFiles = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories).ToList();
                        var nonVpkFiles = allFiles.Where(f => !f.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase)).ToList();
                        var vpkFiles = allFiles.Where(f => f.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase)).ToList();

                        info.FilesByDirectory[dirName] = nonVpkFiles;

                        if (dirName.Equals("addons", StringComparison.OrdinalIgnoreCase))
                        {
                            info.VpkFiles.AddRange(vpkFiles);
                        }
                        else if (vpkFiles.Any())
                        {
                            Console.WriteLine($"[DEBUG] Warning: Found {vpkFiles.Count} VPK files in {dirName} directory");
                            info.VpkFiles.AddRange(vpkFiles);
                        }
                    }
                }
            }
        }

        private void FindUnhandledVpks(string tempDir, ModStructureInfo info)
        {
            var allVpks = Directory.GetFiles(tempDir, "*.vpk", SearchOption.AllDirectories).ToList();
            foreach (var vpk in allVpks)
            {
                bool alreadyHandled = false;
                foreach (var dirFiles in info.FilesByDirectory.Values)
                {
                    if (dirFiles.Contains(vpk))
                    {
                        alreadyHandled = true;
                        break;
                    }
                }

                if (!alreadyHandled && !info.VpkFiles.Contains(vpk))
                {
                    info.VpkFiles.Add(vpk);
                    Console.WriteLine($"[DEBUG] Found unhandled VPK file: {vpk}");
                }
            }
        }

        private void DetermineModType(ModStructureInfo info)
        {
            bool hasVpks = info.VpkFiles.Any();
            bool hasDirectories = info.Directories.Any(kvp =>
                info.FilesByDirectory.ContainsKey(kvp.Key) && info.FilesByDirectory[kvp.Key].Any());

            if (hasVpks && hasDirectories)
                info.Type = ModType.Mixed;
            else if (hasVpks)
                info.Type = ModType.VpkOnly;
            else if (hasDirectories)
                info.Type = ModType.DirectoryBased;
            else
                info.Type = ModType.VpkOnly; // Default fallback

            Console.WriteLine($"[DEBUG] Total VPK files found: {info.VpkFiles.Count}");
            Console.WriteLine($"[DEBUG] Directory mappings found: {info.Directories.Count}");
            foreach (var dir in info.Directories)
            {
                var fileCount = info.FilesByDirectory.ContainsKey(dir.Key) ? info.FilesByDirectory[dir.Key].Count : 0;
                Console.WriteLine($"[DEBUG] - {dir.Key}: {fileCount} non-VPK files");
            }
        }

        public bool HasComplexStructure(string tempDir)
        {
            try
            {
                var vpkFiles = Directory.GetFiles(tempDir, "*.vpk", SearchOption.AllDirectories);

                Console.WriteLine($"[DEBUG] Complex structure check: Found {vpkFiles.Length} VPK files");

                if (vpkFiles.Length == 0)
                {
                    Console.WriteLine("[DEBUG] No VPK files found - not complex");
                    return false;
                }

                var vpkDirectories = vpkFiles
                    .Select(f => Path.GetDirectoryName(f))
                    .Where(d => !string.IsNullOrEmpty(d))
                    .Distinct()
                    .ToList();

                Console.WriteLine($"[DEBUG] VPK directories count: {vpkDirectories.Count}");
                foreach (var dir in vpkDirectories)
                {
                    Console.WriteLine($"[DEBUG] VPK directory: {dir}");
                }

                var allDirectories = Directory.GetDirectories(tempDir, "*", SearchOption.AllDirectories);
                var folderNames = allDirectories
                    .Select(d => Path.GetFileName(d).ToLowerInvariant())
                    .Where(name => !string.IsNullOrEmpty(name));

                Console.WriteLine($"[DEBUG] All folder names: {string.Join(", ", folderNames)}");

                var choiceIndicators = new[] {
            "option", "choice", "separate", "both", "version", "variant",
            "pick", "choose", "select", "alternative", "alt", "optional",
            "v1", "v2", "v3", "type", "style", "mode"
        };

                bool hasChoiceKeywords = folderNames.Any(name =>
                    choiceIndicators.Any(keyword => name.Contains(keyword)));
                bool hasNumberedFolders = folderNames.Count(name =>
                    int.TryParse(name, out int num) && num >= 1 && num <= 10) >= 2;
                var vpkBaseNames = vpkFiles
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .Select(name => {
                        var cleanName = name;
                        if (cleanName.EndsWith("_dir", StringComparison.OrdinalIgnoreCase))
                            cleanName = cleanName.Substring(0, cleanName.Length - 4);
                        if (cleanName.EndsWith("_001", StringComparison.OrdinalIgnoreCase))
                            cleanName = cleanName.Substring(0, cleanName.Length - 4);
                        return cleanName.ToLowerInvariant();
                    })
                    .Distinct()
                    .ToList();

                bool hasMultipleVpkSets = vpkBaseNames.Count > 1;

                Console.WriteLine($"[DEBUG] Has choice keywords: {hasChoiceKeywords}");
                Console.WriteLine($"[DEBUG] Has numbered folders: {hasNumberedFolders}");
                Console.WriteLine($"[DEBUG] Has multiple VPK sets: {hasMultipleVpkSets}");
                Console.WriteLine($"[DEBUG] VPK directories > 1: {vpkDirectories.Count > 1}");
                bool isComplex = (vpkDirectories.Count > 1 && hasChoiceKeywords) ||
                                hasChoiceKeywords ||
                                hasNumberedFolders ||
                                hasMultipleVpkSets;

                Console.WriteLine($"[DEBUG] Final result - Is complex: {isComplex}");
                return isComplex;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] HasComplexStructure error: {ex.Message}");
                return false; // If we can't analyze it, assume it's simple
            }
        }

        public FileType DetermineFileType(string fileName, string dirType)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();

            switch (dirType.ToLowerInvariant())
            {
                case "maps":
                    return FileType.Map;
                case "cfg":
                    return FileType.Config;
                default:
                    if (extension == ".vpk")
                        return FileType.Vpk;
                    else
                        return FileType.Asset;
            }
        }
    }
}