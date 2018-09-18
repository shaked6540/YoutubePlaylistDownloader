using MahApps.Metro.Controls.Dialogs;
using System;
using System.Net;
using System.Windows.Controls;

namespace YoutubePlaylistDownloader
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : UserControl
    {
        public About()
        {
            InitializeComponent();
            GlobalConsts.HideAboutButton();
            GlobalConsts.ShowHomeButton();
            GlobalConsts.ShowSettingsButton();
            AboutRun.Text += GlobalConsts.VERSION;
        }

        private async void UpdateButton_Click(object sender, System.Windows.RoutedEventArgs e)
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
                    else
                        await GlobalConsts.ShowMessage($"{FindResource("NoUpdates")}", $"{FindResource("NoUpdatesAvailable")}");
                }
            }
            catch (Exception ex)
            {
                await GlobalConsts.ShowMessage($"{FindResource("Error")}", $"{FindResource("CannotUpdate")} {ex.Message}");
            }
        }
    }
}
