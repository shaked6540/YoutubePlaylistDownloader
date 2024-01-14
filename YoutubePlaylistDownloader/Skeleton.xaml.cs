namespace YoutubePlaylistDownloader;

/// <summary>
/// Interaction logic for Skeleton.xaml
/// </summary>
public partial class Skeleton : MetroWindow
{

    private bool exit = false;

    public Skeleton()
    {
        //Initialize the app
        InitializeComponent();
        SetWindow();
        GlobalConsts.Current = this;
        //Go to main menu
        GlobalConsts.LoadPage(new MainPage());

        if (GlobalConsts.settings.CheckForProgramUpdates)
            CheckForUpdates().ConfigureAwait(false);
    }

    private async Task CheckForUpdates()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue() { NoCache = true };
            var response = await client.GetStringAsync("https://raw.githubusercontent.com/shaked6540/YoutubePlaylistDownloader/master/YoutubePlaylistDownloader/latestVersionWithRevision.txt");
            var latestVersion = Version.Parse(response);

            if (latestVersion > GlobalConsts.VERSION)
            {
                var changelog = await client.GetStringAsync("https://raw.githubusercontent.com/shaked6540/YoutubePlaylistDownloader/master/YoutubePlaylistDownloader/changelog.txt");
                var dialogSettings = new MetroDialogSettings()
                {
                    AffirmativeButtonText = $"{FindResource("UpdateNow")}",
                    NegativeButtonText = $"{FindResource("No")}",
                    FirstAuxiliaryButtonText = $"{FindResource("UpdateWhenIExit")}",
                    ColorScheme = MetroDialogColorScheme.Theme,
                    DefaultButtonFocus = MessageDialogResult.Affirmative,
                };

                var update = await this.ShowMessageAsync($"{FindResource("NewVersionAvailable")}", $"{FindResource("DoYouWantToUpdate")}\n{changelog}",
                    MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, dialogSettings);
                if (update == MessageDialogResult.Affirmative)
                    GlobalConsts.LoadPage(new DownloadUpdate(latestVersion, changelog));

                else if (update == MessageDialogResult.FirstAuxiliary)
                {
                    GlobalConsts.UpdateControl = new DownloadUpdate(latestVersion, changelog, true).UpdateLaterStillDownloading();
                }
            }
        }
        catch (Exception ex)
        {
            await GlobalConsts.Log(ex.ToString(), "Skeleton CheckForUpdates");
        }
    }

    public Task<MessageDialogResult> CustomYesNoDialog(string title, string message, MetroDialogSettings dialogSettings)
    {
        return this.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegative, dialogSettings);
    }
    public async Task ShowSelectableDialog(string title, string message, Action retryAction, Action cancelAction = null)
    {

        await Dispatcher.InvokeAsync(async () =>
        {
            var uc = new UserControl();

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            var text = new Run(message);
            Paragraph paragraph = new();
            paragraph.Inlines.Add(text);

            var contentTextBox = new RichTextBox
            {
                IsReadOnly = true,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Pen,
                Height = double.NaN,
                IsReadOnlyCaretVisible = false
            };
            contentTextBox.Document.Blocks.Add(paragraph);
            Grid.SetColumn(contentTextBox, 0);
            Grid.SetColumnSpan(contentTextBox, 10);
            Grid.SetRow(contentTextBox, 0);

            var retryButton = new Button()
            {
                Style = (Style)FindResource("MahApps.Styles.Button.Dialogs.Accent"),
                Content = FindResource("Retry"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5),
                MinWidth = 100,
                Height = 40
            };
            var backButton = new Button()
            {
                Style = (Style)FindResource("MahApps.Styles.Button.Dialogs"),
                Content = FindResource("Back"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5),
                MinWidth = 100,
                Height = 40
            };

            var copyToClipboardButton = new Button()
            {
                Style = (Style)FindResource("MahApps.Styles.Button.Dialogs"),
                Content = FindResource("CopyToClipboard"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5),
                MinWidth = 125,
                Width = double.NaN,
                Height = 40
            };
            var saveToTextFileButton = new Button()
            {
                Style = (Style)FindResource("MahApps.Styles.Button.Dialogs"),
                Content = FindResource("SaveToTextFile"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5),
                MinWidth = 100,
                Height = 40
            };

            Grid.SetRow(retryButton, 1);
            Grid.SetColumn(retryButton, 3);
            Grid.SetRow(backButton, 1);
            Grid.SetColumn(backButton, 2);
            Grid.SetRow(copyToClipboardButton, 1);
            Grid.SetColumn(copyToClipboardButton, 1);
            Grid.SetRow(saveToTextFileButton, 1);
            Grid.SetColumn(saveToTextFileButton, 0);

            retryButton.Click += (a, b) => retryAction();

            if (cancelAction != null)
            {
                backButton.Click += (a, b) => cancelAction();
            }

            copyToClipboardButton.Click += (a, b) => Clipboard.SetText(message);
            saveToTextFileButton.Click += (a, b) =>
            {
                Microsoft.Win32.SaveFileDialog fileDialog = new()
                {
                    FileName = "Videos not downloaded",
                    DefaultExt = ".txt",
                    Filter = "Text documents (.txt)|*.txt"
                };
                if (fileDialog.ShowDialog() == true)
                {
                    File.WriteAllText(fileDialog.FileName, message);
                }
            };

            grid.Children.Add(contentTextBox);
            grid.Children.Add(backButton);
            grid.Children.Add(retryButton);
            grid.Children.Add(copyToClipboardButton);
            grid.Children.Add(saveToTextFileButton);
            uc.Content = grid;
            var dialog = new CustomDialog() { Content = uc, Title = title };
            retryButton.Click += async (a, b) => await Dispatcher.InvokeAsync(async () => await this.HideMetroDialogAsync(dialog));
            backButton.Click += async (a, b) => await Dispatcher.InvokeAsync(async () => await this.HideMetroDialogAsync(dialog));
            await this.ShowMetroDialogAsync(dialog);

        });
    }
    public async Task ShowMessage(string title, string message)
    {
        await this.ShowMessageAsync(title, message);
        if (DefaultFlyout.IsOpen)
            DefaultFlyout.IsOpen = false;
    }

    public async Task<MessageDialogResult> ShowYesNoDialog(string title, string message)
    {
        return await this.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
        {
            AffirmativeButtonText = (string)FindResource("Yes"),
            NegativeButtonText = (string)FindResource("No")
        });
    }

    private void SetWindow()
    {

        WindowStyle = WindowStyle.None;
        IgnoreTaskbarOnMaximize = false;
        ShowTitleBar = false;
        ResizeMode = ResizeMode.CanResizeWithGrip;
        Closing += MainWindow_Closing;

    }

    private async void MainWindow_Closing(object sender, CancelEventArgs e)
    {
        if (!exit && GlobalConsts.settings.ConfirmExit)
        {
            e.Cancel = true;

            var exitMessage = $"{FindResource("ExitMessage")}";

            var res = await ShowYesNoDialog((string)FindResource("Exit"), exitMessage);
            if (res == MessageDialogResult.Affirmative)
            {

                exit = true;
                Close();
            }
        }
        else if (GlobalConsts.UpdateOnExit)
        {
            if (GlobalConsts.UpdateLater && !GlobalConsts.UpdateFinishedDownloading)
            {
                e.Cancel = true;
                GlobalConsts.LoadPage(GlobalConsts.UpdateControl?.UpdateLaterStillDownloading());
                return;
            }
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        GlobalConsts.LoadPage(new Settings());
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        GlobalConsts.LoadPage(new About());
    }

    private void Home_Click(object sender, RoutedEventArgs e)
    {
        GlobalConsts.LoadPage(GlobalConsts.MainPage.Load());
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        GlobalConsts.LoadPage(new Help());
    }
}
