using ControlzEx.Theming;
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

            if (GlobalConsts.settings.Theme == "Dark") NightModeCheckBox.IsChecked = true;
            CheckForUpdatesCheckBox.IsChecked = GlobalConsts.settings.CheckForProgramUpdates;
            SaveDownloadOptionsCheckBox.IsChecked = GlobalConsts.settings.SaveDownloadOptions;
            LimitConvertionsCheckBox.IsChecked = GlobalConsts.settings.LimitConvertions;
            ConfirmOnExitCheckBox.IsChecked = GlobalConsts.settings.ConfirmExit;
            ActualConvertionTextBox.Text = GlobalConsts.settings.ActualConvertionsLimit.ToString();
            ActualConvertionTextBox.TextChanged += ActualConvertionTextBox_TextChanged;

            NightModeCheckBox.Checked += NightModeCheckBox_Checked;
            NightModeCheckBox.Unchecked += NightModeCheckBox_Unchecked;

            GlobalConsts.HideSettingsButton();
            GlobalConsts.ShowHomeButton();
            GlobalConsts.ShowAboutButton();
            GlobalConsts.ShowHelpButton();
        }

        private void FillLanguages()
        {
            var languages = ((string)FindResource("LanguageList")).Split(';');
            LanguageComboBox.ItemsSource = languages;
            LanguageComboBox.SelectedItem = GlobalConsts.settings.Language;
        }

        private void FillAccents()
        {
            var accents = ThemeManager.Current.ColorSchemes;
            comboBox.ItemsSource = accents;
            comboBox.SelectedItem = ((IEnumerable<string>)comboBox.ItemsSource).FirstOrDefault(x => x == GlobalConsts.settings.Accent);
        }

        private void NightModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.ChangeTheme(Application.Current, $"Dark.{GlobalConsts.settings.Accent}");
            ThemeManager.Current.ChangeTheme(Application.Current, $"Light.{GlobalConsts.settings.Accent}");
            GlobalConsts.settings.Theme = "Light";
        }

        private void NightModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.ChangeTheme(Application.Current, $"Light.{GlobalConsts.settings.Accent}");
            ThemeManager.Current.ChangeTheme(Application.Current, $"Dark.{GlobalConsts.settings.Accent}");
            GlobalConsts.settings.Theme = "Dark";
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ThemeManager.Current.ChangeTheme(Application.Current, $"{GlobalConsts.settings.Theme}.{comboBox.SelectedItem}");
            GlobalConsts.settings.Accent = (string)comboBox.SelectedItem;
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
            GlobalConsts.settings.CheckForProgramUpdates = CheckForUpdatesCheckBox.IsChecked.Value;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.settings.CheckForProgramUpdates = CheckForUpdatesCheckBox.IsChecked.Value;
        }

        private void SaveDownloadOptionsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.settings.SaveDownloadOptions = SaveDownloadOptionsCheckBox.IsChecked.Value;
        }

        private void SaveDownloadOptionsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.settings.SaveDownloadOptions = SaveDownloadOptionsCheckBox.IsChecked.Value;
        }

        private void LimitConvertionsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.settings.LimitConvertions = LimitConvertionsCheckBox.IsChecked.Value;
        }

        private void LimitConvertionsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.settings.LimitConvertions = LimitConvertionsCheckBox.IsChecked.Value;
        }

        private void ActualConvertionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(ActualConvertionTextBox.Text, out var actual) && actual > 0 && actual < GlobalConsts.settings.MaximumConverstionsCount)
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
                GlobalConsts.settings.ActualConvertionsLimit = actual;
            }
            else
            {
                ActualConvertionTextBox.Background = GlobalConsts.ErrorBrush;
            }
        }

        private void ConfirmOnExitCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.settings.ConfirmExit = ConfirmOnExitCheckBox.IsChecked.Value;
        }

        private void ConfirmOnExitCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.settings.ConfirmExit = ConfirmOnExitCheckBox.IsChecked.Value;
        }
    }
}
