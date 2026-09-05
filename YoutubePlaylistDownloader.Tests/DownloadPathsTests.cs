using YoutubePlaylistDownloader.Utilities;
using Xunit;

namespace YoutubePlaylistDownloader.Tests;

public class DownloadPathsTests
{
    [Fact]
    public void Create_uses_part_files_for_all_intermediates()
    {
        var paths = DownloadPaths.Create("C:\\Temp\\", "video-id", "mp4", "m4a")
            .WithDestination("C:\\Downloads\\video.mp4");

        Assert.Equal("C:\\Temp\\video-id.part", paths.Input);
        Assert.Equal("C:\\Temp\\video-id.part.mp4", paths.Output);
        Assert.Equal("C:\\Temp\\video-id-audio.m4a.part", paths.Audio);
        Assert.Equal("C:\\Temp\\video-id.srt.part", paths.Captions);
        Assert.Equal("C:\\Downloads\\video.mp4", paths.Destination);
    }
}
