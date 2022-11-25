using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubePlaylistDownloader.Objects;
using YoutubePlaylistDownloader.Utilities;

namespace YoutubePlaylistDownloader
{
    static class GlobalConsts
    {
        #region Const Variables
        public static Skeleton Current;
        public static MainPage MainPage;
        public static AppTheme Theme;
        public static Accent Accent;
        public static Brush ErrorBrush;
        public static readonly WebClient WebClient;
        public static string Language;
        public static readonly string TempFolderPath;
        public static string SaveDirectory;
        public static readonly string CurrentDir;
        public static readonly string FFmpegFilePath;
        private static readonly string ConfigFilePath;
        private static readonly string ErrorFilePath;
        public static readonly Version VERSION = new Version(1, 9, 0);
        public static bool UpdateOnExit;
        public static string UpdateSetupLocation;
        public static bool OptionExpanderIsExpanded;
        public static bool UpdateFinishedDownloading;
        public static bool UpdateLater;
        public static DownloadUpdate UpdateControl;
        public static readonly string ChannelSubscriptionsFilePath;
        private static bool checkForSubscriptionUpdates;
        public static bool CheckForProgramUpdates;
        public static TimeSpan SubscriptionsUpdateDelay;
        private static DownloadSettings downloadSettings;
        public static bool SaveDownloadOptions;
        public static readonly string DownloadSettingsFilePath;
        public static readonly ObservableCollection<QueuedDownload> Downloads;
        public static bool LimitConvertions;
        public static int MaximumConverstionsCount, ActualConvertionsLimit;
        private static SemaphoreSlim convertionLocker;
        public static bool ConfirmExit;

        public static bool CheckForSubscriptionUpdates
        {
            get => checkForSubscriptionUpdates;
            set
            {
                checkForSubscriptionUpdates = value;
                if (checkForSubscriptionUpdates)
                    SubscriptionManager.UpdateAllSubscriptions();
                else
                    SubscriptionManager.CancelAll();
            }

        }
        public static AppTheme Opposite { get { return Theme.Name == "BaseLight" ? ThemeManager.GetAppTheme("BaseDark") : ThemeManager.GetAppTheme("BaseLight"); } }
        public static YoutubeClient YoutubeClient { get => new YoutubeClient(); }
        public static SemaphoreSlim ConversionsLocker { get => convertionLocker; set { if (convertionLocker == null) convertionLocker = value; } }
        public static DownloadSettings DownloadSettings
        {
            get
            {
                if (downloadSettings == null)
                    downloadSettings = new DownloadSettings("mp3", false, YoutubeHelpers.High720, false, false, false, false, "192", false, "en", false, false, 0, 0, false, true, false, true, 4);

                return downloadSettings;
            }
            set
            {
                if (value != null)
                {
                    downloadSettings = value;

                    if (SaveDownloadOptions)
                        File.WriteAllText(DownloadSettingsFilePath, Newtonsoft.Json.JsonConvert.SerializeObject(downloadSettings));
                }
            }
        }

        #endregion

