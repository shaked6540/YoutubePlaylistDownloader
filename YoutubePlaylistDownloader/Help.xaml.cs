using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace YoutubePlaylistDownloader
{
    /// <summary>
    /// Interaction logic for Help.xaml
    /// </summary>
    public partial class Help : UserControl
    {
        public Help()
        {
            InitializeComponent();
            GlobalConsts.HideHelpButton();
            GlobalConsts.ShowAboutButton();
            GlobalConsts.ShowSubscriptionsButton();
            GlobalConsts.ShowHomeButton();
            GlobalConsts.ShowSettingsButton();
        }
    }
}
