using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YoutubePlaylistDownloader.Objects;

namespace YoutubePlaylistDownloader
{
    /// <summary>
    /// Interaction logic for DownloadUpdate.xaml
    /// </summary>
    public partial class DownloadUpdate : UserControl, IDownload
    {
        private Version latestVersion;
        private bool downloadFinished;
        private string updateSetupLocation;

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

        public DownloadUpdate(Version latestVersion, string changelog, bool updateLater = false)
        {
            InitializeComponent();
            if (!updateLater)
            {
                GlobalConsts.HideSettingsButton();
                GlobalConsts.HideAboutButton();
                GlobalConsts.HideHomeButton();
                GlobalConsts.HideSubscriptionsButton();
                GlobalConsts.HideHelpButton();
            }
            this.latestVersion = latestVersion;
            ChangelogRun.Text = changelog;
            downloadFinished = false;
            updateSetupLocation = $"{GlobalConsts.TempFolderPath}Setup {latestVersion}.exe";

            ImageUrl = $"https://raw.githubusercontent.com/shaked6540/YoutubePlaylistDownloader/master/YoutubePlaylistDownloader/finalIcon.ico";
            Title = $"{FindResource("DownloadingUpdateSetup")}";
            CurrentStatus = (string)FindResource("Loading");
            TotalDownloaded = $"(0/1)";
            CurrentProgressPrecent = 0;
            CurrentDownloadSpeed = string.Empty;

            StartUpdate().ConfigureAwait(false);

            GlobalConsts.Downloads.Add(new QueuedDownload(this));
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
                    string cancelled = $"{FindResource("UpdateCancelled")}";
                    HeadlineTextBlock.Text = cancelled;
                    CurrentStatus = cancelled;
                    Title = string.Empty;
                    CurrentDownloadGrid.Visibility = Visibility.Collapsed;
                });
            }
            else if (e.Error != null)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    string error = $"{FindResource("Error")}";
                    UpdateLaterButton.Visibility = Visibility.Collapsed;
                    CurrentDownloadGrid.Visibility = Visibility.Collapsed;
                    HeadlineTextBlock.Text = error;
                    CurrentStatus = error;
                    Title = string.Empty;
                });
                await GlobalConsts.ShowMessage($"{FindResource($"Error")}", $"{FindResource("ErrorWhileUpdating")}");
            }
            else
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    string complete = $"{FindResource("UpdateComplete")}";
                    HeadlineTextBlock.Text = complete;
                    CurrentStatus = complete;
                    Title = string.Empty;
                    TotalDownloaded = "(1/1)";
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
                CurrentProgressPrecent = e.ProgressPercentage;
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
                    GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
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
            GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
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
            GlobalConsts.UpdateOnExit = true;

            return this;
        }
        public void OpenFolder_Click(object sender, RoutedEventArgs e) { }

        public void Exit()
        {
            if (!downloadFinished)
            {
                GlobalConsts.WebClient.CancelAsync();
                GlobalConsts.WebClient.Dispose();
                GlobalConsts.UpdateOnExit = false;
                GlobalConsts.UpdateSetupLocation = string.Empty;
                GlobalConsts.UpdateLater = false;
            }
            else
            {
                GlobalConsts.UpdateOnExit = true;
                GlobalConsts.UpdateControl = this;
                GlobalConsts.UpdateLater = true;
                GlobalConsts.WebClient.DownloadFileCompleted += DownloadCompletedLater;
                GlobalConsts.WebClient.DownloadFileCompleted -= DownloadFileCompleted;
                GlobalConsts.UpdateSetupLocation = updateSetupLocation;
            }
        }

        public Task<bool> Cancel()
        {
            Exit();
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

                }
                updateSetupLocation = null;
                currentTitle = null;
                currentStatus = null;
                totalDownloaded = null;
                currentDownloadSpeed = null;
                PropertyChanged = null;

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