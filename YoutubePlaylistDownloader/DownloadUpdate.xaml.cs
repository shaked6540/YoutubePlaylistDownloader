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
        private string updateSetupLocation;

        public DownloadUpdate(double latestVersion)
        {
            InitializeComponent();
            GlobalConsts.HideSettingsButton();
            GlobalConsts.HideAboutButton();
            GlobalConsts.HideHomeButton();
            this.latestVersion = latestVersion;
            downloadFinished = false;
            updateSetupLocation = $"{GlobalConsts.TempFolderPath}Setup {latestVersion}.exe";
            StartUpdate().ConfigureAwait(false);
        }

        private async Task StartUpdate()
        {
            await Dispatcher.InvokeAsync(() => HeadlineTextBlock.Text = $"{FindResource("DownloadingUpdateSetup")}");
            var downloadLink = await GlobalConsts.WebClient.DownloadStringTaskAsync("https://raw.githubusercontent.com/shaked6540/YoutubePlaylistDownloader/master/YoutubePlaylistDownloader/latestVersionLink.txt");
            GlobalConsts.WebClient.DownloadProgressChanged += DownloadProgressChanged;
            GlobalConsts.WebClient.DownloadFileCompleted += DownloadFileCompleted;
            GlobalConsts.WebClient.DownloadFileAsync(new Uri(downloadLink), updateSetupLocation);
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
                    UpdateLaterButton.Visibility = Visibility.Collapsed;
                    CurrentDownloadGrid.Visibility = Visibility.Collapsed;
                    HeadlineTextBlock.Text = $"{FindResource("Error")}";

                });
                await GlobalConsts.ShowMessage($"{FindResource($"Error")}", $"{FindResource("ErrorWhileUpdating")}");
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
                GlobalConsts.UpdateFinishedDownloading = true;
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
                Process.Start(updateSetupLocation);
                Environment.Exit(0);
            }
            else
            {
                try
                {
                    GlobalConsts.WebClient.CancelAsync();
                    GlobalConsts.WebClient.Dispose();
                    GlobalConsts.UpdateOnExit = false;
                    GlobalConsts.UpdateSetupLocation = string.Empty;
                    GlobalConsts.UpdateLater = false;
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
            GlobalConsts.UpdateControl = this;
            GlobalConsts.UpdateLater = true;
            GlobalConsts.WebClient.DownloadFileCompleted += DownloadCompletedLater;
            GlobalConsts.WebClient.DownloadFileCompleted -= DownloadFileCompleted;
            GlobalConsts.UpdateSetupLocation = updateSetupLocation;
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
            else
            {
                downloadFinished = true;
                GlobalConsts.UpdateFinishedDownloading = true;
                GlobalConsts.UpdateLater = true;
                UpdateNowButton.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Collapsed;
            }
        }

        public DownloadUpdate UpdateLaterStillDownloading()
        {
            UpdateLaterButton.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Visible;
            GlobalConsts.UpdateLater = true;

            return this;
        }

    }
}
