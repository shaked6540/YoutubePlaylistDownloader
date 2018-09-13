using Encryptor;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YoutubeExplode.Models;

namespace YoutubePlaylistDownloader
{
    static class GlobalConsts
    {
        //The const variables are variables that can be accessed from all over the solution.
        #region Const Variables

        public static Skeleton Current;
        public static AppTheme Theme;
        public static Accent Accent;
        public static Brush ErrorBrush;
        public static string Language;
        public static readonly string TempFolderPath;
        public static string SaveDirectory;
        public static readonly string CurrentDir;
        private static readonly string ConfigFilePath;
        private static readonly string ErrorFilePath;
        public const double VERSION = 0.9;
        public static bool UpdateOnExit;
        public static string UpdateSetupLocation;


        public static AppTheme Opposite { get { return Theme.Name == "BaseLight" ? ThemeManager.GetAppTheme("BaseDark") : ThemeManager.GetAppTheme("BaseLight"); } }

        #endregion

        static GlobalConsts()
        {
            CurrentDir = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.ToString();
            ConfigFilePath = CurrentDir + "\\TopSecretFile.dll";
            ErrorFilePath = CurrentDir + $"\\{Assembly.GetExecutingAssembly().GetName().Name}.log";
            ErrorBrush = Brushes.Crimson;
            Language = "English";
            SaveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            TempFolderPath = Path.GetTempPath() + "YoutubePlaylistDownloader\\";
            UpdateOnExit = false;
            UpdateSetupLocation = string.Empty;
        }

