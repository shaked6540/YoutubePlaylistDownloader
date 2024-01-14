namespace YoutubePlaylistDownloader.Utilities;

public static class ExtensionMethods
{
    public static async Task WhenAll(params ValueTask[] tasks)
    {
        var toAwait = new List<Task>();

        foreach (var valueTask in tasks)
        {
            if (!valueTask.IsCompletedSuccessfully)
                toAwait.Add(valueTask.AsTask());
        }

        await Task.WhenAll(toAwait).ConfigureAwait(false);
    }
}
