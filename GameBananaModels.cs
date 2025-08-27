using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Deadlock_Mod_Loader2
{
    public class GameBananaPaginatedResponse<T>
    {
        [JsonProperty("_aRecords")]
        public List<T> Records { get; set; }

        [JsonProperty("_nRecordCount")]
        public int RecordCount { get; set; }

        [JsonProperty("_nPerpage")]
        public int PerPage { get; set; }

        [JsonProperty("_nPage")]
        public int Page { get; set; }
    }

    public class GameBananaSubmission
    {
        [JsonProperty("_idRow")]
        public int Id { get; set; }

        [JsonProperty("_sName")]
        public string Name { get; set; }

        [JsonProperty("_sText")]
        public string Description { get; set; }

        [JsonProperty("_tsDateAdded")]
        public long DateAddedTimestamp { get; set; }

        [JsonProperty("_tsDateModified")]
        public long DateModifiedTimestamp { get; set; }

        [JsonProperty("_nViewCount")]
        public int ViewCount { get; set; }

        [JsonProperty("_nLikeCount")]
        public int LikeCount { get; set; }

        [JsonProperty("_nDownloadCount")]
        public int DownloadCount { get; set; }

        [JsonProperty("_aSubmitter")]
        public GameBananaUser Submitter { get; set; }

        [JsonProperty("_aPreviewMedia")]
        public GameBananaPreviewMedia PreviewMedia { get; set; }

        [JsonProperty("_aCategory")]
        public GameBananaCategory Category { get; set; }

        [JsonProperty("_aFiles")]
        public List<GameBananaFile> Files { get; set; }

        public DateTime DateAddedDateTime => DateTimeOffset.FromUnixTimeSeconds(DateAddedTimestamp).DateTime;
        public DateTime DateModifiedDateTime => DateTimeOffset.FromUnixTimeSeconds(DateModifiedTimestamp).DateTime;

        public string AuthorName => Submitter?.Name ?? "Unknown";

        public string PreviewImageUrl => PreviewMedia?.Images?.FirstOrDefault()?.Base ?? "";

        public GameBananaFile DownloadFile => Files?.Find(f => f.FileName.EndsWith(".zip") ||
                                                               f.FileName.EndsWith(".rar") ||
                                                               f.FileName.EndsWith(".7z") ||
                                                               f.FileName.EndsWith(".vpk"));
    }

    public class GameBananaUser
    {
        [JsonProperty("_sName")]
        public string Name { get; set; }

        [JsonProperty("_idRow")]
        public int Id { get; set; }
    }

    public class GameBananaPreviewMedia
    {
        [JsonProperty("_aImages")]
        public List<GameBananaImage> Images { get; set; }
    }

    public class GameBananaImage
    {
        [JsonProperty("_sFile")]
        public string FileName { get; set; }

        [JsonProperty("_sBaseUrl")]
        public string BaseUrl { get; set; }

        [JsonProperty("_sFile220")]
        public string File220 { get; set; }

        [JsonProperty("_sFile530")]
        public string File530 { get; set; }

        [JsonProperty("_sFile100")]
        public string File100 { get; set; }

        public string Base => !string.IsNullOrEmpty(BaseUrl) && !string.IsNullOrEmpty(File530)
                            ? BaseUrl + "/" + File530 : "";
    }

    public class GameBananaFile
    {
        [JsonProperty("_idRow")]
        public int Id { get; set; }

        [JsonProperty("_sFile")]
        public string FileName { get; set; }

        [JsonProperty("_nFilesize")]
        public long FileSize { get; set; }

        [JsonProperty("_sDescription")]
        public string Description { get; set; }

        [JsonProperty("_sDownloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonProperty("_nDownloadCount")]
        public int DownloadCount { get; set; }

        public string FileSizeFormatted
        {
            get
            {
                if (FileSize <= 0) return "N/A";
                string[] sizes = { "B", "KB", "MB", "GB" };
                double len = FileSize;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
        }
    }
    public class GameBananaCoreApiResponse
    {
        [JsonProperty("_aFiles")]
        public List<GameBananaCoreFile> _aFiles { get; set; }
    }

    public class GameBananaCoreFile
    {
        [JsonProperty("_idRow")]
        public int _idRow { get; set; }

        [JsonProperty("_sFile")]
        public string _sFile { get; set; }

        [JsonProperty("_nFilesize")]
        public long _nFilesize { get; set; }

        [JsonProperty("_sDescription")]
        public string _sDescription { get; set; }

        [JsonProperty("_sDownloadUrl")]
        public string _sDownloadUrl { get; set; }

        [JsonProperty("_nDownloadCount")]
        public int _nDownloadCount { get; set; }
    }

    public class GameBananaDetailedMod : GameBananaSubmission
    {
        [JsonProperty("_aCategory")]
        public new GameBananaCategory Category { get; set; }

        [JsonProperty("_aTags")]
        public List<string> Tags { get; set; }

        [JsonProperty("_aAlternateFileSources")]
        public List<GameBananaAlternateFileSource> AlternateFileSources { get; set; }
    }

    public class GameBananaCategory
    {
        [JsonProperty("_sName")]
        public string Name { get; set; }

        [JsonProperty("_idRow")]
        public int Id { get; set; }
    }

    public class GameBananaAlternateFileSource
    {
        [JsonProperty("_sName")]
        public string Name { get; set; }

        [JsonProperty("_sUrl")]
        public string Url { get; set; }
    }
}