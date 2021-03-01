using System.Collections.Generic;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace YoutubePlaylistDownloader.Objects
{
    public class FullPlaylist
    {
        public Playlist BasePlaylist { get; private set; }
        public IEnumerable<PlaylistVideo> Videos { get; private set; }

        public FullPlaylist(Playlist basePlaylist, IEnumerable<PlaylistVideo> videos)
        {
            BasePlaylist = basePlaylist;
            Videos = videos;
        }
    }
}
