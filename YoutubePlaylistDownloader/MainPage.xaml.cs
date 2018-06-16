using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YoutubeExplode;
using YoutubeExplode.Models;
using System.Collections.Generic;
using YoutubeExplode.Models.MediaStreams;
namespace YoutubePlaylistDownloader
{
    public partial class MainPage : UserControl
    {
        private Playlist list;
        private bool convert = false;
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

        private readonly string[] FileTypes = { "mp3", "aac", "opus", "wav" };

        public MainPage()
        {
            InitializeComponent();
            GlobalConsts.ShowSettingsButton();
            GlobalConsts.ShowAboutButton();
            GlobalConsts.HideHomeButton();
            ExtensionsDropDown.ItemsSource = FileTypes;
            ResulotionDropDown.ItemsSource = Resolutions.Keys;
        }

        private async void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var client = new YoutubeClient();
                if (YoutubeClient.TryParsePlaylistId(PlaylistLinkTextBox.Text, out string playlistId))
                {
                    _ = Task.Run(async () =>
                    {
                        list = await client.GetPlaylistAsync(playlistId).ConfigureAwait(false);
                        Dispatcher.Invoke(() =>
                        {
                            PlaylistInfoGrid.Visibility = Visibility.Visible;
                            PlaylistTitleTextBlock.Text = list.Title;
                            PlaylistDescriptionTextBlock.Text = list.Description;
                            PlaylistAuthorTextBlock.Text = list.Author;
                            PlaylistViewsTextBlock.Text = list.Statistics.ViewCount.ToString();
                            DownloadButton.IsEnabled = true;
                        });

                    }).ConfigureAwait(false);

                }
                else
                {
                    PlaylistInfoGrid.Visibility = Visibility.Collapsed;
                    PlaylistTitleTextBlock.Text = string.Empty;
                    PlaylistDescriptionTextBlock.Text = string.Empty;
                    PlaylistAuthorTextBlock.Text = string.Empty;
                    PlaylistViewsTextBlock.Text = string.Empty;
                    DownloadButton.IsEnabled = false;
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
            var type = "aac";

            if (PreferCheckBox.IsChecked.Value)
                vq = Resolutions[(string)ResulotionDropDown.SelectedValue];

            if (convert)
                type = (string)ExtensionsDropDown.SelectedItem;

            GlobalConsts.LoadPage(new DownloadPage(list, convert, vq, type));
        }

        private string CleanFileName(string file)
        {
            var s = Path.GetInvalidFileNameChars();
            foreach (var c in s)
                file = file.Replace(c.ToString(), string.Empty);

            return file;
        }
     
    }
}
