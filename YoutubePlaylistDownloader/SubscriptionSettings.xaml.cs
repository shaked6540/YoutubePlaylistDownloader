using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using YoutubeExplode.Models.MediaStreams;
using YoutubePlaylistDownloader.Objects;

namespace YoutubePlaylistDownloader
{
    /// <summary>
    /// Interaction logic for SubscriptionSettings.xaml
    /// </summary>
    public partial class SubscriptionSettings : UserControl
    {
        private readonly Subscription Subscription;
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
        public SubscriptionSettings(Subscription subscription)
        {
            InitializeComponent();
            ResulotionDropDown.ItemsSource = Resolutions.Keys;
            ExtensionsDropDown.ItemsSource = FileTypes;

            Subscription = subscription;
            LoadInfo().ConfigureAwait(false);
        }

        private async Task LoadInfo()
        {
            var channel = await Subscription.GetChannel();
            await Dispatcher.InvokeAsync(() =>
            {
                TitleLabel.Content = channel.Title;
                IdLabel.Content = Subscription.ChannelId;
                SaveDirectoryTextBox.Text = Subscription.SavePath;
                PreferCheckBox.IsChecked = Subscription.PreferQuality;
                PreferHighestFPSCheckBox.IsChecked = Subscription.PreferHighestFPS;
                ResulotionDropDown.SelectedItem = Resolutions.FirstOrDefault(x => x.Value == Subscription.Quality).Key;
                ConvertCheckBox.IsChecked = Subscription.Convert;
                ExtensionsDropDown.SelectedItem = Subscription.SaveFormat;
                BitrateCheckBox.IsChecked = Subscription.SetBitrate;
                BitRateTextBox.Text = Subscription.Bitrate.Replace("k", string.Empty);
                ChannelLogo.Source = new BitmapImage(new Uri(channel.LogoUrl));
            });

        }

        private void SaveDirectoryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string dir = SaveDirectoryTextBox.Text;
            if (System.IO.Directory.Exists(dir))
            {
                Subscription.SavePath = dir;
                SaveDirectoryTextBox.Background = null;
                SaveButton.IsEnabled = true;
            }
            else
            {
                SaveDirectoryTextBox.Background = GlobalConsts.ErrorBrush;
                SaveButton.IsEnabled = false;
            }
        }

        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.RootFolder = Environment.SpecialFolder.Desktop;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    SaveDirectoryTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            TextBox_MouseDoubleClick(null, null);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Subscription.SavePath = SaveDirectoryTextBox.Text;
            Subscription.PreferQuality = PreferCheckBox.IsChecked.Value;
            Subscription.PreferHighestFPS = PreferHighestFPSCheckBox.IsChecked.Value;
            Subscription.Quality = Resolutions[(string)ResulotionDropDown.SelectedValue];
            Subscription.Convert = ConvertCheckBox.IsChecked.Value;
            Subscription.SaveFormat = ExtensionsDropDown.SelectedItem.ToString();
            Subscription.SetBitrate = BitrateCheckBox.IsChecked.Value;

            if (Subscription.SetBitrate && BitRateTextBox.Text.All(x => char.IsDigit(x)))
                Subscription.Bitrate = string.Concat(BitRateTextBox.Text, "k");
            else
            {
                Subscription.SetBitrate = false;
                Subscription.Bitrate = string.Empty;
            }
            Subscription.AudioOnly = AudioOnlyCheckBox.IsChecked.Value;


            SubscriptionManager.SaveSubscriptions();
            GlobalConsts.CloseFlyout();
        }
    }
}
