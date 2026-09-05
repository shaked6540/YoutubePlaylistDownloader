namespace YoutubePlaylistDownloader.Utilities;

public static class AtomicFile
{
    public static void WriteAllText(string destinationPath, string contents)
    {
        var partialPath = destinationPath + ".part";
        try
        {
            File.WriteAllText(partialPath, contents);
            File.Move(partialPath, destinationPath, true);
        }
        catch
        {
            if (File.Exists(partialPath))
                File.Delete(partialPath);
            throw;
        }
    }

    public static void CopyAndReplace(string sourcePath, string destinationPath)
    {
        var partialPath = destinationPath + ".part";
        try
        {
            File.Copy(sourcePath, partialPath, true);
            File.Move(partialPath, destinationPath, true);
        }
        catch
        {
            if (File.Exists(partialPath))
                File.Delete(partialPath);
            throw;
        }
    }
}
