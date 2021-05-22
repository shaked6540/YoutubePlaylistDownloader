using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using YoutubeExplode.Videos.Streams;
using YoutubePlaylistDownloader.Objects;
using YoutubePlaylistDownloader.Utilities;

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
        private readonly Dictionary<string, string> Languages = new Dictionary<string, string>() { { "aa", "Afar" }, { "ab", "Abkhazian" }, { "af", "Afrikaans" }, { "ak", "Akan" }, { "sq", "Albanian" }, { "am", "Amharic" }, { "ar", "Arabic" }, { "an", "Aragonese" }, { "hy", "Armenian" }, { "as", "Assamese" }, { "av", "Avaric" }, { "ae", "Avestan" }, { "ay", "Aymara" }, { "az", "Azerbaijani" }, { "ba", "Bashkir" }, { "bm", "Bambara" }, { "eu", "Basque" }, { "be", "Belarusian" }, { "bn", "Bengali" }, { "bh", "Bihari languages" }, { "bi", "Bislama" }, { "bs", "Bosnian" }, { "br", "Breton" }, { "bg", "Bulgarian" }, { "my", "Burmese" }, { "ca", "Catalan" }, { "ch", "Chamorro" }, { "ce", "Chechen" }, { "zh", "Chinese" }, { "cu", "Church Slavic" }, { "cv", "Chuvash" }, { "kw", "Cornish" }, { "co", "Corsican" }, { "cr", "Cree" }, { "cs", "Czech" }, { "da", "Danish" }, { "dv", "Divehi" }, { "nl", "Dutch" }, { "dz", "Dzongkha" }, { "en", "English" }, { "eo", "Esperanto" }, { "et", "Estonian" }, { "ee", "Ewe" }, { "fo", "Faroese" }, { "fj", "Fijian" }, { "fi", "Finnish" }, { "fr", "French" }, { "fy", "Western Frisian" }, { "ff", "Fulah" }, { "ka", "Georgian" }, { "de", "German" }, { "gd", "Gaelic" }, { "ga", "Irish" }, { "gl", "Galician" }, { "gv", "Manx" }, { "el", "Greek" }, { "gn", "Guarani" }, { "gu", "Gujarati" }, { "ht", "Haitian" }, { "ha", "Hausa" }, { "he", "Hebrew" }, { "hz", "Herero" }, { "hi", "Hindi" }, { "ho", "Hiri Motu" }, { "hr", "Croatian" }, { "hu", "Hungarian" }, { "ig", "Igbo" }, { "is", "Icelandic" }, { "io", "Ido" }, { "ii", "Sichuan Yi" }, { "iu", "Inuktitut" }, { "ie", "Interlingue" }, { "ia", "Interlingua" }, { "id", "Indonesian" }, { "ik", "Inupiaq" }, { "it", "Italian" }, { "jv", "Javanese" }, { "ja", "Japanese" }, { "kl", "Kalaallisut" }, { "kn", "Kannada" }, { "ks", "Kashmiri" }, { "kr", "Kanuri" }, { "kk", "Kazakh" }, { "km", "Central Khmer" }, { "ki", "Kikuyu" }, { "rw", "Kinyarwanda" }, { "ky", "Kirghiz" }, { "kv", "Komi" }, { "kg", "Kongo" }, { "ko", "Korean" }, { "kj", "Kuanyama" }, { "ku", "Kurdish" }, { "lo", "Lao" }, { "la", "Latin" }, { "lv", "Latvian" }, { "li", "Limburgan" }, { "ln", "Lingala" }, { "lt", "Lithuanian" }, { "lb", "Luxembourgish" }, { "lu", "Luba-Katanga" }, { "lg", "Ganda" }, { "mk", "Macedonian" }, { "mh", "Marshallese" }, { "ml", "Malayalam" }, { "mi", "Maori" }, { "mr", "Marathi" }, { "ms", "Malay" }, { "mg", "Malagasy" }, { "mt", "Maltese" }, { "mn", "Mongolian" }, { "na", "Nauru" }, { "nv", "Navajo" }, { "nr", "Ndebele, South" }, { "nd", "Ndebele, North" }, { "ng", "Ndonga" }, { "ne", "Nepali" }, { "nn", "Norwegian" }, { "nb", "Bokmål" }, { "no", "Norwegian" }, { "ny", "Chichewa" }, { "oc", "Occitan" }, { "oj", "Ojibwa" }, { "or", "Oriya" }, { "om", "Oromo" }, { "os", "Ossetian" }, { "pa", "Panjabi" }, { "fa", "Persian" }, { "pi", "Pali" }, { "pl", "Polish" }, { "pt", "Portuguese" }, { "ps", "Pushto" }, { "qu", "Quechua" }, { "rm", "Romansh" }, { "ro", "Romanian" }, { "rn", "Rundi" }, { "ru", "Russian" }, { "sg", "Sango" }, { "sa", "Sanskrit" }, { "si", "Sinhala" }, { "sk", "Slovak" }, { "sl", "Slovenian" }, { "se", "Northern Sami" }, { "sm", "Samoan" }, { "sn", "Shona" }, { "sd", "Sindhi" }, { "so", "Somali" }, { "st", "Sotho, Southern" }, { "es", "Spanish" }, { "sc", "Sardinian" }, { "sr", "Serbian" }, { "ss", "Swati" }, { "su", "Sundanese" }, { "sw", "Swahili" }, { "sv", "Swedish" }, { "ty", "Tahitian" }, { "ta", "Tamil" }, { "tt", "Tatar" }, { "te", "Telugu" }, { "tg", "Tajik" }, { "tl", "Tagalog" }, { "th", "Thai" }, { "bo", "Tibetan" }, { "ti", "Tigrinya" }, { "to", "Tonga (Tonga Islands)" }, { "tn", "Tswana" }, { "ts", "Tsonga" }, { "tk", "Turkmen" }, { "tr", "Turkish" }, { "tw", "Twi" }, { "ug", "Uighur" }, { "uk", "Ukrainian" }, { "ur", "Urdu" }, { "uz", "Uzbek" }, { "ve", "Venda" }, { "vi", "Vietnamese" }, { "vo", "Volapük" }, { "cy", "Welsh" }, { "wa", "Walloon" }, { "wo", "Wolof" }, { "xh", "Xhosa" }, { "yi", "Yiddish" }, { "yo", "Yoruba" }, { "za", "Zhuang" }, { "zu", "Zulu" }, };

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
                SaveDirectoryTextBox.Text = Subscription.Settings.SavePath;
                PreferCheckBox.IsChecked = Subscription.Settings.PreferQuality;
                PreferHighestFPSCheckBox.IsChecked = Subscription.Settings.PreferHighestFPS;
                ResulotionDropDown.SelectedItem = Resolutions.FirstOrDefault(x => x.Value == Subscription.Settings.Quality).Key;
                ConvertCheckBox.IsChecked = Subscription.Settings.Convert;
                ExtensionsDropDown.SelectedItem = Subscription.Settings.SaveFormat;
                BitrateCheckBox.IsChecked = Subscription.Settings.SetBitrate;
                BitRateTextBox.Text = Subscription.Settings.Bitrate;
                CaptionsCheckBox.IsChecked = Subscription.Settings.DownloadCaptions;
                CaptionsLanguagesComboBox.SelectedItem = Languages[Subscription.Settings.CaptionsLanguage ?? "en"];
                ChannelLogo.Source = new BitmapImage(new Uri(channel.Thumbnails.FirstOrDefault()?.Url));
                FilterByLengthCheckBox.IsChecked = Subscription.Settings.FilterVideosByLength;
                var FilterByLengthShorterOrLongerDropDownItemSource = new[] { FindResource("Longer"), FindResource("Shorter") };
                FilterByLengthShorterOrLongerDropDown.ItemsSource = FilterByLengthShorterOrLongerDropDownItemSource;
                FilterByLengthShorterOrLongerDropDown.SelectedItem = Subscription.Settings.FilterMode ? FilterByLengthShorterOrLongerDropDownItemSource[0] : FilterByLengthShorterOrLongerDropDownItemSource[1];
                FilterByLengthTextBox.Text = Subscription.Settings.FilterByLengthValue.ToString();
            });

        }

        private void SaveDirectoryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string dir = SaveDirectoryTextBox.Text;
            if (System.IO.Directory.Exists(dir))
            {
                Subscription.Settings.SavePath = dir;
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
            Subscription.Settings.SavePath = SaveDirectoryTextBox.Text;
            Subscription.Settings.PreferQuality = PreferCheckBox.IsChecked.Value;
            Subscription.Settings.PreferHighestFPS = PreferHighestFPSCheckBox.IsChecked.Value;
            Subscription.Settings.Quality = Resolutions[(string)ResulotionDropDown.SelectedValue];
            Subscription.Settings.Convert = ConvertCheckBox.IsChecked.Value;
            Subscription.Settings.SaveFormat = ExtensionsDropDown.SelectedItem.ToString();
            Subscription.Settings.SetBitrate = BitrateCheckBox.IsChecked.Value;
            Subscription.Settings.DownloadCaptions = CaptionsCheckBox.IsChecked.Value;
            Subscription.Settings.CaptionsLanguage = Languages.FirstOrDefault(x => x.Value.Equals((string)CaptionsLanguagesComboBox.SelectedItem, StringComparison.OrdinalIgnoreCase)).Key;
            Subscription.Settings.FilterVideosByLength = FilterByLengthCheckBox.IsChecked.Value;

            if (double.TryParse(FilterByLengthTextBox.Text, out double result))
            {
                Subscription.Settings.FilterByLengthValue = result;
            }
            
            Subscription.Settings.FilterMode = FilterByLengthShorterOrLongerDropDown.SelectedItem.Equals(FindResource("Longer"));

            if (Subscription.Settings.SetBitrate && BitRateTextBox.Text.All(x => char.IsDigit(x)))
                Subscription.Settings.Bitrate = BitRateTextBox.Text;
            else
            {
                Subscription.Settings.SetBitrate = false;
                Subscription.Settings.Bitrate = string.Empty;
            }
            Subscription.Settings.AudioOnly = AudioOnlyCheckBox.IsChecked.Value;


            SubscriptionManager.SaveSubscriptions();
            GlobalConsts.CloseFlyout();
        }

        private void FilterByLengthTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!double.TryParse(FilterByLengthTextBox.Text, out double value))
            {
                if (!string.IsNullOrWhiteSpace(FilterByLengthTextBox.Text))
                    FilterByLengthTextBox.Background = GlobalConsts.ErrorBrush;

            }
            else
            {
                FilterByLengthTextBox.Background = null;
            }
        }
    }
}
