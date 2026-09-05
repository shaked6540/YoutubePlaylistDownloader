namespace YoutubePlaylistDownloader.Utilities;

public sealed record DownloadPaths(string Input, string Output, string Destination, string Audio, string Captions)
{
    public static DownloadPaths Create(string tempFolder, string baseName, string extension, string audioExtension = null)
    {
        var input = $"{tempFolder}{baseName}.part";
        var output = $"{tempFolder}{baseName}.part.{extension}";
        var audio = audioExtension == null ? string.Empty : $"{tempFolder}{baseName}-audio.{audioExtension}.part";
        var captions = $"{tempFolder}{baseName}.srt.part";
        return new(input, output, string.Empty, audio, captions);
    }

    public DownloadPaths WithDestination(string destination) => this with { Destination = destination };
}
