using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using YoutubeExplode;
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;

namespace YoutubePlaylistDownloader
{
    public partial class MainPage : UserControl
    {
        private YoutubeClient client;
        private Playlist list = null;
        private Video video = null;
        private Channel channel = null;
        private readonly Dictionary<string, VideoQuality> Resolutions = new Dictionary<string, VideoQuality>()
        {
            { "144p", VideoQuality.Low144 },
            { "240p", VideoQuality.Low240 },
            { "360p", VideoQuality.Medium360 },
            { "480p", VideoQuality.Medium480 },
            { "720p", VideoQuality.High720 },
            { "1080p", VideoQuality.High1080 },
            { "1440p", VideoQuality.High1440 },
            { "2160p", VideoQuality.High2160 },
            { "2880p", VideoQuality.High2880 },
            { "3072p", VideoQuality.High3072 },
            { "4320p", VideoQuality.High4320 }
        };

        private readonly string[] FileTypes = { "mp3", "aac", "opus", "wav", "flac", "m4a", "ogg", "webm" };

        public MainPage()
        {
            InitializeComponent();
            GlobalConsts.ShowSettingsButton();
            GlobalConsts.ShowAboutButton();
            GlobalConsts.HideHomeButton();
            ExtensionsDropDown.ItemsSource = FileTypes;
            ResulotionDropDown.ItemsSource = Resolutions.Keys;
            ResulotionDropDown.SelectedIndex = 4;
            OptionsExpander.IsExpanded = GlobalConsts.OptionExpanderIsExpanded;
            client = new YoutubeClient();
        }

        private async void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (YoutubeClient.TryParsePlaylistId(PlaylistLinkTextBox.Text, out string playlistId))
                {
                    _ = Task.Run(async () =>
                    {
                        list = await client.GetPlaylistAsync(playlistId).ConfigureAwait(false);
                        video = null;
                        await UpdatePlaylistInfo(Visibility.Visible, list.Title, list.Author, list.Statistics.ViewCount.ToString(), list.Videos.Count.ToString(), $"https://img.youtube.com/vi/{list?.Videos?.FirstOrDefault()?.Id}/0.jpg", true, true);
                    }).ConfigureAwait(false);
                }
                else if (YoutubeClient.TryParseChannelId(PlaylistLinkTextBox.Text, out string channelId))
                {
                    _ = Task.Run(async () =>
                    {
                        channel = await client.GetChannelAsync(channelId).ConfigureAwait(false);
                        list = await client.GetPlaylistAsync(channel.GetChannelVideosPlaylistId());
                        video = null;
                        await UpdatePlaylistInfo(Visibility.Visible, channel.Title, list.Author, list.Statistics.ViewCount.ToString(), list.Videos.Count.ToString(), $"https://img.youtube.com/vi/{channel.LogoUrl}/0.jpg", true, true);
                    }).ConfigureAwait(false);
                }
                else if (YoutubeClient.TryParseVideoId(PlaylistLinkTextBox.Text, out string videoId))
                {
                    _ = Task.Run(async () =>
                    {
                        video = await client.GetVideoAsync(videoId);
                        list = null;

                        await UpdatePlaylistInfo(Visibility.Visible, video.Title, video.Author, video.Statistics.ViewCount.ToString(), string.Empty, $"https://img.youtube.com/vi/{video.Id}/0.jpg", true, false);
                    }).ConfigureAwait(false);
                }
                else
                {
                    await UpdatePlaylistInfo().ConfigureAwait(false);
                }
            }

            catch (Exception ex)
            {
                await GlobalConsts.ShowMessage((string)FindResource("Error"), ex.Message);
            }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var convert = ConvertCheckBox.IsChecked.Value;
            var vq = VideoQuality.High720;
            var type = "mp3";
            string bitrate = null;
            int startIndex = 0, endIndex = 0;

            if (DownloadByIndexCheckBox.IsChecked.Value)
            {
                if (!string.IsNullOrWhiteSpace(StartIndexTextBox.Text) && StartIndexTextBox.Text.All(c => char.IsDigit(c)) == true)
                    startIndex = int.Parse(StartIndexTextBox.Text) - 1;

                if (!string.IsNullOrWhiteSpace(EndIndexTextBox.Text) && EndIndexTextBox.Text.All(c => char.IsDigit(c)) == true)
                    endIndex = int.Parse(EndIndexTextBox.Text) - 1;
            }

            if (BitrateCheckBox.IsChecked.Value)
                if (!string.IsNullOrWhiteSpace(BitRateTextBox.Text) && BitRateTextBox.Text.All(c => char.IsDigit(c)))
                    bitrate = BitRateTextBox.Text;


            if (PreferCheckBox.IsChecked.Value)
                vq = Resolutions[(string)ResulotionDropDown.SelectedValue];

            if (convert)
                type = (string)ExtensionsDropDown.SelectedItem;

            if (list != null && video == null)
            {
                startIndex = startIndex <= 0 ? 0 : startIndex;
                endIndex = endIndex <= 0 ? list.Videos.Count - 1 : endIndex;

                GlobalConsts.LoadPage(new DownloadPage(list, convert, vq, type, bitrate, startIndex, endIndex));
            }
            else if (list == null && video != null)
                GlobalConsts.LoadPage(new DownloadVideo(video, convert, vq, type, bitrate));
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

                if (showIndexes)
                    DownloadSubIndexStackPanel.Visibility = Visibility.Visible;
                else
                    DownloadSubIndexStackPanel.Visibility = Visibility.Collapsed;

                DownloadButton.IsEnabled = downloadEnabled;

            });

        private void OptionsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            GlobalConsts.OptionExpanderIsExpanded = OptionsExpander.IsExpanded;
        }
    }
}
