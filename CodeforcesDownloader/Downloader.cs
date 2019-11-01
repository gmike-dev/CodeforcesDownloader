using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeforcesDownloader.REST;
using NLog;

namespace CodeforcesDownloader
{
  internal sealed class Downloader : IDisposable
  {
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
    
    private readonly Options options;
    private readonly Client restClient;
    private readonly string destinationFolder;
    private readonly SourceTextLoader sourceTextLoader;
    private readonly StatementDownloader statementDownloader;
    private readonly Lazy<IReadOnlyDictionary<int, Contest>> gyms;

    private IReadOnlyDictionary<int, Contest> Gyms => this.gyms.Value;

    public void Run()
    {
      Log.Trace($"Download data. Destination - {this.destinationFolder}");

      foreach (var submission in this.EnumerateAllUserSubmissions())
        this.ProcessSubmission(submission);

      Log.Info($"Done. Check folder {this.destinationFolder}");
    }

    private void ProcessSubmission(Submission submission)
    {
      bool isGym = this.Gyms.ContainsKey(submission.ContestId.Value);
      if (isGym && this.options.Cookie == default)
      {
        Log.Warn(
          $"Cannot process submission for gym #{submission.ContestId}: specify cookie for authorized requests support");
        return;
      }

      var problemFolder = Utils.GetOrCreateDirectory(Path.Join(this.destinationFolder,
        Utils.NormalizeFileName(this.options.Handle),
        isGym ? "gyms" : "contests",
        submission.ContestId.ToString(),
        Utils.NormalizeFileName($"{submission.Problem.Index}. {submission.Problem.Name}"))).FullName;

      if (!isGym)
        this.statementDownloader.DownloadStatement(submission.Problem, isGym, problemFolder);

      this.DownloadSourceText(submission, isGym, problemFolder);
    }
    
    private Dictionary<int, Contest> LoadGyms()
    {
      Log.Trace("Load gyms list");
      return this.restClient.ContestList(true).Result.ToDictionary(c => c.Id);
    }

    private void DownloadSourceText(Submission submission, bool isGym, string problemFolder)
    {
      var sourceTextFileName = $"{submission.Id}.{GetSourceTextExt(submission.ProgrammingLanguage)}";

      string filePath = Path.Combine(problemFolder, sourceTextFileName);
      if (File.Exists(filePath))
      {
        Log.Info($"File already exists: {filePath}");
        return;
      }
      var sourceText = this.sourceTextLoader.GetSourceTextAsync(submission, isGym).Result;
      if (sourceText == null)
        return;
      using var streamWriter = new StreamWriter(filePath);
      streamWriter.Write(sourceText);
      Log.Info($"File created: {filePath}");
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

    private IEnumerable<Submission> EnumerateAllUserSubmissions()
    {
      var from = 1;
      var submissions = this.restClient.UserStatus(this.options.Handle, from, 10).Result;
      while (submissions.Any())
      {
        foreach (var submission in submissions.Where(s => s.Verdict == "OK" && s.ContestId != null))
        {
          yield return submission;
        }
        from += submissions.Length;
        submissions = this.restClient.UserStatus(this.options.Handle, from, 10).Result;
      }
    }

    public void Dispose()
    {
      this.restClient.Dispose();
    }

    public Downloader(Options options)
    {
      this.options = options;
      this.destinationFolder = Environment.ExpandEnvironmentVariables(options.Folder);
      this.restClient = new Client(new Throttle(TimeSpan.FromMilliseconds(250)));
      var htmlRequestsThrottle = new Throttle(TimeSpan.FromMilliseconds(1000), true);
      this.sourceTextLoader = new SourceTextLoader(htmlRequestsThrottle, options.Cookie);
      this.statementDownloader = new StatementDownloader(htmlRequestsThrottle, options.WgetExePath);
      this.gyms = new Lazy<IReadOnlyDictionary<int, Contest>>(this.LoadGyms);
    }
  }
}
