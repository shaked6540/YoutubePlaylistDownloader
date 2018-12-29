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
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;
using YoutubePlaylistDownloader.Objects;
using System.ComponentModel;
using YoutubeExplode;

namespace YoutubePlaylistDownloader
{
    /// <summary>
    /// Interaction logic for DownloadPage.xaml
    /// </summary>
    public partial class DownloadPage : UserControl, IDisposable, IDownload
    {
        private Playlist Playlist;
        private string FileType, CaptionsLanguage;
        private int DownloadedCount, StartIndex, EndIndex, Maximum;
        private List<Process> ffmpegList;
        private CancellationTokenSource cts;
        private VideoQuality Quality;
        private string Bitrate;
        private List<Tuple<string, string>> NotDownloaded;
        private IEnumerable<Video> Videos;
        private bool AudioOnly, PreferHighestFPS, DownloadCaptions;
        private string SavePath;
        private Subscription Subscription;
        const int megaBytes = 1 << 20;
        private bool silent;
        private FixedQueue<double> downloadSpeeds;
        private Dictionary<Video, int> indexes = new Dictionary<Video, int>();

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


        public DownloadPage(Playlist playlist, DownloadSettings settings, int startIndex = 0, int endIndex = 0, string savePath = "",
            IEnumerable<Video> videos = null, Subscription subscription = null,
            bool silent = false, CancellationTokenSource cancellationToken = null)
        {
            InitializeComponent();
            downloadSpeeds = new FixedQueue<double>(50);

            if (playlist == null)
                Videos = videos;
            else
                Videos = playlist.Videos;

            startIndex = startIndex <= 0 ? 0 : startIndex;
            endIndex = endIndex <= 0 ? Videos.Count() - 1 : endIndex;

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
            NotDownloaded = new List<Tuple<string, string>>();
            Maximum = EndIndex - StartIndex + 1;
            DownloadedVideosProgressBar.Maximum = Maximum;
            Playlist = playlist;
            FileType = settings.SaveFormat;
            DownloadedCount = 0;
            Quality = settings.Quality;
            DownloadCaptions = settings.DownloadCaptions;
            CaptionsLanguage = settings.CaptionsLanguage;
            SavePath = string.IsNullOrWhiteSpace(savePath) ? GlobalConsts.SaveDirectory : savePath;

            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);

            Subscription = subscription;
            AudioOnly = settings.AudioOnly;
            PreferHighestFPS = settings.PreferHighestFPS;

            if (!string.IsNullOrWhiteSpace(settings.Bitrate) && settings.Bitrate.All(x=> char.IsDigit(x)))
                Bitrate = $"-b:a {settings.Bitrate}k";
            else
                Bitrate = string.Empty;

            StillDownloading = true;

            ImageUrl = $"https://img.youtube.com/vi/{Videos?.FirstOrDefault()?.Id}/0.jpg";
            Title = playlist?.Title;
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

