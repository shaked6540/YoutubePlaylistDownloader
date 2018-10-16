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
using YoutubePlaylistDownloader.Objects;

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

            GlobalConsts.HideHomeButton();
            GlobalConsts.ShowSettingsButton();
            GlobalConsts.ShowAboutButton();
            GlobalConsts.ShowHelpButton();
            GlobalConsts.ShowSubscriptionsButton();

            ExtensionsDropDown.ItemsSource = FileTypes;
            ResulotionDropDown.ItemsSource = Resolutions.Keys;

            OptionsExpander.IsExpanded = GlobalConsts.OptionExpanderIsExpanded;
            client = GlobalConsts.YoutubeClient;

            if (GlobalConsts.SaveDownloadOptions)
                SetSettings();
            else
                ResulotionDropDown.SelectedIndex = 4;

            void UpdateSize(object s, SizeChangedEventArgs e)
            {
                double height = GlobalConsts.GetOffset() - (HeadlineStackPanel.ActualHeight + 75);
                GridScrollViewer.Height = height;
                QueueScrollViewer.Height = height;
                GridScrollViewer.UpdateLayout();
                QueueScrollViewer.UpdateLayout();
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

        private void SetSettings()
        {
            var settings = GlobalConsts.DownloadSettings;
            ExtensionsDropDown.SelectedItem = settings.SaveFormat;
            ResulotionDropDown.SelectedItem = Resolutions.FirstOrDefault(x => x.Value == settings.Quality).Key;
            PreferCheckBox.IsChecked = settings.PreferQuality;
            PreferHighestFPSCheckBox.IsChecked = settings.PreferHighestFPS;
            ConvertCheckBox.IsChecked = settings.Convert;
            BitrateCheckBox.IsChecked = settings.SetBitrate;
            BitRateTextBox.Text = string.IsNullOrWhiteSpace(settings.Bitrate) ? "192" : settings.Bitrate;
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
                        await UpdatePlaylistInfo(Visibility.Visible, channel.Title, list.Author, list.Statistics.ViewCount.ToString(), list.Videos.Count.ToString(), channel.LogoUrl, true, true);
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
                await GlobalConsts.Log(ex.ToString(), "MainPage TextBox_TextChanged");
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
            bool audioOnly = AudioOnlyCheckBox.IsChecked.Value;
            bool preferHighestFPS = PreferHighestFPSCheckBox.IsChecked.Value;

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

            GlobalConsts.DownloadSettings = new DownloadSettings(type, audioOnly, vq, preferHighestFPS, PreferCheckBox.IsChecked.Value, convert, BitrateCheckBox.IsChecked.Value, bitrate);

            if (list != null && video == null)
            {
                GlobalConsts.LoadPage(new DownloadPage(list, convert, vq, type, bitrate, startIndex, endIndex, audioOnly, preferHighestFPS));
                PlaylistLinkTextBox.Text = string.Empty;
            }
            else if (list == null && video != null)
            {
                GlobalConsts.LoadPage(new DownloadVideo(video, convert, vq, type, bitrate, audioOnly, preferHighestFPS));
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

                if (showIndexes)
                    DownloadSubIndexStackPanel.Visibility = Visibility.Visible;
                else
                    DownloadSubIndexStackPanel.Visibility = Visibility.Collapsed;

                DownloadButton.IsEnabled = downloadEnabled;
                DownloadInBackgroundButton.IsEnabled = downloadEnabled;

            });


        private void DownloadInBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            var convert = ConvertCheckBox.IsChecked.Value;
            var vq = VideoQuality.High720;
            var type = "mp3";
            string bitrate = null;
            int startIndex = 0, endIndex = 0;
            bool audioOnly = AudioOnlyCheckBox.IsChecked.Value;
            bool preferHighestFPS = PreferHighestFPSCheckBox.IsChecked.Value;

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

            GlobalConsts.DownloadSettings = new DownloadSettings(type, audioOnly, vq, preferHighestFPS, PreferCheckBox.IsChecked.Value, convert, BitrateCheckBox.IsChecked.Value, bitrate);

            if (list != null && video == null)
            {
                _ = new DownloadPage(list, convert, vq, type, bitrate, startIndex, endIndex, audioOnly, preferHighestFPS, silent: true);
                PlaylistLinkTextBox.Text = string.Empty;
            }
            else if (list == null && video != null)
            {
                _ = new DownloadVideo(video, convert, vq, type, bitrate, audioOnly, preferHighestFPS, silent: true);
                PlaylistLinkTextBox.Text = string.Empty;
            }
        }

        private void OptionsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            GlobalConsts.OptionExpanderIsExpanded = OptionsExpander.IsExpanded;
        }

        private void PreferCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.PreferQuality = PreferCheckBox.IsChecked.Value;
                GlobalConsts.DownloadSettings.Quality = Resolutions[(string)ResulotionDropDown.SelectedValue];
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void PreferCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.PreferQuality = PreferCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void ResulotionDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.Quality = Resolutions[(string)ResulotionDropDown.SelectedValue];
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void ConvertCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.Convert = ConvertCheckBox.IsChecked.Value;
                GlobalConsts.DownloadSettings.SaveFormat = (string)ExtensionsDropDown.SelectedItem;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void ConvertCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.Convert = ConvertCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void ExtensionsDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.SaveFormat = (string)ExtensionsDropDown.SelectedItem;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void BitrateCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.SetBitrate = BitrateCheckBox.IsChecked.Value;
                GlobalConsts.DownloadSettings.Bitrate = BitRateTextBox.Text;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void BitrateCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.SetBitrate = BitrateCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void BitRateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.Bitrate = BitRateTextBox.Text;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void PreferHighestFPSCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.PreferHighestFPS = PreferHighestFPSCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void PreferHighestFPSCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.PreferHighestFPS = PreferHighestFPSCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void AudioOnlyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.AudioOnly = AudioOnlyCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void AudioOnlyCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.AudioOnly = AudioOnlyCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }
    }
}
/*
                        <Grid Visibility="Visible" x:Name="DownloadInfoGrid" Margin="7,0" HorizontalAlignment="Left" Width="Auto" MaxHeight="410" >
                            <Grid.RowDefinitions>
                                <RowDefinition Height="Auto"/>
                                <RowDefinition Height="Auto"/>
                                <RowDefinition Height="Auto"/>
                                <RowDefinition/>
                            </Grid.RowDefinitions>

                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="Auto"/>
                                <ColumnDefinition Width="Auto"/>
                                <ColumnDefinition Width="Auto"/>
                                <ColumnDefinition />
                                <ColumnDefinition Width="Auto"/>
                            </Grid.ColumnDefinitions>

                            <!--Col 0-->
                            <Controls:Tile Width="50" Height="50" Grid.RowSpan="5" Margin="2.5">
                                <iconPacks:PackIconModern Kind="Close" Width="40" Height="40"/>
                            </Controls:Tile>

                            <!--Col 1-->
                            <Image x:Name="DownloadInfoImage" Margin="2.5,2.5,10,2.5" Grid.Column="1" Grid.Row="0" Grid.RowSpan="5" Width="200" Height="112.5" />

                            
                            <!--Col 2-->
                            <TextBlock Margin="2.5" Text="{DynamicResource PlaylistTitle}" Grid.Row="0" Grid.Column="2" FontSize="14" />
                            <TextBlock Margin="2.5" Text="S.Title" Grid.Row="1" Grid.Column="2" FontSize="14" />


                            <!--Col 4-->
                            <Grid Grid.Row="0" Grid.Column="4" Grid.RowSpan="4" Margin="2.5">
                                <ProgressBar Height="30" VerticalAlignment="Center" Width="880" />
                                <TextBlock VerticalAlignment="Center" HorizontalAlignment="Center" />
                            </Grid>
                        </Grid>
 */
