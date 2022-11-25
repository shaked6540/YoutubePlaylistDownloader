using MoreLinq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YoutubePlaylistDownloader.Objects;
using YoutubePlaylistDownloader.Utilities;
using System.ComponentModel;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Videos;
using YoutubeExplode.Common;
using YoutubeExplode.Channels;
using MahApps.Metro.Controls.Dialogs;

namespace YoutubePlaylistDownloader
{
    /// <summary>
    /// Interaction logic for DownloadPage.xaml
    /// </summary>
    public partial class DownloadPage : UserControl, IDisposable, IDownload
    {
        private DownloadSettings downloadSettings;
        private FullPlaylist Playlist;
        private string FileType, CaptionsLanguage;
        private int DownloadedCount, StartIndex, EndIndex, Maximum;
        private List<Process> ffmpegList;
        private CancellationTokenSource cts;
        private VideoQuality Quality;
        private string Bitrate;
        private List<Tuple<PlaylistVideo, string>> NotDownloaded;
        private IEnumerable<PlaylistVideo> Videos;
        private bool AudioOnly, PreferHighestFPS, DownloadCaptions, TagAudioFile;
        private string SavePath;
        private Subscription Subscription;
        const int megaBytes = 1 << 20;
        private bool silent;
        private FixedQueue<double> downloadSpeeds;
        private Dictionary<PlaylistVideo, int> indexes = new Dictionary<PlaylistVideo, int>();
        private List<Task> convertionTasks = new List<Task>();

        public bool StillDownloading;

        private string title, currentTitle, currentStatus, totalDownloaded, currentDownloadSpeed;
        private int downloadPrecent;
        public event PropertyChangedEventHandler PropertyChanged;

        public string ImageUrl { get; private set; }

        public string Title
        {
            get => title;
            set
            {
                title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Title"));
            }
        }

        public string TotalDownloaded
        {
            get => totalDownloaded;
            set
            {
                totalDownloaded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TotalDownloaded"));
            }
        }

        public int TotalVideos
        {
            get => Maximum;
            set
            {
                throw new NotSupportedException($"Cannot change value of {nameof(TotalVideos)}");
            }
        }

        public int CurrentProgressPrecent
        {
            get => downloadPrecent;
            set
            {
                downloadPrecent = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentProgressPrecent"));
            }
        }

        public string CurrentDownloadSpeed
        {
            get => currentDownloadSpeed;
            set
            {
                currentDownloadSpeed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentDownloadSpeed"));
            }
        }

        public string CurrentTitle
        {
            get => currentTitle;
            set
            {
                currentTitle = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentTitle"));
            }
        }

        public string CurrentStatus
        {
            get => currentStatus;
            set
            {
                currentStatus = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentStatus"));
            }
        }


        public DownloadPage(FullPlaylist playlist, DownloadSettings settings, string savePath = "", IEnumerable<PlaylistVideo> videos = null, Subscription subscription = null,
            bool silent = false, CancellationTokenSource cancellationToken = null)
        {
            InitializeComponent();
            this.downloadSettings = settings;
            downloadSpeeds = new FixedQueue<double>(50);

            if (playlist == null || playlist.BasePlaylist == null)
                Videos = videos;
            else
                Videos = playlist.Videos;

            if (settings.FilterVideosByLength)
            {
                Videos = settings.FilterMode ?
                    Videos.Where(video => video.Duration.Value.TotalMinutes > settings.FilterByLengthValue) :
                    Videos.Where(video => video.Duration.Value.TotalMinutes < settings.FilterByLengthValue);
            }

            int startIndex = settings.SubsetStartIndex <= 0 ? 0 : settings.SubsetStartIndex;
            int endIndex = settings.SubsetEndIndex <= 0 ? Videos.Count() - 1 : settings.SubsetEndIndex;

            this.silent = silent;

            if (!silent)
            {
                GlobalConsts.HideSettingsButton();
                GlobalConsts.HideAboutButton();
                GlobalConsts.HideHomeButton();
                GlobalConsts.HideSubscriptionsButton();
                GlobalConsts.HideHelpButton();
            }

            cts = cancellationToken ?? new CancellationTokenSource();
            ffmpegList = new List<Process>();
            StartIndex = startIndex;
            EndIndex = endIndex;
            NotDownloaded = new List<Tuple<PlaylistVideo, string>>();
            Maximum = EndIndex - StartIndex + 1;
            DownloadedVideosProgressBar.Maximum = Maximum;
            Playlist = playlist;
            FileType = settings.SaveFormat;
            DownloadedCount = 0;
            Quality = settings.Quality;
            DownloadCaptions = settings.DownloadCaptions;
            CaptionsLanguage = settings.CaptionsLanguage;
            SavePath = string.IsNullOrWhiteSpace(savePath) ? GlobalConsts.SaveDirectory : savePath;

            if (settings.SavePlaylistsInDifferentDirectories && playlist != null)
            {
                if (!string.IsNullOrWhiteSpace(playlist.Title))
                {
                    SavePath += $"\\{GlobalConsts.CleanFileName(playlist.Title)}";
                }
                else if (!string.IsNullOrWhiteSpace(playlist.BasePlaylist?.Title))
                {
                    SavePath += $"\\{GlobalConsts.CleanFileName(playlist?.BasePlaylist?.Title)}";
                }

            }

            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);

