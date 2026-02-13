namespace YoutubePlaylistDownloader;

static class GlobalConsts
{
    #region Const Variables
    public static Skeleton Current;
    public static MainPage MainPage;
    public static System.Windows.Media.Brush ErrorBrush;
    public static readonly string TempFolderPath;
    //public static string SaveDirectory;
    public static readonly string CurrentDir;
    public static readonly string FFmpegFilePath;
    private static readonly string ConfigFilePath;
    private static readonly string ErrorFilePath;
    public static readonly Version VERSION = new(1, 9, 33);
    public static bool UpdateOnExit;
    public static string UpdateSetupLocation;
    public static bool UpdateFinishedDownloading;
    public static bool UpdateLater;
    public static DownloadUpdate UpdateControl;
    public static readonly string ChannelSubscriptionsFilePath;
    public static TimeSpan SubscriptionsUpdateDelay;
    private static DownloadSettings downloadSettings;
    public static readonly string DownloadSettingsFilePath;
    public static readonly ObservableCollection<QueuedDownload> Downloads;
    private static SemaphoreSlim conversionLocker;
    public static Objects.Settings settings;

    public static string OppositeTheme => settings.Theme == "Light" ? "Dark" : "Light";
    public static YoutubeClient YoutubeClient => new();
    public static SemaphoreSlim ConversionsLocker { get => conversionLocker; set => conversionLocker ??= value; }
    public static DownloadSettings DownloadSettings
    {
        get
        {
            downloadSettings ??= new DownloadSettings("mp3", false, YoutubeHelpers.High720, false, false, false, false, "192", false, "en", false, false, 0, 0, false, true, false, true, 4, "$title", false, "mkv", "default");
            return downloadSettings;
        }
        set
        {
            if (value != null)
            {
                downloadSettings = value;

                if (settings.SaveDownloadOptions)
                    File.WriteAllText(DownloadSettingsFilePath, JsonConvert.SerializeObject(downloadSettings));
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
        Downloads = [];
        CurrentDir = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.ToString();
        FFmpegFilePath = $"{CurrentDir}\\ffmpeg.exe";
        var appDataPath = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "\\Youtube Playlist Downloader\\");
        ConfigFilePath = string.Concat(appDataPath, "Settings.json");
        ErrorFilePath = string.Concat(appDataPath, "Errors.txt");
        DownloadSettingsFilePath = string.Concat(appDataPath, "DownloadSettings.json");
        ChannelSubscriptionsFilePath = string.Concat(appDataPath, "Subscriptions.ypds");

        if (!Directory.Exists(appDataPath))
            Directory.CreateDirectory(appDataPath);

        ErrorBrush = Brushes.Crimson;
        settings = new()
        {
            Language = "English"
        };
        TempFolderPath = string.Concat(Path.GetTempPath(), "YoutubePlaylistDownloader\\");
        UpdateOnExit = false;
        UpdateLater = false;
        UpdateSetupLocation = string.Empty;
        SubscriptionsUpdateDelay = TimeSpan.FromMinutes(1);
        Downloads.CollectionChanged += Downloads_CollectionChanged;
    }

    //The const methods are used mainly for saving/loading consts, and handling page\menu management.
    #region Const Methods

    #region Buttons
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
    public static Task ShowSelectableDialog(string title, string message, Action retryAction)
    {
        if (Current.DefaultFlyout.IsOpen)
            Current.DefaultFlyout.IsOpen = false;
        return Current.ShowSelectableDialog(title, message, retryAction);
    }
    public static void LoadPage(UserControl page) => Current.CurrentPage.Content = page;
    public static void SaveConsts()
    {
        try
        {
            File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(settings));
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
        settings = new Objects.Settings("Dark", "Red", "English", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), false, false, true, TimeSpan.FromMinutes(1), true, 20, 2, true, true);
        DownloadSettings = new DownloadSettings("mp3", false, YoutubeHelpers.High720, false, false, false, false, "192", false, "en", false, false, 0, 0, false, true, false, true, 4, "$title", false, "mkv", "default");
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
            settings = JsonConvert.DeserializeObject<Objects.Settings>(File.ReadAllText(ConfigFilePath));
            ConversionsLocker = new SemaphoreSlim(settings.ActualConversionsLimit, settings.MaximumConversionsCount);

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
            DirectoryInfo di = new(Path.GetTempPath() + "YoutubePlaylistDownloader");

            foreach (var file in di.GetFiles())
                try { file.Delete(); } catch { };

            foreach (var dir in di.GetDirectories())
                try { dir.Delete(true); } catch { };
        }
    }
    private static void UpdateTheme()
    {
        try
        {
            ThemeManager.Current.ChangeTheme(Application.Current, $"{OppositeTheme}.{settings.Accent}");
            ThemeManager.Current.ChangeTheme(Application.Current, $"{settings.Theme}.{settings.Accent}");
        }
        catch (Exception ex)
        {
            RestoreDefualts();
            Log(ex.ToString(), "UpdateTheme").ConfigureAwait(false);
        }
    }
    private static void UpdateLanguage()
    {
        var toRemove = Application.Current.Resources.MergedDictionaries.First(x => x.Source.OriginalString.Contains("English"));
        ResourceDictionary r = new()
        {
            Source = new Uri($"/Languages/{settings.Language}.xaml", UriKind.Relative)
        };
        Application.Current.Resources.MergedDictionaries.Add(r);
        Application.Current.Resources.MergedDictionaries.Remove(toRemove);
    }
    public static void ChangeLanguage(string nLang)
    {
        var toRemove = Application.Current.Resources.MergedDictionaries.First(x => x.Source?.OriginalString.Contains(settings.Language) ?? false);
        ResourceDictionary r = new()
        {
            Source = new Uri($"/Languages/{nLang}.xaml", UriKind.Relative)
        };
        Application.Current.Resources.MergedDictionaries.Add(r);
        Application.Current.Resources.MergedDictionaries.Remove(toRemove);
        settings.Language = nLang;
    }
    public static async Task Log(string message, object sender)
    {
        using StreamWriter sw = new(ErrorFilePath, true);
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

    static void CropAndSaveImage(byte[] imageBytes, string imagePath)
    {
        using var imageBuffer = new MemoryStream(imageBytes);
        using var image = System.Drawing.Image.FromStream(imageBuffer);
        var cropRectangle = new Rectangle((image.Width - image.Height) / 2, 0, image.Height, image.Height);
        using var bitmap = new Bitmap(cropRectangle.Width, cropRectangle.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.DrawImage(image, new Rectangle(0, 0, bitmap.Width, bitmap.Height), cropRectangle, GraphicsUnit.Pixel);
        bitmap.Save(imagePath, ImageFormat.Jpeg);
    }

    static readonly string[] ignoredComments = ["Auto-generated by YouTube.", "Provided to YouTube by"];
    internal static readonly string[] IgnoredGeneres = ["download", "out now", "mostercat", "video", "lyric", "release", "ncs", "records"];
    internal static readonly string[] ArtistsSeparators = ["&", "feat.", "feat", "ft.", " ft ", "Feat.", " x ", " X "];
    internal static readonly string[] VideoTitleSeparators = [" - ", " — "];

    static async Task<string> TagMusicFile(Video fullVideo, string file, int vIndex)
    {
        // Index YouTube Auto Generated Description
        var description = fullVideo.Description.Split("\n");
        var title = string.Empty;
        var artists = new List<string>();
        var album = string.Empty;
        var releaseDate = default(DateTime);
        var comment = new StringBuilder();
        var commentIndex = 0;

        try
        {
            foreach (var line in description)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    goto loopEnd;
                }

                if (line.Contains('·'))
                {
                    var titleAndArtists = line.Split('·').Select(x => x.Trim());

                    title = titleAndArtists.FirstOrDefault();
                    artists.AddRange(titleAndArtists.Skip(1));
                    album = description.ElementAtOrDefault(commentIndex + 2);

                    if (album != null && !album.Contains("Auto-generated by YouTube.") && !album.StartsWith("Released on:"))
                    {
                        goto loopEnd;
                    }
                    else
                    {
                        album = string.Empty;
                    }
                }

                if (line.StartsWith("Released on:"))
                {
                    releaseDate = DateTime.Parse(line.Split(":").ElementAtOrDefault(1));
                    goto loopEnd;
                }

                if (!string.IsNullOrWhiteSpace(album) && line.StartsWith(album))
                {
                    goto loopEnd;
                }

                if (ignoredComments.Any(x => line.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
                {
                    goto loopEnd;
                }

                comment.AppendLine(line);

            loopEnd:
                commentIndex++;
            }
        }
        catch (Exception e)
        {
            await Log(e.ToString(), "TagMusicFile inside description loop").ConfigureAwait(false);
            return null;
        }

        if (releaseDate == default)
        {
            releaseDate = fullVideo.UploadDate.Date;
        }

        using (var tagLibFile = TagLib.File.Create(file))
        {
            tagLibFile.Tag.Title = title;
            tagLibFile.Tag.Performers = [.. artists];
            tagLibFile.Tag.Year = (uint)releaseDate.Year;
            tagLibFile.Tag.Comment = comment.ToString();
            tagLibFile.Tag.Album = album;
            tagLibFile.Tag.Track = (uint)vIndex;

            try
            {
                var picturePath = $"{TempFolderPath}{CleanFileName(fullVideo.Title)}.jpg";

                using (var httpClient = new HttpClient())
                {
                    var pictureContent = await httpClient.GetByteArrayAsync($"https://img.youtube.com/vi/{fullVideo.Id}/maxresdefault.jpg").ConfigureAwait(false);
                    CropAndSaveImage(pictureContent, picturePath);
                }

                tagLibFile.Tag.Pictures = [new TagLib.Picture(picturePath)];
            }
            catch (Exception ex)
            {
                await Log("Failed to add picture to file at TagMusicFile", ex.ToString()).ConfigureAwait(false);
            }

            tagLibFile.Save();
        }

        return $"{string.Join(", ", artists)} - {title}";
    }

    public static async Task<string> TagFileBasedOnTitle(IVideo video, int index, string file, FullPlaylist playlist = null)
    {
        var title = video.Title.Replace("—", "-");
        var genre = title.Split('[', ']').ElementAtOrDefault(1);

        if (genre == null)
        {
            genre = string.Empty;
        }
        else if (genre.Length >= title.Length)
        {
            genre = string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            title = title.Replace($"[{genre}]", string.Empty);
            var stringToRemove = title.Split('[', ']', '【', '】').ElementAtOrDefault(1);

            if (!string.IsNullOrWhiteSpace(stringToRemove))
            {
                title = title.Replace($"[{stringToRemove}]", string.Empty);
            }
        }

        title = title.TrimStart(' ', '-', '[', ']').TrimEnd();

        using (var tagLibFile = TagLib.File.Create(file))
        {
            tagLibFile.Tag.Album = playlist?.BasePlaylist?.Title;
            tagLibFile.Tag.Track = (uint)index;
            tagLibFile.Tag.AlbumArtists = [playlist?.BasePlaylist?.Author?.ChannelTitle];

            if (IgnoredGeneres.Any(genre.ToLower().Contains))
            {
                genre = string.Empty;
            }
            else
            {
                tagLibFile.Tag.Genres = genre.Split('/', '\\');
            }

            if (TryGetSongTitleAndPerformersFromTitle(title, out string songTitle, out string[] songPerformers))
            {
                tagLibFile.Tag.Title = songTitle;
                tagLibFile.Tag.Performers = songPerformers;
            }

            try
            {
                var picturePath = $"{TempFolderPath}{CleanFileName(video.Title)}.jpg";

                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync($"https://img.youtube.com/vi/{video.Id}/maxresdefault.jpg").ConfigureAwait(false);

                    using (var pictureStream = File.Create(picturePath))
                    {
                        await response.Content.CopyToAsync(pictureStream).ConfigureAwait(false);
                    }
                }

                tagLibFile.Tag.Pictures = [new TagLib.Picture(picturePath)];
            }
            catch (Exception ex)
            {
                await Log("Failed to add picture to file at TagFileBasedOnTitle", ex.ToString()).ConfigureAwait(false);
            }

            tagLibFile.Save();
        }

        return file;
    }

    public static bool TryGetSongTitleAndPerformersFromTitle(string title, out string songTitle, out string[] songPerformers)
    {
        songTitle = null;
        songPerformers = null;

        var index = title.LastIndexOf('-');

        if (index > 0)
        {
            songTitle = title[(index + 1)..].Trim(' ', '-');

            if (string.IsNullOrWhiteSpace(songTitle))
            {
                index = title.IndexOf('-');

                if (index > 0)
                {
                    songTitle = title[(index + 1)..].Trim(' ', '-');
                }
            }

            songPerformers = title[..(index - 1)].Trim().Split(ArtistsSeparators, StringSplitOptions.RemoveEmptyEntries);

            return true;
        }

        return false;
    }

    public static async Task<string> TagFile(IVideo video, int vIndex, string file, FullPlaylist playlist = null)
    {
        ArgumentNullException.ThrowIfNull(video);

        if (!VideoTitleSeparators.Any(video.Title.Contains))
        {
            var fullVideo = await YoutubeClient.Videos.GetAsync(video.Id).ConfigureAwait(false);

            if (fullVideo.Description.Contains("Auto-generated by YouTube."))
            {
                var fileName = await TagMusicFile(fullVideo, file, vIndex);

                if (fileName != null)
                {
                    return fileName;
                }
            }
        }

        return await TagFileBasedOnTitle(video, vIndex, file, playlist);
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
                downloadSettings = JsonConvert.DeserializeObject<DownloadSettings>(File.ReadAllText(DownloadSettingsFilePath));
            }
            catch (Exception ex)
            {
                Log(ex.ToString(), "LoadDownloadSettings at GlobalConsts").Wait();
                try
                {
                    if (File.Exists(DownloadSettingsFilePath))
                        File.Delete(DownloadSettingsFilePath);
                }
                catch (Exception ex2)
                {
                    Log(ex2.ToString(), "Delete download settings file path").Wait();
                }
                downloadSettings = new DownloadSettings("mp3", false, YoutubeHelpers.High720, false, false, false, false, "192", false, "en", false, false, 0, 0, false, true, false, true, 4, "$title", false, "mkv", "default");
            }
        }
        else
        {
            downloadSettings = new DownloadSettings("mp3", false, YoutubeHelpers.High720, false, false, false, false, "192", false, "en", false, false, 0, 0, false, true, false, true, 4, "$title", false, "mkv", "default");
        }
    }

    public static void SaveDownloadSettings()
    {
        try
        {
            File.WriteAllText(DownloadSettingsFilePath, JsonConvert.SerializeObject(downloadSettings));
        }
        catch (Exception ex)
        {
            Log(ex.ToString(), "SaveDownloadSettings at GlobalConsts").Wait();
        }
    }

    private static void Downloads_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
            foreach (QueuedDownload item in e.NewItems)
                MainPage.QueueStackPanel.Children.Add(item?.GetDisplayGrid());

        if (e.Action == NotifyCollectionChangedAction.Remove)
            foreach (QueuedDownload item in e.OldItems)
            {
                MainPage.QueueStackPanel.Children.Remove(item?.GetDisplayGrid());
                item?.Dispose();
            }
    }

    #endregion
}
