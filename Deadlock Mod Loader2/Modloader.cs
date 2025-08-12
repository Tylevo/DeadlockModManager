using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Deadlock_Mod_Loader2
{
    public class ModLoader
    {
        private string gamePath;
        private string addonsPath;
        private string gameInfoPath;

        // New public method to get the addons path for the "Open Folder" button
        public string GetAddonsPath()
        {
            return addonsPath;
        }

        public bool SetGamePath(string path)
        {
            if (Directory.Exists(path) && Directory.Exists(Path.Combine(path, "citadel")))
            {
                gamePath = path;
                addonsPath = Path.Combine(gamePath, "citadel", "addons");
                gameInfoPath = Path.Combine(gamePath, "citadel", "gameinfo.gi");
                return true;
            }
            return false;
        }

        public void InitialSetup()
        {
            if (string.IsNullOrEmpty(gamePath)) return;

            if (!Directory.Exists(addonsPath))
            {
                Directory.CreateDirectory(addonsPath);
            }

            if (File.Exists(gameInfoPath))
            {
                var lines = File.ReadAllLines(gameInfoPath).ToList();
                bool alreadyModded = lines.Any(line => line.Trim().Contains("citadel/addons"));
                if (alreadyModded) return;

                int searchPathsStartLine = -1;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Trim().Equals("SearchPaths", StringComparison.OrdinalIgnoreCase))
                    {
                        searchPathsStartLine = i;
                        break;
                    }
                }

                if (searchPathsStartLine != -1)
                {
                    int braceCount = 0;
                    bool inBlock = false;
                    for (int i = searchPathsStartLine; i < lines.Count; i++)
                    {
                        if (lines[i].Contains("{")) { inBlock = true; braceCount++; }
                        if (lines[i].Contains("}")) { braceCount--; }
                        if (inBlock && braceCount == 0)
                        {
                            lines.RemoveRange(searchPathsStartLine, (i - searchPathsStartLine) + 1);
                            break;
                        }
                    }
                }

                int fileSystemEndIndex = -1;
                int fileSystemBraceIndex = -1;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Trim().Equals("FileSystem", StringComparison.OrdinalIgnoreCase))
                    {
                        int braceCount = 0;
                        bool inBlock = false;
                        for (int j = i; j < lines.Count; j++)
                        {
                            if (lines[j].Contains("{"))
                            {
                                if (!inBlock) fileSystemBraceIndex = j;
                                inBlock = true;
                                braceCount++;
                            }
                            if (lines[j].Contains("}")) { braceCount--; }
                            if (inBlock && braceCount == 0)
                            {
                                fileSystemEndIndex = j;
                                break;
                            }
                        }
                        break;
                    }
                }

                if (fileSystemBraceIndex != -1)
                {
                    var newSearchPaths = new List<string>
                    {
                        "		SearchPaths",
                        "		{",
                        "			Game				citadel/addons",
                        "			Mod					citadel",
                        "			Write				citadel",
                        "			Game				citadel",
                        "			Write				core",
                        "			Mod					core",
                        "			Game				core",
                        "		}"
                    };
                    lines.InsertRange(fileSystemBraceIndex + 1, newSearchPaths);
                }

                bool addonConfigExists = lines.Any(line => line.Trim().Equals("AddonConfig", StringComparison.OrdinalIgnoreCase));
                if (!addonConfigExists && fileSystemEndIndex != -1)
                {
                    var addonConfig = new List<string>
                    {
                        "	AddonConfig",
                        "	{",
                        "		\"UseOfficialAddons\"\t\"1\"",
                        "	}"
                    };
                    lines.InsertRange(fileSystemEndIndex + 1, addonConfig);
                }

                File.WriteAllLines(gameInfoPath, lines);
                MessageBox.Show("gameinfo.gi has been updated for mod support.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("gameinfo.gi not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public List<string> GetInstalledMods()
        {
            if (Directory.Exists(addonsPath))
            {
                return Directory.GetFiles(addonsPath)
                    .Where(file => file.EndsWith(".vpk") || file.EndsWith(".vpk.disabled"))
                    .Select(Path.GetFileName)
                    .ToList();
            }
            return new List<string>();
        }

        public void EnableMod(string modName)
        {
            string disabledPath = Path.Combine(addonsPath, modName);
            if (disabledPath.EndsWith(".vpk.disabled"))
            {
                string enabledPath = disabledPath.Replace(".vpk.disabled", ".vpk");
                File.Move(disabledPath, enabledPath);
            }
        }

        public void DisableMod(string modName)
        {
            string enabledPath = Path.Combine(addonsPath, modName);
            if (enabledPath.EndsWith(".vpk"))
            {
                string disabledPath = enabledPath + ".disabled";
                File.Move(enabledPath, disabledPath);
            }
        }

        public bool InstallDroppedMod(string sourceFile)
        {
            if (string.IsNullOrEmpty(addonsPath))
            {
                MessageBox.Show("Please set the game path first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            string destFile = Path.Combine(addonsPath, Path.GetFileName(sourceFile));
            File.Copy(sourceFile, destFile, true);
            return true;
        }

        public bool UninstallMod(string modName)
        {
            if (string.IsNullOrEmpty(modName)) return false;

            string modPath = Path.Combine(addonsPath, modName);
            if (File.Exists(modPath))
            {
                File.Delete(modPath);
                return true;
            }
            return false;
        }
    }
}
