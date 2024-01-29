namespace YoutubePlaylistDownloader;

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
        GlobalConsts.ShowHomeButton();
        GlobalConsts.ShowSettingsButton();
    }
}
