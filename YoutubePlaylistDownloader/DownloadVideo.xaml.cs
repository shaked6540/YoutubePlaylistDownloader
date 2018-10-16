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
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;
using YoutubePlaylistDownloader.Objects;

namespace YoutubePlaylistDownloader
{
    /// <summary>
    /// Interaction logic for DownloadVideo.xaml
    /// </summary>
    public partial class DownloadVideo : UserControl, IDisposable, IDownload
    {
        private Video Video;
        private string FileType;
        private int DownloadedCount;
        private List<Process> ffmpegList;
        private CancellationTokenSource cts;
        private VideoQuality Quality;
        private string Bitrate;
        private bool AudioOnly, PreferHighestFPS;
        private List<Tuple<string, string>> NotDownloaded;
        const int megaBytes = 1 << 20;
        private bool silent;
        private FixedQueue<double> downloadSpeeds;

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
            get => 1;
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


        public DownloadVideo(Video video, bool convert, VideoQuality quality = VideoQuality.High720, string fileType = "mp3", string bitrate = null,
            bool audioOnly = false, bool preferHighestFPS = false, bool silent = false)
        {
            InitializeComponent();
            downloadSpeeds = new FixedQueue<double>(50);

            if (!silent)
            {
                GlobalConsts.HideSettingsButton();
                GlobalConsts.HideAboutButton();
                GlobalConsts.HideHomeButton();
                GlobalConsts.HideSubscriptionsButton();
                GlobalConsts.HideHelpButton();
            }
            this.silent = silent;
            cts = new CancellationTokenSource();
            ffmpegList = new List<Process>();
            NotDownloaded = new List<Tuple<string, string>>();
            Video = video;
            FileType = fileType;
            AudioOnly = audioOnly;
            PreferHighestFPS = preferHighestFPS;

            DownloadedCount = 0;
            Quality = quality;
            if (bitrate != null)
                Bitrate = $"-b:a {bitrate}k";
            else
                Bitrate = string.Empty;

            ImageUrl = $"https://img.youtube.com/vi/{video?.Id}/0.jpg";
            Title = string.Empty;
            CurrentStatus = (string)FindResource("Loading");
            TotalDownloaded = $"(0/1)";
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

            var client = GlobalConsts.YoutubeClient;
            try
            {
                await Dispatcher.InvokeAsync(() => Update(0, Video));
                downloadSpeeds.Clear();

                var streamInfoSet = await client.GetVideoMediaStreamInfosAsync(Video.Id);
                var cleanFileName = GlobalConsts.CleanFileName(Video.Title).Replace("$", "S");
                var bestQuality = streamInfoSet.Audio.MaxBy(x => x.AudioEncoding);
                var fileLoc = $"{GlobalConsts.TempFolderPath}{cleanFileName}";

                if (AudioOnly)
                    FileType = bestQuality.Container.GetFileExtension();

                var outputFileLoc = $"{GlobalConsts.TempFolderPath}{cleanFileName}.{FileType}";
                var copyFileLoc = $"{GlobalConsts.SaveDirectory}\\{cleanFileName}.{FileType}";

                CurrentTitle = Video.Title;
                CurrentStatus = (string)FindResource("Downloading");

                using (var stream = new ProgressStream(File.Create(fileLoc)))
                {
                    Stopwatch sw = new Stopwatch();
                    TimeSpan ts = new TimeSpan(0);
                    string downloadSpeedText = (string)FindResource("DownloadSpeed");
                    int seconds = 1;
                    stream.BytesWritten += async (sender, args) =>
                    {
                        try
                        {
                            var precent = Convert.ToInt32(args.StreamLength * 100 / bestQuality.Size);

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
                                CurrentProgressPrecent = precent;
                                CurrentDownloadProgressBarTextBlock.Text = $"{precent}%";
                                if (sw.Elapsed.Seconds == seconds && delta.TotalMilliseconds > 0)
                                {
                                    string speed = string.Concat(downloadSpeedText, Math.Round(downloadSpeeds.Average(), 2), " MiB/s");
                                    DownloadSpeedTextBlock.Text = speed;
                                    CurrentDownloadSpeed = speed;
                                    DownloadSpeedTextBlock.Visibility = Visibility.Visible;
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
                            Arguments = $"-i \"{fileLoc}\" -vn -y {Bitrate} \"{outputFileLoc}\"",
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
                            await GlobalConsts.TagFile(Video, 0, outputFileLoc);

                            File.Copy(outputFileLoc, copyFileLoc, true);
                            File.Delete(outputFileLoc);
                            File.Delete(fileLoc);
                        }
                        catch (Exception ex)
                        {
                            await GlobalConsts.Log(ex.ToString(), "DownloadVideo with convert");
                        }
                    };
                    ffmpeg.Start();
                    ffmpegList.Add(ffmpeg);
                }
                else
                {
                    File.Copy(fileLoc, copyFileLoc, true);
                    File.Delete(fileLoc);
                    try
                    {
                        await GlobalConsts.TagFile(Video, 0, copyFileLoc);
                    }
                    catch { }
                }

                DownloadedCount++;
                TotalDownloaded = "(1/1)";
            }
            catch (OperationCanceledException)
            {
                goto exit;
            }
            catch (Exception ex)
            {
                await GlobalConsts.Log(ex.ToString(), "DownloadVideo DownlaodWithConvert");
                NotDownloaded.Add(new Tuple<string, string>(Video.Title, ex.Message));
            }

            exit:

            if (NotDownloaded.Any())
                await GlobalConsts.ShowMessage($"{FindResource("CouldntDownload")}", string.Concat($"{FindResource("ListOfNotDownloadedVideos")}\n", string.Join("\n", NotDownloaded.Select(x => string.Concat(x.Item1, " Reason: ", x.Item2)))));


            while (ffmpegList.Count > 0)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    string converting = string.Concat(FindResource("StillConverting"), " ", ffmpegList.Count, " ", FindResource("files"));
                    HeadlineTextBlock.Text = (string)FindResource("AllDone");
                    CurrentDownloadProgressBar.IsIndeterminate = true;
                    ConvertingTextBlock.Visibility = Visibility.Visible;
                    ConvertingTextBlock.Text = converting;
                    CurrentDownloadProgressBarTextBlock.Visibility = Visibility.Collapsed;
                    DownloadSpeedTextBlock.Visibility = Visibility.Collapsed;
                    Title = string.Empty;
                    CurrentStatus = converting;
                    CurrentProgressPrecent = 100;
                    CurrentDownloadSpeed = string.Empty;
                });
                await Task.Delay(1000);
            }

