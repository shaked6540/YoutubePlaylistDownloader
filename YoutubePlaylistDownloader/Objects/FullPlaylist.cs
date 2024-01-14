namespace YoutubePlaylistDownloader.Objects;

public class FullPlaylist(Playlist basePlaylist, IEnumerable<PlaylistVideo> videos, string title = null)
{
    public Playlist BasePlaylist { get; private set; } = basePlaylist;
    public IEnumerable<PlaylistVideo> Videos { get; private set; } = videos;

    public string Title { get; private set; } = basePlaylist?.Title ?? title;
}
