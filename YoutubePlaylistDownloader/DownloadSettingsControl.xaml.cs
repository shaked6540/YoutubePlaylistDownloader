using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using YoutubeExplode.Videos.Streams;
using YoutubePlaylistDownloader.Utilities;

namespace YoutubePlaylistDownloader
{
    /// <summary>
    /// Interaction logic for DownloadSettings.xaml
    /// </summary>
    public partial class DownloadSettingsControl : UserControl
    {
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

        public DownloadSettingsControl()
        {
            InitializeComponent();

            ResulotionDropDown.ItemsSource = Resolutions.Keys;
            ExtensionsDropDown.ItemsSource = FileTypes;
            CaptionsLanguagesComboBox.ItemsSource = Languages.Values;

            var settings = GlobalConsts.DownloadSettings;
            SaveDirectoryTextBox.Text = GlobalConsts.SaveDirectory;
            ExtensionsDropDown.SelectedItem = settings.SaveFormat;
            ResulotionDropDown.SelectedItem = Resolutions.FirstOrDefault(x => x.Value == settings.Quality).Key;
            PreferCheckBox.IsChecked = settings.PreferQuality;
            PreferHighestFPSCheckBox.IsChecked = settings.PreferHighestFPS;
            ConvertCheckBox.IsChecked = settings.Convert;
            BitrateCheckBox.IsChecked = settings.SetBitrate;
            BitRateTextBox.Text = string.IsNullOrWhiteSpace(settings.Bitrate) ? "192" : settings.Bitrate;
            CaptionsCheckBox.IsChecked = settings.DownloadCaptions;
            CaptionsLanguagesComboBox.SelectedItem = Languages[settings.CaptionsLanguage ?? "en"];
            AudioOnlyCheckBox.IsChecked = settings.AudioOnly;
            UniquePlaylistDirectoryCheckBox.IsChecked = settings.SavePlaylistsInDifferentDirectories;
            PlaylistIndexCheckBox.IsChecked = settings.Subset;
            PlaylistStartIndexTextBox.Text = settings.SubsetStartIndex.ToString();
            PlaylistEndIndexTextBox.Text = settings.SubsetEndIndex.ToString();
            OpenDestinationFolderCheckBox.IsChecked = settings.OpenDestinationFolderWhenDone;
            TagAudioFileCheckBox.IsChecked = settings.TagAudioFile;
            FilterByLengthCheckBox.IsChecked = settings.FilterVideosByLength;
            var FilterByLengthShorterOrLongerDropDownItemSource = new[] { FindResource("Longer"), FindResource("Shorter") };
            FilterByLengthShorterOrLongerDropDown.ItemsSource = FilterByLengthShorterOrLongerDropDownItemSource;
            FilterByLengthShorterOrLongerDropDown.SelectedItem = settings.FilterMode ? FilterByLengthShorterOrLongerDropDownItemSource[0] : FilterByLengthShorterOrLongerDropDownItemSource[1];
            FilterByLengthTextBox.Text = settings.FilterByLengthValue.ToString();

            SubscribeToEvents();

        }

        private void SubscribeToEvents()
        {
            PreferCheckBox.Checked += PreferCheckBox_Checked;
            PreferCheckBox.Unchecked += PreferCheckBox_Unchecked;
            ResulotionDropDown.SelectionChanged += ResulotionDropDown_SelectionChanged;
            PreferHighestFPSCheckBox.Checked += PreferHighestFPSCheckBox_Checked;
            PreferHighestFPSCheckBox.Unchecked += PreferHighestFPSCheckBox_Unchecked;
            CaptionsCheckBox.Checked += CaptionsCheckBox_Checked;
            CaptionsCheckBox.Unchecked += CaptionsCheckBox_Unchecked;
            CaptionsLanguagesComboBox.SelectionChanged += CaptionsLanguagesComboBox_SelectionChanged;
            ConvertCheckBox.Checked += ConvertCheckBox_Checked;
            ConvertCheckBox.Unchecked += ConvertCheckBox_Unchecked;
            ExtensionsDropDown.SelectionChanged += ExtensionsDropDown_SelectionChanged;
            BitrateCheckBox.Checked += BitrateCheckBox_Checked;
            BitrateCheckBox.Unchecked += BitrateCheckBox_Unchecked;
            BitRateTextBox.TextChanged += BitRateTextBox_TextChanged;
            AudioOnlyCheckBox.Checked += AudioOnlyCheckBox_Checked;
            AudioOnlyCheckBox.Unchecked += AudioOnlyCheckBox_Unchecked;
            UniquePlaylistDirectoryCheckBox.Checked += UniquePlaylistDirectoryCheckBox_Checked;
            UniquePlaylistDirectoryCheckBox.Unchecked += UniquePlaylistDirectoryCheckBox_Unchecked;
            PlaylistIndexCheckBox.Checked += PlaylistIndexCheckBox_Checked;
            PlaylistIndexCheckBox.Unchecked += PlaylistIndexCheckBox_Unchecked;
            PlaylistStartIndexTextBox.TextChanged += PlaylistStartIndexTextBox_TextChanged;
            PlaylistEndIndexTextBox.TextChanged += PlaylistEndIndexTextBox_TextChanged;
            OpenDestinationFolderCheckBox.Checked += OpenDestinationFolderCheckBox_Checked;
            OpenDestinationFolderCheckBox.Unchecked += OpenDestinationFolderCheckBox_Unchecked;
            TagAudioFileCheckBox.Checked += TagAudioFileCheckBox_Checked;
            TagAudioFileCheckBox.Unchecked += TagAudioFileCheckBox_Unchecked;
            FilterByLengthCheckBox.Checked += FilterByLengthCheckBox_Checked;
            FilterByLengthCheckBox.Unchecked += FilterByLengthCheckBox_Checked;
            FilterByLengthShorterOrLongerDropDown.SelectionChanged += FilterByLengthShorterOrLongerDropDown_SelectionChanged;
            FilterByLengthTextBox.TextChanged += FilterByLengthTextBox_TextChanged;
        }

