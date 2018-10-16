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

namespace YoutubePlaylistDownloader
{
    /// <summary>
    /// Interaction logic for DownloadPage.xaml
    /// </summary>
    public partial class DownloadPage : UserControl, IDisposable, IDownload
    {
        private Playlist Playlist;
        private string FileType;
        private int DownloadedCount, StartIndex, EndIndex, Maximum;
        private List<Process> ffmpegList;
        private CancellationTokenSource cts;
        private VideoQuality Quality;
        private string Bitrate;
        private List<Tuple<string, string>> NotDownloaded;
        private IEnumerable<Video> Videos;
        private bool AudioOnly, PreferHighestFPS;
        private string SavePath;
        private Subscription Subscription;
        const int megaBytes = 1 << 20;
        private bool silent;
        private FixedQueue<double> downloadSpeeds;

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


        public DownloadPage(Playlist playlist, bool convert, VideoQuality quality = VideoQuality.High720, string fileType = "mp3",
            string bitrate = null, int startIndex = 0, int endIndex = 0, bool audioOnly = false,
            bool preferHighestFPS = false, string savePath = "", IEnumerable<Video> videos = null, Subscription subscription = null, bool silent = false, CancellationTokenSource cancellationToken = null)
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
            FileType = fileType;
            DownloadedCount = 0;
            Quality = quality;
            SavePath = string.IsNullOrWhiteSpace(savePath) ? GlobalConsts.SaveDirectory : savePath;

            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);

            Subscription = subscription;
            AudioOnly = audioOnly;
            PreferHighestFPS = preferHighestFPS;

            if (!string.IsNullOrWhiteSpace(bitrate) && bitrate.All(x=> char.IsDigit(x)))
                Bitrate = $"-b:a {bitrate}k";
            else
                Bitrate = string.Empty;

            StillDownloading = true;

            ImageUrl = $"https://img.youtube.com/vi/{Videos?.FirstOrDefault()?.Id}/0.jpg";
            Title = playlist?.Title;
            CurrentTitle = (string)FindResource("Loading");
            TotalDownloaded = $"(0/{Maximum})";
            CurrentProgressPrecent = 0;
            CurrentDownloadSpeed = $"{FindResource("DownloadSpeed")}: 0 MiB/s";

            if (convert || audioOnly)
                StartDownloadingWithConverting(cts.Token).ConfigureAwait(false);
            else
                StartDownloading(cts.Token).ConfigureAwait(false);

            GlobalConsts.Downloads.Add(new QueuedDownload(this));
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
                                        CurrentDownloadProgressBar.Value = precent;
                                        CurrentDownloadProgressBarTextBlock.Text = $"{precent}%";
                                        if (sw.Elapsed.Seconds == seconds && delta.TotalMilliseconds > 0)
                                        {
                                            CurrentDownloadSpeed = string.Concat(downloadSpeedText, Math.Round(downloadSpeeds.Average(), 2), " MiB/s");
                                            DownloadSpeedTextBlock.Text = CurrentDownloadSpeed;
                                            DownloadSpeedTextBlock.Visibility = Visibility.Visible;
                                            seconds += 1;
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
                                    await GlobalConsts.TagFile(video, i + 1, outputFileLoc, Playlist);

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
                            ffmpeg.Start();
                            ffmpegList.Add(ffmpeg);
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
                        string status = string.Concat(FindResource("StillConverting"), " ", ffmpegList.Count, " ", FindResource("files"));
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
                        CurrentTitle = string.Empty;

                    });
                    await Task.Delay(1000);
                }

                CurrentDownloadGrid.Visibility = Visibility.Collapsed;
                ConvertingTextBlock.Visibility = Visibility.Collapsed;
                DownloadSpeedTextBlock.Visibility = Visibility.Collapsed;
                CurrentStatus = string.Empty;
                CurrentDownloadSpeed = string.Empty;
                CurrentProgressPrecent = 100;
                Title = string.Concat(FindResource("AllDone"));
                CurrentTitle = string.Empty;
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
                            await Task.WhenAll(videoTask, audioTask);
                            sw.Stop();
                        }
                    }

                    var ffmpeg = new Process()
                    {
                        EnableRaisingEvents = true,
                        StartInfo = new ProcessStartInfo()
                        {
                            FileName = $"{GlobalConsts.CurrentDir}\\ffmpeg.exe",
                            Arguments = $"-i \"{fileLoc}\" -i \"{audioLoc}\" -y -c copy \"{outputFileLoc}\"",
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
                    ffmpeg.Start();
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
                    string status = string.Concat(FindResource("StillConverting"), " ", ffmpegList.Count, " ", FindResource("files"));
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
                    CurrentTitle = string.Empty;
                });
                await Task.Delay(1000);
            }

            if (Subscription != null)
                Subscription.LatestVideoDownloaded = Videos.MaxBy(x => x.UploadDate).UploadDate.DateTime.Date;

            StillDownloading = false;

            CurrentDownloadGrid.Visibility = Visibility.Collapsed;
            TotalDownloadedGrid.Visibility = Visibility.Collapsed;
            ConvertingTextBlock.Visibility = Visibility.Collapsed;
            DownloadSpeedTextBlock.Visibility = Visibility.Collapsed;
            CurrentStatus = string.Empty;
            CurrentDownloadSpeed = string.Empty;
            CurrentProgressPrecent = 100;
            Title = string.Concat(FindResource("AllDone"));
            CurrentTitle = string.Empty;

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
                var yesno = await GlobalConsts.ShowYesNoDialog($"{FindResource("StillConverting")}", $"{FindResource("StillConverting")} {ffmpegList.Count(x => !x.HasExited)} {FindResource("files")} {FindResource("AreYouSureExit")}");
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