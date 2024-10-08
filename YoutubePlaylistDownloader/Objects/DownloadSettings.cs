namespace YoutubePlaylistDownloader.Objects;

[JsonObject]
public class DownloadSettings
{
    [JsonProperty]
    public string SavePath { get; set; }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    [DefaultValue("mkv")]
    public string VideoSaveFormat { get; set; }

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

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    [DefaultValue("$title")]
    public string FilenamePattern { get; set; } = "$title";

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    [DefaultValue(false)]
    public bool SkipExisting { get; set; }


    [JsonConstructor]
    public DownloadSettings(string saveFormat, bool audioOnly, VideoQuality quality, bool preferHighestFPS,
    bool preferQuality, bool convert, bool setBitrate, string bitrate, bool downloadCaptions, string captionsLanguage,
    bool savePlaylistsInDifferentDirectories, bool subset, int subsetStartIndex, int subsetEndIndex, bool openDestinationFolderWhenDone,
    bool tagAudioFile, bool filterVideosByLength, bool filterMode, double filterByLengthValue, string filenamePattern, bool skipExisting,
    string videoSaveFormat)
    {
        SaveFormat = saveFormat;
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
        FilenamePattern = filenamePattern;
        SkipExisting = skipExisting;
        VideoSaveFormat = videoSaveFormat;
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
        FilenamePattern = settings.FilenamePattern;
        SkipExisting = settings.SkipExisting;
        VideoSaveFormat = settings.VideoSaveFormat;
    }

    public string GetFilenameByPattern(IVideo video, int index, string file, FullPlaylist playlist = null)
    {
        if (video == null)
        {
            return file;
        }

        var title = video.Title.Replace("—", "-");
        var genre = title.Split('[', ']').ElementAtOrDefault(1);
        var artist = string.Empty;
        var songTitle = string.Empty;

        if (genre == null)
        {
            genre = string.Empty;
        }
        else if (genre.Length >= title.Length)
        {
            genre = string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            title = title.Replace($"[{genre}]", string.Empty);
            var stringToRemove = title.Split('[', ']', '【', '】').ElementAtOrDefault(1);

            if (!string.IsNullOrWhiteSpace(stringToRemove))
            {
                title = title.Replace($"[{stringToRemove}]", string.Empty);
            }
        }

        title = title.TrimStart(' ', '-', '[', ']').TrimEnd();

        if (GlobalConsts.IgnoredGeneres.Any(genre.ToLower().Contains))
        {
            genre = string.Empty;
        }

        if (GlobalConsts.TryGetSongTitleAndPerformersFromTitle(title, out songTitle, out string[] songPerformers))
        {
            artist = string.Join(", ", songPerformers);
        }

        var result = FilenamePattern
            .Replace("$title", title)
            .Replace("$index", (index + 1).ToString())
            .Replace("$artist", artist)
            .Replace("$songtitle", songTitle)
            .Replace("$channel", video.Author.ChannelTitle)
            .Replace("$videoid", video.Id)
            .Replace("$playlist", playlist?.Title)
            .Replace("$genre", genre);

        return string.IsNullOrWhiteSpace(result) ? title : result;
    }

    public DownloadSettings Clone() => new(this);
}
