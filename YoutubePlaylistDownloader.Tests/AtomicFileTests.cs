using YoutubePlaylistDownloader.Utilities;
using Xunit;

namespace YoutubePlaylistDownloader.Tests;

public class AtomicFileTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "ypd-tests-" + Guid.NewGuid());

    public AtomicFileTests() => Directory.CreateDirectory(directory);

    [Fact]
    public void CopyAndReplace_writes_complete_destination_and_removes_partial_file()
    {
        var source = Path.Combine(directory, "source.bin");
        var destination = Path.Combine(directory, "destination.bin");
        File.WriteAllText(source, "complete content");

        AtomicFile.CopyAndReplace(source, destination);

        Assert.Equal("complete content", File.ReadAllText(destination));
        Assert.False(File.Exists(destination + ".part"));
    }

    [Fact]
    public void CopyAndReplace_preserves_existing_destination_when_source_is_missing()
    {
        var source = Path.Combine(directory, "missing.bin");
        var destination = Path.Combine(directory, "destination.bin");
        File.WriteAllText(destination, "existing content");

        Assert.Throws<FileNotFoundException>(() => AtomicFile.CopyAndReplace(source, destination));

        Assert.Equal("existing content", File.ReadAllText(destination));
        Assert.False(File.Exists(destination + ".part"));
    }

    [Fact]
    public void WriteAllText_replaces_destination_without_leaving_partial_file()
    {
        var destination = Path.Combine(directory, "settings.json");
        File.WriteAllText(destination, "old");

        AtomicFile.WriteAllText(destination, "new");

        Assert.Equal("new", File.ReadAllText(destination));
        Assert.False(File.Exists(destination + ".part"));
    }

    public void Dispose() => Directory.Delete(directory, true);
}
