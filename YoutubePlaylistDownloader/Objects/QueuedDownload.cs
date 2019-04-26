using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace YoutubePlaylistDownloader.Objects
{
    class QueuedDownload : IDisposable
    {
        private Border border;
        private IDownload item;
        private bool built;

        public QueuedDownload(IDownload downloadItem)
        {
            item = downloadItem;
            built = false;
        }

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

            Grid DisplayGrid = new Grid();
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
                BorderThickness = new Thickness(1)
            };
            border.SetResourceReference(Border.BorderBrushProperty, "BlackBrush");

            border.Child = DisplayGrid;

            Tile stopButton = new Tile
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
            

            Image image = new Image
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
                title = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Margin = margin,
                    FontSize = 14,
                    Text = item.Title
                },
                currentTitle = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Margin = margin,
                    FontSize = 14
                },
                totalDownloaded = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Margin = margin,
                    FontSize = 14
                },
                currentStatus = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Margin = margin,
                    FontSize = 14
                },
                downloadSpeed = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Margin = margin,
                    FontSize = 14
                },
                downloadPrecent = new TextBlock
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
                currentTitleBinding = new Binding
                {
                    Source = item,
                    Path = new PropertyPath("CurrentTitle"),
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Mode = BindingMode.OneWay
                },
                totalDownloadedBinding = new Binding
                {
                    Source = item,
                    Path = new PropertyPath("TotalDownloaded"),
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Mode = BindingMode.OneWay
                },
                currentStatusBinding = new Binding
                {
                    Source = item,
                    Path = new PropertyPath("CurrentStatus"),
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Mode = BindingMode.OneWay
                },
                downloadSpeedBinding = new Binding
                {
                    Source = item,
                    Path = new PropertyPath("CurrentDownloadSpeed"),
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Mode = BindingMode.OneWay
                },
                progressBinding = new Binding
                {
                    Source = item,
                    Path = new PropertyPath("CurrentProgressPrecent"),
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Mode = BindingMode.OneWay
                },
                downloadPrecentBinding = new Binding
                {
                    Source = item,
                    Path = new PropertyPath("CurrentProgressPrecent"),
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Mode = BindingMode.OneWay
                };

            Grid progressBarGrid = new Grid { Margin = margin };

            MetroProgressBar
                progressBar = new MetroProgressBar
                {
                    Width = 550,
                    Height = 30,
                    VerticalAlignment = VerticalAlignment.Center
                };

            progressBarGrid.Children.Add(progressBar);
            progressBarGrid.Children.Add(downloadPrecent);

            //Col 3:
            Grid.SetRow(progressBarGrid, 0);
            Grid.SetRowSpan(progressBarGrid, 6);
            Grid.SetColumn(progressBarGrid, 3);

            currentTitle.SetBinding(TextBlock.TextProperty, currentTitleBinding);
            totalDownloaded.SetBinding(TextBlock.TextProperty, totalDownloadedBinding);
            currentStatus.SetBinding(TextBlock.TextProperty, currentStatusBinding);
            downloadSpeed.SetBinding(TextBlock.TextProperty, downloadSpeedBinding);
            progressBar.SetBinding(ProgressBar.ValueProperty, progressBinding);
            downloadPrecent.SetBinding(TextBlock.TextProperty, downloadPrecentBinding);

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
}
