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
            if (GlobalConsts.UpdateOnExit && !string.IsNullOrWhiteSpace(GlobalConsts.UpdateSetupLocation))
            {
                Process.Start(GlobalConsts.UpdateSetupLocation);
                base.OnExit(e);
            }
            else
            {
                GlobalConsts.SaveConsts();
                GlobalConsts.CleanTempFolder();
                base.OnExit(e);
            }
        }

        async void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {

            var frame = new StackTrace(e.Exception).GetFrame(0);
            var file = frame.GetFileName();
            var line = frame.GetFileLineNumber();
            await GlobalConsts.Log($"{e.Exception}", "Unhandled exception");
            await GlobalConsts.ShowMessage((string)FindResource("Error"), (string)FindResource("ErrorMessage"));

            // Don't crash at the moment of truth >.<
#if !DEBUG
                e.Handled = true;
#endif
        }
    }
}
