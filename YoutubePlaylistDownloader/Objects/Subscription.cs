using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;

namespace YoutubePlaylistDownloader.Objects
{
    [JsonObject]
    public class Subscription
    {
        [JsonIgnore]
        private Channel channel = null;

        [JsonIgnore]
        private DateTime lastVideoDownloaded;

        [JsonIgnore]
        private DownloadPage downloadPage = null;

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
        }

        public async Task<Channel> GetChannel()
        {
            if (channel == null)
                channel = await GlobalConsts.YoutubeClient.GetChannelAsync(ChannelId);

            return channel;
        }
        public async Task DownloadMissingVideos()
        {
            if (downloadPage != null && downloadPage.StillDownloading)
                return;

            else
            {
                var playlist = await GlobalConsts.YoutubeClient.GetChannelUploadsAsync(ChannelId);
                var missingVideos = playlist.Where(x => x.UploadDate.ToUniversalTime().Date >= LatestVideoDownloaded && !DownloadedVideos.Contains(x.Id)).ToList();

                if (missingVideos.Any())
                    downloadPage = new DownloadPage(null, Convert, Quality, SaveFormat, Bitrate, 0, 0, AudioOnly, PreferHighestFPS, SavePath, missingVideos, this, true);
            }
        }
        public bool StillDownloading()
        {
            if (downloadPage == null)
                return false;

            return downloadPage.StillDownloading;
        }
        public DownloadPage GetDownloadPage() => downloadPage;
    }
}
