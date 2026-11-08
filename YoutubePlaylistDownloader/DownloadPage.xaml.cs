using MoreLinq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YoutubeExplode.Channels;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubePlaylistDownloader.Objects;
using YoutubePlaylistDownloader.Utilities;

namespace YoutubePlaylistDownloader
{
    /// <summary>
    /// Interaction logic for DownloadPage.xaml
    /// </summary>
    public partial class DownloadPage : UserControl, IDisposable, IDownload
    {
        private readonly DownloadSettings downloadSettings;
        private FullPlaylist Playlist;
        private string FileType;
        private string VideoSaveFormat;
        private readonly string CaptionsLanguage;
        private int DownloadedCount;
        private readonly int StartIndex;
        private readonly int EndIndex;
        private readonly int Maximum;
        private List<Process> ffmpegList;
        private readonly CancellationTokenSource cts;
        private readonly VideoQuality Quality;
        private string Bitrate;
        private List<Tuple<IVideo, string>> NotDownloaded;
        private IEnumerable<IVideo> Videos;
        private readonly bool AudioOnly;
        private readonly bool PreferHighestFPS;
        private bool DownloadCaptions;
        private readonly bool TagAudioFile;
        private readonly string SavePath;
        const int megaBytes = 1 << 20;
        private readonly bool silent;
        private FixedQueue<double> downloadSpeeds;
        private readonly Dictionary<IVideo, int> indexes = [];
        private readonly List<Task> conversionTasks = [];
        private readonly object ffmpegLock = new();
        private int convertingCount;

        public bool StillDownloading;

        private string title, currentTitle, currentStatus, totalDownloaded, currentDownloadSpeed;
        private int downloadPercent;
        public event PropertyChangedEventHandler PropertyChanged;

        public string ImageUrl { get; private set; }

        public string Title
        {
            get => title;
            set
            {
                title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }

        public string TotalDownloaded
        {
            get => totalDownloaded;
            set
            {
                totalDownloaded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalDownloaded)));
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

