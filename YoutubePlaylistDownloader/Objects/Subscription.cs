using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;
using System.Threading;
using System.Windows;

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
        public string SavePath { get; set; }

        [JsonProperty]
        public string SaveFormat { get; set; }

        [JsonProperty]
        public bool AudioOnly { get; set; }

        [JsonProperty]
        public VideoQuality Quality { get; set; }

        [JsonProperty]
        public bool PreferHighestFPS { get; set; }

        [JsonProperty]
        public bool PreferQuality { get; set; }

        [JsonProperty]
        public bool Convert { get; set; }

        [JsonProperty]
        public bool SetBitrate { get; set; }

        [JsonProperty]
        public string Bitrate { get; set; }

        [JsonProperty]
        public List<string> DownloadedVideos { get; set; }


        [JsonConstructor]
        public Subscription(DateTime latestVideoDownloaded, string channelId, string savePath, string saveForamt,
            bool audioOnly, VideoQuality quality, bool preferHighestFPS, bool preferQuality, bool convert,
            bool setBitrate, string bitrate, List<string> downloadedVideos)
        {
            LatestVideoDownloaded = latestVideoDownloaded;
            ChannelId = channelId;
            SavePath = savePath;
            SaveFormat = saveForamt;
            AudioOnly = audioOnly;
            Quality = quality;
            PreferHighestFPS = preferHighestFPS;
            DownloadedVideos = downloadedVideos;
            PreferQuality = preferQuality;
            Convert = convert;
            Bitrate = bitrate;
            SetBitrate = setBitrate;

            locker = new SemaphoreSlim(1);
            cts = new CancellationTokenSource();
        }

        public async Task<Channel> GetChannel()
        {
            if (channel == null)
                channel = await GlobalConsts.YoutubeClient.GetChannelAsync(ChannelId);

            return channel;
        }
        private async Task DownloadMissingVideos()
        {
            if (downloadPage != null && downloadPage.StillDownloading)
                return;

            else
            {
                var playlist = await GlobalConsts.YoutubeClient.GetChannelUploadsAsync(ChannelId);
                var missingVideos = playlist.Where(x => x.UploadDate.ToUniversalTime().Date >= LatestVideoDownloaded && !DownloadedVideos.Contains(x.Id)).ToList();

                if (missingVideos.Any())
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        downloadPage = new DownloadPage(null, Convert, Quality, SaveFormat, Bitrate, 0, 0, AudioOnly, PreferHighestFPS, SavePath, missingVideos, this, true, cts);
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
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
