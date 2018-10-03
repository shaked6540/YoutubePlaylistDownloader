using Newtonsoft.Json;
using YoutubeExplode.Models.MediaStreams;

namespace YoutubePlaylistDownloader.Objects
{
    class DownloadSettings
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

        [JsonConstructor]
        public DownloadSettings(string saveForamt, bool audioOnly, VideoQuality quality, bool preferHighestFPS,
            bool preferQuality, bool convert, bool setBitrate, string bitrate)
        {
            SaveFormat = saveForamt;
            AudioOnly = audioOnly;
            Quality = quality;
            PreferHighestFPS = preferHighestFPS;
            PreferQuality = preferQuality;
            Convert = convert;
            Bitrate = bitrate;
            SetBitrate = setBitrate;
        }
    }
}