        static GlobalConsts()
        {
            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new VideoQualityConverter());
                return settings;
            };
            Downloads = new ObservableCollection<QueuedDownload>();
            CurrentDir = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.ToString();
            FFmpegFilePath = $"{CurrentDir}\\ffmpeg.exe";
            string appDataPath = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "\\Youtube Playlist Downloader\\");
            ConfigFilePath = string.Concat(appDataPath, "Settings.json");
            ErrorFilePath = string.Concat(appDataPath, "Errors.txt");
            DownloadSettingsFilePath = string.Concat(appDataPath, "DownloadSettings.json");
            ChannelSubscriptionsFilePath = string.Concat(appDataPath, "Subscriptions.ypds");

            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);

            ErrorBrush = Brushes.Crimson;
            Language = "English";
            SaveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            TempFolderPath = string.Concat(Path.GetTempPath(), "YoutubePlaylistDownloader\\");
            UpdateOnExit = false;
            UpdateLater = false;
            UpdateSetupLocation = string.Empty;
            WebClient = new WebClient
            {
                CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore)
            };
            SubscriptionsUpdateDelay = TimeSpan.FromMinutes(1);
            checkForSubscriptionUpdates = false;
            Downloads.CollectionChanged += Downloads_CollectionChanged;
        }

        //The const methods are used mainly for saving/loading consts, and handling page\menu management.
        #region Const Methods

        #region Buttons
        public static void HideSubscriptionsButton()
        {
            Current.SubscriptionsButton.Visibility = Visibility.Collapsed;
        }
        public static void HideHelpButton()
        {
            Current.HelpButton.Visibility = Visibility.Collapsed;
        }
        public static void HideHomeButton()
        {
            Current.HomeButton.Visibility = Visibility.Collapsed;
        }
        public static void HideAboutButton()
        {
            Current.AboutButton.Visibility = Visibility.Collapsed;
        }
        public static void HideSettingsButton()
        {
            Current.SettingsButton.Visibility = Visibility.Collapsed;
        }
        public static void ShowSettingsButton()
        {
            Current.SettingsButton.Visibility = Visibility.Visible;
        }
        public static void ShowHelpButton()
        {
            Current.HelpButton.Visibility = Visibility.Visible;
        }
        public static void ShowAboutButton()
        {
            Current.AboutButton.Visibility = Visibility.Visible;
        }
        public static void ShowHomeButton()
        {
            Current.HomeButton.Visibility = Visibility.Visible;
        }
        public static void ShowSubscriptionsButton()
        {
            Current.SubscriptionsButton.Visibility = Visibility.Visible;
        }
        #endregion

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
        public async static Task<MessageDialogResult> CustomYesNoDialog(string title, string message, MetroDialogSettings dialogSettings)
        {
            if (Current.DefaultFlyout.IsOpen)
                Current.DefaultFlyout.IsOpen = false;
            return await Current.CustomYesNoDialog(title, message, dialogSettings).ConfigureAwait(false);
        }
        public static void LoadPage(UserControl page) => Current.CurrentPage.Content = page;
        public static void SaveConsts()
        {
            try
            {
                var settings = new Objects.Settings(Theme.Name, Accent.Name, Language, SaveDirectory, OptionExpanderIsExpanded, CheckForSubscriptionUpdates, CheckForProgramUpdates, SubscriptionsUpdateDelay, SaveDownloadOptions, MaximumConverstionsCount, ActualConvertionsLimit, LimitConvertions, ConfirmExit);
                File.WriteAllText(ConfigFilePath, Newtonsoft.Json.JsonConvert.SerializeObject(settings));
                SubscriptionManager.SaveSubscriptions();
                SaveDownloadSettings();
            }
            catch (Exception ex)
            {
                Log(ex.ToString(), "SaveConsts").Wait();
            }
        }
        public static void RestoreDefualts()
        {
            Log("Restoring defaults", "RestoreDefaults at GlobalConsts").Wait();

            Theme = ThemeManager.GetAppTheme("BaseDark");
            Accent = ThemeManager.GetAccent("Red");
            Language = "English";
            OptionExpanderIsExpanded = true;
            checkForSubscriptionUpdates = false;
            CheckForProgramUpdates = true;
            SubscriptionsUpdateDelay = TimeSpan.FromMinutes(1);
            SaveDownloadOptions = true;
            MaximumConverstionsCount = 20;
            ActualConvertionsLimit = 2;
            LimitConvertions = true;

            DownloadSettings = new DownloadSettings("mp3", false, YoutubeHelpers.High720, false, false, false, false, "192", false, "en", false, false, 0, 0, false, true, false, true, 4);
            SaveConsts();
        }
        public static void LoadConsts()
        {

            if (!File.Exists(ConfigFilePath))
            {
                Log("Config file does not exist, restoring defaults", "LoadConsts at GlobalConsts").Wait();

                RestoreDefualts();
                return;
            }

            try
            {
                var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Objects.Settings>(File.ReadAllText(ConfigFilePath));
                Theme = ThemeManager.GetAppTheme(settings.Theme);
                Accent = ThemeManager.GetAccent(settings.Accent);
                Language = settings.Language;
                SaveDirectory = settings.SaveDirectory;
                OptionExpanderIsExpanded = settings.OptionExpanderIsExpanded;
                CheckForSubscriptionUpdates = settings.CheckForSubscriptionUpdates;
                CheckForProgramUpdates = settings.CheckForProgramUpdates;
                SubscriptionsUpdateDelay = settings.SubscriptionsDelay;
                SaveDownloadOptions = settings.SaveDownloadOptions;
                MaximumConverstionsCount = settings.MaximumConverstionsCount;
                ActualConvertionsLimit = settings.ActualConvertionsLimit;
                LimitConvertions = settings.LimitConvertions;
                ConfirmExit = settings.ConfirmExit;

                ConversionsLocker = new SemaphoreSlim(ActualConvertionsLimit, MaximumConverstionsCount);

                LoadDownloadSettings();
            }
            catch (Exception ex)
            {
                Log(ex.ToString(), "LoadConsts at GlobalConsts").Wait();
                RestoreDefualts();
            }
            UpdateTheme();
            UpdateLanguage();

        }
        public static void CreateTempFolder()
        {
            try
            {
                if (!Directory.Exists(Path.GetTempPath() + "YoutubePlaylistDownloader"))
                    Directory.CreateDirectory(Path.GetTempPath() + "YoutubePlaylistDownloader");
            }
            catch (Exception ex)
            {
                Log($"Failed to create temp folder, {ex}", "CreateTempFolder at GlobalConsts").Wait();
            }

        }
        public static void CleanTempFolder()
        {
            if (Directory.Exists(Path.GetTempPath() + "YoutubePlaylistDownloader"))
            {
                DirectoryInfo di = new DirectoryInfo(Path.GetTempPath() + "YoutubePlaylistDownloader");

                foreach (FileInfo file in di.GetFiles())
                    try { file.Delete(); } catch { };

                foreach (DirectoryInfo dir in di.GetDirectories())
                    try { dir.Delete(true); } catch { };
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
            using StreamWriter sw = new StreamWriter(ErrorFilePath, true);
            await sw.WriteLineAsync($"[{DateTime.Now.ToUniversalTime()}], [{sender}]:\n\n{message}\n\n").ConfigureAwait(false);

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
        public static async Task TagFile(PlaylistVideo video, int vIndex, string file, FullPlaylist playlist = null)
        {
            if (video == null)
                throw new ArgumentNullException($"{nameof(video)} was null, can't tag a file without a video title");

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

            t.Tag.Album = playlist?.BasePlaylist?.Title;
            t.Tag.Track = (uint)vIndex;
            //t.Tag.Year = (uint)video.UploadDate.Year;
            ///t.Tag.DateTagged = video.UploadDate.UtcDateTime;
            t.Tag.AlbumArtists = new[] { playlist?.BasePlaylist?.Author.Title };
            var lowerGenre = genre.ToLower();
            if (new[] { "download", "out now", "mostercat", "video", "lyric", "release", "ncs" }.Any(x => lowerGenre.Contains(x)))
                genre = string.Empty;
            else
                t.Tag.Genres = genre.Split('/', '\\');

            //try
            //{
            //    TagLib.Id3v2.Tag.DefaultVersion = 3;
            //    TagLib.Id3v2.Tag.ForceDefaultVersion = true;
            //    var frame = TagLib.Id3v2.PopularimeterFrame.Get((TagLib.Id3v2.Tag)t.GetTag(TagLib.TagTypes.Id3v2, true), "WindowsUser", true);
            //    frame.Rating = Convert.ToByte((video.Engagement.LikeCount * 255) / (video.Engagement.LikeCount + video.Engagement.DislikeCount));
            //}
            //catch
            //{

            //}

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
        public static void LoadFlyoutPage(UserControl page)
        {
            Current.DefaultFlyoutUserControl.Content = page;
            Current.DefaultFlyout.IsOpen = true;
        }
        public static void CloseFlyout()
        {
            Current.DefaultFlyout.IsOpen = false;
            Current.DefaultFlyoutUserControl.Content = null;
        }
        public static double GetOffset()
        {
            return Current.ActualHeight - 95;
        }
        private static void LoadDownloadSettings()
        {
            if (File.Exists(DownloadSettingsFilePath))
            {
                try
                {
                    downloadSettings = Newtonsoft.Json.JsonConvert.DeserializeObject<DownloadSettings>(File.ReadAllText(DownloadSettingsFilePath));
                }
                catch (Exception ex)
                {
                    Log(ex.ToString(), "LoadDownloadSettings at GlobalConsts").Wait();
                    try
                    {
                        if (File.Exists(DownloadSettingsFilePath))
                            File.Delete(DownloadSettingsFilePath);
                    }
                    catch(Exception ex2)
                    {
                        Log(ex2.ToString(), "Delete download settings file path").Wait();
                    }
                    downloadSettings = new DownloadSettings("mp3", false, YoutubeHelpers.High720, false, false, false, false, "192", false, "en", false, false, 0, 0, false, true, false, true, 4);
                }
            }
            else
            {
                downloadSettings = new DownloadSettings("mp3", false, YoutubeHelpers.High720, false, false, false, false, "192", false, "en", false, false, 0, 0, false, true, false, true, 4);
            }
        }
        public static void SaveDownloadSettings()
        {
            try
            {
                File.WriteAllText(DownloadSettingsFilePath, Newtonsoft.Json.JsonConvert.SerializeObject(downloadSettings));
            }
            catch (Exception ex)
            {
                Log(ex.ToString(), "SaveDownloadSettings at GlobalConsts").Wait();
            }
        }
        private static void Downloads_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                foreach (QueuedDownload item in e.NewItems)
                    MainPage.QueueStackPanel.Children.Add(item?.GetDisplayGrid());

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                foreach (QueuedDownload item in e.OldItems)
                {
                    MainPage.QueueStackPanel.Children.Remove(item?.GetDisplayGrid());
                    item?.Dispose();
                }
        }
        #endregion

    }
}
