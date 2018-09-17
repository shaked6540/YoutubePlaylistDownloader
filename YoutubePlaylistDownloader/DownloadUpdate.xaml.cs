using System;
using System.ComponentModel;
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
        private double latestVersion;
        private bool downloadFinished;

        public DownloadUpdate(double latestVersion)
        {
            InitializeComponent();
            GlobalConsts.HideSettingsButton();
            GlobalConsts.HideAboutButton();
            GlobalConsts.HideHomeButton();
            this.latestVersion = latestVersion;
            downloadFinished = false;
            StartUpdate().ConfigureAwait(false);
        }

        private async Task StartUpdate()
        {
            await Dispatcher.InvokeAsync(() => HeadlineTextBlock.Text = $"{FindResource("DownloadingUpdateSetup")}");
            var downloadLink = await GlobalConsts.WebClient.DownloadStringTaskAsync("https://raw.githubusercontent.com/shaked6540/YoutubePlaylistDownloader/master/YoutubePlaylistDownloader/latestVersionLink.txt");
            GlobalConsts.WebClient.DownloadProgressChanged += DownloadProgressChanged;
            GlobalConsts.WebClient.DownloadFileCompleted += DownloadFileCompleted;
            GlobalConsts.WebClient.DownloadFileAsync(new Uri(downloadLink), $"{GlobalConsts.TempFolderPath}Setup{latestVersion}.exe");
        }

        private async void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
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
        }

        private async void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                CurrentDownloadProgressBar.Value = e.ProgressPercentage;
                CurrentDownloadProgressBarTextBlock.Text = $"{e.ProgressPercentage}%";
            });
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
                    GlobalConsts.WebClient.CancelAsync();
                    GlobalConsts.WebClient.Dispose();
                }
                finally
                {
                    GlobalConsts.LoadPage(new MainPage());
                }
            }
        }

        private void ExitLater_Click(object sender, RoutedEventArgs e)
        {
            GlobalConsts.UpdateOnExit = true;
            GlobalConsts.WebClient.DownloadFileCompleted += DownloadCompletedLater;
            GlobalConsts.WebClient.DownloadProgressChanged -= DownloadProgressChanged;
            GlobalConsts.WebClient.DownloadFileCompleted -= DownloadFileCompleted;
            GlobalConsts.UpdateSetupLocation = $"{GlobalConsts.TempFolderPath}Setup{latestVersion}.exe";
            GlobalConsts.LoadPage(new MainPage());
        }

        private async void DownloadCompletedLater(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                GlobalConsts.UpdateOnExit = false;
                GlobalConsts.UpdateSetupLocation = string.Empty;
                await GlobalConsts.ShowMessage($"{FindResource("UpdateFailed")}", $"{string.Concat(FindResource("CannotUpdate"), e.Error.Message)}");
            }
            else if (e.Cancelled)
            {
                GlobalConsts.UpdateOnExit = false;
                GlobalConsts.UpdateSetupLocation = string.Empty;
                await GlobalConsts.ShowMessage($"{FindResource("UpdateFailed")}", $"{string.Concat(FindResource("UpdateCancelled"), e.Error.Message)}");
            }
        }
    }
}
