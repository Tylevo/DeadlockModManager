using System;
using System.Collections.Generic;

namespace Deadlock_Mod_Loader2
{
    public class ModInfo
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string FolderName { get; set; }
        public List<FileMapping> FileMappings { get; set; } = new List<FileMapping>();
        public List<DirectoryMapping> DirectoryMappings { get; set; } = new List<DirectoryMapping>();
        public ModType Type { get; set; } = ModType.VpkOnly;
    }

    public enum ModType
    {
        VpkOnly,        // Only contains VPK files
        DirectoryBased, // Contains directory structure (maps, cfg, etc.)
        Mixed           // Contains both VPKs and directories
    }

    public class FileMapping
    {
        public string OriginalName { get; set; }
        public string CurrentName { get; set; }
        public string PakPrefix { get; set; }
        public string RelativePath { get; set; }
        public FileType Type { get; set; } = FileType.Vpk;
    }

    public enum FileType
    {
        Vpk,
        Map,
        Config,
        Asset,
        Other
    }

    public class DirectoryMapping
    {
        public string SourcePath { get; set; }      // Original path in the mod
        public string TargetPath { get; set; }     // Where it maps to in the game
        public string ModPrefix { get; set; }      // Unique prefix for this mod's files
        public List<string> Files { get; set; } = new List<string>(); // Files in this directory
    }

    public class ActiveModInfo
    {
        public string ModName { get; set; }
        public List<string> PakPrefixes { get; set; } = new List<string>();
        public string OriginalFolderName { get; set; }
        public ModType Type { get; set; } = ModType.VpkOnly;
        public List<string> ActiveDirectories { get; set; } = new List<string>();
    }

    public class ModStructureInfo
    {
        public bool HasCitadelStructure { get; set; }
        public string CitadelPath { get; set; }
        public Dictionary<string, string> Directories { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, List<string>> FilesByDirectory { get; set; } = new Dictionary<string, List<string>>();
        public List<string> VpkFiles { get; set; } = new List<string>();
        public ModType Type { get; set; }
    }

    public class ModProfile
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastUsed { get; set; }
        public List<string> ActiveModFolderNames { get; set; } = new List<string>();
        public Dictionary<string, int> ModLoadOrder { get; set; } = new Dictionary<string, int>();
    }

    public class VpkGroup
    {
        public string BaseName { get; set; }
        public List<string> Files { get; set; } = new List<string>();
    }
}