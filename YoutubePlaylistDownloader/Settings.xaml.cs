using MahApps.Metro;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
            SaveDownloadOptionsCheckBox.IsChecked = GlobalConsts.SaveDownloadOptions;
            LimitConvertionsCheckBox.IsChecked = GlobalConsts.LimitConvertions;
            ConfirmOnExitCheckBox.IsChecked = GlobalConsts.ConfirmExit;
            ActualConvertionTextBox.Text = GlobalConsts.ActualConvertionsLimit.ToString();
            ActualConvertionTextBox.TextChanged += ActualConvertionTextBox_TextChanged;

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

        private void LimitConvertionsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.LimitConvertions = LimitConvertionsCheckBox.IsChecked.Value;
        }

        private void LimitConvertionsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.LimitConvertions = LimitConvertionsCheckBox.IsChecked.Value;
        }

        private void ActualConvertionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(ActualConvertionTextBox.Text, out var actual) && actual > 0 && actual < GlobalConsts.MaximumConverstionsCount)
            {
                ActualConvertionTextBox.Background = null;
                int delta = actual - GlobalConsts.ConversionsLocker.CurrentCount;
                if (delta > 0)
                    GlobalConsts.ConversionsLocker.Release(delta);
                else
                {
                    delta = System.Math.Abs(delta);
                    for (int i = 0; i < delta; i++)
                    {
                        GlobalConsts.ConversionsLocker.Wait();
                    }
                }
                GlobalConsts.ActualConvertionsLimit = actual;
            }
            else
            {
                ActualConvertionTextBox.Background = GlobalConsts.ErrorBrush;
            }
        }

        private void ConfirmOnExitCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.ConfirmExit = ConfirmOnExitCheckBox.IsChecked.Value;
        }

        private void ConfirmOnExitCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.ConfirmExit = ConfirmOnExitCheckBox.IsChecked.Value;
        }
    }
}
