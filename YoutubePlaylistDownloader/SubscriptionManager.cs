using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using YoutubePlaylistDownloader.Objects;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace YoutubePlaylistDownloader
{
    public static class SubscriptionManager
    {
        public static readonly ObservableCollection<Subscription> Subscriptions;
        private static CancellationTokenSource cts;

        static SubscriptionManager()
        {
            try
            {
                if (File.Exists(GlobalConsts.ChannelSubscriptionsFilePath))
                {
                    Subscriptions = JsonConvert.DeserializeObject<ObservableCollection<Subscription>>(File.ReadAllText(GlobalConsts.ChannelSubscriptionsFilePath));
                    if (Subscriptions == null)
                        throw new ArgumentNullException(nameof(Subscriptions), "DeserializeObject returned null - need to reset subscriptions file");
                }
                else
                    Subscriptions = new ObservableCollection<Subscription>();
            }
            catch
            {
                Subscriptions = new ObservableCollection<Subscription>();
                try
                {
                    File.Delete(GlobalConsts.ChannelSubscriptionsFilePath);
                }
                catch (Exception ex)
                {
                    GlobalConsts.Log(ex.ToString(), "SubscriptionsManager.ctor, error deleting subscriptions file").Wait();
                }
            }
            finally
            {
                cts = new CancellationTokenSource();
            }

            if (Subscriptions != null)
                Subscriptions.CollectionChanged += (s, e) => SaveSubscriptions();
        }

        public static void SaveSubscriptions()
        {
            File.WriteAllText(GlobalConsts.ChannelSubscriptionsFilePath, JsonConvert.SerializeObject(Subscriptions));
        }
        public static void UpdateAllSubscriptions()
        {
            try
            {
                Task.Run(async () =>
                {
                    foreach (var sub in Subscriptions.ToList())
                        await sub.RefreshUpdate();

                }, cts.Token);
            }
            catch(OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                GlobalConsts.Log(ex.ToString(), "UpdateAllSubscriptions - manager").Wait();
            }
        }
        public static void CancelAll()
        {
            cts?.Cancel(true);
            foreach (var sub in Subscriptions.ToList())
                sub.CancelUpdate();
            cts = new CancellationTokenSource();
        }
    }
}
