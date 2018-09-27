using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace YoutubePlaylistDownloader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            GlobalConsts.LoadConsts();
            GlobalConsts.CreateTempFolder();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            GlobalConsts.SaveConsts();
            if (GlobalConsts.UpdateOnExit && !string.IsNullOrWhiteSpace(GlobalConsts.UpdateSetupLocation) && GlobalConsts.UpdateFinishedDownloading)
            {
                Process.Start(GlobalConsts.UpdateSetupLocation);
            }
            else
            {
                GlobalConsts.CleanTempFolder();
            }
            base.OnExit(e);
        }

        async void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {   
            await GlobalConsts.ShowMessage((string)FindResource("Error"), (string)FindResource("ErrorMessage"));
            await GlobalConsts.Log($"{e.Exception}", "Unhandled exception");

            // Don't crash at the moment of truth >.<
#if !DEBUG
                e.Handled = true;
#endif
        }
    }
}
