using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeforcesDownloader.REST;
using CodeforcesDownloader.REST.Models;
using Microsoft.Extensions.Logging;

namespace CodeforcesDownloader;

internal interface IDownloader
{
  void Run();
}

internal sealed class Downloader : IDownloader
{
  private readonly ILogger<Downloader> logger;
  private readonly Options options;
  private readonly IRestClient restClient;
  private readonly string destinationFolder;
  private readonly ISourceTextLoader sourceTextLoader;
  private readonly IStatementDownloader statementDownloader;
  private readonly Lazy<IReadOnlyDictionary<int, Contest>> gyms;

  private IReadOnlyDictionary<int, Contest> Gyms => this.gyms.Value;

  public void Run()
  {
    this.logger.LogTrace("Download data to {destinationFolder}", this.destinationFolder);

    foreach (var submission in this.EnumerateAllUserSubmissions())
      this.ProcessSubmission(submission);

    this.logger.LogInformation("Done. Check {destinationFolder}", this.destinationFolder);
  }

  private void ProcessSubmission(Submission submission)
  {
    var isGym = this.Gyms.ContainsKey(submission.ContestId.Value);
    if (isGym && this.options.Cookie == default)
    {
      this.logger.LogWarning(
        "Cannot process submission for gym {gymId}: specify cookie for authorized requests support",
        submission.ContestId);
      return;
    }

    var problemFolder = this.GetOrCreateDirectory(Path.Join(this.destinationFolder,
      Utils.NormalizeFileName(this.options.Handle),
      isGym ? "gyms" : "contests",
      submission.ContestId.ToString(),
      Utils.NormalizeFileName($"{submission.Problem.Index}. {submission.Problem.Name}"))).FullName;

    if (!isGym)
      this.statementDownloader.DownloadStatement(submission.Problem, isGym, problemFolder);

    this.DownloadSourceText(submission, isGym, problemFolder);
  }

  private DirectoryInfo GetOrCreateDirectory(string targetPath)
  {
    var downloadFolder = new DirectoryInfo(Environment.ExpandEnvironmentVariables(targetPath));
    if (downloadFolder.Exists)
      return downloadFolder;

    this.logger.LogTrace("Create {downloadFolder}", downloadFolder.FullName);
    downloadFolder.Create();
    return downloadFolder;
  }

  private Dictionary<int, Contest> LoadGyms()
  {
    this.logger.LogTrace("Load gyms list");
    return this.restClient.ContestList(true).Result.ToDictionary(c => c.Id);
  }

  private void DownloadSourceText(Submission submission, bool isGym, string problemFolder)
  {
    var sourceTextFileName = $"{submission.Id}.{GetSourceTextExt(submission.ProgrammingLanguage)}";

    var filePath = Path.Combine(problemFolder, sourceTextFileName);
    if (File.Exists(filePath))
    {
      this.logger.LogInformation("{file} already exists", filePath);
      return;
    }
    var sourceText = this.sourceTextLoader.GetSourceTextAsync(submission, isGym).Result;
    if (sourceText == null)
      return;
    using var streamWriter = new StreamWriter(filePath);
    streamWriter.Write(sourceText);
    this.logger.LogInformation("{file} created", filePath);
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

  public Downloader(Options options, ILogger<Downloader> logger, ISourceTextLoader sourceTextLoader,
    IStatementDownloader statementDownloader, IRestClient restClient)
  {
    this.logger = logger;
    this.options = options;
    this.destinationFolder = Environment.ExpandEnvironmentVariables(options.Folder);
    this.restClient = restClient;
    this.sourceTextLoader = sourceTextLoader;
    this.statementDownloader = statementDownloader;
    this.gyms = new Lazy<IReadOnlyDictionary<int, Contest>>(this.LoadGyms);
  }
}
