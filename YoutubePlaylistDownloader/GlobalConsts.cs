using Encryptor;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YoutubePlaylistDownloader
{
    static class GlobalConsts
    {
        //The const variables are variables that can be accessed from all over the solution.
        #region Const Variables

        public static Skeleton Current;
        public static AppTheme Theme;
        public static Accent Accent;
        public static Brush ErrorBrush;
        public static string Language;
        public static readonly string ApplicationName;
        public static readonly string TempFolderPath;
        public static string SaveDirectory;
        public static readonly string CurrentDir;
        private static readonly string ConfigFilePath;
        private static readonly string ErrorFilePath;
        


        public static AppTheme Opposite { get { return Theme.Name == "BaseLight" ? ThemeManager.GetAppTheme("BaseDark") : ThemeManager.GetAppTheme("BaseLight"); } }

        #endregion

        static GlobalConsts()
        {
            CurrentDir = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.ToString();
            ConfigFilePath = CurrentDir + "\\TopSecretFile.dll";
            ErrorFilePath = CurrentDir + $"\\{Assembly.GetExecutingAssembly().GetName().Name}.log";
            ErrorBrush = Brushes.Crimson;
            Language = "English";
            ApplicationName = "YoutubePlaylistDownloader";
            SaveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            TempFolderPath = Path.GetTempPath() + "YoutubePlaylistDownloader\\";
        }

        //The const methods are used mainly for saving/loading consts, and handling page\menu management.
        #region Const Methods
        public static void HideSettingsButton()
        {
            Current.SettingsButton.Visibility = Visibility.Collapsed;
        }

        public static void ShowSettingsButton()
        {
            Current.SettingsButton.Visibility = Visibility.Visible;
        }

        public static void HideAboutButton()
        {
            Current.AboutButton.Visibility = Visibility.Collapsed;
        }

        public static void ShowAboutButton()
        {
            Current.AboutButton.Visibility = Visibility.Visible;
        }

        public static void HideHomeButton()
        {
            Current.HomeButton.Visibility = Visibility.Collapsed;
        }

        public static void ShowHomeButton()
        {
            Current.HomeButton.Visibility = Visibility.Visible;
        }

        public static async Task ShowMessage(string title, string message)
        {
            if (Current.DefaultFlyout.IsOpen)
                Current.DefaultFlyout.IsOpen = false;
            await Current.ShowMessage(title, message).ConfigureAwait(false);
        }

        public static async Task<MessageDialogResult> ShowYesNoDialog(string title, string message)
        {
            if (Current.DefaultFlyout.IsOpen)
                Current.DefaultFlyout.IsOpen = false;
            return await Current.ShowYesNoDialog(title, message).ConfigureAwait(false);
        }

        public static void LoadPage(UserControl page) => Current.CurrentPage.Content = page;
        public static void SaveConsts()
        {
            //Save the app settings for next use
            using (StreamWriter sw = new StreamWriter(ConfigFilePath, false))
            {
                sw.WriteLine(Theme.Name.Encrypt());
                sw.WriteLine(Accent.Name.Encrypt());
                sw.WriteLine(Language.Encrypt());
                sw.WriteLine(SaveDirectory.Encrypt());
            }

        }
        private static void RestoreDefualts()
        {
            Theme = ThemeManager.GetAppTheme("BaseLight");
            Accent = ThemeManager.GetAccent("Cobalt");
            Language = "English";

            SaveConsts();
        }
        public static void LoadConsts()
        {
            if (!File.Exists(ConfigFilePath))
            {
                RestoreDefualts();
                return;
            }


            var lines = File.ReadAllLines(ConfigFilePath);


            Theme = ThemeManager.GetAppTheme(lines[0].Decrypt());
            Accent = ThemeManager.GetAccent(lines[1].Decrypt());
            Language = lines[2].Decrypt();
            SaveDirectory = lines[3].Decrypt();

            UpdateTheme();
            UpdateLanguage();

        }

        public static void CreateTempFolder()
        {
            if (!Directory.Exists(Path.GetTempPath() + "YoutubePlaylistDownloader"))
                Directory.CreateDirectory(Path.GetTempPath() + "YoutubePlaylistDownloader");
            
        }
        public static void CleanTempFolder()
        {
            if (Directory.Exists(Path.GetTempPath() + "YoutubePlaylistDownloader"))
            {
                DirectoryInfo di = new DirectoryInfo(Path.GetTempPath() + "YoutubePlaylistDownloader");

                foreach (FileInfo file in di.GetFiles())
                    file.Delete();

                foreach (DirectoryInfo dir in di.GetDirectories())
                    dir.Delete(true);
            }
        }
        private static void UpdateTheme()
        {
            ThemeManager.ChangeAppStyle(Application.Current, Accent, Opposite);
            ThemeManager.ChangeAppTheme(Application.Current, Theme.Name);
        }
        private static void UpdateLanguage()
        {
            ResourceDictionary toRemove = Application.Current.Resources.MergedDictionaries.First(x => x.Source.OriginalString.Contains("English"));
            ResourceDictionary r = new ResourceDictionary()
            {
                Source = new Uri($"/Languages/{Language}.xaml", UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Add(r);
            Application.Current.Resources.MergedDictionaries.Remove(toRemove);
        }
        public static void ChangeLanguage(string nLang)
        {
            ResourceDictionary toRemove = Application.Current.Resources.MergedDictionaries.First(x => x.Source.OriginalString.Contains(Language));
            ResourceDictionary r = new ResourceDictionary()
            {
                Source = new Uri($"/Languages/{nLang}.xaml", UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Add(r);
            Application.Current.Resources.MergedDictionaries.Remove(toRemove);
            Language = nLang;
        }
        public static async Task Log(string message, object sender)
        {
            using (StreamWriter sw = new StreamWriter(ErrorFilePath, true))
            {
                await sw.WriteLineAsync($"[{DateTime.Now.ToString()}], [{sender}]:\t{message}");
            }
        }

        #endregion


    }
}
