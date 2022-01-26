using System.Collections.Generic;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace YoutubePlaylistDownloader.Objects
{
    public class FullPlaylist
    {
        public Playlist BasePlaylist { get; private set; }
        public IEnumerable<PlaylistVideo> Videos { get; private set; }

        public string Title { get; private set; }

        public FullPlaylist(Playlist basePlaylist, IEnumerable<PlaylistVideo> videos, string title = null)
        {
            BasePlaylist = basePlaylist;
            Videos = videos;
            Title = basePlaylist?.Title ?? title;
        }
    }
}
