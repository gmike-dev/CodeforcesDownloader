using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CodeforcesDownloader.REST;
using HtmlAgilityPack;
using NLog;

namespace CodeforcesDownloader;

internal sealed class StatementDownloader
{
  private readonly Throttle throttle;
  private readonly string wgetExePath;
  private readonly string lang;
  private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
  private const string StatementFileName = "statement.html";
  private static readonly string ContentDirectoryName = $"{StatementFileName}_files";

  public void DownloadStatement(Problem problem, bool gym, string problemDirectory)
  {
    if (problem == null)
      throw new ArgumentNullException(nameof(problem));
    if (problem.ContestId == null)
      throw new ArgumentNullException(nameof(problem.ContestId));
    if (gym)
      throw new NotSupportedException();

    var statementFileName = Path.Join(problemDirectory, "statement.html");
    if (File.Exists(statementFileName))
    {
      Log.Trace($"File exists: {statementFileName}");
      return;
    }

    var pageUri = $"https://codeforces.com/contest/{problem.ContestId}/problem/{problem.Index}?lang={this.lang}";
    if (!this.throttle.Do(() => this.DownloadUseWget(pageUri, problemDirectory)))
      return;

    var contentDirectory = Path.Join(problemDirectory, ContentDirectoryName);
    var statementHtmlFile = Directory.EnumerateFiles(contentDirectory, $"{problem.Index}*.html").FirstOrDefault();
    if (statementHtmlFile == null)
    {
      Log.Error($"Downloaded statement html not found");
      Directory.Delete(contentDirectory, true);
      return;
    }
    File.Move(statementHtmlFile, statementFileName);

    var doc = new HtmlDocument();
    doc.Load(statementFileName);
    TrimStatementHtmlContent(doc);
    FixHtmlContentReferences(doc, contentDirectory);
    doc.Save(statementFileName);
  }

  private static void FixHtmlContentReferences(HtmlDocument doc, string contentDirectory)
  {
    var head = doc.DocumentNode.ChildNodes["html"];
    var innerHtml = head.InnerHtml;
    foreach (var contentFile in Directory.EnumerateFiles(contentDirectory))
    {
      var fileName = Path.GetFileName(contentFile);
      innerHtml = innerHtml.Replace(fileName, $"{ContentDirectoryName}/{fileName}");
    }
    head.InnerHtml = innerHtml;
  }

  private static void TrimStatementHtmlContent(HtmlDocument doc)
  {
    var problemStatementNode = doc.DocumentNode.SelectSingleNode("//div[@class='problem-statement']");
    if (problemStatementNode == null)
    {
      Log.Warn($"Cannot trim statement html");
      return;
    }

    problemStatementNode.Remove();
    var body = doc.DocumentNode.ChildNodes["html"].ChildNodes["body"];
    body.RemoveAllChildren();
    body.AppendChild(problemStatementNode);
  }

  private bool DownloadUseWget(string pageUrl, string directory)
  {
    var si = new ProcessStartInfo
    {
      FileName = this.wgetExePath,
      Arguments =
        $"-q -p -k -H --restrict-file-names=windows -E -nv -e robots=off -nd -P {ContentDirectoryName} -U \"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:70.0) Gecko/20100101 Firefox/70.0\" {pageUrl}",
      WorkingDirectory = directory,
      CreateNoWindow = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      StandardErrorEncoding = Encoding.UTF8
    };
    using var process = Process.Start(si);
    if (process == null)
    {
      Log.Error($"Cannot start process {si.FileName} {si.Arguments}");
      return false;
    }
    var error = process.StandardError.ReadToEnd();
    process.WaitForExit();
    if (!string.IsNullOrEmpty(error))
    {
      Log.Error($"Command \"{si.FileName} {si.Arguments}\" executed with error (exit code {process.ExitCode}): " +
        error);
      return false;
    }
    Log.Trace($"Command \"{si.FileName} {si.Arguments}\" executed successfully");
    return true;
  }

  public StatementDownloader(Throttle throttle, string wgetExePath, string lang = "ru")
  {
    this.throttle = throttle;
    this.wgetExePath = wgetExePath;
    this.lang = lang;
  }
}