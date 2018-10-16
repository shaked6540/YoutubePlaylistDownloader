using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace YoutubePlaylistDownloader.Objects
{
    interface IDownload : INotifyPropertyChanged, IDisposable
    {
        string ImageUrl { get; }
        string Title { get; set; }
        string TotalDownloaded { get; set; }
        int TotalVideos { get; set; }
        int CurrentProgressPrecent { get; set; }
        string CurrentDownloadSpeed { get; set; }
        string CurrentTitle { get; set; }
        string CurrentStatus { get; set; }
        Task<bool> Cancel();
    }
}
