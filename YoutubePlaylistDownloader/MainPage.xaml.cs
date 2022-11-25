using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Channels;
using YoutubePlaylistDownloader.Utilities;
using YoutubePlaylistDownloader.Objects;
using System.IO;
using YoutubeExplode.Common;

namespace YoutubePlaylistDownloader
{
    public partial class MainPage : UserControl
    {
        private YoutubeClient client;
        private FullPlaylist list = null;
        private IEnumerable<PlaylistVideo> VideoList;
        private Channel channel = null;
        private readonly Dictionary<string, VideoQuality> Resolutions = new Dictionary<string, VideoQuality>()
        {
            { "144p", YoutubeHelpers.Low144 },
            { "240p", YoutubeHelpers.Low240 },
            { "360p", YoutubeHelpers.Medium360 },
            { "480p", YoutubeHelpers.Medium480 },
            { "720p", YoutubeHelpers.High720 },
            { "1080p", YoutubeHelpers.High1080 },
            { "1440p", YoutubeHelpers.High1440 },
            { "2160p", YoutubeHelpers.High2160 },
            { "2880p", YoutubeHelpers.High2880 },
            { "3072p", YoutubeHelpers.High3072 },
            { "4320p", YoutubeHelpers.High4320 }
        };

        private readonly string[] FileTypes = { "mp3", "aac", "opus", "wav", "flac", "m4a", "ogg", "webm" };

        public MainPage()
        {
            InitializeComponent();
            DataObject.AddPastingHandler(BulkLinksTextBox, BulkLinksTextBox_OnPaste);
            GlobalConsts.HideHomeButton();
            GlobalConsts.ShowSettingsButton();
            GlobalConsts.ShowAboutButton();
            GlobalConsts.ShowHelpButton();
            GlobalConsts.ShowSubscriptionsButton();

            VideoList = new List<PlaylistVideo>();
            client = GlobalConsts.YoutubeClient;

            void UpdateSize(object s, SizeChangedEventArgs e)
            {
                double height = GlobalConsts.GetOffset() - (HeadlineStackPanel.ActualHeight + 75);
                GridScrollViewer.Height = height;
                QueueScrollViewer.Height = height;
                BulkScrollViewer.Height = height - 110;
                GridScrollViewer.UpdateLayout();
                QueueScrollViewer.UpdateLayout();
                BulkScrollViewer.UpdateLayout();
            }

            GlobalConsts.Current.SizeChanged += UpdateSize;
            Unloaded += (s, e) => GlobalConsts.Current.SizeChanged -= UpdateSize;

            GlobalConsts.MainPage = this;
        }

        public MainPage Load()
        {
            GlobalConsts.HideHomeButton();
            GlobalConsts.ShowSettingsButton();
            GlobalConsts.ShowAboutButton();
            GlobalConsts.ShowHelpButton();
            GlobalConsts.ShowSubscriptionsButton();
            return this;
        }

        private async void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (YoutubeHelpers.TryParsePlaylistId(PlaylistLinkTextBox.Text, out string playlistId))
                {
                    _ = Task.Run(async () =>
                    {
                        Playlist basePlaylist = await client.Playlists.GetAsync(playlistId).ConfigureAwait(false);
                        list = new FullPlaylist(basePlaylist, await client.Playlists.GetVideosAsync(basePlaylist.Id).CollectAsync().ConfigureAwait(false));
                        VideoList = new List<PlaylistVideo>();
                        await UpdatePlaylistInfo(Visibility.Visible, list.BasePlaylist.Title, list.BasePlaylist.Author.Title, "" ,list.Videos.Count().ToString(), $"https://img.youtube.com/vi/{list?.Videos?.FirstOrDefault()?.Id}/0.jpg", true, true);
                    }).ConfigureAwait(false);
                }
                else if (YoutubeHelpers.TryParseChannelId(PlaylistLinkTextBox.Text, out string channelId))
                {
                    _ = Task.Run(async () =>
                    {
                        channel = await client.Channels.GetAsync(channelId).ConfigureAwait(false);
                        list = new FullPlaylist(null, null, channel.Title);
                        VideoList = await client.Channels.GetUploadsAsync(channel.Id).CollectAsync().ConfigureAwait(false);
                        await UpdatePlaylistInfo(Visibility.Visible, channel.Title, totalVideos: VideoList.Count().ToString(), imageUrl: channel.Thumbnails.FirstOrDefault()?.Url, downloadEnabled: true, showIndexes: true);
                    }).ConfigureAwait(false);
                }
                else if (YoutubeHelpers.TryParseUsername(PlaylistLinkTextBox.Text, out string username))
                {
                    _ = Task.Run(async () =>
                    {
                        var channel = await client.Channels.GetByUserAsync(username).ConfigureAwait(false);
                        list = new FullPlaylist(null, null, channel.Title);
                        VideoList = await client.Channels.GetUploadsAsync(channel.Id).CollectAsync().ConfigureAwait(false);
                        await UpdatePlaylistInfo(Visibility.Visible, channel.Title,totalVideos: VideoList.Count().ToString(), imageUrl: channel.Thumbnails.FirstOrDefault()?.Url, downloadEnabled: true, showIndexes: true);
                    }).ConfigureAwait(false);
                }
                else if (YoutubeHelpers.TryParseVideoId(PlaylistLinkTextBox.Text, out string videoId))
                {
                    _ = Task.Run(async () =>
                    {
                        var video = await client.Videos.GetAsync(videoId);
                        VideoList = new List<PlaylistVideo> { new PlaylistVideo(video.Id, video.Title, video.Author, video.Duration, video.Thumbnails) };
                        list = new FullPlaylist(null, null);
                        await UpdatePlaylistInfo(Visibility.Visible, video.Title, video.Author.Title, video.Engagement.ViewCount.ToString(), string.Empty, $"https://img.youtube.com/vi/{video.Id}/0.jpg", true, false);

                    }).ConfigureAwait(false);
                }
                else
                {
                    await UpdatePlaylistInfo().ConfigureAwait(false);
                }
            }

