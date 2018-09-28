using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using YoutubeExplode;
using System;
using System.Linq;
using System.Threading;
using MoreLinq;

namespace YoutubePlaylistDownloader
{
    /// <summary>
    /// Interaction logic for Skeleton.xaml
    /// </summary>
    public partial class Skeleton : MetroWindow
    {

        private bool exit = false;
        private SemaphoreSlim locker = new SemaphoreSlim(1, 1);

        public Skeleton()
        {
            //Initialize the app
            InitializeComponent();
            SetWindow();
            GlobalConsts.Current = this;
            //Go to main menu
            GlobalConsts.LoadPage(new MainPage());

            if (GlobalConsts.CheckForProgramUpdates)
                CheckForUpdates().ConfigureAwait(false);

            DownloadSubscriptions().ConfigureAwait(false);

        }

        public async Task DownloadSubscriptions()
        {
            while (GlobalConsts.CheckForSubscriptionUpdates)
            {
                try
                {
                    await locker.WaitAsync();
                    foreach (var sub in SubscriptionManager.Subscriptions)
                    {
                        try
                        {
                            await sub.DownloadMissingVideos();
                        }
                        catch (Exception ex)
                        {
                            await GlobalConsts.Log(ex.ToString(), "DownloadSubscriptions");
                            // maybe also inform the user that something failed
                        }
                    }
                }
                catch(Exception ex)
                {
                    await GlobalConsts.Log(ex.ToString(), "DownloadSubscriptions - out of while loop");
                }
                finally
                {
                    locker.Release();
                }
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }
        private async Task CheckForUpdates()
        {
            try
            {
                using (var wc = new WebClient() { CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore) })
                {
                    var latestVersion = double.Parse(await wc.DownloadStringTaskAsync("https://raw.githubusercontent.com/shaked6540/YoutubePlaylistDownloader/master/YoutubePlaylistDownloader/latestVersion.txt"));
                    if (latestVersion > GlobalConsts.VERSION)
                    {
                        var changelog = await wc.DownloadStringTaskAsync("https://raw.githubusercontent.com/shaked6540/YoutubePlaylistDownloader/master/YoutubePlaylistDownloader/changelog.txt");
                        var dialogSettings = new MetroDialogSettings()
                        {
                            AffirmativeButtonText = $"{FindResource("UpdateNow")}",
                            NegativeButtonText = $"{FindResource("No")}",
                            FirstAuxiliaryButtonText = $"{FindResource("UpdateWhenIExit")}",
                            ColorScheme = MetroDialogColorScheme.Theme,
                            DefaultButtonFocus = MessageDialogResult.Affirmative,
                        };
                        var update = await this.ShowMessageAsync($"{FindResource("NewVersionAvailable")}", $"{FindResource("DoYouWantToUpdate")}\n{changelog}",
                            MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, dialogSettings);
                        if (update == MessageDialogResult.Affirmative)
                            GlobalConsts.LoadPage(new DownloadUpdate(latestVersion, changelog));

                        else if (update == MessageDialogResult.FirstAuxiliary)
                        {
                            GlobalConsts.UpdateControl = new DownloadUpdate(latestVersion, changelog, true).UpdateLaterStillDownloading();
                        }
                    }
                }
            }
            catch { }
        }

        public async Task ShowMessage(string title, string message)
        {
            await this.ShowMessageAsync(title, message);
            if (DefaultFlyout.IsOpen)
                DefaultFlyout.IsOpen = false;
        }

        public async Task<MessageDialogResult> ShowYesNoDialog(string title, string message)
        {
            return await this.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = (string)FindResource("Yes"), NegativeButtonText = (string)FindResource("No") });
        }

        private void SetWindow()
        {

            WindowStyle = WindowStyle.None;
            IgnoreTaskbarOnMaximize = true;
            ShowTitleBar = false;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            Closing += MainWindow_Closing;

        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!exit)
            {
                e.Cancel = true;

                var exitMessage = $"{FindResource("ExitMessage")}";

                bool loadSubPage = false;
                DownloadPage page = SubscriptionManager.Subscriptions.FirstOrDefault(x => x.StillDownloading())?.GetDownloadPage();
                if (page != null)
                {
                    exitMessage = $"{FindResource("StillDownloadingSubscriptionsExit")}";
                    loadSubPage = true;
                }
                var res = await ShowYesNoDialog((string)FindResource("Exit"), exitMessage);
                if (res == MessageDialogResult.Affirmative)
                {
                    if (GlobalConsts.UpdateLater && !GlobalConsts.UpdateFinishedDownloading)
                    {
                        GlobalConsts.LoadPage(GlobalConsts.UpdateControl?.UpdateLaterStillDownloading());
                        return;
                    }
                    exit = true;
                    page = null;
                    Close();
                }
                else if (loadSubPage)
                    GlobalConsts.LoadPage(page);
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            GlobalConsts.LoadPage(new Settings());
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            GlobalConsts.LoadPage(new About());
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            GlobalConsts.LoadPage(new MainPage());
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            GlobalConsts.LoadPage(new Help());
        }

        private void SubscriptionsButton_Click(object sender, RoutedEventArgs e)
        {
            GlobalConsts.LoadPage(new SubscriptionsPage());
        }
    }
}
