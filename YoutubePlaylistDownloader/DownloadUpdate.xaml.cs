using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace YoutubePlaylistDownloader
{
    /// <summary>
    /// Interaction logic for DownloadUpdate.xaml
    /// </summary>
    public partial class DownloadUpdate : UserControl
    {
        private CancellationTokenSource cts;
        private double latestVersion;
        private WebClient webClient;
        private bool downloadFinished;

        public DownloadUpdate(double latestVersion)
        {
            InitializeComponent();
            GlobalConsts.HideSettingsButton();
            GlobalConsts.HideAboutButton();
            GlobalConsts.HideHomeButton();
            this.latestVersion = latestVersion;
            downloadFinished = false;
            webClient = new WebClient();
            StartUpdate().ConfigureAwait(false);

        }

        private async Task StartUpdate()
        {
            await Dispatcher.InvokeAsync(() => HeadlineTextBlock.Text = $"{FindResource("DownloadingUpdateSetup")}");
            var downloadLink = await webClient.DownloadStringTaskAsync("https://raw.githubusercontent.com/shaked6540/YoutubePlaylistDownloader/master/latestVersionLink.txt");

            webClient.DownloadProgressChanged += async (s, e) =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    CurrentDownloadProgressBar.Value = e.ProgressPercentage;
                    CurrentDownloadProgressBarTextBlock.Text = $"{e.ProgressPercentage}%";
                });
            };

            webClient.DownloadFileCompleted += async (s, e) =>
            {
                if (e.Cancelled)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        HeadlineTextBlock.Text = $"{FindResource("UpdateCancelled")}";
                        CurrentDownloadGrid.Visibility = Visibility.Collapsed;
                    });
                }
                else if (e.Error != null)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        HeadlineTextBlock.Text = $"{FindResource("ErrorWhileUpdating")}";
                        CurrentDownloadGrid.Visibility = Visibility.Collapsed;
                    });
                }
                else
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        HeadlineTextBlock.Text = $"{FindResource("UpdateComplete")}";
                        CurrentDownloadGrid.Visibility = Visibility.Collapsed;
                        UpdateNowButton.Visibility = Visibility.Visible;
                        UpdateLaterButton.Visibility = Visibility.Visible;
                        BackButton.Visibility = Visibility.Collapsed;
                    });
                    downloadFinished = true;
                }
            };

            webClient.DownloadFileAsync(new Uri(downloadLink), $"{GlobalConsts.TempFolderPath}Setup{latestVersion}.exe");


        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (downloadFinished)
            {
                Process.Start($"{GlobalConsts.TempFolderPath}Setup{latestVersion}.exe");
                Environment.Exit(0);
            }
            else
            {
                try
                {
                    webClient.CancelAsync();
                    webClient.Dispose();
                }
                finally
                {
                    GlobalConsts.LoadPage(new MainPage());
                }
            }
        }

        private void ExitLater_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
