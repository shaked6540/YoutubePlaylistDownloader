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
using YoutubeExplode;
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;
using YoutubePlaylistDownloader.Objects;

namespace YoutubePlaylistDownloader
{
    /// <summary>
    /// Interaction logic for DownloadVideo.xaml
    /// </summary>
    public partial class DownloadVideo : UserControl, IDisposable
    {
        private Video Video;
        private string FileType;
        private int DownloadedCount;
        private List<Process> ffmpegList;
        private CancellationTokenSource cts;
        private VideoQuality Quality;
        private string Bitrate;

        public DownloadVideo(Video video, bool convert, VideoQuality quality = VideoQuality.High720, string fileType = "mp3", string bitrate = null)
        {
            InitializeComponent();
            GlobalConsts.HideSettingsButton();
            GlobalConsts.HideAboutButton();
            GlobalConsts.HideHomeButton();
            cts = new CancellationTokenSource();
            ffmpegList = new List<Process>();
            Video = video;
            FileType = fileType;
            DownloadedCount = 0;
            Quality = quality;
            if (bitrate != null)
                Bitrate = $"-b:a {bitrate}k";
            else
                Bitrate = string.Empty;

            if (convert)
                StartDownloadingWithConverting(cts.Token).ConfigureAwait(false);
            else
                StartDownloading(cts.Token).ConfigureAwait(false);

        }

        public async Task StartDownloadingWithConverting(CancellationToken token)
        {

            var client = new YoutubeClient();
            try
            {
                await Dispatcher.InvokeAsync(() => Update(0, Video));

                var streamInfoSet = await client.GetVideoMediaStreamInfosAsync(Video.Id);
                var cleanFileName = GlobalConsts.CleanFileName(Video.Title).Replace("$", "S");
                var bestQuality = streamInfoSet.Audio.MaxBy(x => x.AudioEncoding);
                var fileLoc = $"{GlobalConsts.TempFolderPath}{cleanFileName}";
                var outputFileLoc = $"{GlobalConsts.TempFolderPath}{cleanFileName}.{FileType}";
                var copyFileLoc = $"{GlobalConsts.SaveDirectory}\\{cleanFileName}.{FileType}";

                using (var stream = new ProgressStream(File.Create(fileLoc)))
                {
                    stream.BytesWritten += async (sender, args) =>
                    {
                        var precent = args.StreamLength * 100 / bestQuality.Size;
                        await Dispatcher.InvokeAsync(() =>
                        {
                            CurrentDownloadProgressBar.Value = precent;
                            CurrentDownloadProgressBarTextBlock.Text = $"{precent}%";
                        });
                    };
                    await client.DownloadMediaStreamAsync(bestQuality, stream, cancellationToken: token);
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
                        ffmpegList.Remove(ffmpeg);
                        await GlobalConsts.TagFile(Video, 0, outputFileLoc);

                        File.Copy(outputFileLoc, copyFileLoc, true);
                        File.Delete(outputFileLoc);
                        File.Delete(fileLoc);
                    };
                    ffmpeg.Start();
                    ffmpegList.Add(ffmpeg);
                    DownloadedCount++;
                }
            }
            catch (OperationCanceledException)
            {
                goto exit;
            }
            catch (Exception)
            {
            }

