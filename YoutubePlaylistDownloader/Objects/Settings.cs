using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace YoutubePlaylistDownloader.Objects
{
    [JsonObject]
    public class Settings
    {
        [JsonProperty]
        public string Theme { get; set; }

        [JsonProperty]
        public string Accent { get; set; }

        [JsonProperty]
        public string Language { get; set; }

        [JsonProperty]
        public string SaveDirectory { get; set; }

        [JsonProperty]
        public bool OptionExpanderIsExpanded { get; set; }

        [JsonProperty]
        public bool CheckForSubscriptionUpdates { get; set; }

        [JsonProperty]
        public bool CheckForProgramUpdates { get; set; }

        [JsonProperty]
        public TimeSpan SubscriptionsDelay { get; set; }

        [JsonProperty]
        public bool SaveDownloadOptions { get; set; }

        [JsonProperty]
        public int MaximumConverstionsCount { get; set; }

        [JsonProperty]
        public int ActualConvertionsLimit { get; set; }

        [JsonProperty]
        public bool LimitConvertions { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool ConfirmExit { get; set; }

        [JsonConstructor]
        public Settings(string theme, string accent, string language, string saveDirectory, bool optionExpanderIsExpanded, bool checkForSubscriptionUpdates, bool checkForProgramUpdates, TimeSpan subscriptionsDelay, bool saveDownloadOptions, int maximumConverstionsCount,  int actualConvertionsLimit, bool limitConvertions, bool confirmExit)
        {
            Theme = theme;
            Accent = accent;
            Language = language;
            SaveDirectory = saveDirectory;
            OptionExpanderIsExpanded = optionExpanderIsExpanded;
            CheckForSubscriptionUpdates = checkForSubscriptionUpdates;
            CheckForProgramUpdates = checkForProgramUpdates;
            SubscriptionsDelay = subscriptionsDelay;
            SaveDownloadOptions = saveDownloadOptions;
            MaximumConverstionsCount = maximumConverstionsCount;
            ActualConvertionsLimit = actualConvertionsLimit;
            LimitConvertions = limitConvertions;
            ConfirmExit = confirmExit;
        }

    }
}
