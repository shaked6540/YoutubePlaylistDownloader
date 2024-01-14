namespace YoutubePlaylistDownloader.Objects;

class QueuedDownload(IDownload downloadItem) : IDisposable
{
    private Border border;
    private IDownload item = downloadItem;
    private bool built = false;

    public Border GetDisplayGrid()
    {
        if (border == default(Border))
            Build();

        return border;
    }

    public void Build()
    {
        if (built) return;
        if (item == null)
        {
            built = true;
            return;
        }

        var DisplayGrid = new Grid();
        DisplayGrid.MouseLeftButtonDown += (sender, args) =>
        {
            if (args.ClickCount == 2)
                item.OpenFolder_Click(null, null);
        };
        DisplayGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        DisplayGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        DisplayGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        DisplayGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        DisplayGrid.RowDefinitions.Add(new RowDefinition());

        DisplayGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        DisplayGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        DisplayGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(400) });
        DisplayGrid.ColumnDefinitions.Add(new ColumnDefinition());

        var margin = new Thickness(2.5);

        border = new Border
        {
            Margin = margin,
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.White,
            Child = DisplayGrid
        };

        var stopButton = new Tile
        {
            Width = 50,
            Height = 50,
            Margin = margin,
            Content = new PackIconModern { Width = 40, Height = 40, Kind = PackIconModernKind.Close },
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        stopButton.Click += async (s, e) =>
        {
            var remove = await item.Cancel();
            if (remove)
                GlobalConsts.Downloads.Remove(this);
        };

        //Col 0:
        Grid.SetRow(stopButton, 0);
        Grid.SetRowSpan(stopButton, 6);
        Grid.SetColumn(stopButton, 0);


        var image = new Image
        {
            Width = 200,
            Height = 112.5,
            Margin = margin,
            Source = new BitmapImage(new Uri(item.ImageUrl)),
        };

        //Col 1:
        Grid.SetRow(image, 0);
        Grid.SetRowSpan(image, 6);
        Grid.SetColumn(image, 1);

        TextBlock
            title = new()
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = margin,
                FontSize = 14,
                Text = item.Title
            },
            currentTitle = new()
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = margin,
                FontSize = 14
            },
            totalDownloaded = new()
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = margin,
                FontSize = 14
            },
            currentStatus = new()
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = margin,
                FontSize = 14
            },
            downloadSpeed = new()
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = margin,
                FontSize = 14
            },
            downloadPercent = new()
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

        //Col 2:
        Grid.SetRow(title, 0);
        Grid.SetColumn(title, 2);
        Grid.SetRow(currentTitle, 1);
        Grid.SetColumn(currentTitle, 2);
        Grid.SetRow(totalDownloaded, 2);
        Grid.SetColumn(totalDownloaded, 2);
        Grid.SetRow(currentStatus, 3);
        Grid.SetColumn(currentStatus, 2);
        Grid.SetRow(downloadSpeed, 4);
        Grid.SetColumn(downloadSpeed, 2);

        Binding
            currentTitleBinding = new()
            {
                Source = item,
                Path = new PropertyPath("CurrentTitle"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.OneWay
            },
            totalDownloadedBinding = new()
            {
                Source = item,
                Path = new PropertyPath("TotalDownloaded"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.OneWay
            },
            currentStatusBinding = new()
            {
                Source = item,
                Path = new PropertyPath("CurrentStatus"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.OneWay
            },
            downloadSpeedBinding = new()
            {
                Source = item,
                Path = new PropertyPath("CurrentDownloadSpeed"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.OneWay
            },
            progressBinding = new()
            {
                Source = item,
                Path = new PropertyPath("CurrentProgressPercent"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.OneWay
            },
            downloadPercentBinding = new()
            {
                Source = item,
                Path = new PropertyPath("CurrentProgressPercent"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.OneWay
            };

        var progressBarGrid = new Grid { Margin = margin };

        var
            progressBar = new MetroProgressBar
            {
                Width = 550,
                Height = 30,
                VerticalAlignment = VerticalAlignment.Center
            };

        progressBarGrid.Children.Add(progressBar);
        progressBarGrid.Children.Add(downloadPercent);

        //Col 3:
        Grid.SetRow(progressBarGrid, 0);
        Grid.SetRowSpan(progressBarGrid, 6);
        Grid.SetColumn(progressBarGrid, 3);

        currentTitle.SetBinding(TextBlock.TextProperty, currentTitleBinding);
        totalDownloaded.SetBinding(TextBlock.TextProperty, totalDownloadedBinding);
        currentStatus.SetBinding(TextBlock.TextProperty, currentStatusBinding);
        downloadSpeed.SetBinding(TextBlock.TextProperty, downloadSpeedBinding);
        progressBar.SetBinding(RangeBase.ValueProperty, progressBinding);
        downloadPercent.SetBinding(TextBlock.TextProperty, downloadPercentBinding);

        //add everything to grid
        DisplayGrid.Children.Add(stopButton);
        DisplayGrid.Children.Add(image);
        DisplayGrid.Children.Add(title);
        DisplayGrid.Children.Add(currentTitle);
        DisplayGrid.Children.Add(totalDownloaded);
        DisplayGrid.Children.Add(currentStatus);
        DisplayGrid.Children.Add(downloadSpeed);
        DisplayGrid.Children.Add(progressBarGrid);

        built = true;
    }

    #region IDisposable Support
    private bool disposedValue = false;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                item.Dispose();
            }
            border = null;
            item = null;

            disposedValue = true;
        }
    }
    public void Dispose()
    {
        Dispose(true);
    }
    #endregion
}
