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

        [JsonConstructor]
        public DownloadSettings(string saveForamt, bool audioOnly, VideoQuality quality, bool preferHighestFPS,
            bool preferQuality, bool convert, bool setBitrate, string bitrate, bool downloadCaptions, string captionsLanguage)
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
        }

        public DownloadSettings Clone() => new DownloadSettings(this);
        
    }
}
