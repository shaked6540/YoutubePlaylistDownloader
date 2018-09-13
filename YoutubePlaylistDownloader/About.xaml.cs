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
    }
}
