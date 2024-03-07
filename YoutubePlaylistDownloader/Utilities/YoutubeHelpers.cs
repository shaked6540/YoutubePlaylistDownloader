namespace YoutubePlaylistDownloader.Utilities;

public static partial class YoutubeHelpers
{
    /// <summary>
    /// Verifies that the given string is syntactically a valid YouTube video ID.
    /// </summary>
    public static bool ValidateVideoId(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
            return false;

        // Video IDs are always 11 characters
        if (videoId.Length != 11)
            return false;

        return !VideoRegex().IsMatch(videoId);
    }

    /// <summary>
    /// Tries to parse video ID from a YouTube video URL.
    /// </summary>
    public static bool TryParseVideoId(string videoUrl, out string videoId)
    {
        videoId = default;

        if (string.IsNullOrWhiteSpace(videoUrl))
            return false;

        var result = VideoId.TryParse(videoUrl);

        if (result != null)
        {
            videoId = result.Value;
            return true;
        }


        return false;
    }

    /// <summary>
    /// Parses video ID from a YouTube video URL.
    /// </summary>
    public static string ParseVideoId(string videoUrl) =>
        TryParseVideoId(videoUrl, out var result)
            ? result!
            : throw new FormatException($"Could not parse video ID from given string [{videoUrl}].");

    /// <summary>
    /// Verifies that the given string is syntactically a valid YouTube playlist ID.
    /// </summary>
    public static bool ValidatePlaylistId(string playlistId)
    {
        if (string.IsNullOrWhiteSpace(playlistId))
            return false;

        return PlaylistId.TryParse(playlistId) != null;
    }

    /// <summary>
    /// Tries to parse playlist ID from a YouTube playlist URL.
    /// </summary>
    public static bool TryParsePlaylistId(string playlistUrl, out PlaylistId? playlistId)
    {
        playlistId = default;

        if (string.IsNullOrWhiteSpace(playlistUrl))
            return false;

        // https://www.youtube.com/playlist?list=PLOU2XLYxmsIJGErt5rrCqaSGTMyyqNt2H
        var regularMatch = RegularRegex().Match(playlistUrl).Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(regularMatch) && ValidatePlaylistId(regularMatch))
        {
            playlistId = PlaylistId.Parse(regularMatch);
            return true;
        }