        //The const methods are used mainly for saving/loading consts, and handling page\menu management.
        #region Const Methods
        public static void HideSettingsButton()
        {
            Current.SettingsButton.Visibility = Visibility.Collapsed;
        }
        public static void ShowSettingsButton()
        {
            Current.SettingsButton.Visibility = Visibility.Visible;
        }
        public static void HideAboutButton()
        {
            Current.AboutButton.Visibility = Visibility.Collapsed;
        }
        public static void ShowAboutButton()
        {
            Current.AboutButton.Visibility = Visibility.Visible;
        }
        public static void HideHomeButton()
        {
            Current.HomeButton.Visibility = Visibility.Collapsed;
        }
        public static void ShowHomeButton()
        {
            Current.HomeButton.Visibility = Visibility.Visible;
        }
        public static async Task ShowMessage(string title, string message)
        {
            if (Current.DefaultFlyout.IsOpen)
                Current.DefaultFlyout.IsOpen = false;
            await Current.ShowMessage(title, message).ConfigureAwait(false);
        }
        public static async Task<MessageDialogResult> ShowYesNoDialog(string title, string message)
        {
            if (Current.DefaultFlyout.IsOpen)
                Current.DefaultFlyout.IsOpen = false;
            return await Current.ShowYesNoDialog(title, message).ConfigureAwait(false);
        }
        public static void LoadPage(UserControl page) => Current.CurrentPage.Content = page;
        public static void SaveConsts()
        {
            //Save the app settings for next use
            using (StreamWriter sw = new StreamWriter(ConfigFilePath, false))
            {
                sw.WriteLine(Theme.Name.Encrypt());
                sw.WriteLine(Accent.Name.Encrypt());
                sw.WriteLine(Language.Encrypt());
                sw.WriteLine(SaveDirectory.Encrypt());
            }

        }
        private static void RestoreDefualts()
        {
            Theme = ThemeManager.GetAppTheme("BaseLight");
            Accent = ThemeManager.GetAccent("Cobalt");
            Language = "English";

            SaveConsts();
        }
        public static void LoadConsts()
        {
            if (!File.Exists(ConfigFilePath))
            {
                RestoreDefualts();
                return;
            }


            var lines = File.ReadAllLines(ConfigFilePath);


            Theme = ThemeManager.GetAppTheme(lines[0].Decrypt());
            Accent = ThemeManager.GetAccent(lines[1].Decrypt());
            Language = lines[2].Decrypt();
            SaveDirectory = lines[3].Decrypt();

            UpdateTheme();
            UpdateLanguage();

        }
        public static void CreateTempFolder()
        {
            if (!Directory.Exists(Path.GetTempPath() + "YoutubePlaylistDownloader"))
                Directory.CreateDirectory(Path.GetTempPath() + "YoutubePlaylistDownloader");
            
        }
        public static void CleanTempFolder()
        {
            if (Directory.Exists(Path.GetTempPath() + "YoutubePlaylistDownloader"))
            {
                DirectoryInfo di = new DirectoryInfo(Path.GetTempPath() + "YoutubePlaylistDownloader");

                foreach (FileInfo file in di.GetFiles())
                    file.Delete();

                foreach (DirectoryInfo dir in di.GetDirectories())
                    dir.Delete(true);
            }
        }
        private static void UpdateTheme()
        {
            ThemeManager.ChangeAppStyle(Application.Current, Accent, Opposite);
            ThemeManager.ChangeAppTheme(Application.Current, Theme.Name);
        }
        private static void UpdateLanguage()
        {
            ResourceDictionary toRemove = Application.Current.Resources.MergedDictionaries.First(x => x.Source.OriginalString.Contains("English"));
            ResourceDictionary r = new ResourceDictionary()
            {
                Source = new Uri($"/Languages/{Language}.xaml", UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Add(r);
            Application.Current.Resources.MergedDictionaries.Remove(toRemove);
        }
        public static void ChangeLanguage(string nLang)
        {
            ResourceDictionary toRemove = Application.Current.Resources.MergedDictionaries.First(x => x.Source.OriginalString.Contains(Language));
            ResourceDictionary r = new ResourceDictionary()
            {
                Source = new Uri($"/Languages/{nLang}.xaml", UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Add(r);
            Application.Current.Resources.MergedDictionaries.Remove(toRemove);
            Language = nLang;
        }
        public static async Task Log(string message, object sender)
        {
            using (StreamWriter sw = new StreamWriter(ErrorFilePath, true))
            {
                await sw.WriteLineAsync($"[{DateTime.Now.ToString()}], [{sender}]:\t{message}");
            }
        }
        public static string CleanFileName(string filename)
        {
            var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            var invalidReStr = string.Format(@"[{0}]+", invalidChars);

            var reservedWords = new[]
            {
                "CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4",
                "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4",
                "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
            };

            var sanitisedNamePart = Regex.Replace(filename, invalidReStr, "_");
            foreach (var reservedWord in reservedWords)
            {
                var reservedWordPattern = string.Format("^{0}\\.", reservedWord);
                sanitisedNamePart = Regex.Replace(sanitisedNamePart, reservedWordPattern, "_reservedWord_.", RegexOptions.IgnoreCase);
            }

            return sanitisedNamePart;
        }
        public static async Task TagFile(Video video, int vIndex, string file, Playlist playlist = null)
        {
            var genre = video.Title.Split('[', ']').ElementAtOrDefault(1);


            if (genre == null)
                genre = string.Empty;

            else if (genre.Length >= video.Title.Length)
                genre = string.Empty;


            var title = video.Title;

            if (!string.IsNullOrWhiteSpace(genre))
            {
                title = video.Title.Replace($"[{genre}]", string.Empty);
                var rm = title.Split('[', ']', '【', '】').ElementAtOrDefault(1);
                if (!string.IsNullOrWhiteSpace(rm))
                    title = title.Replace($"[{rm}]", string.Empty);
            }
            title = title.TrimStart(' ', '-', '[', ']');

            var t = TagLib.File.Create(file);

            t.Tag.Album = playlist?.Title;
            t.Tag.Track = (uint)vIndex;
            t.Tag.Year = (uint)video.UploadDate.Year;
            t.Tag.DateTagged = video.UploadDate.UtcDateTime;
            t.Tag.AlbumArtists = new[] { playlist?.Author };
            var lowerGenre = genre.ToLower();
            if (new[] { "download", "out now", "mostercat", "video", "lyric", "release", "ncs" }.Any(x => lowerGenre.Contains(x)))
                genre = string.Empty;
            else
                t.Tag.Genres = genre.Split('/', '\\');
            
            try
            {
                TagLib.Id3v2.Tag.DefaultVersion = 3;
                TagLib.Id3v2.Tag.ForceDefaultVersion = true;
                var frame = TagLib.Id3v2.PopularimeterFrame.Get((TagLib.Id3v2.Tag)t.GetTag(TagLib.TagTypes.Id3v2, true), "WindowsUser", true);
                frame.Rating = Convert.ToByte((video.Statistics.LikeCount * 255) / (video.Statistics.LikeCount + video.Statistics.DislikeCount));
            }
            catch
            {

            }

            var index = title.LastIndexOf('-');
            if (index > 0)
            {
                var vTitle = title.Substring(index + 1).Trim(' ', '-');
                if (string.IsNullOrWhiteSpace(vTitle))
                {
                    index = title.IndexOf('-');
                    if (index > 0)
                        vTitle = title.Substring(index + 1).Trim(' ', '-');
                }
                t.Tag.Title = vTitle;
                t.Tag.Performers = title.Substring(0, index - 1).Trim().Split(new string[] { "&", "feat.", "feat", "ft.", " ft ", "Feat.", " x ", " X " }, StringSplitOptions.RemoveEmptyEntries);
            }

            try
            {
                var picLoc = $"{TempFolderPath}{CleanFileName(video.Title)}.jpg";
                using (var wb = new WebClient())
                    File.WriteAllBytes(picLoc, await wb.DownloadDataTaskAsync($"https://img.youtube.com/vi/{video.Id}/0.jpg").ConfigureAwait(false));

                t.Tag.Pictures = new TagLib.IPicture[] { new TagLib.Picture(picLoc) };
            }
            catch { }

            t.Save();
        }

        #endregion


    }
}
