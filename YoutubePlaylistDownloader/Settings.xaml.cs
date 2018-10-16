using MahApps.Metro;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using YoutubePlaylistDownloader.Objects;

namespace YoutubePlaylistDownloader
{

    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : UserControl
    {

        public Settings()
        {
            InitializeComponent();
            FillAccents();
            FillLanguages();

            if (GlobalConsts.Theme.Name == "BaseDark") NightModeCheckBox.IsChecked = true;
            CheckForUpdatesCheckBox.IsChecked = GlobalConsts.CheckForProgramUpdates;
            SaveDirectoryTextBox.Text = GlobalConsts.SaveDirectory;
            SaveDownloadOptionsCheckBox.IsChecked = GlobalConsts.SaveDownloadOptions;

            NightModeCheckBox.Checked += NightModeCheckBox_Checked;
            NightModeCheckBox.Unchecked += NightModeCheckBox_Unchecked;

            GlobalConsts.HideSettingsButton();
            GlobalConsts.ShowHomeButton();
            GlobalConsts.ShowAboutButton();
            GlobalConsts.ShowHelpButton();
            GlobalConsts.ShowSubscriptionsButton();
        }



        private void FillLanguages()
        {
            var languages = ((string)FindResource("LanguageList")).Split(';');
            LanguageComboBox.ItemsSource = languages;
            LanguageComboBox.SelectedItem = GlobalConsts.Language;
        }

        private void FillAccents()
        {
            var accents = ThemeManager.Accents;
            comboBox.ItemsSource = accents;
            comboBox.DisplayMemberPath = "Name";
            comboBox.SelectedItem = ((IEnumerable<Accent>)comboBox.ItemsSource).FirstOrDefault(x => x.Name == GlobalConsts.Accent.Name);
        }

        private void NightModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ThemeManager.ChangeAppTheme(Application.Current, "BaseDark");
            ThemeManager.ChangeAppTheme(Application.Current, "BaseLight");
            GlobalConsts.Theme = ThemeManager.GetAppTheme("BaseLight");
        }

        private void NightModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ThemeManager.ChangeAppTheme(Application.Current, "BaseLight");
            ThemeManager.ChangeAppTheme(Application.Current, "BaseDark");
            GlobalConsts.Theme = ThemeManager.GetAppTheme("BaseDark");
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var style = ThemeManager.DetectAppStyle();
            ThemeManager.ChangeAppStyle(Application.Current, (Accent)comboBox.SelectedItem, style.Item1);
            GlobalConsts.Accent = (Accent)comboBox.SelectedItem;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            GlobalConsts.SaveConsts();
            GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GlobalConsts.ChangeLanguage((string)LanguageComboBox.SelectedItem);
            FlowDirection = (FlowDirection)FindResource("FlowDirection");
        }

        private void TextBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.RootFolder = System.Environment.SpecialFolder.Desktop;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    SaveDirectoryTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void SaveDirectoryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string dir = SaveDirectoryTextBox.Text;
            if (System.IO.Directory.Exists(dir))
            {
                GlobalConsts.SaveDirectory = dir;
                SaveDirectoryTextBox.Background = null;
            }
            else
                SaveDirectoryTextBox.Background = GlobalConsts.ErrorBrush;

        }

        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            TextBox_MouseDoubleClick(sender, null);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.CheckForProgramUpdates = CheckForUpdatesCheckBox.IsChecked.Value;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.CheckForProgramUpdates = CheckForUpdatesCheckBox.IsChecked.Value;
        }

        private void SaveDownloadOptionsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.SaveDownloadOptions = SaveDownloadOptionsCheckBox.IsChecked.Value;
        }

        private void SaveDownloadOptionsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.SaveDownloadOptions = SaveDownloadOptionsCheckBox.IsChecked.Value;
        }
    }
}
