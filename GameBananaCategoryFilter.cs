using System;
using System.Collections.Generic;

namespace Deadlock_Mod_Loader2
{
    /// <summary>
    /// Appends server-side category filters for GameBanana Mod/Index requests.
    /// </summary>
    internal static class GameBananaCategoryFilter
    {
        private static readonly Dictionary<string, int> CategoryIds =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Skins", 33295 },
                { "HUD", 31713 },
                { "Model Replacement", 33154 },
                { "Gameplay Modifications", 33331 },
                { "Other/Misc", 31710 }
            };
        private static readonly Dictionary<string, int> CharacterCategoryIds =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Abrams", 33306 },
                { "Bebop", 33307 },        // ADDED - was missing
                { "Dynamo", 33308 },
                { "Grey Talon", 33310 },
                { "Haze", 33311 },
                { "Infernus", 33312 },
                { "Ivy", 33313 },          // ADDED - was missing  
                { "Kelvin", 33314 },       // ADDED - was missing
                { "Lady Geist", 33315 },   // ADDED - was missing
                { "Lash", 33316 },
                { "McGinnis", 33317 },     // ADDED - was missing
                { "Mirage", 33318 },
                { "Mo & Krill", 33319 },
                { "Paradox", 33320 },      // ADDED - was missing
                { "Pocket", 33321 },
                { "Seven", 33322 },
                { "Shiv", 33323 },
                { "Vindicta", 33324 },
                { "Viscous", 33325 },
                { "Warden", 33326 },
                { "Wraith", 33327 },
                { "Yamato", 33328 }
            };

        /// <summary>
        /// Appends a supported server-side filter to the Mod/Index URL.
        /// Priority: Character category -> top-level category. If neither resolves, no-op.
        /// </summary>
        internal static void AppendCategoryFilter(ref string url, SearchFilters filters)
        {
            if (filters == null) return;

            int id;
            if (!string.IsNullOrWhiteSpace(filters.Character)
                && CharacterCategoryIds.TryGetValue(filters.Character, out id))
            {
                url += "&_aFilters[Generic_Category]=" + id;
                System.Diagnostics.Debug.WriteLine("[DEBUG] Using server category filter (character): " + filters.Character + " -> " + id);
                return;
            }

            if (!string.IsNullOrWhiteSpace(filters.Category)
                && CategoryIds.TryGetValue(filters.Category, out id))
            {
                url += "&_aFilters[Generic_Category]=" + id;
                System.Diagnostics.Debug.WriteLine("[DEBUG] Using server category filter (category): " + filters.Category + " -> " + id);
            }
        }
    }
}