using System.IO;

namespace CodeforcesDownloader;

internal static class Utils
{
  public static string NormalizeFileName(string fileName)
  {
    foreach (var invalidFileNameChar in Path.GetInvalidFileNameChars())
      fileName = fileName.Replace(invalidFileNameChar, '_');
    return fileName;
  }
}
