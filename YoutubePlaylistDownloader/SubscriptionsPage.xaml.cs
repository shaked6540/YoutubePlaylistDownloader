using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using YoutubePlaylistDownloader.Utilities;

namespace YoutubePlaylistDownloader
{
    using MahApps.Metro.Controls;
    using MahApps.Metro.IconPacks;
    using YoutubeExplode;
    using YoutubePlaylistDownloader.Objects;
    using static SubscriptionManager;

    /// <summary>
    /// Interaction logic for Subscriptions.xaml
    /// </summary>
    public partial class SubscriptionsPage : UserControl
    {
        private string SubscriptionChannelId;

        public SubscriptionsPage()
        {
            InitializeComponent();

            CheckForSubscriptionUpdatesCheckBox.IsChecked = GlobalConsts.CheckForSubscriptionUpdates;

            GlobalConsts.HideSubscriptionsButton();
            GlobalConsts.ShowHelpButton();
            GlobalConsts.ShowSettingsButton();
            GlobalConsts.ShowHomeButton();
            GlobalConsts.ShowAboutButton();

            void UpdateSize(object s, SizeChangedEventArgs e)
            {
                GridScrollViewer.Height = Subscriptions.Count * 105;
                GridScrollViewer.MaxHeight = GlobalConsts.GetOffset() - 165;
                GridScrollViewer.UpdateLayout();
            }

            GlobalConsts.Current.SizeChanged += UpdateSize;
            Unloaded += (s, e) => GlobalConsts.Current.SizeChanged -= UpdateSize;
            

            SubscriptionsUpdateDelayTextBox.Text = GlobalConsts.SubscriptionsUpdateDelay.TotalMinutes.ToString();
            FillSubscriptions().ConfigureAwait(false);


        }
        private async Task FillSubscriptions()
        {
            foreach (var sub in Subscriptions)
                await AddSubscriptionPanel(sub);
        }

        private async void AddChannelSubscriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (YoutubeHelpers.TryParseChannelId(AddChannelSubscriptionTextBox.Text, out string channelId) && !Subscriptions.Any(x => x.ChannelId == channelId))
            {
                SubscriptionChannelId = channelId;
                AddChannelButton.IsEnabled = true;
            }
            else if (YoutubeHelpers.TryParseUsername(AddChannelSubscriptionTextBox.Text, out string username))
            {
                try
                {
                    var client = GlobalConsts.YoutubeClient;
                    var channelID = (await client.Channels.GetByUserAsync(username)).Id.Value;

                    if (!Subscriptions.Any(x => x.ChannelId == channelID))
                    {
                        SubscriptionChannelId = channelID;
                        AddChannelButton.IsEnabled = true;
                    }
                    else
                        AddChannelButton.IsEnabled = false;
                }
                catch
                {
                    AddChannelButton.IsEnabled = false;
                }
            }

            else
                AddChannelButton.IsEnabled = false;
        }

        private async void AddChannelButton_Click(object sender, RoutedEventArgs e)
        {
            var sub = new Subscription(

#if DEBUG
                new DateTime(2018, 9, 25),
#else
                DateTime.Now,
#endif

                SubscriptionChannelId, new DownloadSettings("mp3", false, YoutubeHelpers.High720,
                false, false, false, false, string.Empty, false, "en", false, false, 0, 0, false, true, false, true, 4), new List<string>());


            Subscriptions.Add(sub);

            AddChannelSubscriptionTextBox.Text = string.Empty;
            await AddSubscriptionPanel(sub).ConfigureAwait(false);

            if (GlobalConsts.CheckForSubscriptionUpdates)
                await sub.RefreshUpdate();

        }

