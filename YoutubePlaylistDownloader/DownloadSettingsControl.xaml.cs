using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YoutubeExplode.Models.MediaStreams;

namespace YoutubePlaylistDownloader
{
    /// <summary>
    /// Interaction logic for DownloadSettings.xaml
    /// </summary>
    public partial class DownloadSettingsControl : UserControl
    {
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

        private readonly Dictionary<string, string> Languages = new Dictionary<string, string>() { { "aa", "Afar" }, { "ab", "Abkhazian" }, { "af", "Afrikaans" }, { "ak", "Akan" }, { "sq", "Albanian" }, { "am", "Amharic" }, { "ar", "Arabic" }, { "an", "Aragonese" }, { "hy", "Armenian" }, { "as", "Assamese" }, { "av", "Avaric" }, { "ae", "Avestan" }, { "ay", "Aymara" }, { "az", "Azerbaijani" }, { "ba", "Bashkir" }, { "bm", "Bambara" }, { "eu", "Basque" }, { "be", "Belarusian" }, { "bn", "Bengali" }, { "bh", "Bihari languages" }, { "bi", "Bislama" }, { "bs", "Bosnian" }, { "br", "Breton" }, { "bg", "Bulgarian" }, { "my", "Burmese" }, { "ca", "Catalan" }, { "ch", "Chamorro" }, { "ce", "Chechen" }, { "zh", "Chinese" }, { "cu", "Church Slavic" }, { "cv", "Chuvash" }, { "kw", "Cornish" }, { "co", "Corsican" }, { "cr", "Cree" }, { "cs", "Czech" }, { "da", "Danish" }, { "dv", "Divehi" }, { "nl", "Dutch" }, { "dz", "Dzongkha" }, { "en", "English" }, { "eo", "Esperanto" }, { "et", "Estonian" }, { "ee", "Ewe" }, { "fo", "Faroese" }, { "fj", "Fijian" }, { "fi", "Finnish" }, { "fr", "French" }, { "fy", "Western Frisian" }, { "ff", "Fulah" }, { "ka", "Georgian" }, { "de", "German" }, { "gd", "Gaelic" }, { "ga", "Irish" }, { "gl", "Galician" }, { "gv", "Manx" }, { "el", "Greek" }, { "gn", "Guarani" }, { "gu", "Gujarati" }, { "ht", "Haitian" }, { "ha", "Hausa" }, { "he", "Hebrew" }, { "hz", "Herero" }, { "hi", "Hindi" }, { "ho", "Hiri Motu" }, { "hr", "Croatian" }, { "hu", "Hungarian" }, { "ig", "Igbo" }, { "is", "Icelandic" }, { "io", "Ido" }, { "ii", "Sichuan Yi" }, { "iu", "Inuktitut" }, { "ie", "Interlingue" }, { "ia", "Interlingua" }, { "id", "Indonesian" }, { "ik", "Inupiaq" }, { "it", "Italian" }, { "jv", "Javanese" }, { "ja", "Japanese" }, { "kl", "Kalaallisut" }, { "kn", "Kannada" }, { "ks", "Kashmiri" }, { "kr", "Kanuri" }, { "kk", "Kazakh" }, { "km", "Central Khmer" }, { "ki", "Kikuyu" }, { "rw", "Kinyarwanda" }, { "ky", "Kirghiz" }, { "kv", "Komi" }, { "kg", "Kongo" }, { "ko", "Korean" }, { "kj", "Kuanyama" }, { "ku", "Kurdish" }, { "lo", "Lao" }, { "la", "Latin" }, { "lv", "Latvian" }, { "li", "Limburgan" }, { "ln", "Lingala" }, { "lt", "Lithuanian" }, { "lb", "Luxembourgish" }, { "lu", "Luba-Katanga" }, { "lg", "Ganda" }, { "mk", "Macedonian" }, { "mh", "Marshallese" }, { "ml", "Malayalam" }, { "mi", "Maori" }, { "mr", "Marathi" }, { "ms", "Malay" }, { "mg", "Malagasy" }, { "mt", "Maltese" }, { "mn", "Mongolian" }, { "na", "Nauru" }, { "nv", "Navajo" }, { "nr", "Ndebele, South" }, { "nd", "Ndebele, North" }, { "ng", "Ndonga" }, { "ne", "Nepali" }, { "nn", "Norwegian" }, { "nb", "Bokmål" }, { "no", "Norwegian" }, { "ny", "Chichewa" }, { "oc", "Occitan" }, { "oj", "Ojibwa" }, { "or", "Oriya" }, { "om", "Oromo" }, { "os", "Ossetian" }, { "pa", "Panjabi" }, { "fa", "Persian" }, { "pi", "Pali" }, { "pl", "Polish" }, { "pt", "Portuguese" }, { "ps", "Pushto" }, { "qu", "Quechua" }, { "rm", "Romansh" }, { "ro", "Romanian" }, { "rn", "Rundi" }, { "ru", "Russian" }, { "sg", "Sango" }, { "sa", "Sanskrit" }, { "si", "Sinhala" }, { "sk", "Slovak" }, { "sl", "Slovenian" }, { "se", "Northern Sami" }, { "sm", "Samoan" }, { "sn", "Shona" }, { "sd", "Sindhi" }, { "so", "Somali" }, { "st", "Sotho, Southern" }, { "es", "Spanish" }, { "sc", "Sardinian" }, { "sr", "Serbian" }, { "ss", "Swati" }, { "su", "Sundanese" }, { "sw", "Swahili" }, { "sv", "Swedish" }, { "ty", "Tahitian" }, { "ta", "Tamil" }, { "tt", "Tatar" }, { "te", "Telugu" }, { "tg", "Tajik" }, { "tl", "Tagalog" }, { "th", "Thai" }, { "bo", "Tibetan" }, { "ti", "Tigrinya" }, { "to", "Tonga (Tonga Islands)" }, { "tn", "Tswana" }, { "ts", "Tsonga" }, { "tk", "Turkmen" }, { "tr", "Turkish" }, { "tw", "Twi" }, { "ug", "Uighur" }, { "uk", "Ukrainian" }, { "ur", "Urdu" }, { "uz", "Uzbek" }, { "ve", "Venda" }, { "vi", "Vietnamese" }, { "vo", "Volapük" }, { "cy", "Welsh" }, { "wa", "Walloon" }, { "wo", "Wolof" }, { "xh", "Xhosa" }, { "yi", "Yiddish" }, { "yo", "Yoruba" }, { "za", "Zhuang" }, { "zu", "Zulu" }, };

