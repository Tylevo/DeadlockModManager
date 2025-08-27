using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Deadlock_Mod_Loader2
{
    /// <summary>
    /// Advanced text search for partial terms. Uses Util/Search/Results to get Mod IDs,
    /// then hydrates each Mod into full cards via /apiv11/Mod/{id}.
    /// </summary>
    internal static class GameBananaAdvancedSearch
    {
        private const string ROOT = "https://gamebanana.com/apiv11";
        private const int DEADLOCK_GAME_ID = 20948;

        private static readonly string CsvProperties =
            "_sName,_sText,_tsDateAdded,_tsDateModified," +
            "_nViewCount,_nLikeCount,_nDownloadCount," +
            "_aSubmitter[_sName]," +
            "_aPreviewMedia[_aImages[_sBaseUrl,_sFile530,_sFile220]]";

        private static HttpClient CreateHttp()
        {
            var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(30);
            try
            {
                http.DefaultRequestHeaders.UserAgent.Clear();
                http.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                    "(KHTML, like Gecko) Chrome/126 Safari/537.36 Deadlock-Mod-Manager/1.4");
                http.DefaultRequestHeaders.Accept.Clear();
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            catch { }
            return http;
        }

        public static async Task<List<GameBananaSubmission>> SearchAndHydrateAsync(string term, int max = 40)
        {
            var results = new List<GameBananaSubmission>();
            if (string.IsNullOrWhiteSpace(term)) return results;
            if (max < 1) max = 1;

            try
            {
                using (var http = CreateHttp())
                {
                    var url = ROOT + "/Util/Search/Results"
                            + "?_sSearchString=" + Uri.EscapeDataString(term)
                            + "&_sModelName=Mod"
                            + "&_idGameRow=" + DEADLOCK_GAME_ID
                            + "&_nPerpage=" + Math.Min(max, 50)
                            + "&_nPage=1";

                    var resp = await http.GetAsync(url).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine("[DEBUG] AdvancedSearch index failed: " + (int)resp.StatusCode + " " + resp.ReasonPhrase);
                        return results;
                    }

                    var contentType = resp.Content.Headers.ContentType != null ? resp.Content.Headers.ContentType.MediaType : "";
                    var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if ((contentType == null || contentType.IndexOf("json", StringComparison.OrdinalIgnoreCase) < 0) &&
                        !json.TrimStart().StartsWith("{"))
                    {
                        System.Diagnostics.Debug.WriteLine("[DEBUG] AdvancedSearch index returned non-JSON content.");
                        return results;
                    }

                    var root = JObject.Parse(json);
                    var recs = root["_aRecords"] as JArray;
                    if (recs == null || recs.Count == 0) return results;

                    var ids = new List<int>();
                    foreach (var r in recs)
                    {
                        var model = (string)(r["_sModelName"] ?? r["_sModelSlug"] ?? r["model"]);
                        if (model != null && model.Equals("Mod", StringComparison.OrdinalIgnoreCase))
                        {
                            var idToken = r["_idRow"] ?? r["_idSubmissionRow"] ?? r["id"];
                            int idVal;
                            if (idToken != null && int.TryParse(idToken.ToString(), out idVal))
                            {
                                ids.Add(idVal);
                            }
                        }
                    }

                    foreach (var id in ids)
                    {
                        if (results.Count >= max) break;
                        try
                        {
                            var modUrl = ROOT + "/Mod/" + id
                                       + "?_csvProperties=" + Uri.EscapeDataString(CsvProperties);
                            var modResp = await http.GetAsync(modUrl).ConfigureAwait(false);
                            if (!modResp.IsSuccessStatusCode)
                            {
                                System.Diagnostics.Debug.WriteLine("[DEBUG] AdvancedSearch hydrate HTTP " + (int)modResp.StatusCode + " for id " + id);
                                continue;
                            }

                            var modJson = await modResp.Content.ReadAsStringAsync().ConfigureAwait(false);

                            if (modJson.TrimStart().StartsWith("["))
                            {
                                var arr = JArray.Parse(modJson);
                                if (arr.Count > 0)
                                {
                                    var objStr = arr[0].ToString(Formatting.None);
                                    var mod0 = JsonConvert.DeserializeObject<GameBananaSubmission>(objStr);
                                    if (mod0 != null) results.Add(mod0);
                                }
                                continue;
                            }

                            var mod = JsonConvert.DeserializeObject<GameBananaSubmission>(modJson);
                            if (mod != null) results.Add(mod);
                        }
                        catch (Exception exItem)
                        {
                            System.Diagnostics.Debug.WriteLine("[DEBUG] AdvancedSearch hydrate failed for id " + id + ": " + exItem.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] AdvancedSearch failed: " + ex.Message);
            }

            return results;
        }
    }
}
