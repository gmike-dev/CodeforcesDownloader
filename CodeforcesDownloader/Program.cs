using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeforcesDownloader.REST;
using CommandLine;
using NLog;

namespace CodeforcesDownloader
{
  static class Program
  {
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

    static void Main(string[] args)
    {
      Log.Trace($"Start a program");
      SubscribeToUnhandledException();
      Log.Trace($"Parse arguments");
      Parser.Default.ParseArguments<Options>(args)
        .WithParsed(Run)
        .WithNotParsed(_ => Environment.Exit(-1));
    }

    private static void Run(Options options)
    {
      string destinationFolder = Environment.ExpandEnvironmentVariables(Path.Join(options.Folder));

      Log.Trace($"Download data. Destination - {destinationFolder}");

      using var restClient = new Client(new Throttle(TimeSpan.FromMilliseconds(250)));
      using var sourceTextLoader = new SubmissionSourceTextLoader(new Throttle(TimeSpan.FromMilliseconds(1000), true));
      foreach (var submission in EnumerateAllUserSubmissions(restClient, options.Handle))
      {
        var submissionFolder = Path.Join(
          destinationFolder, options.Handle, "contests", submission.ContestId.ToString());

        var sourceTextFileName = $"{submission.Id}.{GetSourceTextExt(submission.ProgrammingLanguage)}";

        string filePath = Path.Combine(GetOrCreateDirectory(submissionFolder).FullName, sourceTextFileName);
        if (File.Exists(filePath))
        {
          Log.Info($"File already exists: {filePath}");
          continue;
        }
        var sourceText = sourceTextLoader.GetSourceTextAsync(submission).Result;
        if (sourceText == null)
          continue;
        using var streamWriter = new StreamWriter(filePath);
        streamWriter.Write(sourceText);
        Log.Info($"File created: {filePath}");
      }

      Log.Info($"Done. Check folder {destinationFolder}");
    }

    private static string GetSourceTextExt(string programmingLanguage)
    {
      return programmingLanguage switch
      {
        { } s when s.Contains("Python") => "py",
        "PyPy" => "py",
        { } s when s.Contains("Java") => "java",
        { } s when s.Contains("C++") || s.Contains("Clang++") => "cpp",
        { } s when s.Contains("C#") => "cs",
        "FPC" => "pas",
        "Delphi" => "pas",
        _ => "txt"
      };
    }

    private static DirectoryInfo GetOrCreateDirectory(string targetPath)
    {
      var downloadFolder = new DirectoryInfo(Environment.ExpandEnvironmentVariables(targetPath));
      if (downloadFolder.Exists)
      {
        return downloadFolder;
      }

      Log.Trace($"Create folder {downloadFolder.FullName}");
      downloadFolder.Create();
      return downloadFolder;
    }

    private static IEnumerable<Submission> EnumerateAllUserSubmissions(Client restClient, string handle)
    {
      var from = 1;
      var submissions = restClient.UserStatus(handle, from, 10).Result;
      while (submissions.Any())
      {
        foreach (var submission in submissions.Where(s => s.Verdict == "OK" && s.ContestId != null))
        {
          yield return submission;
        }
        from += submissions.Length;
        submissions = restClient.UserStatus(handle, from, 10).Result;
      }
    }

    private static void SubscribeToUnhandledException()
    {
      AppDomain.CurrentDomain.UnhandledException += (s, e) =>
      {
        Log.Error(e.ExceptionObject);
        if (e.IsTerminating)
          Environment.Exit(-1);
      };
    }
  }
}