            CurrentDownloadGrid.Visibility = Visibility.Collapsed;
            ConvertingTextBlock.Visibility = Visibility.Collapsed;
            Title = string.Empty;
            CurrentStatus = (string)FindResource("AllDone");
            CurrentProgressPrecent = 100;
            CurrentDownloadSpeed = string.Empty;

            Dispose();
        }

        public async Task StartDownloading(CancellationToken token)
        {
            var client = GlobalConsts.YoutubeClient;
            await Dispatcher.InvokeAsync(() => Update(0, Video));
            try
            {
                downloadSpeeds.Clear();

                var streamInfoSet = await client.GetVideoMediaStreamInfosAsync(Video.Id);
                MediaStreamInfo bestQuality, bestAudio = null;
                var videoList = streamInfoSet.Video.OrderByDescending(x => x.VideoQuality == Quality);

                if (PreferHighestFPS)
                    videoList = videoList.ThenByDescending(x => x.Framerate).ThenByDescending(x => x.VideoQuality > Quality).ThenByDescending(x => x.VideoQuality);
                else
                    videoList = videoList.ThenByDescending(x => x.VideoQuality > Quality).ThenByDescending(x => x.VideoQuality);

                bestQuality = videoList.FirstOrDefault();
                bestAudio = streamInfoSet.Audio.OrderByDescending(x => x.AudioEncoding).FirstOrDefault();
                var cleanVideoName = GlobalConsts.CleanFileName(Video.Title);
                var fileLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoName}";
                var outputFileLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoName}.mkv";
                var copyFileLoc = $"{GlobalConsts.SaveDirectory}\\{cleanVideoName}.mkv";

                string audioLoc = null;
                if (bestAudio != null)
                    audioLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoName}.{bestAudio.Container.GetFileExtension()}";

                CurrentTitle = Video.Title;
                CurrentStatus = (string)FindResource("Downloading");

                using (var stream = new ProgressStream(File.Create(fileLoc)))
                {
                    Stopwatch sw = new Stopwatch();
                    TimeSpan ts = new TimeSpan(0);
                    string downloadSpeedText = (string)FindResource("DownloadSpeed");
                    int seconds = 1;
                    stream.BytesWritten += async (sender, args) =>
                    {
                        try
                        {
                            var precent = Convert.ToInt32(args.StreamLength * 100 / bestQuality.Size);

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
                                CurrentProgressPrecent = precent;
                                CurrentDownloadProgressBarTextBlock.Text = $"{precent}%";
                                if (sw.Elapsed.Seconds == seconds && delta.TotalMilliseconds > 0)
                                {
                                    string speed = string.Concat(downloadSpeedText, Math.Round(downloadSpeeds.Average(), 2), " MiB/s");
                                    DownloadSpeedTextBlock.Text = speed;
                                    CurrentDownloadSpeed = speed;
                                    DownloadSpeedTextBlock.Visibility = Visibility.Visible;
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
                        File.Delete(outputFileLoc);
                        File.Delete(audioLoc);
                        File.Delete(fileLoc);
                    }
                    catch(Exception ex)
                    {
                        await GlobalConsts.Log(ex.ToString(), "DownloadVideo without convert");
                    }
                };
                ffmpeg.Start();
                ffmpegList.Add(ffmpeg);
                DownloadedCount++;
                TotalDownloaded = "(1/1)";


            }
            catch (OperationCanceledException)
            {
                goto exit;
            }
            catch (Exception ex)
            {
                await GlobalConsts.Log(ex.ToString(), "DownloadVideo Downlaod");
                NotDownloaded.Add(new Tuple<string, string>(Video.Title, ex.Message));
            }

            exit:

            if (NotDownloaded.Any())
                await GlobalConsts.ShowMessage($"{FindResource("CouldntDownload")}", string.Concat($"{FindResource("ListOfNotDownloadedVideos")}\n", string.Join("\n", NotDownloaded.Select(x => string.Concat(x.Item1, " Reason: ", x.Item2)))));

            while (ffmpegList.Count > 0)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    string converting = string.Concat(FindResource("StillConverting"), " ", ffmpegList.Count, " ", FindResource("files"));
                    HeadlineTextBlock.Text = (string)FindResource("AllDone");
                    CurrentDownloadProgressBar.IsIndeterminate = true;
                    ConvertingTextBlock.Visibility = Visibility.Visible;
                    ConvertingTextBlock.Text = converting;
                    CurrentDownloadProgressBarTextBlock.Visibility = Visibility.Collapsed;
                    DownloadSpeedTextBlock.Visibility = Visibility.Collapsed;
                    Title = string.Empty;
                    CurrentStatus = converting;
                    CurrentProgressPrecent = 100;
                    CurrentDownloadSpeed = string.Empty;
                });
                await Task.Delay(1000);
            }

            CurrentDownloadGrid.Visibility = Visibility.Collapsed;
            ConvertingTextBlock.Visibility = Visibility.Collapsed;
            Title = string.Empty;
            CurrentStatus = (string)FindResource("AllDone");
            CurrentProgressPrecent = 100;
            CurrentDownloadSpeed = string.Empty;

            Dispose();
        }

        private void Update(int precent, Video video)
        {
            CurrentDownloadProgressBar.Value = precent;
            HeadlineTextBlock.Text = (string)FindResource("CurrentlyDownlading") + video.Title;
            CurrentDownloadProgressBarTextBlock.Text = $"{precent}%";
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

        private void Background_Exit(object sender, RoutedEventArgs e)
        {
            GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
        }

        public async Task<bool> ExitAsync()
        {
            cts?.Cancel(true);
            if (ffmpegList.Count > 0)
            {
                var yesno = await GlobalConsts.ShowYesNoDialog($"{FindResource("StillConverting")}", $"{FindResource("StillConverting")} {ffmpegList.Count(x => !x.HasExited)} {FindResource("files")} {FindResource("AreYouSureExit")}");
                if (yesno == MahApps.Metro.Controls.Dialogs.MessageDialogResult.Negative)
                    return false;
            }
            ffmpegList.ForEach(x => { try { x.Kill(); } catch { } });

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
                    cts.Cancel(true);
                    cts.Dispose();
                    try
                    {
                        ffmpegList.ForEach(x => { try { x.Kill(); } catch { } });
                    }
                    catch { }
                    ffmpegList.Clear();
                    NotDownloaded.Clear();
                    downloadSpeeds.Clear();
                }

                Video = null;
                ffmpegList = null;
                disposedValue = true;
                FileType = null;
                Bitrate = null;
                NotDownloaded = null;
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