            catch (Exception ex)
            {
                await GlobalConsts.Log(ex.ToString(), "MainPage TextBox_TextChanged");
                await GlobalConsts.ShowMessage((string)FindResource("Error"), ex.Message);
            }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (list != null || VideoList.Any())
            {
                if (!CanDownload())
                {
                    GlobalConsts.ShowMessage((string)FindResource("Error"), $"{string.Format((string)FindResource("FileDoesNotExist"), GlobalConsts.FFmpegFilePath)}").ConfigureAwait(false);
                    return;
                }

                GlobalConsts.LoadPage(new DownloadPage(list, GlobalConsts.DownloadSettings.Clone(), videos: VideoList));
                VideoList= new List<PlaylistVideo>();
                PlaylistLinkTextBox.Text = string.Empty;
            }
        }

        private async Task UpdatePlaylistInfo(Visibility vis = Visibility.Collapsed, string title = "", string author = "", string views = "", string totalVideos = "", string imageUrl = "", bool downloadEnabled = false, bool showIndexes = false)
            => await Dispatcher.InvokeAsync(() =>
            {
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    PlaylistInfoImage.Source = new BitmapImage(new Uri(imageUrl));
                    PlaylistInfoImage.Visibility = Visibility.Visible;
                }
                else
                    PlaylistInfoImage.Visibility = Visibility.Collapsed;

                PlaylistInfoGrid.Visibility = vis;
                PlaylistTitleTextBlock.Text = title;
                PlaylistAuthorTextBlock.Text = author;
                PlaylistViewsTextBlock.Text = views;

                if (!string.IsNullOrWhiteSpace(totalVideos))
                {
                    PlaylistTotalVideosTextBlockText.Visibility = Visibility.Visible;
                    PlaylistTotalVideosTextBlock.Visibility = Visibility.Visible;
                    PlaylistTotalVideosTextBlock.Text = totalVideos;
                }
                else
                {
                    PlaylistTotalVideosTextBlockText.Visibility = Visibility.Collapsed;
                    PlaylistTotalVideosTextBlock.Visibility = Visibility.Collapsed;
                }

                DownloadButton.IsEnabled = downloadEnabled;
                DownloadInBackgroundButton.IsEnabled = downloadEnabled;

            });

        private void DownloadInBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            if (list != null || VideoList.Any())
            {
                if (!CanDownload())
                {
                    GlobalConsts.ShowMessage((string)FindResource("Error"), $"{string.Format((string)FindResource("FileDoesNotExist"), GlobalConsts.FFmpegFilePath)}").ConfigureAwait(false);
                    return;
                }

                new DownloadPage(list, GlobalConsts.DownloadSettings.Clone(), silent: true, videos: VideoList);
                VideoList= new List<PlaylistVideo>();
                PlaylistLinkTextBox.Text = string.Empty;
            }
        }

        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            GlobalConsts.LoadFlyoutPage(new DownloadSettingsControl());
        }

        private void BulkDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var links = BulkLinksTextBox.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (!CanDownload())
            {
                GlobalConsts.ShowMessage((string)FindResource("Error"), $"{string.Format((string)FindResource("FileDoesNotExist"), GlobalConsts.FFmpegFilePath)}").ConfigureAwait(false);
                return;
            }

            _ = DownloadPage.SequenceDownload(links, GlobalConsts.DownloadSettings.Clone(), silent: true);
            BulkLinksTextBox.Text = string.Empty;
            MetroAnimatedTabControl.SelectedItem = QueueMetroTabItem;
        }

        public void ChangeToQueueTab()
        {
            MetroAnimatedTabControl.SelectedItem = QueueMetroTabItem;
        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            BulkDownloadButton.IsEnabled = !string.IsNullOrWhiteSpace(BulkLinksTextBox.Text);
        }

        private bool CanDownload()
        {
            return GlobalConsts.DownloadSettings.AudioOnly || File.Exists(GlobalConsts.FFmpegFilePath);
        }

        private void BulkLinksTextBox_PreviewDrop(object sender, DragEventArgs e)
        {
            var data = e.Data.GetData(DataFormats.Text, true);
            if (data != null)
            {
                string dataAsString = (string)data;
                dataAsString += Environment.NewLine;
                BulkLinksTextBox.Text += dataAsString;
                BulkLinksTextBox.SelectionStart = BulkLinksTextBox.Text.Length;
                e.Handled = true;
            }
        }

        private void BulkLinksTextBox_OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            var text = e.SourceDataObject.GetData(DataFormats.Text, true);
            if (text != null)
            {
                var textAsString = (string)text;
                textAsString += Environment.NewLine;
                BulkLinksTextBox.Text += textAsString;
                BulkLinksTextBox.SelectionStart = BulkLinksTextBox.Text.Length;
                e.CancelCommand();
                e.Handled = true;
            }
            
        }
    }
}