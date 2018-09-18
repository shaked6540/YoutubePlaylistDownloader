using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace YoutubePlaylistDownloader
{
    /// <summary>
    /// Interaction logic for Skeleton.xaml
    /// </summary>
    public partial class Skeleton : MetroWindow
    {
        public Skeleton()
        {
            //Initialize the app
            InitializeComponent();
            SetWindow();
            GlobalConsts.Current = this;

            //Go to main menu
            GlobalConsts.LoadPage(new MainPage());

            CheckForUpdates().ConfigureAwait(false);

        }

        private async Task CheckForUpdates()
        {
            try
            {
                using (var wc = new WebClient())
                {
                    var latestVersion = double.Parse(await wc.DownloadStringTaskAsync("https://raw.githubusercontent.com/shaked6540/YoutubePlaylistDownloader/master/YoutubePlaylistDownloader/latestVersion.txt"));

                    if (latestVersion > GlobalConsts.VERSION)
                    {
                        var changelog = await wc.DownloadStringTaskAsync("https://raw.githubusercontent.com/shaked6540/YoutubePlaylistDownloader/master/YoutubePlaylistDownloader/changelog.txt");
                        var update = await GlobalConsts.ShowYesNoDialog($"{FindResource("NewVersionAvailable")}", $"{FindResource("DoYouWantToUpdate")}\n{changelog}");
                        if (update == MessageDialogResult.Affirmative)
                            GlobalConsts.LoadPage(new DownloadUpdate(latestVersion));
                    }
                }
            }
            catch { }
        }

        private bool exit = false;

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
                var res = await ShowYesNoDialog((string)FindResource("Exit"), (string)FindResource("ExitMessage"));
                if (res == MessageDialogResult.Affirmative)
                {
                    if (!GlobalConsts.UpdateFinishedDownloading)
                    {
                        GlobalConsts.LoadPage(GlobalConsts.UpdateControl);
                        try
                        {
                            await GlobalConsts.UpdateControl.UpdateTask();
                        }
                        catch { }
                    }
                    exit = true;
                    Close();
                }
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            GlobalConsts.LoadPage(new Factory.Settings());
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            GlobalConsts.LoadPage(new About());
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            GlobalConsts.LoadPage(new MainPage());
        }
    }
}
