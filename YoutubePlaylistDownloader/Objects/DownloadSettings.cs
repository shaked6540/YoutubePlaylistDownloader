using Newtonsoft.Json;
using System.ComponentModel;
using YoutubeExplode.Videos.Streams;

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

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(true)]
        public bool TagAudioFile { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        public bool FilterVideosByLength { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        // true = longer than, false = shorter than
        public bool FilterMode { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(4.0)]
        public double FilterByLengthValue { get; set; }


        [JsonConstructor]
        public DownloadSettings(string saveForamt, bool audioOnly, VideoQuality quality, bool preferHighestFPS,
            bool preferQuality, bool convert, bool setBitrate, string bitrate, bool downloadCaptions, string captionsLanguage,
            bool savePlaylistsInDifferentDirectories, bool subset, int subsetStartIndex, int subsetEndIndex, bool openDestinationFolderWhenDone,
            bool tagAudioFile, bool filterVideosByLength, bool filterMode, double filterByLengthValue)
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
            TagAudioFile = tagAudioFile;
            FilterVideosByLength = filterVideosByLength;
            FilterMode = filterMode;
            FilterByLengthValue = filterByLengthValue;
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
            TagAudioFile = settings.TagAudioFile;
            FilterVideosByLength = settings.FilterVideosByLength;
            FilterMode = settings.FilterMode;
            FilterByLengthValue = settings.FilterByLengthValue;
        }

        public DownloadSettings Clone() => new DownloadSettings(this);
        
    }
}
