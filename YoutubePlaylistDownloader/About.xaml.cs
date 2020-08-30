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
            GlobalConsts.ShowHelpButton();
            GlobalConsts.ShowSubscriptionsButton();

            AboutRun.Text += GlobalConsts.VERSION;
        }

        private async void UpdateButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                using (var wc = new WebClient())
                {
                    var latestVersion = Version.Parse(await wc.DownloadStringTaskAsync("https://raw.githubusercontent.com/shaked6540/YoutubePlaylistDownloader/master/YoutubePlaylistDownloader/latestVersionWithRevision.txt"));

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
                        var update = await GlobalConsts.Current.ShowMessageAsync($"{FindResource("NewVersionAvailable")}", $"{FindResource("DoYouWantToUpdate")}\n{changelog}",
                            MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, dialogSettings);
                        if (update == MessageDialogResult.Affirmative)
                            GlobalConsts.LoadPage(new DownloadUpdate(latestVersion, changelog));

                        else if (update == MessageDialogResult.FirstAuxiliary)
                        {
                            GlobalConsts.UpdateControl = new DownloadUpdate(latestVersion, changelog, true).UpdateLaterStillDownloading();
                        }
                    }
                    else
                        await GlobalConsts.ShowMessage($"{FindResource("NoUpdates")}", $"{FindResource("NoUpdatesAvailable")}");
                }
            }
            catch (Exception ex)
            {
                await GlobalConsts.Log(ex.ToString(), "About UpdateButton_Click");
                await GlobalConsts.ShowMessage($"{FindResource("Error")}", $"{FindResource("CannotUpdate")} {ex.Message}");
            }
        }
    }
}
