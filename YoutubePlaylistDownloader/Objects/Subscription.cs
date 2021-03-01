using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using YoutubeExplode.Channels;
using YoutubeExplode;

namespace YoutubePlaylistDownloader.Objects
{
    [JsonObject]
    public class Subscription : IDisposable
    {
        [JsonIgnore]
        private Channel channel = null;

        [JsonIgnore]
        private DateTime lastVideoDownloaded;

        [JsonIgnore]
        private DownloadPage downloadPage = null;

        [JsonIgnore]
        private readonly SemaphoreSlim locker;

        [JsonIgnore]
        private CancellationTokenSource cts;

        [JsonProperty]
        public DateTime LatestVideoDownloaded
        {
            get => lastVideoDownloaded.Date;
            set
            {
                var date = value.ToUniversalTime().Date;
                if (date > LatestVideoDownloaded)
                    lastVideoDownloaded = date;
            }
        }

        [JsonProperty]
        public string ChannelId { get; set; }

        [JsonProperty]
        public DownloadSettings Settings { get; set; }

        [JsonProperty]
        public List<string> DownloadedVideos { get; set; }


        [JsonConstructor]
        public Subscription(DateTime latestVideoDownloaded, string channelId, DownloadSettings settings, List<string> downloadedVideos)
        {
            LatestVideoDownloaded = latestVideoDownloaded;
            ChannelId = channelId;
            Settings = settings;
            DownloadedVideos = downloadedVideos;

            locker = new SemaphoreSlim(1);
            cts = new CancellationTokenSource();
        }

        public async Task<Channel> GetChannel()
        {
            if (channel == null)
                channel = await GlobalConsts.YoutubeClient.Channels.GetAsync(ChannelId);

            return channel;
        }
        private async Task DownloadMissingVideos()
        {
            if (downloadPage != null && downloadPage.StillDownloading)
                return;

            else
            {
                //var playlist = await GlobalConsts.YoutubeClient.Channels.GetUploadsAsync(ChannelId).BufferAsync().ConfigureAwait(false);

                //List<YoutubeExplode.Videos.Video> missingVideos;


                //if (Settings.FilterVideosByLength)
                //{
                //    missingVideos = Settings.FilterMode ?
                //        playlist.Where(x => x.UploadDate.ToUniversalTime().Date >= LatestVideoDownloaded && !DownloadedVideos.Contains(x.Id) && x.Duration.TotalMinutes > Settings.FilterByLengthValue).ToList()
                //        :
                //        playlist.Where(x => x.UploadDate.ToUniversalTime().Date >= LatestVideoDownloaded && !DownloadedVideos.Contains(x.Id) && x.Duration.TotalMinutes < Settings.FilterByLengthValue).ToList();
                //}
                //else
                //{
                //    missingVideos = playlist.Where(x => x.UploadDate.ToUniversalTime().Date >= LatestVideoDownloaded && !DownloadedVideos.Contains(x.Id)).ToList();
                //}

                //if (missingVideos.Any())
                //{
                //    await Application.Current.Dispatcher.InvokeAsync(() =>
                //    {
                //        downloadPage = new DownloadPage(null, Settings, videos: missingVideos, subscription: this, silent: true, cancellationToken: cts);
                //    }, System.Windows.Threading.DispatcherPriority.Background);
                //}
            }
        }
        public bool StillDownloading()
        {
            if (downloadPage == null)
                return false;

            return downloadPage.StillDownloading;
        }
        public DownloadPage GetDownloadPage() => downloadPage;
        public Task RefreshUpdate()
        {
            cts?.Cancel(true);
            cts = new CancellationTokenSource();
            return UpdateSubscription();
        }
        public async Task UpdateSubscription()
        {
            try
            {
                await locker.WaitAsync();
                while (GlobalConsts.CheckForSubscriptionUpdates)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    await DownloadMissingVideos();
                    await Task.Delay(GlobalConsts.SubscriptionsUpdateDelay);
                }
            }
            catch (OperationCanceledException){ }
            catch(Exception ex)
            {
                await GlobalConsts.Log(ex.ToString(), "Update Subscription");
            }
            finally
            {
                locker.Release();
            }
        }
        public void CancelUpdate()
        {
            cts?.Cancel(true);
        }

        #region IDisposable Support
        private bool disposedValue = false; 
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    locker?.Dispose();
                    downloadPage?.Dispose();
                    cts?.Dispose();
                }

                Settings = null;
                downloadPage = null;
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
