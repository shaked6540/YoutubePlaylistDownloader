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
        var updatePath = GlobalConsts.UpdateSetupLocation;
        var partialPath = $"{updatePath}.part";
        try
        {
            await Dispatcher.InvokeAsync(() => HeadlineTextBlock.Text = $"{FindResource("DownloadingUpdateSetup")}");
            var latestVersionLink = await httpClient.GetAsync("https://raw.githubusercontent.com/shaked6540/YoutubePlaylistDownloader/master/YoutubePlaylistDownloader/latestVersionLink.txt", cancellationTokenSource.Token);
            latestVersionLink.EnsureSuccessStatusCode();
            var downloadUrl = (await latestVersionLink.Content.ReadAsStringAsync(cancellationTokenSource.Token)).Trim();
            if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
                (uri.Host != "github.com" && uri.Host != "objects.githubusercontent.com"))
                throw new InvalidDataException("The update URL is not a trusted GitHub HTTPS URL.");

            using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token);
            response.EnsureSuccessStatusCode();
            var finalUri = response.RequestMessage?.RequestUri;
            if (finalUri is null || finalUri.Scheme != Uri.UriSchemeHttps ||
                (finalUri.Host != "github.com" && finalUri.Host != "objects.githubusercontent.com"))
                throw new InvalidDataException("The update URL redirected to an untrusted host.");
            await Dispatcher.InvokeAsync(() => CurrentDownloadProgressBar.Maximum = response.Content.Headers.ContentLength ?? 0);

            await using (var file = new ProgressStream(new FileStream(partialPath, FileMode.Create, FileAccess.Write, FileShare.None)))
            {
                file.BytesWritten += DownloadProgressChanged;
                await response.Content.CopyToAsync(file, cancellationTokenSource.Token);
            }

            if (!File.Exists(partialPath) || new FileInfo(partialPath).Length < 2)
                throw new InvalidDataException("The downloaded update is empty.");

            await using (var header = File.OpenRead(partialPath))
            {
                var signature = new byte[2];
                if (await header.ReadAsync(signature, cancellationTokenSource.Token) != 2 || signature[0] != 'M' || signature[1] != 'Z')
                    throw new InvalidDataException("The downloaded update is not a Windows executable.");
            }

            File.Move(partialPath, updatePath, true);
            var completed = new AsyncCompletedEventArgs(null, false, null);
            if (GlobalConsts.UpdateLater)
                await DownloadCompletedLater(this, completed);
            else
                await DownloadFileCompleted(this, completed);
        }
        catch (OperationCanceledException)
        {
            await CleanupPartialFile(partialPath);
            var cancelled = new AsyncCompletedEventArgs(null, true, null);
            if (GlobalConsts.UpdateLater)
                await DownloadCompletedLater(this, cancelled);
            else
                await DownloadFileCompleted(this, cancelled);
        }
        catch (Exception ex)
        {
            await CleanupPartialFile(partialPath);
            var failed = new AsyncCompletedEventArgs(ex, false, null);
            if (GlobalConsts.UpdateLater)
                await DownloadCompletedLater(this, failed);
            else
                await DownloadFileCompleted(this, failed);
        }
    }

    private static async Task CleanupPartialFile(string partialPath)
    {
        if (!File.Exists(partialPath))
            return;

        try
        {
            File.Delete(partialPath);
        }
        catch (Exception ex)
        {
            await GlobalConsts.Log(ex.ToString(), "DownloadUpdate cleanup partial file");
        }
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