        public static async Task SequenceDownload(string[] links, DownloadSettings settings, bool silent = false)
        {
            var client = GlobalConsts.YoutubeClient;
            Playlist playlist;
            List<Video> videos = new List<Video>();
            var notDownloaded = new List<(string, string)>();
            foreach (var link in links)
            {
                async Task Download(Playlist playlistD, IEnumerable<Video> videosD)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var downloadPage = new DownloadPage(playlistD, settings, videos: videosD, silent: silent);
                    });
                }
                try
                {
                    if (YoutubeClient.TryParsePlaylistId(link, out string playlistId))
                    {
                        playlist = await client.GetPlaylistAsync(playlistId).ConfigureAwait(false);
                        videos.Clear();
                        await Download(playlist, videos);
                    }
                    else if (YoutubeClient.TryParseChannelId(link, out string channelId))
                    {
                        playlist = await client.GetPlaylistAsync((await client.GetChannelAsync(channelId).ConfigureAwait(false)).GetChannelVideosPlaylistId());
                        videos.Clear();
                        await Download(playlist, videos);
                    }
                    else if (YoutubeClient.TryParseUsername(link, out string username))
                    {
                        string channelID = await client.GetChannelIdAsync(username).ConfigureAwait(false);
                        var channel = await client.GetChannelAsync(channelID).ConfigureAwait(false);
                        playlist = await client.GetPlaylistAsync(channel.GetChannelVideosPlaylistId()).ConfigureAwait(false);
                        videos.Clear();
                        await Download(playlist, videos);
                    }
                    else if (YoutubeClient.TryParseVideoId(link, out string videoId))
                    {
                        var video = await client.GetVideoAsync(videoId);
                        videos.Clear();
                        videos.Add(video);
                        playlist = null;
                        await Download(playlist, videos);
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
                await GlobalConsts.ShowMessage($"{Application.Current.FindResource("CouldntDownload")}",
                    string.Concat($"{Application.Current.FindResource("ListOfNotDownloadedVideos")}\n",
                    string.Join("\n", notDownloaded.Select(x => string.Concat(x.Item1, " Reason: ", x.Item2)))));
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
                for (int i = StartIndex; i <= EndIndex; i++)
                {
                    var video = Videos.ElementAtOrDefault(i);

                    if (video == default(Video))
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

                        var streamInfoSet = await client.GetVideoMediaStreamInfosAsync(video.Id);
                        var bestQuality = streamInfoSet.Audio.MaxBy(x => x.AudioEncoding);
                        var cleanFileName = GlobalConsts.CleanFileName(video.Title);
                        var fileLoc = $"{GlobalConsts.TempFolderPath}{cleanFileName}";

                        if (AudioOnly)
                            FileType = bestQuality.Container.GetFileExtension();

                        var outputFileLoc = $"{GlobalConsts.TempFolderPath}{cleanFileName}.{FileType}";
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
                                    var precent = Convert.ToInt32(args.StreamLength * 100 / bestQuality.Size);
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
                                catch(OperationCanceledException)
                                {

                                }
                                catch(Exception ex)
                                {
                                    await GlobalConsts.Log(ex.ToString(), "BytesWrittenEventHandler at ProgressStream in DownloadPage");
                                }

                            };
                            await client.DownloadMediaStreamAsync(bestQuality, stream, cancellationToken: token);
                            sw.Stop();
                        }
                        if (!AudioOnly)
                        {
                            var ffmpeg = new Process()
                            {
                                EnableRaisingEvents = true,
                                StartInfo = new ProcessStartInfo()
                                {
                                    FileName = $"{GlobalConsts.CurrentDir}\\ffmpeg.exe",
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
                                    await GlobalConsts.TagFile(video, indexes[video], outputFileLoc, Playlist);

                                    File.Copy(outputFileLoc, copyFileLoc, true);
                                    File.Delete(outputFileLoc);

                                    if (Subscription != null)
                                        Subscription.LatestVideoDownloaded = video.UploadDate.ToUniversalTime().Date;
                                }
                                catch(Exception ex)
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
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        await GlobalConsts.ConversionsLocker.WaitAsync(cts.Token);
                                        ffmpeg.Start();
                                        convertingCount++;
                                        ffmpeg.Exited += (x, y) => GlobalConsts.ConversionsLocker.Release();
                                        ffmpegList.Add(ffmpeg);
                                    }
                                    catch (OperationCanceledException)
                                    {

                                    }
                                    catch (Exception ex)
                                    {
                                        await GlobalConsts.Log(ex.ToString(), "ConvertionLocker at StartDownloadingWithConverting at DownloadPage.xaml.cs");
                                    }
                                });
                            }
                        }
                        else
                        {
                            File.Copy(fileLoc, copyFileLoc, true);

                            if (Subscription != null)
                                Subscription.LatestVideoDownloaded = video.UploadDate.ToUniversalTime().Date;

                            File.Delete(fileLoc);
                            try
                            {
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
                        NotDownloaded.Add(new Tuple<string, string>(video.Title, ex.Message));
                    }
                }

                exit:

                if (NotDownloaded.Any())
                    await GlobalConsts.ShowMessage($"{FindResource("CouldntDownload")}", string.Concat($"{FindResource("ListOfNotDownloadedVideos")}\n", string.Join("\n", NotDownloaded.Select(x => string.Concat(x.Item1, " Reason: ", x.Item2)))));

                while (ffmpegList.Count > 0)
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

            }
            catch (Exception ex)
            {
                await GlobalConsts.Log(ex.ToString(), "DownloadPage With converting");

            }
            finally
            {
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
            for (int i = StartIndex; i <= EndIndex; i++)
            {
                var video = Videos.ElementAtOrDefault(i);

                if (video == default(Video))
                    goto exit;

                try
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        DownloadSpeedTextBlock.Visibility = Visibility.Collapsed;
                        Update(0, video);
                    });

                    downloadSpeeds.Clear();

                    var streamInfoSet = await client.GetVideoMediaStreamInfosAsync(video.Id);
                    MediaStreamInfo bestQuality, bestAudio = null;

                    var videoList = streamInfoSet.Video.OrderByDescending(x => x.VideoQuality == Quality);

                    if (PreferHighestFPS)
                        videoList = videoList.ThenByDescending(x => x.Framerate).ThenByDescending(x => x.VideoQuality > Quality).ThenByDescending(x => x.VideoQuality);
                    else
                        videoList = videoList.ThenByDescending(x => x.VideoQuality > Quality).ThenByDescending(x => x.VideoQuality);

                    bestQuality = videoList.FirstOrDefault();
                    bestAudio = streamInfoSet.Audio.MaxBy(x => x.AudioEncoding);

                    var cleanVideoName = GlobalConsts.CleanFileName(video.Title);
                    var fileLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoName}";
                    var outputFileLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoName}.mkv";
                    var copyFileLoc = $"{SavePath}\\{cleanVideoName}.mkv";
                    var audioLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoName}.{bestAudio.Container.GetFileExtension()}";
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
                                var precent = Convert.ToInt32(args.StreamLength * 100 / bestQuality.Size);
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
                                    catch(Exception ex)
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
                        var videoTask = client.DownloadMediaStreamAsync(bestQuality, stream, cancellationToken: token);
                        using (var audioStream = File.Create(audioLoc))
                        {
                            var audioTask = client.DownloadMediaStreamAsync(bestAudio, audioStream);

                            caption:
                            if (DownloadCaptions)
                            {
                                var captionsInfo = await client.GetVideoClosedCaptionTrackInfosAsync(video.Id);
                                var captions = captionsInfo.FirstOrDefault(x => x.Language.Name.Equals(CaptionsLanguage, StringComparison.OrdinalIgnoreCase));

                                if (captions == default)
                                {
                                    DownloadCaptions = false;
                                    goto caption;
                                }

                                using (var captionsStream = File.Create(captionsLoc))
                                {
                                    var captionsTask = client.DownloadClosedCaptionTrackAsync(captions, captionsStream, cancellationToken: token);
                                    await Task.WhenAll(videoTask, audioTask, captionsTask);
                                    sw.Stop();
                                    ffmpegArguments = $"-i \"{fileLoc}\" -i \"{audioLoc}\" -i \"{captionsLoc}\" -y -c copy \"{outputFileLoc}\"";
                                }
                            }
                            else
                            {
                                ffmpegArguments = $"-i \"{fileLoc}\" -i \"{audioLoc}\" -y -c copy \"{outputFileLoc}\"";
                                await Task.WhenAll(videoTask, audioTask);
                                sw.Stop();
                            }
                        }
                    }

                    var ffmpeg = new Process()
                    {
                        EnableRaisingEvents = true,
                        StartInfo = new ProcessStartInfo()
                        {
                            FileName = $"{GlobalConsts.CurrentDir}\\ffmpeg.exe",
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
                        catch(Exception ex)
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
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await GlobalConsts.ConversionsLocker.WaitAsync(cts.Token);
                                ffmpeg.Start();
                                convertingCount++;
                                ffmpeg.Exited += (x, y) => GlobalConsts.ConversionsLocker.Release();
                                ffmpegList.Add(ffmpeg);
                            }
                            catch (OperationCanceledException)
                            {

                            }
                            catch (Exception ex)
                            {
                                await GlobalConsts.Log(ex.ToString(), "ConvertionLocker at StartDownloading at DownloadPage.xaml.cs");
                            }
                        });
                    }
                    ffmpegList.Add(ffmpeg);
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
                    NotDownloaded.Add(new Tuple<string, string>(video.Title, ex.Message));
                }
            }

            exit:

            if (NotDownloaded.Any())
                await GlobalConsts.ShowMessage($"{FindResource("CouldntDownload")}", string.Concat($"{FindResource("ListOfNotDownloadedVideos")}\n", string.Join("\n", NotDownloaded.Select(x => string.Concat(x.Item1, " Reason: ", x.Item2)))));

            while (ffmpegList.Count > 0)
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
                Subscription.LatestVideoDownloaded = Videos.MaxBy(x => x.UploadDate).UploadDate.DateTime.Date;

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

            Dispose();
        }

        private void Background_Exit(object sender, RoutedEventArgs e)
        {
            GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
        }

        private void Update(int precent, Video video)
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

        private async Task<bool> ExitAsync()
        {
            cts?.Cancel(true);
            if (ffmpegList.Count > 0)
            {
                var yesno = await GlobalConsts.ShowYesNoDialog($"{FindResource("StillConverting")}", $"{FindResource("StillConverting")} {ffmpegList.Count} {FindResource("files")} {FindResource("AreYouSureExit")}");
                if (yesno == MahApps.Metro.Controls.Dialogs.MessageDialogResult.Negative)
                    return false;
            }
            ffmpegList.ForEach(x => { try { x.Kill(); } catch { } });
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
                SavePath = null;
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