using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubePlaylistDownloader.Objects;
using System.IO;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace YoutubePlaylistDownloader
{
    public static class SubscriptionManager
    {
        public static readonly ObservableCollection<Subscription> Subscriptions;

        static SubscriptionManager()
        {
            try
            {
                if (File.Exists(GlobalConsts.ChannelSubscriptionsFilePath))
                    Subscriptions = JsonConvert.DeserializeObject<ObservableCollection<Subscription>>(File.ReadAllText(GlobalConsts.ChannelSubscriptionsFilePath));
                else
                    Subscriptions = new ObservableCollection<Subscription>();
            }
            catch
            {
                Subscriptions = new ObservableCollection<Subscription>();
            }

            Subscriptions.CollectionChanged += (s, e) => SaveSubscriptions();
        }

        public static void SaveSubscriptions()
        {
            File.WriteAllText(GlobalConsts.ChannelSubscriptionsFilePath, JsonConvert.SerializeObject(Subscriptions));
        }

        
    }
}