        private void FilterByLengthTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!double.TryParse(FilterByLengthTextBox.Text, out double value))
            {
                if (!string.IsNullOrWhiteSpace(FilterByLengthTextBox.Text))
                    FilterByLengthTextBox.Background = GlobalConsts.ErrorBrush;

                if (GlobalConsts.SaveDownloadOptions)
                {
                    GlobalConsts.DownloadSettings.FilterByLengthValue = 4;
                    GlobalConsts.DownloadSettings.FilterVideosByLength = false;
                    GlobalConsts.SaveDownloadSettings();
                }
            }
            else
            {
                FilterByLengthTextBox.Background = null;
                if (GlobalConsts.SaveDownloadOptions)
                {
                    GlobalConsts.DownloadSettings.FilterByLengthValue = value;
                    GlobalConsts.SaveDownloadSettings();
                }
            }
        }

        private void FilterByLengthShorterOrLongerDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.FilterMode = FilterByLengthShorterOrLongerDropDown.SelectedItem.Equals(FindResource("Longer"));
                GlobalConsts.SaveDownloadSettings();
            } 
        }

        private void FilterByLengthCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.FilterVideosByLength = FilterByLengthCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void TagAudioFileCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.TagAudioFile = TagAudioFileCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void TagAudioFileCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.TagAudioFile = TagAudioFileCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void OpenDestinationFolderCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.OpenDestinationFolderWhenDone = OpenDestinationFolderCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void OpenDestinationFolderCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.OpenDestinationFolderWhenDone = OpenDestinationFolderCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void PlaylistEndIndexTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(PlaylistEndIndexTextBox.Text, out int endIndex))
            {
                if (!string.IsNullOrWhiteSpace(PlaylistEndIndexTextBox.Text))
                    PlaylistEndIndexTextBox.Background = GlobalConsts.ErrorBrush;

                if (GlobalConsts.SaveDownloadOptions)
                {
                    GlobalConsts.DownloadSettings.SubsetStartIndex = 0;
                    GlobalConsts.SaveDownloadSettings();
                }
            }
            else
            {
                PlaylistEndIndexTextBox.Background = null;
                if (GlobalConsts.SaveDownloadOptions)
                {
                    GlobalConsts.DownloadSettings.SubsetEndIndex = endIndex;
                    GlobalConsts.SaveDownloadSettings();
                }
            }
        }

        private void PlaylistStartIndexTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(PlaylistStartIndexTextBox.Text, out int startIndex) || startIndex < 1)
            {
                PlaylistStartIndexTextBox.Background = GlobalConsts.ErrorBrush;
                if (GlobalConsts.SaveDownloadOptions)
                {
                    GlobalConsts.DownloadSettings.SubsetStartIndex = 0;
                    GlobalConsts.SaveDownloadSettings();
                }
            }
            else
            {
                PlaylistStartIndexTextBox.Background = null;
                if (GlobalConsts.SaveDownloadOptions)
                {
                    GlobalConsts.DownloadSettings.SubsetStartIndex = startIndex - 1;
                    GlobalConsts.SaveDownloadSettings();
                }
            }
        }

        private void PlaylistIndexCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.Subset = PlaylistIndexCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void PlaylistIndexCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.Subset = PlaylistIndexCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void UniquePlaylistDirectoryCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.SavePlaylistsInDifferentDirectories = UniquePlaylistDirectoryCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void UniquePlaylistDirectoryCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.SavePlaylistsInDifferentDirectories = UniquePlaylistDirectoryCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void CaptionsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.DownloadCaptions = CaptionsCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void CaptionsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.DownloadCaptions = CaptionsCheckBox.IsChecked.Value;
                GlobalConsts.SaveDownloadSettings();
            }
        }

        private void CaptionsLanguagesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GlobalConsts.SaveDownloadOptions)
            {
                GlobalConsts.DownloadSettings.CaptionsLanguage = Languages.FirstOrDefault(x => x.Value.Equals((string)CaptionsLanguagesComboBox.SelectedItem, StringComparison.OrdinalIgnoreCase)).Key;

                if (CaptionsCheckBox.IsChecked.Value && GlobalConsts.DownloadSettings.CaptionsLanguage == default)
                    GlobalConsts.DownloadSettings.CaptionsLanguage = "en";

                GlobalConsts.SaveDownloadSettings();
            }
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

        private void TextBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.RootFolder = System.Environment.SpecialFolder.Desktop;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    SaveDirectoryTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void SaveDirectoryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string dir = SaveDirectoryTextBox.Text;
            if (System.IO.Directory.Exists(dir))
            {
                GlobalConsts.SaveDirectory = dir;
                SaveDirectoryTextBox.Background = null;
            }
            else
                SaveDirectoryTextBox.Background = GlobalConsts.ErrorBrush;

        }

        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            TextBox_MouseDoubleClick(sender, null);
        }


    }
}