        public int CurrentProgressPercent
        {
            get => downloadPercent;
            set
            {
                downloadPercent = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentProgressPercent)));
            }
        }

        public string CurrentDownloadSpeed
        {
            get => currentDownloadSpeed;
            set
            {
                currentDownloadSpeed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentDownloadSpeed)));
            }
        }

        public string CurrentTitle
        {
            get => currentTitle;
            set
            {
                currentTitle = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTitle)));
            }
        }

        public string CurrentStatus
        {
            get => currentStatus;
            set
            {
                currentStatus = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentStatus)));
            }
        }


        public DownloadPage(FullPlaylist playlist, DownloadSettings settings, string savePath = "", IEnumerable<IVideo> videos = null,
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
                GlobalConsts.HideHelpButton();
            }

            cts = cancellationToken ?? new CancellationTokenSource();
            ffmpegList = new List<Process>();
            StartIndex = startIndex;
            EndIndex = endIndex;
            NotDownloaded = new List<Tuple<IVideo, string>>();
            Maximum = EndIndex - StartIndex + 1;
            DownloadedVideosProgressBar.Maximum = Maximum;
            Playlist = playlist;
            FileType = settings.SaveFormat;
            VideoSaveFormat = settings.VideoSaveFormat;
            DownloadedCount = 0;
            Quality = settings.Quality;
            DownloadCaptions = settings.DownloadCaptions;
            CaptionsLanguage = settings.CaptionsLanguage;
            SavePath = string.IsNullOrWhiteSpace(savePath) ? GlobalConsts.settings.SaveDirectory : savePath;

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

            AudioOnly = settings.AudioOnly;
            TagAudioFile = settings.TagAudioFile;
            PreferHighestFPS = settings.PreferHighestFPS;

            if (settings.SetBitrate && !string.IsNullOrWhiteSpace(settings.Bitrate) && settings.Bitrate.All(x => char.IsDigit(x)))
                Bitrate = $"-b:a {settings.Bitrate}k";
            else
                Bitrate = string.Empty;

            StillDownloading = true;

            ImageUrl = $"https://img.youtube.com/vi/{Videos?.FirstOrDefault()?.Id}/maxresdefault.jpg";
            Title = playlist?.BasePlaylist?.Title;
            CurrentTitle = (string)FindResource("Loading");
            TotalDownloaded = $"(0/{Maximum})";
            CurrentProgressPercent = 0;
            CurrentDownloadSpeed = $"{FindResource("DownloadSpeed")}: 0 MiB/s";

            if (settings.Convert || settings.AudioOnly)
                StartDownloadingWithConverting(playlist.BasePlaylist?.Id, cts.Token).ConfigureAwait(false);
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
            IEnumerable<IVideo> videos = new List<IVideo>();
            var notDownloaded = new List<(string, string)>();
            foreach (var link in links)
            {
                async Task Download(FullPlaylist playlistD, IEnumerable<IVideo> videosD)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var downloadPage = new DownloadPage(playlistD, settings, videos: videosD, silent: silent);
                    });
                }
                try
                {
                    if (YoutubeHelpers.TryParsePlaylistId(link, out var playlistId))
                    {
                        basePlaylist = await client.Playlists.GetAsync(playlistId.Value).ConfigureAwait(false);
                        var dlVideoSource = client.Playlists.GetVideosAsync(basePlaylist.Id);
                        var dlVideos = YoutubeHelpers.IsMixPlaylist(playlistId)
                            ? await dlVideoSource.CollectAsync(GlobalConsts.MaxMixPlaylistVideos).ConfigureAwait(false)
                            : await dlVideoSource.CollectAsync().ConfigureAwait(false);
                        fullPlaylist = new FullPlaylist(basePlaylist, dlVideos);
                        await Download(fullPlaylist, new List<IVideo>());
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
                        fullPlaylist = new FullPlaylist(null, new[] { video }, video.Title);
                        await Download(fullPlaylist, new[] { video });
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
                await GlobalConsts.ShowSelectableDialog($"{Application.Current.FindResource("CouldntDownload")}",
                      string.Concat($"{Application.Current.FindResource("ListOfNotDownloadedVideos")}\n", string.Join("\n", notDownloaded.Select(x => string.Concat(x.Item1, " Reason: ", x.Item2)))),
                      () =>
                      {
                          _ = DownloadPage.SequenceDownload(notDownloaded.Select(x => x.Item1), settings).ConfigureAwait(false);
                          GlobalConsts.MainPage.ChangeToQueueTab();
                          GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
                      });
            }
        }

        public async Task StartDownloadingWithConverting(PlaylistId? playlistId, CancellationToken token)
        {
            try
            {
                if (StartIndex > Videos.Count() - 1)
                {
                    await GlobalConsts.ShowMessage($"{FindResource("NoVideosToDownload")}", $"{FindResource("ThereAreNoVideosToDownload")}");
                    StillDownloading = false;
                    GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
                }

                var client = GlobalConsts.YoutubeClient;
                var shownFailures = 0;
                conversionTasks.Clear();
                for (int i = StartIndex; i <= EndIndex; i++)
                {
                    var video = Videos.ElementAtOrDefault(i);

                    if (video == default(IVideo))
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

                        var streamInfoSet = await client.Videos.Streams.GetManifestAsync(video.Id, token);
                        IStreamInfo bestQuality;
                        if (downloadSettings.VideoLanguage == "default")
                        {
                            var defaultLangaugeVideos = streamInfoSet.GetAudioOnlyStreams().Where(x => x.IsAudioLanguageDefault.HasValue && x.IsAudioLanguageDefault.Value);
                            if (defaultLangaugeVideos.Any())
                            {
                                bestQuality = defaultLangaugeVideos.GetWithHighestBitrate();
                            }
                            else
                            {
                                bestQuality = streamInfoSet.GetAudioOnlyStreams().GetWithHighestBitrate();
                            }
                        }
                        else
                        {
                            var videoesInRequestedLanguage = streamInfoSet.GetAudioOnlyStreams().Where(x => x.AudioLanguage.HasValue && x.AudioLanguage.Value.Code.Contains(downloadSettings.VideoLanguage));
                            if (videoesInRequestedLanguage.Any())
                            {
                                bestQuality = videoesInRequestedLanguage.GetWithHighestBitrate();
                            }
                            else
                            {
                                var defaultAudioStreams = streamInfoSet.GetAudioOnlyStreams().Where(x => x.IsAudioLanguageDefault.HasValue && x.IsAudioLanguageDefault.Value);
                                if (defaultAudioStreams.Any())
                                {
                                    bestQuality = defaultAudioStreams.GetWithHighestBitrate();
                                }
                                else
                                {
                                    bestQuality = streamInfoSet.GetAudioOnlyStreams().GetWithHighestBitrate();
                                }
                            }
                        }
                        var cleanFileNameWithID = GlobalConsts.CleanFileName(video.Title + video.Id);
                        var cleanFileName = GlobalConsts.CleanFileName(downloadSettings.GetFilenameByPattern(video, i, title, Playlist));
                        var fileLoc = $"{GlobalConsts.TempFolderPath}{cleanFileNameWithID}";

                        if (AudioOnly)
                            FileType = bestQuality.Container.Name;

                        var outputFileLoc = $"{GlobalConsts.TempFolderPath}{cleanFileNameWithID}.{FileType}";
                        var copyFileLoc = $"{SavePath}\\{cleanFileName}.{FileType}";

                        if (GlobalConsts.DownloadSettings.SkipExisting && File.Exists(copyFileLoc))
                        {
                            CurrentStatus = string.Concat(FindResource("Skipping"));
                            CurrentTitle = video.Title;
                            CurrentProgressPercent = 0;
                            CurrentDownloadSpeed = "0 MiB/s";

                            DownloadedCount++;
                            TotalDownloaded = $"({DownloadedCount}/{Maximum})";

                            continue;
                        }

                        CurrentStatus = string.Concat(FindResource("Downloading"));
                        CurrentTitle = video.Title;
                        CurrentProgressPercent = 0;
                        CurrentDownloadSpeed = "0 MiB/s";

                        using (var stream = new ProgressStream(File.Create(fileLoc)))
                        {
                            Stopwatch sw = new();
                            TimeSpan ts = new(0);
                            int seconds = 1;
                            string downloadSpeedText = (string)FindResource("DownloadSpeed");

                            stream.BytesWritten += async (sender, args) =>
                            {
                                try
                                {
                                    var percent = Convert.ToInt32(args.StreamLength * 100 / bestQuality.Size.Bytes);
                                    CurrentProgressPercent = percent;
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
                                            CurrentDownloadProgressBar.Value = percent;
                                            CurrentDownloadProgressBarTextBlock.Text = $"{percent}%";
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
                                    if (token.IsCancellationRequested)
                                        return;

                                    if (!File.Exists(outputFileLoc))
                                    {
                                        await GlobalConsts.Log($"ffmpeg exited with code {ffmpeg.ExitCode} and did not produce {outputFileLoc}", "DownloadPage with convert");
                                        ReportPostProcessingFailure(video, $"ffmpeg failed to convert the file (exit code {ffmpeg.ExitCode})");
                                        return;
                                    }

                                    if (TagAudioFile)
                                    {
                                        try
                                        {
                                            var videoIndex = indexes[video];
                                            var afterTagName = await GlobalConsts.TagFile(video, videoIndex, outputFileLoc, Playlist);
                                            FileType = new String(copyFileLoc.Skip(copyFileLoc.LastIndexOf('.') + 1).ToArray());
                                            if (afterTagName != outputFileLoc)
                                            {
                                                if (playlistId.HasValue)
                                                    video = new PlaylistVideo(playlistId.Value, video.Id, afterTagName, video.Author, video.Duration, video.Thumbnails);
                                                cleanFileName = GlobalConsts.CleanFileName(downloadSettings.GetFilenameByPattern(video, videoIndex - 1, title, Playlist));
                                                copyFileLoc = $"{SavePath}\\{cleanFileName}.{FileType}";
                                            }
                                        }
                                        catch (Exception tagEx)
                                        {
                                            await GlobalConsts.Log(tagEx.ToString(), "TagFile at DownloadPage with convert");
                                        }
                                    }
                                    var copyFileLocCounter = 1;
                                    while (File.Exists(copyFileLoc))
                                    {
                                        copyFileLoc = $"{SavePath}\\{cleanFileName}-{copyFileLocCounter}.{FileType}";
                                        copyFileLocCounter++;
                                    }
                                    File.Copy(outputFileLoc, copyFileLoc, true);
                                    TryDelete(outputFileLoc, fileLoc);
                                }
                                catch (Exception ex)
                                {
                                    await GlobalConsts.Log(ex.ToString(), "DownloadPage with convert");
                                    ReportPostProcessingFailure(video, ex.Message);
                                }
                                finally
                                {
                                    UntrackFfmpeg(ffmpeg);
                                    Interlocked.Decrement(ref convertingCount);
                                }
                            };
                            if (GlobalConsts.settings.LimitConversions)
                                conversionTasks.Add(Task.Run(() => RunFfmpegAsync(ffmpeg, video, token)));
                            else
                                await RunFfmpegAsync(ffmpeg, video, token);
                        }
                        else
                        {
                            File.Copy(fileLoc, copyFileLoc, true);

                            File.Delete(fileLoc);
                            try
                            {
                                if (TagAudioFile)
                                {
                                    var afterTagName = await GlobalConsts.TagFile(video, i + 1, copyFileLoc, Playlist);
                                    if (afterTagName != outputFileLoc)
                                    {
                                        if (playlistId.HasValue)
                                            video = new PlaylistVideo(playlistId.Value, video.Id, afterTagName, video.Author, video.Duration, video.Thumbnails);
                                        cleanFileName = GlobalConsts.CleanFileName(downloadSettings.GetFilenameByPattern(video, i, title, Playlist));
                                        copyFileLoc = $"{SavePath}\\{cleanFileName}.{FileType}";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                await GlobalConsts.Log(ex.ToString(), "TagFile at download with convert playlist");
                            }
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
                        await GlobalConsts.Log(ex.ToString(), "DownloadPage DownloadWithConvert");
                        NotDownloaded.Add(new Tuple<IVideo, string>(video, ex.Message));
                    }
                }

            exit:

                shownFailures = await ShowNotDownloadedAsync(0);

                while (ffmpegList.Count > 0 || conversionTasks?.Count(x => !x.IsCompleted) > 0)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        string status = string.Concat(FindResource("StillConverting"), " ", convertingCount, " ", FindResource("files"));
                        HeadlineTextBlock.Text = (string)FindResource("AllDone");
                        CurrentDownloadProgressBar.IsIndeterminate = true;
                        TotalDownloadedGrid.Visibility = Visibility.Collapsed;
                        TotalDownloadsProgressBarTextBlock.Text = $"({DownloadedCount} {FindResource("Of")} {Maximum})";
                        DownloadedVideosProgressBar.Value = Maximum;
                        ConvertingTextBlock.Visibility = Visibility.Visible;
                        ConvertingTextBlock.Text = status;
                        CurrentDownloadProgressBarTextBlock.Visibility = Visibility.Collapsed;
                        DownloadSpeedTextBlock.Visibility = Visibility.Collapsed;
                        CurrentStatus = status;
                        CurrentDownloadSpeed = "0 MiB/s";
                        CurrentProgressPercent = 100;
                        Title = string.Concat(FindResource("Converting"));

                    });
                    await Task.Delay(1000, token);
                }

                await ShowNotDownloadedAsync(shownFailures);

                await Dispatcher.InvokeAsync(() =>
                {
                    string allDone = (string)FindResource("AllDone");
                    CurrentDownloadGrid.Visibility = Visibility.Collapsed;
                    ConvertingTextBlock.Visibility = Visibility.Collapsed;
                    DownloadSpeedTextBlock.Visibility = Visibility.Collapsed;
                    CurrentStatus = string.Empty;
                    CurrentDownloadSpeed = string.Empty;
                    CurrentProgressPercent = 100;
                    Title = allDone;
                    HeadlineTextBlock.Text = allDone;
                    TotalDownloadedGrid.Visibility = Visibility.Collapsed;
                    TotalDownloadsProgressBarTextBlock.Text = $"({DownloadedCount} {FindResource("Of")} {Maximum})";
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
                await Task.WhenAll(conversionTasks);
                StillDownloading = false;
                Dispose();
            }
        }

        public async Task StartDownloading(CancellationToken token)
        {
            if (StartIndex > Videos.Count() - 1)
            {
                await GlobalConsts.ShowMessage($"{FindResource("NoVideosToDownload")}", $"{FindResource("ThereAreNoVideosToDownload")}");
                StillDownloading = false;
                GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
            }

            var client = GlobalConsts.YoutubeClient;
            var shownFailures = 0;
            conversionTasks.Clear();
            for (int i = StartIndex; i <= EndIndex; i++)
            {
                var video = Videos.ElementAtOrDefault(i);

                if (video == default(IVideo))
                    goto exit;

                try
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        DownloadSpeedTextBlock.Visibility = Visibility.Collapsed;
                        Update(0, video);
                    });

                    downloadSpeeds.Clear();

                    var streamInfoSet = await client.Videos.Streams.GetManifestAsync(video.Id, token);
                    IVideoStreamInfo bestQuality = null;
                    IStreamInfo bestAudio = null;

                    var videoList = streamInfoSet.GetVideoOnlyStreams().OrderByDescending(x => x.VideoQuality == Quality);

                    if (PreferHighestFPS)
                        videoList = videoList.ThenByDescending(x => x.VideoQuality.Framerate).ThenBy(x => Math.Abs(x.VideoQuality.MaxHeight - Quality.MaxHeight));
                    else
                        videoList = videoList.ThenBy(x => Math.Abs(x.VideoQuality.MaxHeight - Quality.MaxHeight));

                    bestQuality = videoList.FirstOrDefault();
                    if (downloadSettings.VideoLanguage == "default")
                    {
                        var defaultLangaugeVideos = streamInfoSet.GetAudioOnlyStreams().Where(x => x.IsAudioLanguageDefault.HasValue && x.IsAudioLanguageDefault.Value);
                        if (defaultLangaugeVideos.Any())
                        {
                            bestAudio = defaultLangaugeVideos.GetWithHighestBitrate();
                        }
                        else
                        {
                            bestAudio = streamInfoSet.GetAudioOnlyStreams().GetWithHighestBitrate();
                        }
                    }
                    else
                    {
                        var videoesInRequestedLanguage = streamInfoSet.GetAudioOnlyStreams().Where(x => x.AudioLanguage.HasValue && x.AudioLanguage.Value.Code.Contains(downloadSettings.VideoLanguage));
                        if (videoesInRequestedLanguage.Any())
                        {
                            bestAudio = videoesInRequestedLanguage.GetWithHighestBitrate();
                        }
                        else
                        {
                            var defaultAudioStreams = streamInfoSet.GetAudioOnlyStreams().Where(x => x.IsAudioLanguageDefault.HasValue && x.IsAudioLanguageDefault.Value);
                            if (defaultAudioStreams.Any())
                            {
                                bestAudio = defaultAudioStreams.GetWithHighestBitrate();
                            }
                            else
                            {
                                bestAudio = streamInfoSet.GetAudioOnlyStreams().GetWithHighestBitrate();
                            }
                        }
                    }
                    var cleanVideoNameWithId = GlobalConsts.CleanFileName(video.Title + video.Id);
                    var cleanVideoName = GlobalConsts.CleanFileName(downloadSettings.GetFilenameByPattern(video, i, title, Playlist));
                    var fileLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoNameWithId}";
                    var outputFileLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoNameWithId}.{VideoSaveFormat}";
                    var copyFileLoc = $"{SavePath}\\{cleanVideoName}.{VideoSaveFormat}";
                    var audioLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoNameWithId}-audio.{bestAudio.Container.Name}";
                    var captionsLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoNameWithId}.srt";

                    if (GlobalConsts.DownloadSettings.SkipExisting && File.Exists(copyFileLoc))
                    {
                        CurrentStatus = string.Concat(FindResource("Skipping"));
                        CurrentTitle = video.Title;
                        CurrentProgressPercent = 0;
                        CurrentDownloadSpeed = "0 MiB/s";

                        DownloadedCount++;
                        TotalDownloaded = $"({DownloadedCount}/{Maximum})";

                        continue;
                    }

                    CurrentStatus = string.Concat(FindResource("Downloading"));
                    CurrentTitle = video.Title;
                    CurrentProgressPercent = 0;
                    CurrentDownloadSpeed = "0 MiB/s";

                    string ffmpegArguments = "";

                    using (var stream = new ProgressStream(File.Create(fileLoc)))
                    {
                        Stopwatch sw = new();
                        TimeSpan ts = new(0);
                        int seconds = 1;
                        string downloadSpeedText = (string)FindResource("DownloadSpeed");

                        stream.BytesWritten += async (sender, args) =>
                        {
                            try
                            {
                                var percent = Convert.ToInt32(args.StreamLength * 100 / bestQuality.Size.Bytes);
                                CurrentProgressPercent = percent;
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
                                        CurrentDownloadProgressBar.Value = percent;
                                        CurrentDownloadProgressBarTextBlock.Text = $"{percent}%";
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
                            var audioTask = client.Videos.Streams.CopyToAsync(bestAudio, audioStream, cancellationToken: token);

                        caption:
                            if (DownloadCaptions)
                            {
                                var captionsInfo = await client.Videos.ClosedCaptions.GetManifestAsync(video.Id, token);
                                var captions = captionsInfo.TryGetByLanguage(CaptionsLanguage);

                                if (captions == null)
                                {
                                    DownloadCaptions = false;
                                    goto caption;
                                }

                                var captionsTask = client.Videos.ClosedCaptions.DownloadAsync(captions, captionsLoc, cancellationToken: token);
                                await ExtensionMethods.WhenAll(videoTask, audioTask, captionsTask);
                                sw.Stop();
                                if (VideoSaveFormat != "mkv")
                                {
                                    ffmpegArguments = $"-i \"{fileLoc}\" -i \"{audioLoc}\" -y -c copy \"{outputFileLoc}\"";
                                    File.Copy(captionsLoc, $"{SavePath}\\{cleanVideoName}.srt");
                                }
                                else
                                {
                                    ffmpegArguments = $"-i \"{fileLoc}\" -i \"{audioLoc}\" -i \"{captionsLoc}\" -y -c copy \"{outputFileLoc}\"";
                                }
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
                            if (token.IsCancellationRequested)
                                return;

                            if (!File.Exists(outputFileLoc))
                            {
                                await GlobalConsts.Log($"ffmpeg exited with code {ffmpeg.ExitCode} and did not produce {outputFileLoc}", "DownloadPage without convert");
                                ReportPostProcessingFailure(video, $"ffmpeg failed to merge the streams (exit code {ffmpeg.ExitCode})");
                                return;
                            }

                            var copyFileLocCounter = 1;
                            while (File.Exists(copyFileLoc))
                            {
                                copyFileLoc = $"{SavePath}\\{cleanVideoName}-{copyFileLocCounter}.{VideoSaveFormat}";
                                copyFileLocCounter++;
                            }
                            File.Copy(outputFileLoc, copyFileLoc, true);
                            TryDelete(outputFileLoc, audioLoc, fileLoc);
                        }
                        catch (Exception ex)
                        {
                            await GlobalConsts.Log(ex.ToString(), "DownloadPage without convert");
                            ReportPostProcessingFailure(video, ex.Message);
                        }
                        finally
                        {
                            UntrackFfmpeg(ffmpeg);
                            Interlocked.Decrement(ref convertingCount);
                        }
                    };

                    if (GlobalConsts.settings.LimitConversions)
                        conversionTasks.Add(Task.Run(() => RunFfmpegAsync(ffmpeg, video, token)));
                    else
                        await RunFfmpegAsync(ffmpeg, video, token);

                    DownloadedCount++;
                    TotalDownloaded = $"({DownloadedCount}/{Maximum})";

                }
                catch (OperationCanceledException)
                {
                    goto exit;
                }
                catch (Exception ex)
                {
                    await GlobalConsts.Log(ex.ToString(), "DownloadPage DownloadWithConvert");
                    NotDownloaded.Add(new Tuple<IVideo, string>(video, ex.Message));
                }
            }

        exit:

            shownFailures = await ShowNotDownloadedAsync(0);

            while (ffmpegList.Count > 0 || conversionTasks?.Count(x => !x.IsCompleted) > 0)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    string status = string.Concat(FindResource("StillConverting"), " ", convertingCount, " ", FindResource("files"));
                    HeadlineTextBlock.Text = (string)FindResource("AllDone");
                    CurrentDownloadProgressBar.IsIndeterminate = true;
                    TotalDownloadedGrid.Visibility = Visibility.Collapsed;
                    TotalDownloadsProgressBarTextBlock.Text = $"({DownloadedCount} {FindResource("Of")} {Maximum})";
                    DownloadedVideosProgressBar.Value = Maximum;
                    ConvertingTextBlock.Visibility = Visibility.Visible;
                    ConvertingTextBlock.Text = status;
                    CurrentDownloadProgressBarTextBlock.Visibility = Visibility.Collapsed;
                    DownloadSpeedTextBlock.Visibility = Visibility.Collapsed;
                    CurrentStatus = status;
                    CurrentDownloadSpeed = "0 MiB/s";
                    CurrentProgressPercent = 100;
                    Title = string.Concat(FindResource("Converting"));
                });
                await Task.Delay(1000, token);
            }

            await ShowNotDownloadedAsync(shownFailures);

            StillDownloading = false;

            await Dispatcher.InvokeAsync(() =>
            {
                string allDone = (string)FindResource("AllDone");
                CurrentDownloadGrid.Visibility = Visibility.Collapsed;
                ConvertingTextBlock.Visibility = Visibility.Collapsed;
                DownloadSpeedTextBlock.Visibility = Visibility.Collapsed;
                CurrentStatus = string.Empty;
                CurrentDownloadSpeed = string.Empty;
                CurrentProgressPercent = 100;
                Title = allDone;
                HeadlineTextBlock.Text = allDone;
                TotalDownloadedGrid.Visibility = Visibility.Collapsed;
                TotalDownloadsProgressBarTextBlock.Text = $"({DownloadedCount} {FindResource("Of")} {Maximum})";
                DownloadedVideosProgressBar.Value = Maximum;
                CurrentDownloadProgressBarTextBlock.Visibility = Visibility.Collapsed;
            });

            await Task.WhenAll(conversionTasks);

            if (GlobalConsts.DownloadSettings.OpenDestinationFolderWhenDone)
                OpenFolder_Click(null, null);

            Dispose();
        }

        private async Task RunFfmpegAsync(Process ffmpeg, IVideo video, CancellationToken token)
        {
            var locker = GlobalConsts.settings.LimitConversions ? GlobalConsts.ConversionsLocker : null;
            var acquired = false;
            Interlocked.Increment(ref convertingCount);
            try
            {
                if (locker != null)
                {
                    await locker.WaitAsync(token);
                    acquired = true;
                    ffmpeg.Exited += (x, y) => locker.Release();
                }

                // Tracked before Start so a fast exit can't run its cleanup before the process is in the list.
                lock (ffmpegLock)
                    ffmpegList?.Add(ffmpeg);
                ffmpeg.Start();
            }
            catch (Exception ex)
            {
                UntrackFfmpeg(ffmpeg);
                Interlocked.Decrement(ref convertingCount);
                if (acquired)
                    locker.Release();

                if (ex is not OperationCanceledException)
                {
                    await GlobalConsts.Log(ex.ToString(), "RunFfmpegAsync at DownloadPage.xaml.cs");
                    ReportPostProcessingFailure(video, $"Could not start ffmpeg: {ex.Message}");
                }
            }
        }

        private void UntrackFfmpeg(Process ffmpeg)
        {
            lock (ffmpegLock)
                ffmpegList?.Remove(ffmpeg);
        }

        private void ReportPostProcessingFailure(IVideo video, string reason)
        {
            var failures = NotDownloaded;
            if (failures == null)
                return;

            lock (failures)
                failures.Add(new Tuple<IVideo, string>(video, reason));
            Interlocked.Decrement(ref DownloadedCount);
        }

        private static void TryDelete(params string[] paths)
        {
            foreach (var path in paths)
            {
                try { File.Delete(path); } catch { }
            }
        }

        private async Task<int> ShowNotDownloadedAsync(int alreadyShown)
        {
            var all = NotDownloaded;
            if (all == null)
                return alreadyShown;

            List<Tuple<IVideo, string>> failed;
            lock (all)
            {
                failed = all.Skip(alreadyShown).ToList();
                alreadyShown = all.Count;
            }

            if (failed.Count > 0)
            {
                await GlobalConsts.ShowSelectableDialog($"{FindResource("CouldntDownload")}",
                      string.Concat($"{FindResource("ListOfNotDownloadedVideos")}\n", string.Join("\n", failed.Select(x => string.Concat(x.Item1.Title, " Reason: ", x.Item2)))),
                      () =>
                      {
                          _ = SequenceDownload(failed.Select(x => x.Item1.Url), downloadSettings).ConfigureAwait(false);
                          GlobalConsts.MainPage.ChangeToQueueTab();
                          GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
                      });
            }

            return alreadyShown;
        }

        private void Background_Exit(object sender, RoutedEventArgs e)
        {
            GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
        }

        private void Update(int percent, IVideo video)
        {
            CurrentDownloadProgressBar.Value = percent;
            HeadlineTextBlock.Text = (string)FindResource("CurrentlyDownloading") + video.Title;
            CurrentDownloadProgressBarTextBlock.Text = $"{percent}%";
            TotalDownloadsProgressBarTextBlock.Text = $"{DownloadedCount} {FindResource("Of")} {Maximum}";
            DownloadedVideosProgressBar.Value = DownloadedCount;
        }

        public DownloadPage LoadFromSilent()
        {
            GlobalConsts.HideSettingsButton();
            GlobalConsts.HideAboutButton();
            GlobalConsts.HideHomeButton();
            GlobalConsts.HideHelpButton();
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
                Process.Start(Environment.GetEnvironmentVariable("WINDIR") + @"\explorer.exe", SavePath);
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
                VideoSaveFormat = null;
                Bitrate = null;
                NotDownloaded = null;
                Videos = null;
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
