using Newtonsoft.Json;
using YoutubeExplode.Models.MediaStreams;

namespace YoutubePlaylistDownloader.Objects
{
    [JsonObject]
    public class DownloadSettings
    {
        [JsonProperty]
        public string SavePath { get; set; }

        [JsonProperty]
        public string SaveFormat { get; set; }

        [JsonProperty]
        public bool AudioOnly { get; set; }

        [JsonProperty]
        public VideoQuality Quality { get; set; }

        [JsonProperty]
        public bool PreferHighestFPS { get; set; }

        [JsonProperty]
        public bool PreferQuality { get; set; }

        [JsonProperty]
        public bool Convert { get; set; }

        [JsonProperty]
        public bool SetBitrate { get; set; }

        [JsonProperty]
        public string Bitrate { get; set; }

        [JsonProperty]
        public bool DownloadCaptions { get; set; }

        [JsonProperty]
        public string CaptionsLanguage { get; set; }

        [JsonProperty]
        public bool SavePlaylistsInDifferentDirectories { get; set; }

        [JsonProperty]
        public bool Subset { get; set; }

        [JsonProperty]
        public int SubsetStartIndex { get; set; }

        [JsonProperty]
        public int SubsetEndIndex { get; set; }

        [JsonProperty]
        public bool OpenDestinationFolderWhenDone { get; set; }


        [JsonConstructor]
        public DownloadSettings(string saveForamt, bool audioOnly, VideoQuality quality, bool preferHighestFPS,
            bool preferQuality, bool convert, bool setBitrate, string bitrate, bool downloadCaptions, string captionsLanguage,
            bool savePlaylistsInDifferentDirectories, bool subset, int subsetStartIndex, int subsetEndIndex, bool openDestinationFolderWhenDone)
        {
            SaveFormat = saveForamt;
            AudioOnly = audioOnly;
            Quality = quality;
            PreferHighestFPS = preferHighestFPS;
            PreferQuality = preferQuality;
            Convert = convert;
            Bitrate = bitrate;
            SetBitrate = setBitrate;
            DownloadCaptions = downloadCaptions;
            CaptionsLanguage = captionsLanguage;
            SavePlaylistsInDifferentDirectories = savePlaylistsInDifferentDirectories;
            Subset = subset;
            SubsetStartIndex = subsetStartIndex;
            SubsetEndIndex = subsetEndIndex;
            OpenDestinationFolderWhenDone = openDestinationFolderWhenDone;
        }

        public DownloadSettings(DownloadSettings settings)
        {
            SaveFormat = settings.SaveFormat;
            AudioOnly = settings.AudioOnly;
            Quality = settings.Quality;
            PreferHighestFPS = settings.PreferHighestFPS;
            PreferQuality = settings.PreferQuality;
            Convert = settings.Convert;
            Bitrate = settings.Bitrate;
            SetBitrate = settings.SetBitrate;
            DownloadCaptions = settings.DownloadCaptions;
            CaptionsLanguage = settings.CaptionsLanguage;
            SavePlaylistsInDifferentDirectories = settings.SavePlaylistsInDifferentDirectories;
            Subset = settings.Subset;
            SubsetStartIndex = settings.SubsetStartIndex;
            SubsetEndIndex = settings.SubsetEndIndex;
            OpenDestinationFolderWhenDone = settings.OpenDestinationFolderWhenDone;
        }

        public DownloadSettings Clone() => new DownloadSettings(this);
        
    }
}
