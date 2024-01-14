namespace YoutubePlaylistDownloader;

/// <summary>
/// Interaction logic for DownloadUpdate.xaml
/// </summary>
public partial class DownloadUpdate : UserControl, IDownload
{
    private bool downloadFinished;

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
        get => 1;
        set => throw new NotSupportedException($"Cannot change value of {nameof(TotalVideos)}");
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
    private HttpClient httpClient;
    private readonly CancellationTokenSource cancellationTokenSource;

    public DownloadUpdate(Version latestVersion, string changelog, bool updateLater = false)
    {
        InitializeComponent();
        if (!updateLater)
        {
            GlobalConsts.HideSettingsButton();
            GlobalConsts.HideAboutButton();
            GlobalConsts.HideHomeButton();
            GlobalConsts.HideHelpButton();
        }
        ChangelogRun.Text = changelog;
        downloadFinished = false;
        GlobalConsts.UpdateSetupLocation = $"{GlobalConsts.TempFolderPath}Setup {latestVersion}.exe";

        ImageUrl = $"https://raw.githubusercontent.com/shaked6540/YoutubePlaylistDownloader/master/YoutubePlaylistDownloader/finalIcon.ico";
        Title = $"{FindResource("DownloadingUpdateSetup")}";
        CurrentStatus = (string)FindResource("Loading");
        TotalDownloaded = $"(0/1)";
        CurrentProgressPercent = 0;
        CurrentDownloadSpeed = string.Empty;
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue() { NoCache = true };
        cancellationTokenSource = new CancellationTokenSource();

        StartUpdate().ConfigureAwait(false);

        GlobalConsts.Downloads.Add(new QueuedDownload(this));
    }

    private async Task StartUpdate()
    {
        await Dispatcher.InvokeAsync(() => HeadlineTextBlock.Text = $"{FindResource("DownloadingUpdateSetup")}");
        using var fs = new ProgressStream(new FileStream(GlobalConsts.UpdateSetupLocation, FileMode.Create));
        fs.BytesWritten += DownloadProgressChanged;
        var latestVersionLink = await httpClient.GetAsync("https://raw.githubusercontent.com/shaked6540/YoutubePlaylistDownloader/master/YoutubePlaylistDownloader/latestVersionLink.txt").ConfigureAwait(false);
        var response = await httpClient.GetAsync(await latestVersionLink.Content.ReadAsStringAsync().ConfigureAwait(false)).ConfigureAwait(false);
        await Dispatcher.InvokeAsync(() => CurrentDownloadProgressBar.Maximum = response.Content.Headers.ContentLength ?? 0);
        await response.Content.CopyToAsync(fs, cancellationTokenSource.Token).ContinueWith(async a =>
        {
            if (GlobalConsts.UpdateLater)
                await DownloadCompletedLater(this, new AsyncCompletedEventArgs(null, a.IsCanceled, null));
            else
                await DownloadFileCompleted(this, new AsyncCompletedEventArgs(null, a.IsCanceled, null));
        });
    }

    private async void DownloadProgressChanged(object sender, ProgressStreamReportEventArgs args)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            CurrentDownloadProgressBar.Value += args.BytesMoved;
            CurrentDownloadProgressBarTextBlock.Text = $"{(CurrentDownloadProgressBar.Value / CurrentDownloadProgressBar.Maximum) * 100}%";
        });
    }

    private async Task DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
    {
        if (e.Cancelled)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                var cancelled = $"{FindResource("UpdateCancelled")}";
                HeadlineTextBlock.Text = cancelled;
                CurrentDownloadGrid.Visibility = Visibility.Collapsed;
            });
        }
        else if (e.Error != null)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                var error = $"{FindResource("Error")}";
                UpdateLaterButton.Visibility = Visibility.Collapsed;
                CurrentDownloadGrid.Visibility = Visibility.Collapsed;
                HeadlineTextBlock.Text = error;
            });
            await GlobalConsts.ShowMessage($"{FindResource($"Error")}", $"{FindResource("ErrorWhileUpdating")}");
        }
        else
        {
            await Dispatcher.InvokeAsync(() =>
            {
                var complete = $"{FindResource("UpdateComplete")}";
                HeadlineTextBlock.Text = complete;
                CurrentDownloadGrid.Visibility = Visibility.Collapsed;
                UpdateNowButton.Visibility = Visibility.Visible;
                UpdateLaterButton.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Collapsed;
            });
            GlobalConsts.UpdateFinishedDownloading = true;
            downloadFinished = true;
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        if (downloadFinished)
        {
            Process.Start(GlobalConsts.UpdateSetupLocation);
            Environment.Exit(0);
        }
        else
        {
            try
            {
                cancellationTokenSource.Cancel();
                httpClient.CancelPendingRequests();
                httpClient.Dispose();
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
        GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
    }

    private async Task DownloadCompletedLater(object sender, AsyncCompletedEventArgs e)

    {
        await Dispatcher.InvokeAsync(async () =>
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
                await GlobalConsts.ShowMessage($"{FindResource("UpdateFailed")}", $"{string.Concat(FindResource("UpdateCancelled"), e.Error?.Message ?? "")}");
            }
            else
            {
                downloadFinished = true;
                GlobalConsts.UpdateFinishedDownloading = true;
                GlobalConsts.UpdateLater = true;
                UpdateNowButton.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Collapsed;
            }
        });
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
            httpClient.CancelPendingRequests();
            httpClient.Dispose();
            GlobalConsts.UpdateOnExit = false;
            GlobalConsts.UpdateSetupLocation = string.Empty;
            GlobalConsts.UpdateLater = false;
        }
        else
        {
            GlobalConsts.UpdateOnExit = true;
            GlobalConsts.UpdateControl = this;
            GlobalConsts.UpdateLater = true;
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
            httpClient = null;
            GlobalConsts.UpdateSetupLocation = null;
            disposedValue = true;
        }
    }
    public void Dispose()
    {
        Dispose(true);
    }
    #endregion
}