            Subscription = subscription;
            AudioOnly = settings.AudioOnly;
            TagAudioFile = settings.TagAudioFile;
            PreferHighestFPS = settings.PreferHighestFPS;

            if (settings.SetBitrate && !string.IsNullOrWhiteSpace(settings.Bitrate) && settings.Bitrate.All(x => char.IsDigit(x)))
                Bitrate = $"-b:a {settings.Bitrate}k";
            else
                Bitrate = string.Empty;

            StillDownloading = true;

            ImageUrl = $"https://img.youtube.com/vi/{Videos?.FirstOrDefault()?.Id}/0.jpg";
            Title = playlist?.BasePlaylist?.Title;
            CurrentTitle = (string)FindResource("Loading");
            TotalDownloaded = $"(0/{Maximum})";
            CurrentProgressPrecent = 0;
            CurrentDownloadSpeed = $"{FindResource("DownloadSpeed")}: 0 MiB/s";

            if (settings.Convert || settings.AudioOnly)
                StartDownloadingWithConverting(cts.Token).ConfigureAwait(false);
            else
                StartDownloading(cts.Token).ConfigureAwait(false);

            GlobalConsts.Downloads.Add(new QueuedDownload(this));
        }

        public static async Task SequenceDownload(IEnumerable<string> links, DownloadSettings settings, bool silent = false)
        {
            var client = GlobalConsts.YoutubeClient;
            Playlist basePlaylist;
            FullPlaylist fullPlaylist;
            Channel channel;
            IEnumerable<PlaylistVideo> videos = new List<PlaylistVideo>();
            var notDownloaded = new List<(string, string)>();
            foreach (var link in links)
            {
                async Task Download(FullPlaylist playlistD, IEnumerable<PlaylistVideo> videosD)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var downloadPage = new DownloadPage(playlistD, settings, videos: videosD, silent: silent);
                    });
                }
                try
                {
                    if (YoutubeHelpers.TryParsePlaylistId(link, out string playlistId))
                    {
                        basePlaylist = await client.Playlists.GetAsync(playlistId).ConfigureAwait(false);
                        fullPlaylist = new FullPlaylist(basePlaylist, await client.Playlists.GetVideosAsync(basePlaylist.Id).CollectAsync().ConfigureAwait(false));
                        await Download(fullPlaylist, new List<PlaylistVideo>());
                    }
                    else if (YoutubeHelpers.TryParseChannelId(link, out string channelId))
                    {
                        channel = await client.Channels.GetAsync(channelId).ConfigureAwait(false);
                        videos = await client.Channels.GetUploadsAsync(channelId).CollectAsync().ConfigureAwait(false);
                        fullPlaylist = new FullPlaylist(null, null, channel.Title);
                        await Download(fullPlaylist, videos);
                    }
                    else if (YoutubeHelpers.TryParseUsername(link, out string username))
                    {
                        channel = await client.Channels.GetByUserAsync(username).ConfigureAwait(false);
                        videos = await client.Channels.GetUploadsAsync(channel.Id).CollectAsync().ConfigureAwait(false);
                        fullPlaylist = new FullPlaylist(null, null, channel.Title);
                        await Download(fullPlaylist, videos);
                    }
                    else if (YoutubeHelpers.TryParseVideoId(link, out string videoId))
                    {
                        var video = await client.Videos.GetAsync(videoId);
                        await Download(null, new[] { new PlaylistVideo(video.Id, video.Title, video.Author, video.Duration, video.Thumbnails) });
                    }
                    else
                    {
                        throw new Exception(Application.Current.FindResource("NoVideosToDownload").ToString());
                    }
                }
                catch (Exception ex)
                {
                    notDownloaded.Add((link, ex.Message));
                    await GlobalConsts.Log(ex.ToString(), "SequenceDownload at DownloadPage.xaml.cs");
                }
            }

            if (notDownloaded.Any())
            {
                var result = await GlobalConsts.CustomYesNoDialog($"{Application.Current.FindResource("CouldntDownload")}",
                      string.Concat($"{Application.Current.FindResource("ListOfNotDownloadedVideos")}\n", string.Join("\n", notDownloaded.Select(x => string.Concat(x.Item1, " Reason: ", x.Item2)))),
                      new MetroDialogSettings()
                      {
                          AffirmativeButtonText = $"{Application.Current.FindResource("Retry")}",
                          NegativeButtonText = $"{Application.Current.FindResource("Back")}",
                          ColorScheme = MetroDialogColorScheme.Theme,
                          DefaultButtonFocus = MessageDialogResult.Affirmative,
                      });
                if (result == MessageDialogResult.Affirmative)
                {
                    _ = DownloadPage.SequenceDownload(notDownloaded.Select(x => x.Item1), settings).ConfigureAwait(false);
                    GlobalConsts.MainPage.ChangeToQueueTab();
                    GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
                }
            }
        }

        public async Task StartDownloadingWithConverting(CancellationToken token)
        {
            try
            {
                if (StartIndex > Videos.Count() - 1)
                {
                    await GlobalConsts.ShowMessage($"{FindResource("NoVideosToDownload")}", $"{FindResource("ThereAreNoVidoesToDownload")}");
                    StillDownloading = false;
                    GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
                }

                var client = GlobalConsts.YoutubeClient;
                int convertingCount = 0;
                convertionTasks.Clear();
                for (int i = StartIndex; i <= EndIndex; i++)
                {
                    var video = Videos.ElementAtOrDefault(i);

                    if (video == default(PlaylistVideo))
                        goto exit;

                    indexes.Add(video, i + 1);
                    try
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            DownloadSpeedTextBlock.Visibility = Visibility.Collapsed;
                            Update(0, video);
                        });

                        downloadSpeeds.Clear();

                        var streamInfoSet = await client.Videos.Streams.GetManifestAsync(video.Id);
                        var bestQuality = streamInfoSet.GetAudioOnlyStreams().GetWithHighestBitrate();
                        var cleanFileNameWithID = GlobalConsts.CleanFileName(video.Title + video.Id);
                        var cleanFileName = GlobalConsts.CleanFileName(video.Title);
                        var fileLoc = $"{GlobalConsts.TempFolderPath}{cleanFileNameWithID}";

                        if (AudioOnly)
                            FileType = bestQuality.Container.Name;

                        var outputFileLoc = $"{GlobalConsts.TempFolderPath}{cleanFileNameWithID}.{FileType}";
                        var copyFileLoc = $"{SavePath}\\{cleanFileName}.{FileType}";

                        CurrentStatus = string.Concat(FindResource("Downloading"));
                        CurrentTitle = video.Title;
                        CurrentProgressPrecent = 0;
                        CurrentDownloadSpeed = "0 MiB/s";

                        using (var stream = new ProgressStream(File.Create(fileLoc)))
                        {
                            Stopwatch sw = new Stopwatch();
                            TimeSpan ts = new TimeSpan(0);
                            int seconds = 1;
                            string downloadSpeedText = (string)FindResource("DownloadSpeed");

                            stream.BytesWritten += async (sender, args) =>
                            {
                                try
                                {
                                    var precent = Convert.ToInt32(args.StreamLength * 100 / bestQuality.Size.Bytes);
                                    CurrentProgressPrecent = precent;
                                    double speedInMB = 0;
                                    var delta = sw.Elapsed - ts;
                                    ts = sw.Elapsed;
                                    try
                                    {
                                        var speedInBytes = args.BytesMoved / delta.TotalSeconds;
                                        speedInMB = Math.Round(speedInBytes / megaBytes, 2);
                                        downloadSpeeds.Enqueue(speedInMB);
                                    }
                                    catch (DivideByZeroException)
                                    {

                                    }

                                    if (!sw.IsRunning)
                                        sw.Start();

                                    await Dispatcher.InvokeAsync(() =>
                                    {
                                        try
                                        {
                                            CurrentDownloadProgressBar.Value = precent;
                                            CurrentDownloadProgressBarTextBlock.Text = $"{precent}%";
                                            if (sw.Elapsed.Seconds == seconds && delta.TotalMilliseconds > 0)
                                            {
                                                CurrentDownloadSpeed = string.Concat(downloadSpeedText, Math.Round(downloadSpeeds.Average(), 2), " MiB/s");
                                                DownloadSpeedTextBlock.Text = CurrentDownloadSpeed;
                                                DownloadSpeedTextBlock.Visibility = Visibility.Visible;
                                                seconds += 1;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            GlobalConsts.Log(ex.ToString(), "Dispatcher.InvokeAsync at DownloadPage.xaml.cs StartDownloadingWithConverting").Wait();
                                        }
                                    }, System.Windows.Threading.DispatcherPriority.Normal, cts.Token);
                                }
                                catch (OperationCanceledException)
                                {

                                }
                                catch (Exception ex)
                                {
                                    await GlobalConsts.Log(ex.ToString(), "BytesWrittenEventHandler at ProgressStream in DownloadPage");
                                }

                            };
                            await client.Videos.Streams.CopyToAsync(bestQuality, stream, cancellationToken: token);
                            sw.Stop();
                        }
                        if (!AudioOnly)
                        {
                            var ffmpeg = new Process()
                            {
                                EnableRaisingEvents = true,
                                StartInfo = new ProcessStartInfo()
                                {
                                    FileName = GlobalConsts.FFmpegFilePath,
                                    Arguments = $"-i \"{fileLoc}\" -y {Bitrate} \"{outputFileLoc}\"",
                                    CreateNoWindow = true,
                                    UseShellExecute = false
                                }
                            };

                            token.ThrowIfCancellationRequested();
                            ffmpeg.Exited += async (x, y) =>
                            {
                                try
                                {
                                    ffmpegList?.Remove(ffmpeg);
                                    convertingCount--;

                                    if (TagAudioFile)
                                        await GlobalConsts.TagFile(video, indexes[video], outputFileLoc, Playlist);

                                    int copyFileLocCounter = 1;
                                    while (File.Exists(copyFileLoc))
                                    {
                                        copyFileLoc = $"{SavePath}\\{cleanFileName}-{copyFileLocCounter}.{FileType}";
                                        copyFileLocCounter++;
                                    }
                                    File.Copy(outputFileLoc, copyFileLoc, true);
                                    File.Delete(outputFileLoc);

                                    if (Subscription != null)
                                        Subscription.LatestVideoDownloaded = DateTime.UtcNow;
                                }
                                catch (Exception ex)
                                {
                                    await GlobalConsts.Log(ex.ToString(), "DownloadPage with convert");
                                }
                            };
                            if (!GlobalConsts.LimitConvertions)
                            {
                                ffmpeg.Start();
                                convertingCount++;
                                ffmpegList.Add(ffmpeg);
                            }
                            else
                            {
                                convertionTasks.Add(Task.Run(async () =>
                                {
                                    try
                                    {
                                        convertingCount++;
                                        await GlobalConsts.ConversionsLocker.WaitAsync(cts.Token);
                                        ffmpeg.Start();
                                        ffmpeg.Exited += (x, y) => GlobalConsts.ConversionsLocker.Release();
                                        ffmpegList.Add(ffmpeg);
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        GlobalConsts.ConversionsLocker.Release();
                                    }
                                    catch (Exception ex)
                                    {
                                        await GlobalConsts.Log(ex.ToString(), "ConvertionLocker at StartDownloadingWithConverting at DownloadPage.xaml.cs");
                                    }
                                }));
                            }
                        }
                        else
                        {
                            File.Copy(fileLoc, copyFileLoc, true);

                            if (Subscription != null)
                                Subscription.LatestVideoDownloaded = DateTime.UtcNow;

                            File.Delete(fileLoc);
                            try
                            {
                                if (TagAudioFile)
                                    await GlobalConsts.TagFile(video, i + 1, copyFileLoc, Playlist);
                            }
                            catch (Exception ex)
                            {
                                await GlobalConsts.Log(ex.ToString(), "TagFile at download with convert playlist");
                            }
                        }

                        if (Subscription != null)
                            Subscription.DownloadedVideos.Add(video.Id);

                        DownloadedCount++;
                        TotalDownloaded = $"({DownloadedCount}/{Maximum})";

                    }
                    catch (OperationCanceledException)
                    {
                        goto exit;
                    }
                    catch (Exception ex)
                    {
                        await GlobalConsts.Log(ex.ToString(), "DownloadPage DownlaodWithConvert");
                        NotDownloaded.Add(new Tuple<PlaylistVideo, string>(video, ex.Message));
                    }
                }

            exit:

                if (NotDownloaded.Any())
                {
                    var result = await GlobalConsts.CustomYesNoDialog($"{FindResource("CouldntDownload")}",
                          string.Concat($"{FindResource("ListOfNotDownloadedVideos")}\n", string.Join("\n", NotDownloaded.Select(x => string.Concat(x.Item1.Title, " Reason: ", x.Item2)))),
                          new MetroDialogSettings()
                          {
                              AffirmativeButtonText = $"{FindResource("Retry")}",
                              NegativeButtonText = $"{FindResource("Back")}",
                              ColorScheme = MetroDialogColorScheme.Theme,
                              DefaultButtonFocus = MessageDialogResult.Affirmative,
                          });
                    if (result == MessageDialogResult.Affirmative)
                    {
                        _ = DownloadPage.SequenceDownload(NotDownloaded.Select(x => x.Item1.Url), this.downloadSettings).ConfigureAwait(false);
                        GlobalConsts.MainPage.ChangeToQueueTab();
                        GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
                    }
                }

                while (ffmpegList.Count > 0 || convertionTasks?.Count(x => !x.IsCompleted) > 0)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        string status = string.Concat(FindResource("StillConverting"), " ", convertingCount, " ", FindResource("files"));
                        HeadlineTextBlock.Text = (string)FindResource("AllDone");
                        CurrentDownloadProgressBar.IsIndeterminate = true;
                        TotalDownloadedGrid.Visibility = Visibility.Collapsed;
                        TotalDownloadsProgressBarTextBlock.Text = $"({DownloadedCount}\\{Maximum})";
                        DownloadedVideosProgressBar.Value = Maximum;
                        ConvertingTextBlock.Visibility = Visibility.Visible;
                        ConvertingTextBlock.Text = status;
                        CurrentDownloadProgressBarTextBlock.Visibility = Visibility.Collapsed;
                        DownloadSpeedTextBlock.Visibility = Visibility.Collapsed;
                        CurrentStatus = status;
                        CurrentDownloadSpeed = "0 MiB/s";
                        CurrentProgressPrecent = 100;
                        Title = string.Concat(FindResource("Converting"));

                    });
                    await Task.Delay(1000);
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    string allDone = (string)FindResource("AllDone");
                    CurrentDownloadGrid.Visibility = Visibility.Collapsed;
                    ConvertingTextBlock.Visibility = Visibility.Collapsed;
                    DownloadSpeedTextBlock.Visibility = Visibility.Collapsed;
                    CurrentStatus = string.Empty;
                    CurrentDownloadSpeed = string.Empty;
                    CurrentProgressPrecent = 100;
                    Title = allDone;
                    HeadlineTextBlock.Text = allDone;
                    TotalDownloadedGrid.Visibility = Visibility.Collapsed;
                    TotalDownloadsProgressBarTextBlock.Text = $"({DownloadedCount}\\{Maximum})";
                    DownloadedVideosProgressBar.Value = Maximum;
                    CurrentDownloadProgressBarTextBlock.Visibility = Visibility.Collapsed;
                });

                if (GlobalConsts.DownloadSettings.OpenDestinationFolderWhenDone)
                    OpenFolder_Click(null, null);

            }
            catch (Exception ex)
            {
                await GlobalConsts.Log(ex.ToString(), "DownloadPage With converting");

            }
            finally
            {
                await Task.WhenAll(convertionTasks);
                StillDownloading = false;
                Dispose();
            }
        }

        public async Task StartDownloading(CancellationToken token)
        {
            if (StartIndex > Videos.Count() - 1)
            {
                await GlobalConsts.ShowMessage($"{FindResource("NoVideosToDownload")}", $"{FindResource("ThereAreNoVidoesToDownload")}");
                StillDownloading = false;
                GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
            }

            var client = GlobalConsts.YoutubeClient;
            int convertingCount = 0;
            convertionTasks.Clear();
            for (int i = StartIndex; i <= EndIndex; i++)
            {
                var video = Videos.ElementAtOrDefault(i);

                if (video == default(PlaylistVideo))
                    goto exit;

                try
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        DownloadSpeedTextBlock.Visibility = Visibility.Collapsed;
                        Update(0, video);
                    });

                    downloadSpeeds.Clear();

                    var streamInfoSet = await client.Videos.Streams.GetManifestAsync(video.Id);
                    IVideoStreamInfo bestQuality = null;
                    IStreamInfo bestAudio = null;

                    var videoList = streamInfoSet.GetVideoOnlyStreams().OrderByDescending(x => x.VideoQuality == Quality);

                    if (PreferHighestFPS)
                        videoList = videoList.ThenByDescending(x => x.VideoQuality.Framerate).ThenBy(x => Math.Abs(x.VideoQuality.MaxHeight - Quality.MaxHeight));
                    else
                        videoList = videoList.ThenBy(x => Math.Abs(x.VideoQuality.MaxHeight - Quality.MaxHeight));

                    bestQuality = videoList.FirstOrDefault();
                    bestAudio = streamInfoSet.GetAudioOnlyStreams().GetWithHighestBitrate();

                    var cleanVideoName = GlobalConsts.CleanFileName(video.Title);
                    var fileLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoName}";
                    var outputFileLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoName}.mkv";
                    var copyFileLoc = $"{SavePath}\\{cleanVideoName}.mkv";
                    var audioLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoName}.{bestAudio.Container.Name}";
                    var captionsLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoName}.srt";

                    CurrentStatus = string.Concat(FindResource("Downloading"));
                    CurrentTitle = video.Title;
                    CurrentProgressPrecent = 0;
                    CurrentDownloadSpeed = "0 MiB/s";

                    string ffmpegArguments = "";

                    using (var stream = new ProgressStream(File.Create(fileLoc)))
                    {
                        Stopwatch sw = new Stopwatch();
                        TimeSpan ts = new TimeSpan(0);
                        int seconds = 1;
                        string downloadSpeedText = (string)FindResource("DownloadSpeed");

                        stream.BytesWritten += async (sender, args) =>
                        {
                            try
                            {
                                var precent = Convert.ToInt32(args.StreamLength * 100 / bestQuality.Size.Bytes);
                                CurrentProgressPrecent = precent;
                                double speedInMB = 0;
                                var delta = sw.Elapsed - ts;
                                ts = sw.Elapsed;
                                try
                                {
                                    var speedInBytes = args.BytesMoved / delta.TotalSeconds;
                                    speedInMB = Math.Round(speedInBytes / megaBytes, 2);
                                    downloadSpeeds.Enqueue(speedInMB);
                                }
                                catch (DivideByZeroException)
                                {

                                }
                                if (!sw.IsRunning)
                                    sw.Start();

                                await Dispatcher.InvokeAsync(() =>
                                {
                                    try
                                    {
                                        CurrentDownloadProgressBar.Value = precent;
                                        CurrentDownloadProgressBarTextBlock.Text = $"{precent}%";
                                        if (sw.Elapsed.Seconds == seconds && delta.TotalMilliseconds > 0)
                                        {
                                            string speed = string.Concat(downloadSpeedText, Math.Round(downloadSpeeds.Average(), 2), " MiB/s");
                                            DownloadSpeedTextBlock.Text = speed;
                                            DownloadSpeedTextBlock.Visibility = Visibility.Visible;
                                            CurrentDownloadSpeed = speed;
                                            seconds += 1;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        GlobalConsts.Log(ex.ToString(), "Dispatcher.InvokeAsync at DownloadPage.xaml.cs StartDownloading").Wait();
                                    }
                                }, System.Windows.Threading.DispatcherPriority.Normal, cts.Token);
                            }
                            catch (OperationCanceledException)
                            {

                            }
                            catch (Exception ex)
                            {
                                await GlobalConsts.Log(ex.ToString(), "BytesWrittenEventHandler at ProgressStream in DownloadPage");
                            }

                        };
                        var videoTask = client.Videos.Streams.CopyToAsync(bestQuality, stream, cancellationToken: token);
                        using (var audioStream = File.Create(audioLoc))
                        {
                            var audioTask = client.Videos.Streams.CopyToAsync(bestAudio, audioStream);

                        caption:
                            if (DownloadCaptions)
                            {
                                var captionsInfo = await client.Videos.ClosedCaptions.GetManifestAsync(video.Id);
                                var captions = captionsInfo.TryGetByLanguage(CaptionsLanguage);

                                if (captions == null)
                                {
                                    DownloadCaptions = false;
                                    goto caption;
                                }

                                var captionsTask = client.Videos.ClosedCaptions.DownloadAsync(captions, captionsLoc, cancellationToken: token);
                                await ExtensionMethods.WhenAll(videoTask, audioTask, captionsTask);
                                sw.Stop();
                                ffmpegArguments = $"-i \"{fileLoc}\" -i \"{audioLoc}\" -i \"{captionsLoc}\" -y -c copy \"{outputFileLoc}\"";
                            }
                            else
                            {
                                ffmpegArguments = $"-i \"{fileLoc}\" -i \"{audioLoc}\" -y -c copy \"{outputFileLoc}\"";
                                await ExtensionMethods.WhenAll(videoTask, audioTask);
                                sw.Stop();
                            }
                        }
                    }

                    var ffmpeg = new Process()
                    {
                        EnableRaisingEvents = true,
                        StartInfo = new ProcessStartInfo()
                        {
                            FileName = GlobalConsts.FFmpegFilePath,
                            Arguments = ffmpegArguments,
                            CreateNoWindow = true,
                            UseShellExecute = false,
                        }
                    };

                    token.ThrowIfCancellationRequested();
                    ffmpeg.Exited += async (x, y) =>
                    {
                        try
                        {
                            ffmpegList?.Remove(ffmpeg);
                            convertingCount--;
                            File.Copy(outputFileLoc, copyFileLoc, true);

                            if (Subscription != null)
                                Subscription.DownloadedVideos.Add(video.Id);

                            File.Delete(outputFileLoc);
                            File.Delete(audioLoc);
                            File.Delete(fileLoc);
                        }
                        catch (Exception ex)
                        {
                            await GlobalConsts.Log(ex.ToString(), "DownloadPage without convert");
                        }
                    };

                    if (!GlobalConsts.LimitConvertions)
                    {
                        ffmpeg.Start();
                        convertingCount++;
                        ffmpegList.Add(ffmpeg);
                    }
                    else
                    {
                        convertionTasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                convertingCount++;
                                await GlobalConsts.ConversionsLocker.WaitAsync(cts.Token);
                                ffmpeg.Start();
                                ffmpeg.Exited += (x, y) => GlobalConsts.ConversionsLocker.Release();
                                ffmpegList.Add(ffmpeg);
                            }
                            catch (OperationCanceledException)
                            {
                                GlobalConsts.ConversionsLocker.Release();
                            }
                            catch (Exception ex)
                            {
                                await GlobalConsts.Log(ex.ToString(), "ConvertionLocker at StartDownloading at DownloadPage.xaml.cs");
                            }
                        }));
                    }

                    DownloadedCount++;
                    TotalDownloaded = $"({DownloadedCount}/{Maximum})";

                }
                catch (OperationCanceledException)
                {
                    goto exit;
                }
                catch (Exception ex)
                {
                    await GlobalConsts.Log(ex.ToString(), "DownloadPage DownlaodWithConvert");
                    NotDownloaded.Add(new Tuple<PlaylistVideo, string>(video, ex.Message));
                }
            }

        exit:

            if (NotDownloaded.Any())
            {
                var result = await GlobalConsts.CustomYesNoDialog($"{FindResource("CouldntDownload")}",
                      string.Concat($"{FindResource("ListOfNotDownloadedVideos")}\n", string.Join("\n", NotDownloaded.Select(x => string.Concat(x.Item1.Title, " Reason: ", x.Item2)))),
                      new MetroDialogSettings()
                      {
                          AffirmativeButtonText = $"{FindResource("Retry")}",
                          NegativeButtonText = $"{FindResource("Back")}",
                          ColorScheme = MetroDialogColorScheme.Theme,
                          DefaultButtonFocus = MessageDialogResult.Affirmative,
                      });
                if (result == MessageDialogResult.Affirmative)
                {
                    _ = DownloadPage.SequenceDownload(NotDownloaded.Select(x => x.Item1.Url), this.downloadSettings).ConfigureAwait(false);
                    GlobalConsts.MainPage.ChangeToQueueTab();
                    GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
                }

            }

            while (ffmpegList.Count > 0 || convertionTasks?.Count(x => !x.IsCompleted) > 0)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    string status = string.Concat(FindResource("StillConverting"), " ", convertingCount, " ", FindResource("files"));
                    HeadlineTextBlock.Text = (string)FindResource("AllDone");
                    CurrentDownloadProgressBar.IsIndeterminate = true;
                    TotalDownloadedGrid.Visibility = Visibility.Collapsed;
                    TotalDownloadsProgressBarTextBlock.Text = $"({DownloadedCount}\\{Maximum})";
                    DownloadedVideosProgressBar.Value = Maximum;
                    ConvertingTextBlock.Visibility = Visibility.Visible;
                    ConvertingTextBlock.Text = status;
                    CurrentDownloadProgressBarTextBlock.Visibility = Visibility.Collapsed;
                    DownloadSpeedTextBlock.Visibility = Visibility.Collapsed;
                    CurrentStatus = status;
                    CurrentDownloadSpeed = "0 MiB/s";
                    CurrentProgressPrecent = 100;
                    Title = string.Concat(FindResource("Converting"));
                });
                await Task.Delay(1000);
            }

            if (Subscription != null)
                Subscription.LatestVideoDownloaded = DateTime.UtcNow;

            StillDownloading = false;

            await Dispatcher.InvokeAsync(() =>
            {
                string allDone = (string)FindResource("AllDone");
                CurrentDownloadGrid.Visibility = Visibility.Collapsed;
                ConvertingTextBlock.Visibility = Visibility.Collapsed;
                DownloadSpeedTextBlock.Visibility = Visibility.Collapsed;
                CurrentStatus = string.Empty;
                CurrentDownloadSpeed = string.Empty;
                CurrentProgressPrecent = 100;
                Title = allDone;
                HeadlineTextBlock.Text = allDone;
                TotalDownloadedGrid.Visibility = Visibility.Collapsed;
                TotalDownloadsProgressBarTextBlock.Text = $"({DownloadedCount}\\{Maximum})";
                DownloadedVideosProgressBar.Value = Maximum;
                CurrentDownloadProgressBarTextBlock.Visibility = Visibility.Collapsed;
            });

            await Task.WhenAll(convertionTasks);

            if (GlobalConsts.DownloadSettings.OpenDestinationFolderWhenDone)
                OpenFolder_Click(null, null);

            Dispose();
        }

        private void Background_Exit(object sender, RoutedEventArgs e)
        {
            GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
        }

        private void Update(int precent, PlaylistVideo video)
        {
            CurrentDownloadProgressBar.Value = precent;
            HeadlineTextBlock.Text = (string)FindResource("CurrentlyDownlading") + video.Title;
            CurrentDownloadProgressBarTextBlock.Text = $"{precent}%";
            TotalDownloadsProgressBarTextBlock.Text = $"{DownloadedCount}\\{Maximum}";
            DownloadedVideosProgressBar.Value = DownloadedCount;
        }

        public DownloadPage LoadFromSilent()
        {
            GlobalConsts.HideSettingsButton();
            GlobalConsts.HideAboutButton();
            GlobalConsts.HideHomeButton();
            GlobalConsts.HideHelpButton();
            GlobalConsts.HideSubscriptionsButton();

            return this;
        }

        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (!disposedValue)
            {
                await ExitAsync();
                return;
            }
            GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
        }
        public async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(SavePath);
            }
            catch (Exception ex)
            {
                await GlobalConsts.Log($"Error opening save path: {ex}", "DownloadPage.xaml.cs at OpenFolder_Click");
            }
        }

        private async Task<bool> ExitAsync()
        {
            cts?.Cancel(true);
            if (ffmpegList.Count > 0)
            {
                var yesno = await GlobalConsts.ShowYesNoDialog($"{FindResource("StillConverting")}", $"{FindResource("StillConverting")} {ffmpegList.Count} {FindResource("files")} {FindResource("AreYouSureExit")}");
                if (yesno == MahApps.Metro.Controls.Dialogs.MessageDialogResult.Negative)
                    return false;
            }
            ffmpegList?.ForEach(x => { try { x.Kill(); } catch { } });
            StillDownloading = false;
            if (!silent)
                GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());

            return true;
        }

        public Task<bool> Cancel()
        {
            if (!disposedValue)
                return ExitAsync();

            return Task.FromResult(true);
        }

        #region IDisposable Support
        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Subscription?.Dispose();
                    cts?.Cancel(true);
                    cts?.Dispose();
                    try
                    {
                        ffmpegList?.ForEach(x => { try { x.Kill(); } catch { } });
                        ffmpegList?.Clear();
                    }
                    catch { }
                    NotDownloaded?.Clear();
                    downloadSpeeds?.Clear();
                }

                StillDownloading = false;
                Playlist = null;
                ffmpegList = null;
                disposedValue = true;
                Videos = null;
                FileType = null;
                Bitrate = null;
                NotDownloaded = null;
                Videos = null;
                Subscription = null;
                downloadSpeeds = null;
                title = null;
                currentTitle = null;
                currentStatus = null;
                totalDownloaded = null;
                currentDownloadSpeed = null;
                PropertyChanged = null;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}