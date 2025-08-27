using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Deadlock_Mod_Loader2
{
    public class SearchFilters
    {
        public string Category { get; set; }
        public string Character { get; set; }

        public static class Categories
        {
            public const string Skins = "Skins";
            public const string GameplayModifications = "Gameplay Modifications";
            public const string HUD = "HUD";
            public const string ModelReplacement = "Model Replacement";
            public const string OtherMisc = "Other/Misc";
        }

        public static class Characters
        {
            public const string Abrams = "Abrams";
            public const string Bebop = "Bebop";
            public const string Dynamo = "Dynamo";
            public const string GreyTalon = "Grey Talon";
            public const string Haze = "Haze";
            public const string Infernus = "Infernus";
            public const string Ivy = "Ivy";
            public const string Kelvin = "Kelvin";
            public const string LadyGeist = "Lady Geist";
            public const string Lash = "Lash";
            public const string McGinnis = "McGinnis";
            public const string Mirage = "Mirage";
            public const string Mo = "Mo & Krill";
            public const string Paradox = "Paradox";
            public const string Pocket = "Pocket";
            public const string Seven = "Seven";
            public const string Shiv = "Shiv";
            public const string Vindicta = "Vindicta";
            public const string Viscous = "Viscous";
            public const string Warden = "Warden";
            public const string Wraith = "Wraith";
            public const string Yamato = "Yamato";
        }
    }

    public class GameBananaSearchService
    {
        private static readonly HttpClient http = new HttpClient();
        private const int DEADLOCK_GAME_ID = 20948;
        private const string APIV11_ROOT = "https://gamebanana.com/apiv11/Mod";

        static GameBananaSearchService()
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 |
                    System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;
            }
            catch { }

            http.DefaultRequestHeaders.UserAgent.Clear();
            http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126 Safari/537.36 Deadlock-Mod-Manager/1.4");
            http.Timeout = TimeSpan.FromSeconds(30);
        }
        private static List<GameBananaSubmission> SortFilteredMods(List<GameBananaSubmission> mods, string sortType)
        {
            if (mods == null || mods.Count == 0) return mods;

            switch (sortType)
            {
                case "Recently Updated":
                    return mods.OrderByDescending(m => m.DateModifiedDateTime).ToList();
                case "Most Downloaded":
                    return mods.OrderByDescending(m => m.DownloadCount).ToList();
                case "Most Liked":
                    return mods.OrderByDescending(m => m.LikeCount).ToList();
                case "Newest":
                default:
                    return mods.OrderByDescending(m => m.DateAddedDateTime).ToList();
            }
        }

        public static async Task<List<GameBananaSubmission>> FetchModsAsync(string search, string sort, int page, SearchFilters filters)
        {
            if (filters != null
                && string.IsNullOrWhiteSpace(filters.Character)
                && !string.IsNullOrWhiteSpace(filters.Category)
                && string.IsNullOrWhiteSpace(search))
            {
                List<GameBananaSubmission> categoryMods;

                if (string.Equals(filters.Category, SearchFilters.Categories.Skins, StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] FIXED: Fetching all skins from all hero categories, page {page}");
                    categoryMods = await FetchAllSkinsFromAllHeroesAsync(page);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] FIXED: Fetching all mods from {filters.Category} category, page {page}");
                    categoryMods = await FetchCategoryModsAsync(filters.Category, page);
                }
                if (categoryMods != null && categoryMods.Count > 0 && sort != "Newest")
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] POST-PROCESSING: Sorting {categoryMods.Count} {filters.Category} mods by {sort}");
                    categoryMods = SortFilteredMods(categoryMods, sort);
                }

                return categoryMods;
            }

            bool hasFilters = filters != null && (!string.IsNullOrEmpty(filters.Category) || !string.IsNullOrEmpty(filters.Character));
            var sortAlias = hasFilters ? "Generic_Newest" : GetSortAlias(sort);

            var fields = string.Join(",", new[]
            {
                "_idRow", "_sName", "_sText", "_tsDateAdded", "_tsDateModified",
                "_nViewCount", "_nLikeCount", "_nDownloadCount",
                "_aSubmitter[_sName]",
                "_aPreviewMedia[_aImages[_sBaseUrl,_sFile530,_sFile220]]",
                "_aCategory[_sName,_idRow]"
            });

            int pageSize = string.IsNullOrEmpty(search) ? 25 : 50;

            System.Diagnostics.Debug.WriteLine($"[DEBUG] FetchModsAsync search='{search}', filters: {hasFilters}, pageSize: {pageSize}, sort: {sortAlias}");

            var url = APIV11_ROOT + "/Index" +
                      "?_nPerpage=" + pageSize +
                      "&_nPage=" + page +
                      "&_sSort=" + Uri.EscapeDataString(sortAlias) +
                      "&_aFilters[Generic_Game]=" + DEADLOCK_GAME_ID +
                      "&_sFields=" + Uri.EscapeDataString(fields);

            if (!string.IsNullOrWhiteSpace(search))
                url += "&_sSearchString=" + Uri.EscapeDataString(search);

            if (filters != null)
                GameBananaCategoryFilter.AppendCategoryFilter(ref url, filters);

            var allMods = new List<GameBananaSubmission>();

            try
            {
                int maxPages = hasFilters ? 4 : 1;
                for (int currentPage = page; currentPage <= page + maxPages - 1; currentPage++)
                {
                    var pageUrl = (currentPage == page)
                        ? url
                        : url.Replace("_nPage=" + page, "_nPage=" + currentPage);

                    var response = await http.GetAsync(pageUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine("[DEBUG] Page " + currentPage + " failed with status: " + response.StatusCode);
                        continue;
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<GameBananaPaginatedResponse<GameBananaSubmission>>(json);
                    var pageMods = result?.Records ?? new List<GameBananaSubmission>();

                    System.Diagnostics.Debug.WriteLine("[DEBUG] Page " + currentPage + " returned " + pageMods.Count + " mods");

                    if (pageMods.Count == 0)
                        break;

                    allMods.AddRange(pageMods);
                }

                if (hasFilters || allMods.Count > 0)
                {
                    int before = allMods.Count;
                    allMods = ApplyFilters(allMods, filters);
                    System.Diagnostics.Debug.WriteLine("[DEBUG] After ApplyFilters: " + allMods.Count + " (was " + before + ")");
                    if (hasFilters && allMods.Count > 0 && sort != "Newest")
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] POST-PROCESSING: Re-sorting {allMods.Count} filtered results by {sort}");
                        allMods = SortFilteredMods(allMods, sort);
                    }
                }

                return allMods;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Exception in FetchModsAsync: " + ex.Message);
                return new List<GameBananaSubmission>();
            }
        }
        private static async Task<List<GameBananaSubmission>> FetchCategoryModsAsync(string category, int page = 1)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] FIXED: FetchCategoryModsAsync for {category}, page {page}");

            var fields = string.Join(",", new[]
            {
                "_idRow", "_sName", "_sText", "_tsDateAdded", "_tsDateModified",
                "_nViewCount", "_nLikeCount", "_nDownloadCount",
                "_aSubmitter[_sName]",
                "_aPreviewMedia[_aImages[_sBaseUrl,_sFile530,_sFile220]]",
                "_aCategory[_sName,_idRow]"
            });

            var baseUrl = APIV11_ROOT + "/Index";
            var parameters = new List<string>
            {
                "_nPerpage=50",
                $"_nPage={page}",
                "_sSort=Generic_Newest",
                $"_aFilters[Generic_Game]={DEADLOCK_GAME_ID}",
                $"_sFields={Uri.EscapeDataString(fields)}"
            };

            var categoryFilter = new SearchFilters { Category = category };
            var tempUrl = "";
            GameBananaCategoryFilter.AppendCategoryFilter(ref tempUrl, categoryFilter);

            if (!string.IsNullOrEmpty(tempUrl))
            {
                var filterPart = tempUrl.TrimStart('&');
                parameters.Add(filterPart);
            }

            var url = baseUrl + "?" + string.Join("&", parameters);

            System.Diagnostics.Debug.WriteLine($"[DEBUG] FIXED URL: {url}");

            try
            {
                var response = await http.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Category fetch failed for {category}: {response.StatusCode}");
                    return await FetchCategoryDirectAsync(category);
                }

                var result = JsonConvert.DeserializeObject<GameBananaPaginatedResponse<GameBananaSubmission>>(responseContent);
                var mods = result?.Records ?? new List<GameBananaSubmission>();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Successfully fetched {mods.Count} mods for {category} page {page}");
                return mods;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Exception fetching {category}: {ex.Message}");
                return await FetchCategoryDirectAsync(category);
            }
        }
        private static async Task<List<GameBananaSubmission>> FetchCategoryDirectAsync(string category)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] FIXED: Trying direct category fetch for {category}");

            var fields = "_idRow,_sName,_sText,_tsDateAdded,_aSubmitter[_sName]";

            var baseUrl = APIV11_ROOT + "/Index";
            var parameters = new List<string>
            {
                "_nPerpage=50",
                "_nPage=1",
                "_sSort=Generic_Newest",
                $"_aFilters[Generic_Game]={DEADLOCK_GAME_ID}",
                $"_sFields={fields}"
            };
            int categoryId;
            switch (category)
            {
                case "HUD":
                    categoryId = 31713;
                    break;
                case "Other/Misc":
                    categoryId = 31710;
                    break;
                case "Skins":
                    categoryId = 33295;
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Unknown category: {category}");
                    return new List<GameBananaSubmission>();
            }

            parameters.Add($"_aFilters[Generic_Category]={categoryId}");

            var url = baseUrl + "?" + string.Join("&", parameters);
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Direct URL: {url}");

            try
            {
                var response = await http.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Direct Response Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<GameBananaPaginatedResponse<GameBananaSubmission>>(responseContent);
                    var mods = result?.Records ?? new List<GameBananaSubmission>();

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Direct fetch SUCCESS: {mods.Count} mods for {category}");
                    return mods;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Direct fetch failed: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Error response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Direct fetch exception: {ex.Message}");
            }

            return new List<GameBananaSubmission>();
        }
        private static async Task<List<GameBananaSubmission>> FetchAllSkinsFromAllHeroesAsync(int page = 1)
        {
            var allSkins = new List<GameBananaSubmission>();

            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] FIXED: Fetching skins from main category, page {page}");
                var topLevelFilter = new SearchFilters { Category = SearchFilters.Categories.Skins };
                var topLevelMods = await FetchTopLevelSkinsAsync(topLevelFilter, page);

                if (topLevelMods != null)
                {
                    allSkins.AddRange(topLevelMods);
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] FIXED: Returning {allSkins.Count} skins for page {page}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Failed to fetch skins page {page}: {ex.Message}");
            }

            return allSkins;
        }

        private static async Task<List<GameBananaSubmission>> FetchSingleHeroCategoryAsync(string hero, SearchFilters heroFilter)
        {
            var fields = string.Join(",", new[]
            {
                "_idRow", "_sName", "_sText", "_tsDateAdded", "_tsDateModified",
                "_nViewCount", "_nLikeCount", "_nDownloadCount",
                "_aSubmitter[_sName]",
                "_aPreviewMedia[_aImages[_sBaseUrl,_sFile530,_sFile220]]",
                "_aCategory[_sName,_idRow]"
            });

            var url = APIV11_ROOT + "/Index" +
                      "?_nPerpage=25" +
                      "&_nPage=1" +
                      "&_sSort=Generic_Newest" +
                      "&_aFilters[Generic_Game]=" + DEADLOCK_GAME_ID +
                      "&_sFields=" + Uri.EscapeDataString(fields);

            GameBananaCategoryFilter.AppendCategoryFilter(ref url, heroFilter);

            var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new List<GameBananaSubmission>();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<GameBananaPaginatedResponse<GameBananaSubmission>>(json);
            return result?.Records ?? new List<GameBananaSubmission>();
        }

        private static async Task<List<GameBananaSubmission>> FetchTopLevelSkinsAsync(SearchFilters skinsFilter, int page = 1)
        {
            var fields = string.Join(",", new[]
            {
                "_idRow", "_sName", "_sText", "_tsDateAdded", "_tsDateModified",
                "_nViewCount", "_nLikeCount", "_nDownloadCount",
                "_aSubmitter[_sName]",
                "_aPreviewMedia[_aImages[_sBaseUrl,_sFile530,_sFile220]]",
                "_aCategory[_sName,_idRow]"
            });

            var url = APIV11_ROOT + "/Index" +
                      "?_nPerpage=50" +
                      $"&_nPage={page}" +
                      "&_sSort=Generic_Newest" +
                      "&_aFilters[Generic_Game]=" + DEADLOCK_GAME_ID +
                      "&_sFields=" + Uri.EscapeDataString(fields);

            GameBananaCategoryFilter.AppendCategoryFilter(ref url, skinsFilter);

            var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new List<GameBananaSubmission>();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<GameBananaPaginatedResponse<GameBananaSubmission>>(json);
            return result?.Records ?? new List<GameBananaSubmission>();
        }

        public static async Task<List<GameBananaSubmission>> SearchWithFallbackStrategiesAsync(string searchTerm, string sort, SearchFilters filters)
        {
            var allResults = new List<GameBananaSubmission>();
            var seenIds = new HashSet<int>();
            bool hasFilters = filters != null && (!string.IsNullOrEmpty(filters.Category) || !string.IsNullOrEmpty(filters.Character));
            if (hasFilters && string.IsNullOrWhiteSpace(searchTerm))
            {
                var list = await FetchModsAsync(null, "Newest", 1, filters);
                return list ?? new List<GameBananaSubmission>();
            }
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var characterName = GetCharacterNameIfMatch(searchTerm);

                if (!string.IsNullOrEmpty(characterName))
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] SIMPLE: Detected character search for '{characterName}'");
                    return await SearchCharacterSimple(characterName, searchTerm);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] SIMPLE: Regular text search for '{searchTerm}'");
                    return await SearchTextSimple(searchTerm, sort, filters);
                }
            }

            return new List<GameBananaSubmission>();
        }
        private static string GetCharacterNameIfMatch(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return null;

            var heroes = new[]
            {
                SearchFilters.Characters.Abrams, SearchFilters.Characters.Bebop, SearchFilters.Characters.Dynamo,
                SearchFilters.Characters.GreyTalon, SearchFilters.Characters.Haze, SearchFilters.Characters.Infernus,
                SearchFilters.Characters.Ivy, SearchFilters.Characters.Kelvin, SearchFilters.Characters.LadyGeist,
                SearchFilters.Characters.Lash, SearchFilters.Characters.McGinnis, SearchFilters.Characters.Mirage,
                SearchFilters.Characters.Mo, SearchFilters.Characters.Paradox, SearchFilters.Characters.Pocket,
                SearchFilters.Characters.Seven, SearchFilters.Characters.Shiv, SearchFilters.Characters.Vindicta,
                SearchFilters.Characters.Viscous, SearchFilters.Characters.Warden, SearchFilters.Characters.Wraith,
                SearchFilters.Characters.Yamato
            };

            foreach (var hero in heroes)
            {
                if (string.Equals(hero, searchTerm, StringComparison.OrdinalIgnoreCase))
                    return hero;
            }
            if (searchTerm.Length >= 4)
            {
                var matches = heroes.Where(hero =>
                    hero.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();

                if (matches.Count == 1)
                    return matches[0];
            }

            return null;
        }
        private static async Task<List<GameBananaSubmission>> SearchCharacterSimple(string characterName, string searchTerm)
        {
            var allResults = new List<GameBananaSubmission>();
            var seenIds = new HashSet<int>();
            bool isExactMatch = string.Equals(characterName, searchTerm, StringComparison.OrdinalIgnoreCase);

            try
            {
                var characterFilter = new SearchFilters { Character = characterName };
                var categoryResults = await FetchModsAsync("", "Newest", 1, characterFilter);

                if (categoryResults != null)
                {
                    foreach (var mod in categoryResults)
                    {
                        if (mod != null && seenIds.Add(mod.Id))
                        {
                            allResults.Add(mod);
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Character category gave {categoryResults.Count} results");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Character category search failed: {ex.Message}");
            }
            if (isExactMatch)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] EXACT MATCH: Only returning {allResults.Count} category results for '{searchTerm}'");
                return SortSearchResultsByRelevance(allResults, searchTerm);
            }
            try
            {
                var textResults = await FetchModsAsync(searchTerm, "Newest", 1, null);
                if (textResults != null)
                {
                    foreach (var mod in textResults)
                    {
                        if (mod != null && seenIds.Add(mod.Id))
                        {
                            allResults.Add(mod);
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Text search added more results");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Character text search failed: {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine($"[DEBUG] PARTIAL MATCH: Total {allResults.Count} results for {characterName}");
            return SortSearchResultsByRelevance(allResults, searchTerm);
        }
        private static async Task<List<GameBananaSubmission>> SearchTextSimple(string searchTerm, string sort, SearchFilters filters)
        {
            var allResults = new List<GameBananaSubmission>();
            var seenIds = new HashSet<int>();

            try
            {
                var results = await FetchModsAsync(searchTerm, sort, 1, filters);
                if (results != null)
                {
                    foreach (var mod in results)
                    {
                        if (mod != null && seenIds.Add(mod.Id))
                        {
                            allResults.Add(mod);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Text search failed: {ex.Message}");
            }

            try
            {
                var advanced = await GameBananaAdvancedSearch.SearchAndHydrateAsync(searchTerm, 25);
                if (advanced != null)
                {
                    foreach (var mod in advanced)
                    {
                        if (mod != null && seenIds.Add(mod.Id))
                        {
                            allResults.Add(mod);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Advanced search failed: {ex.Message}");
            }

            var filtered = allResults.Where(mod => {
                if (mod == null) return false;
                var combined = $"{mod.Name} {mod.Description} {mod.AuthorName}";
                return combined.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
            }).ToList();

            System.Diagnostics.Debug.WriteLine($"[DEBUG] SIMPLE: Text search found {filtered.Count} results");
            return SortSearchResultsByRelevance(filtered, searchTerm);
        }

        public static List<GameBananaSubmission> SortSearchResultsByRelevance(List<GameBananaSubmission> mods, string searchTerm)
        {
            return mods.OrderByDescending(mod => {
                int score = 0;

                if (string.Equals(mod.Name, searchTerm, StringComparison.OrdinalIgnoreCase))
                    score += 100;

                if (mod.Name != null && mod.Name.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase))
                    score += 50;

                if (ContainsIgnoreCase(mod.Name, searchTerm))
                    score += 25;

                if (ContainsIgnoreCase(mod.AuthorName, searchTerm))
                    score += 15;

                if (ContainsIgnoreCase(mod.Description, searchTerm))
                    score += 10;

                score += Math.Min(mod.LikeCount / 10, 5);
                score += Math.Min(mod.DownloadCount / 100, 5);

                return score;
            }).ToList();
        }

        private static List<GameBananaSubmission> FilterSearchResults(List<GameBananaSubmission> results, string searchTerm)
        {
            if (results == null || results.Count == 0 || string.IsNullOrWhiteSpace(searchTerm))
                return results ?? new List<GameBananaSubmission>();
            var filtered = results.Where(mod => {
                if (mod == null) return false;

                var combined = $"{mod.Name} {mod.Description} {mod.AuthorName}";
                return combined.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
            }).ToList();

            return filtered;
        }

        private static List<GameBananaSubmission> ApplyFilters(List<GameBananaSubmission> mods, SearchFilters filters)
        {
            if (mods == null || mods.Count == 0) return mods ?? new List<GameBananaSubmission>();

            var filtered = mods.ToList();
            if (string.IsNullOrEmpty(filters?.Category))
            {
                filtered = filtered.Where(m =>
                    m == null || m.Category == null || m.Category.Name == null ||
                    (!string.Equals(m.Category.Name, SearchFilters.Categories.GameplayModifications, StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(m.Category.Name, SearchFilters.Categories.ModelReplacement, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            if (!string.IsNullOrEmpty(filters?.Category))
                filtered = filtered.Where(mod => ModMatchesCategory(mod, filters.Category)).ToList();

            return filtered;
        }

        private static bool ModMatchesCategory(GameBananaSubmission mod, string category)
        {
            if (mod == null || string.IsNullOrEmpty(category)) return true;

            var catName = (mod.Category != null) ? mod.Category.Name : null;
            if (string.IsNullOrWhiteSpace(catName)) return false;

            switch (category)
            {
                case "Skins":
                    return string.Equals(catName, SearchFilters.Categories.Skins, StringComparison.OrdinalIgnoreCase)
                           || IsHeroName(catName);

                case "HUD":
                    return string.Equals(catName, SearchFilters.Categories.HUD, StringComparison.OrdinalIgnoreCase);

                case "Other/Misc":
                    return string.Equals(catName, SearchFilters.Categories.OtherMisc, StringComparison.OrdinalIgnoreCase);

                default:
                    return true;
            }
        }

        private static bool IsHeroName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string[] heroes = new[]
            {
                SearchFilters.Characters.Abrams, SearchFilters.Characters.Bebop, SearchFilters.Characters.Dynamo,
                SearchFilters.Characters.GreyTalon, SearchFilters.Characters.Haze, SearchFilters.Characters.Infernus,
                SearchFilters.Characters.Ivy, SearchFilters.Characters.Kelvin, SearchFilters.Characters.LadyGeist,
                SearchFilters.Characters.Lash, SearchFilters.Characters.McGinnis, SearchFilters.Characters.Mirage,
                SearchFilters.Characters.Mo, SearchFilters.Characters.Paradox, SearchFilters.Characters.Pocket,
                SearchFilters.Characters.Seven, SearchFilters.Characters.Shiv, SearchFilters.Characters.Vindicta,
                SearchFilters.Characters.Viscous, SearchFilters.Characters.Warden, SearchFilters.Characters.Wraith,
                SearchFilters.Characters.Yamato
            };
            for (int i = 0; i < heroes.Length; i++)
                if (string.Equals(name, heroes[i], StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static bool ContainsIgnoreCase(string source, string searchTerm)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(searchTerm))
                return false;

            return source.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetSortAlias(string sort)
        {
            switch (sort)
            {
                case "Most Liked": return "Generic_MostLiked";
                case "Most Downloaded": return "Generic_MostDownloaded";
                case "Recently Updated": return "Generic_LatestModified";
                default: return "Generic_Newest";
            }
        }

        public static async Task<List<GameBananaSubmission>> TryRssOrWebFallbackAsync()
        {
            try
            {
                var rss = await http.GetStringAsync($"https://gamebanana.com/rss/mods?gameid={DEADLOCK_GAME_ID}");
                var names = new List<string>();
                int idx = 0;
                while (true)
                {
                    int i = rss.IndexOf("<title>", idx, StringComparison.Ordinal);
                    if (i < 0) break;
                    i += 7;
                    int j = rss.IndexOf("</title>", i, StringComparison.Ordinal);
                    if (j < 0) break;
                    string val = rss.Substring(i, j - i).Trim();
                    names.Add(val);
                    idx = j + 8;
                }

                if (names.Count > 1)
                {
                    names = names.Skip(1).Take(30).ToList();
                    var list = new List<GameBananaSubmission>();
                    for (int k = 0; k < names.Count; k++)
                    {
                        list.Add(new GameBananaSubmission
                        {
                            Id = 1000 + k,
                            Name = names[k],
                            Description = "Open in GameBanana for details.",
                            DateAddedTimestamp = DateTimeOffset.Now.AddDays(-k).ToUnixTimeSeconds(),
                            Submitter = new GameBananaUser { Name = "GameBanana" }
                        });
                    }
                    return list;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] RSS fallback failed: {ex.Message}");
            }
            return new List<GameBananaSubmission>();
        }

        public static List<GameBananaSubmission> BuildSampleList()
        {
            return new List<GameBananaSubmission>
            {
                new GameBananaSubmission
                {
                    Id = 0,
                    Name = "GameBanana Service Temporarily Unavailable",
                    Description = "GameBanana appears to be experiencing connectivity issues. Please try again later or visit gamebanana.com directly.",
                    DateAddedTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                    Submitter = new GameBananaUser { Name = "System Notice" }
                }
            };
        }
    }
}