        public DownloadSettingsControl()
        {
            InitializeComponent();

            ResulotionDropDown.ItemsSource = Resolutions.Keys;
            ExtensionsDropDown.ItemsSource = FileTypes;
            CaptionsLanguagesComboBox.ItemsSource = Languages.Values;

            var settings = GlobalConsts.DownloadSettings;
            ExtensionsDropDown.SelectedItem = settings.SaveFormat;
            ResulotionDropDown.SelectedItem = Resolutions.FirstOrDefault(x => x.Value == settings.Quality).Key;
            PreferCheckBox.IsChecked = settings.PreferQuality;
            PreferHighestFPSCheckBox.IsChecked = settings.PreferHighestFPS;
            ConvertCheckBox.IsChecked = settings.Convert;
            BitrateCheckBox.IsChecked = settings.SetBitrate;
            BitRateTextBox.Text = string.IsNullOrWhiteSpace(settings.Bitrate) ? "192" : settings.Bitrate;
            CaptionsCheckBox.IsChecked = settings.DownloadCaptions;
            CaptionsLanguagesComboBox.SelectedItem = Languages[settings.CaptionsLanguage ?? "en"];
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            bool convert = ConvertCheckBox.IsChecked.Value;
            bool preferQuality = PreferCheckBox.IsChecked.Value;
            bool setBitrate = BitrateCheckBox.IsChecked.Value;
            var vq = VideoQuality.High720;
            string type = "mp3";
            string bitrate = null;
            bool audioOnly = AudioOnlyCheckBox.IsChecked.Value;
            bool preferHighestFPS = PreferHighestFPSCheckBox.IsChecked.Value;
            bool downloadCaptions = CaptionsCheckBox.IsChecked.Value;
            string captionsLanguage = Languages.FirstOrDefault(x => x.Value.Equals((string)CaptionsLanguagesComboBox.SelectedItem, StringComparison.OrdinalIgnoreCase)).Key;


            if (BitrateCheckBox.IsChecked.Value)
                if (!string.IsNullOrWhiteSpace(BitRateTextBox.Text) && BitRateTextBox.Text.All(c => char.IsDigit(c)))
                    bitrate = BitRateTextBox.Text;


            if (PreferCheckBox.IsChecked.Value)
                vq = Resolutions[(string)ResulotionDropDown.SelectedValue];

            if (convert)
                type = (string)ExtensionsDropDown.SelectedItem;



            GlobalConsts.DownloadSettings = new Objects.DownloadSettings(type, audioOnly, vq, preferHighestFPS, preferQuality, convert, setBitrate, bitrate, downloadCaptions, captionsLanguage);
            GlobalConsts.CloseFlyout();
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
    }
}
