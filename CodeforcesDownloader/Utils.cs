using System;
using System.IO;
using NLog;

namespace CodeforcesDownloader;

internal static class Utils
{
  private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

  public static string NormalizeFileName(string fileName)
  {
    foreach (var invalidFileNameChar in Path.GetInvalidFileNameChars())
    {
      fileName = fileName.Replace(invalidFileNameChar, '_');
    }
    return fileName;
  }

  public static DirectoryInfo GetOrCreateDirectory(string targetPath)
  {
    var downloadFolder = new DirectoryInfo(Environment.ExpandEnvironmentVariables(targetPath));
    if (downloadFolder.Exists)
      return downloadFolder;

    Log.Trace($"Create folder {downloadFolder.FullName}");
    downloadFolder.Create();
    return downloadFolder;
  }
}