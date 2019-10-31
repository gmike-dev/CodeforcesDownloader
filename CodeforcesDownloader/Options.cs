using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace CodeforcesDownloader
{
  public sealed class Options
  {
    [Option('h', "handle", Required = true, HelpText = "User name (handle)")]
    public string Handle { get; set; }

    [Option('f', "folder", Default = @"%USERPROFILE%\Downloads\CodeforcesDownloader",
      HelpText = "Folder to save the data")]
    public string Folder { get; set; }

    [Usage]
    public static IEnumerable<Example> Examples
    {
      get
      {
        yield return new Example("Load all submissions source text for user 'mike'", new Options { Handle = "mike", });
      }
    }
  }
}
