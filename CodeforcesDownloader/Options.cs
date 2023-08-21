using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace CodeforcesDownloader;

public sealed class Options
{
  [Option('h', "handle", Required = true, HelpText = "User name (handle)")]
  public string Handle { get; set; }

  [Option('f', "folder", Default = @"%USERPROFILE%\Downloads\CodeforcesDownloader",
    HelpText = "Folder to save the data")]
  public string Folder { get; set; }

  [Option("--cookie", Required = false, HelpText = "Site cookies (required for gyms load only)")]
  public string Cookie { get; set; }

  [Option("--wget-exe", Required = false, Default = "wget", HelpText = "Path to wget.exe")]
  public string WgetExePath { get; set; }

  [Usage]
  public static IEnumerable<Example> Examples
  {
    get
    {
      yield return new Example("Load all contest submissions source text for user 'mike'",
        new Options { Handle = "mike", });
      yield return new Example(
        "Load all contest and gyms submissions for user 'mike'. Cookie is required for load gym submissions. Take cookie from your browser when you signed at Codeforces.",
        new Options { Handle = "mike", Cookie = @"xxxxx" });
    }
  }
}