            exit:
            while (ffmpegList.Count > 0)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    HeadlineTextBlock.Text = (string)FindResource("AllDone");
                    CurrentDownloadProgressBar.IsIndeterminate = true;
                    ConvertingTextBlock.Text = $"{FindResource("StillConverting")} {ffmpegList.Count} {FindResource("files")}";
                    CurrentDownloadProgressBarTextBlock.Visibility = Visibility.Collapsed;
                });
                await Task.Delay(1000);
            }

            CurrentDownloadGrid.Visibility = Visibility.Collapsed;
            ConvertingTextBlock.Visibility = Visibility.Collapsed;
        }

        public async Task StartDownloading(CancellationToken token)
        {
            var client = new YoutubeClient();
            await Dispatcher.InvokeAsync(() => Update(0, Video));
            try
            {
                var streamInfoSet = await client.GetVideoMediaStreamInfosAsync(Video.Id);
                MediaStreamInfo bestQuality, bestAudio = null;
                bestQuality = streamInfoSet.Video.OrderByDescending(x => x.VideoQuality == Quality).ThenByDescending(x => x.VideoQuality > Quality).ThenByDescending(x => x.VideoQuality).FirstOrDefault();
                bestAudio = streamInfoSet.Audio.OrderByDescending(x => x.AudioEncoding).FirstOrDefault();
                var cleanVideoName = GlobalConsts.CleanFileName(Video.Title);//.Replace("$","S")
                var fileLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoName}";
                var outputFileLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoName}.mkv";
                var copyFileLoc = $"{GlobalConsts.SaveDirectory}\\{cleanVideoName}.mkv";

                string audioLoc = null;
                if (bestAudio != null)
                    audioLoc = $"{GlobalConsts.TempFolderPath}{cleanVideoName}.{bestAudio.Container.GetFileExtension()}";

                using (var stream = new ProgressStream(File.Create(fileLoc)))
                {
                    stream.BytesWritten += async (sender, args) =>
                    {
                        var precent = Convert.ToInt32(args.StreamLength * 100 / bestQuality.Size);
                        await Dispatcher.InvokeAsync(() =>
                        {
                            CurrentDownloadProgressBar.Value = precent;
                            CurrentDownloadProgressBarTextBlock.Text = $"{precent}%";
                        });
                    };
                    var videoTask = client.DownloadMediaStreamAsync(bestQuality, stream, cancellationToken: token);

                    using (var audioStream = File.Create(audioLoc))
                    {
                        var audioTask = client.DownloadMediaStreamAsync(bestAudio, audioStream);
                        await Task.WhenAll(videoTask, audioTask);
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
                    ffmpeg.Exited += (x, y) =>
                    {
                        ffmpegList.Remove(ffmpeg);
                        File.Copy(outputFileLoc, copyFileLoc, true);
                        File.Delete(outputFileLoc);
                        File.Delete(audioLoc);
                        File.Delete(fileLoc);
                    };
                    ffmpeg.Start();
                    ffmpegList.Add(ffmpeg);
                    DownloadedCount++;
                }

            }
            catch (OperationCanceledException)
            {
                goto exit;
            }
            catch (Exception)
            {

            }

            exit:
            while (ffmpegList.Count > 0)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    HeadlineTextBlock.Text = (string)FindResource("AllDone");
                    CurrentDownloadProgressBar.IsIndeterminate = true;
                    ConvertingTextBlock.Text = $"{FindResource("StillConverting")} {ffmpegList.Count} {FindResource("files")}";
                    CurrentDownloadProgressBarTextBlock.Visibility = Visibility.Collapsed;
                });
                await Task.Delay(1000);
            }

            CurrentDownloadGrid.Visibility = Visibility.Collapsed;
            ConvertingTextBlock.Visibility = Visibility.Collapsed;
        }

        private void Update(int precent, Video video)
        {
            CurrentDownloadProgressBar.Value = precent;
            HeadlineTextBlock.Text = (string)FindResource("CurrentlyDownlading") + video.Title;
            CurrentDownloadProgressBarTextBlock.Text = $"{precent}%";
        }

        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel(true);
            if (ffmpegList.Count > 0)
            {
                var yesno = await GlobalConsts.ShowYesNoDialog($"{FindResource("StillConverting")}", $"{FindResource("StillConverting")} {ffmpegList.Count(x => !x.HasExited)} {FindResource("files")} {FindResource("AreYouSureExit")}");
                if (yesno == MahApps.Metro.Controls.Dialogs.MessageDialogResult.Negative)
                    return;
            }
            ffmpegList.ForEach(x => { try { x.Kill(); } catch { } });
            GlobalConsts.LoadPage(new MainPage());
        }

        #region IDisposable Support
        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cts.Dispose();
                    ffmpegList.Clear();

                }

                Video = null;
                ffmpegList = null;
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