        // https://www.youtube.com/watch?v=b8m9zhNAgKs&list=PL9tY0BWXOZFuFEG_GtOBZ8-8wbkH-NVAr
        var compositeMatch = CompositeRegex().Match(playlistUrl).Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(compositeMatch) && ValidatePlaylistId(compositeMatch))
        {
            playlistId = PlaylistId.Parse(compositeMatch);
            return true;
        }

        // https://youtu.be/b8m9zhNAgKs/?list=PL9tY0BWXOZFuFEG_GtOBZ8-8wbkH-NVAr
        var shortCompositeMatch = ShortLinkRegex().Match(playlistUrl).Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(shortCompositeMatch) && ValidatePlaylistId(shortCompositeMatch))
        {
            playlistId = PlaylistId.Parse(shortCompositeMatch);
            return true;
        }

        // https://www.youtube.com/embed/b8m9zhNAgKs/?list=PL9tY0BWXOZFuFEG_GtOBZ8-8wbkH-NVAr
        var embedCompositeMatch = EmbedRegex().Match(playlistUrl).Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(embedCompositeMatch) && ValidatePlaylistId(embedCompositeMatch))
        {
            playlistId = PlaylistId.Parse(embedCompositeMatch);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses playlist ID from a YouTube playlist URL.
    /// </summary>
    public static string ParsePlaylistId(string playlistUrl) =>
        TryParsePlaylistId(playlistUrl, out var result)
            ? result!
            : throw new FormatException($"Could not parse playlist ID from given string [{playlistUrl}].");

    /// <summary>
    /// Verifies that the given string is syntactically a valid YouTube username.
    /// </summary>
    public static bool ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        // Usernames can't be longer than 20 characters
        if (username.Length > 20)
            return false;

        return !UserNameRegex().IsMatch(username);
    }

    /// <summary>
    /// Tries to parse username from a YouTube user URL.
    /// </summary>
    public static bool TryParseUsername(string userUrl, out string username)
    {
        username = default;

        if (string.IsNullOrWhiteSpace(userUrl))
            return false;

        // https://www.youtube.com/user/TheTyrrr
        var regularMatch = UserRegex().Match(userUrl).Groups[1].Value;
        if (ValidateUsername(regularMatch))
        {
            username = regularMatch;
            return true;
        }

        return false;
    }

    public static bool TryParseHandle(string handleUrl, out string handle)
    {
        handle = default;

        if (string.IsNullOrWhiteSpace(handleUrl))
            return false;

        // https://www.youtube.com/@LesIngenieurs
        var handleFormat = HandleRegex().Match(handleUrl).Groups[1].Value;
        if (ValidateUsername(handleFormat))
        {
            handle = handleFormat;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses username from a YouTube user URL.
    /// </summary>
    public static string ParseUsername(string userUrl) =>
        TryParseUsername(userUrl, out var username)
            ? username!
            : throw new FormatException($"Could not parse username from given string [{userUrl}].");

    /// <summary>
    /// Verifies that the given string is syntactically a valid YouTube channel ID.
    /// </summary>
    public static bool ValidateChannelId(string channelId)
    {
        if (string.IsNullOrWhiteSpace(channelId))
            return false;

        // Channel IDs should start with these characters
        if (!channelId.StartsWith("UC", StringComparison.Ordinal))
            return false;

        // Channel IDs are always 24 characters
        if (channelId.Length != 24)
            return false;

        return !VideoRegex().IsMatch(channelId);
    }

    /// <summary>
    /// Tries to parse channel ID from a YouTube channel URL.
    /// </summary>
    public static bool TryParseChannelId(string channelUrl, out string channelId)
    {
        channelId = default;

        if (string.IsNullOrWhiteSpace(channelUrl))
            return false;

        // https://www.youtube.com/channel/UC3xnGqlcL3y-GXz5N3wiTJQ
        var regularMatch = ChannelRegex().Match(channelUrl).Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(regularMatch) && ValidateChannelId(regularMatch))
        {
            channelId = regularMatch;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses channel ID from a YouTube channel URL.
    /// </summary>
    public static string ParseChannelId(string channelUrl) =>
        TryParseChannelId(channelUrl, out var result)
            ? result!
            : throw new FormatException($"Could not parse channel ID from given string [{channelUrl}].");

    public static VideoQuality FromLabel(string label, int framerateFallback)
    {
        // Video quality labels can have the following formats:
        // - 1080p (regular stream, regular fps)
        // - 1080p60 (regular stream, high fps)
        // - 1080s (360° stream, regular fps)
        // - 1080s60 (360° stream, high fps)

        var match = LabelRegex().Match(label);

        var maxHeight = int.Parse(match.Groups[1].Value);
        int? framerate = null;
        try
        {
            framerate = int.Parse(match.Groups[2].Value);
        }
        catch { }

        return new VideoQuality(
            label,
            maxHeight,
            framerate ?? framerateFallback
        );
    }

    public static VideoQuality Low144 = FromLabel("144p", 30);
    public static VideoQuality Low240 = FromLabel("240p", 30);
    public static VideoQuality Medium360 = FromLabel("360p", 30);
    public static VideoQuality Medium480 = FromLabel("480p", 30);
    public static VideoQuality High720 = FromLabel("720p", 30);
    public static VideoQuality High1080 = FromLabel("1080p", 30);
    public static VideoQuality High1440 = FromLabel("1440p", 30);
    public static VideoQuality High2160 = FromLabel("2160p", 30);
    public static VideoQuality High2880 = FromLabel("2880p", 30);
    public static VideoQuality High3072 = FromLabel("3072p", 30);
    public static VideoQuality High4320 = FromLabel("4320p", 30);

    [GeneratedRegex(@"[^0-9a-zA-Z_\-]")]
    private static partial Regex VideoRegex();

    [GeneratedRegex(@"youtube\..+?/channel/(.*?)(?:\?|&|/|$)")]
    private static partial Regex ChannelRegex();

    [GeneratedRegex(@"^(\d+)\w+(\d+)?$")]
    private static partial Regex LabelRegex();

    [GeneratedRegex(@"youtube\..+?/playlist.*?list=(.*?)(?:&|/|$)")]
    private static partial Regex RegularRegex();

    [GeneratedRegex(@"youtube\..+?/watch.*?list=(.*?)(?:&|/|$)")]
    private static partial Regex CompositeRegex();

    [GeneratedRegex(@"youtu\.be/.*?/.*?list=(.*?)(?:&|/|$)")]
    private static partial Regex ShortLinkRegex();

    [GeneratedRegex(@"youtube\..+?/embed/.*?/.*?list=(.*?)(?:&|/|$)")]
    private static partial Regex EmbedRegex();

    [GeneratedRegex(@"youtube\..+?/user/(.*?)(?:\?|&|/|$)")]
    private static partial Regex UserRegex();

    [GeneratedRegex(@"youtube\..+?/@(.+)")]
    private static partial Regex HandleRegex();

    [GeneratedRegex(@"[^0-9a-zA-Z_]")]
    private static partial Regex UserNameRegex();
}