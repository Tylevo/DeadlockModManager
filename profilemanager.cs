using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Deadlock_Mod_Loader2
{
    public class ProfileManager
    {
        private string profilesPath;
        private string currentProfileName = "Default";

        public ProfileManager(string gamePath)
        {
            if (!string.IsNullOrEmpty(gamePath))
            {
                profilesPath = Path.Combine(Path.Combine(gamePath, "citadel", "addons"), "profiles.json");
            }
        }

        public List<ModProfile> LoadProfiles()
        {
            if (string.IsNullOrEmpty(profilesPath) || !File.Exists(profilesPath))
                return new List<ModProfile> { CreateDefaultProfile() };

            try
            {
                string json = File.ReadAllText(profilesPath);
                var profiles = JsonConvert.DeserializeObject<List<ModProfile>>(json) ?? new List<ModProfile>();

                if (!profiles.Any(p => p.Name == "Default"))
                {
                    profiles.Insert(0, CreateDefaultProfile());
                }

                return profiles;
            }
            catch
            {
                return new List<ModProfile> { CreateDefaultProfile() };
            }
        }

        public void SaveProfiles(List<ModProfile> profiles)
        {
            if (string.IsNullOrEmpty(profilesPath)) return;

            try
            {
                string json = JsonConvert.SerializeObject(profiles, Formatting.Indented);
                File.WriteAllText(profilesPath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save profiles: {ex.Message}");
            }
        }

        public ModProfile CreateDefaultProfile()
        {
            return new ModProfile
            {
                Name = "Default",
                Description = "Default mod configuration",
                Created = DateTime.Now,
                LastUsed = DateTime.Now
            };
        }

        public string CurrentProfile
        {
            get => currentProfileName;
            set => currentProfileName = value ?? "Default";
        }
    }
}