        private async Task AddSubscriptionPanel(Subscription sub)
        {
            var channel = await sub.GetChannel();
            _ = Dispatcher.InvokeAsync(() =>
            {
                int row = SubscriptionsGrid.RowDefinitions.Count;
                RowDefinition rowDefinition = new RowDefinition
                {
                    Height = GridLength.Auto,
                };
                SubscriptionsGrid.RowDefinitions.Add(rowDefinition);

                Image logo = new Image
                {
                    MaxWidth = 120,
                    MaxHeight = 120,
                    Width = 98,
                    Height = 98,
                    Margin = new Thickness(2),
                    Source = new BitmapImage(new Uri(channel.Thumbnails.FirstOrDefault()?.Url)),
                    VerticalAlignment = VerticalAlignment.Top
                };
                Grid.SetColumn(logo, 0);
                Grid.SetRow(logo, row);
                SubscriptionsGrid.Children.Add(logo);


                TextBlock description = new TextBlock
                {
                    Height = 40,
                    Width = double.NaN,
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Top
                };
                description.Inlines.Add(string.Concat(FindResource("PlaylistTitle"), channel.Title, "\n"));
                description.Inlines.Add(string.Concat(FindResource("LastVideoDownloadDate"), sub.LatestVideoDownloaded.ToLocalTime().ToShortDateString()));
                Grid.SetColumn(description, 1);
                Grid.SetRow(description, row);
                SubscriptionsGrid.Children.Add(description);

                StackPanel buttonsPanel = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(buttonsPanel, 2);
                Grid.SetRow(buttonsPanel, row);
                SubscriptionsGrid.Children.Add(buttonsPanel);

                Tile settingsButton = new Tile
                {
                    Height = double.NaN,
                    Width = double.NaN,
                    Margin = new Thickness(2.5),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                removeButton = new Tile
                {
                    Height = double.NaN,
                    Width = double.NaN,
                    Margin = new Thickness(2.5),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                removeButton.Click += (s, e) =>
                {
                    rowDefinition.Height = new GridLength(0);
                    Subscriptions.Remove(Subscriptions.FirstOrDefault(x => x.ChannelId.Equals(sub.ChannelId)));
                };

                settingsButton.Click += (s, e) =>
                {
                    GlobalConsts.LoadFlyoutPage(new SubscriptionSettings(sub));
                };

                buttonsPanel.Children.Add(settingsButton);
                buttonsPanel.Children.Add(removeButton);

                WrapPanel settingsButtonWrapPanel = new WrapPanel(), removeButtonWrapPanel = new WrapPanel();

                //Settings button
                PackIconModern settingsButtonIcon = new PackIconModern
                {
                    Kind = PackIconModernKind.Cogs,
                    Width = 35,
                    Height = 35,
                    Margin = new Thickness(5)
                };
                TextBlock settingsButtonTextBlock = new TextBlock
                {
                    Text = $"{FindResource("Settings")}",
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 16,
                    Margin = new Thickness(2.5, 5, 7.5, 5)
                };
                settingsButtonWrapPanel.Children.Add(settingsButtonIcon);
                settingsButtonWrapPanel.Children.Add(settingsButtonTextBlock);
                settingsButton.Content = settingsButtonWrapPanel;

                //Remove button
                PackIconModern removeButtonIcon = new PackIconModern
                {
                    Kind = PackIconModernKind.Close,
                    Width = 35,
                    Height = 35,
                    Margin = new Thickness(5)
                };
                TextBlock removeButtonTextBlock = new TextBlock
                {
                    Text = $"{FindResource("Remove")}",
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 16,
                    Margin = new Thickness(2.5, 5, 7.5, 5)
                };
                removeButtonWrapPanel.Children.Add(removeButtonIcon);
                removeButtonWrapPanel.Children.Add(removeButtonTextBlock);
                removeButton.Content = removeButtonWrapPanel;

                GridScrollViewer.Height = GlobalConsts.Current.ActualHeight - 300;
                GridScrollViewer.UpdateLayout();
            });
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.CheckForSubscriptionUpdates = CheckForSubscriptionUpdatesCheckBox.IsChecked.Value;
            GlobalConsts.SaveConsts();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GlobalConsts.CheckForSubscriptionUpdates = CheckForSubscriptionUpdatesCheckBox.IsChecked.Value;
            GlobalConsts.SaveConsts();
        }

        private void SubscriptionsUpdateDelayTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(SubscriptionsUpdateDelayTextBox.Text, out int delay))
            {
                if (delay <= 0)
                {
                    SubscriptionsUpdateDelayTextBox.Background = GlobalConsts.ErrorBrush;
                    return;
                }
                GlobalConsts.SubscriptionsUpdateDelay = TimeSpan.FromMinutes(delay);
                SubscriptionsUpdateDelayTextBox.Background = null;
                GlobalConsts.SaveConsts();
            }
            else
            {
                SubscriptionsUpdateDelayTextBox.Background = GlobalConsts.ErrorBrush;
            }
        }
    }
}
