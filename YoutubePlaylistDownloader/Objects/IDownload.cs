namespace YoutubePlaylistDownloader.Objects;

interface IDownload : INotifyPropertyChanged, IDisposable
{
    string ImageUrl { get; }
    string Title { get; set; }
    string TotalDownloaded { get; set; }
    int TotalVideos { get; set; }
    int CurrentProgressPercent { get; set; }
    string CurrentDownloadSpeed { get; set; }
    string CurrentTitle { get; set; }
    string CurrentStatus { get; set; }
    void OpenFolder_Click(object sender, RoutedEventArgs e);
    Task<bool> Cancel();
}
