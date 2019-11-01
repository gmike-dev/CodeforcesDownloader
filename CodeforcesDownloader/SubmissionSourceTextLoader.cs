using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using CodeforcesDownloader.REST;
using HtmlAgilityPack;
using NLog;

namespace CodeforcesDownloader
{
  internal sealed class SubmissionSourceTextLoader : IDisposable
  {
    private readonly Throttle throttle;
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

    private readonly HttpClient httpClient;

    public async Task<string> GetSourceTextAsync(Submission submission, bool gym)
    {
      if (submission == null)
        throw new ArgumentNullException(nameof(submission));
      if (submission.ContestId == null)
        throw new ArgumentNullException(nameof(submission.ContestId));

      var contestType = gym ? "gym" : "contest";

      var pageUri = $"https://codeforces.com/{contestType}/{submission.ContestId}/submission/{submission.Id}";
      Log.Trace($"Download {pageUri}");
      var response = await this.throttle.Do(() => this.httpClient.GetAsync(pageUri));
      response.EnsureSuccessStatusCode();

      var page = await response.Content.ReadAsStringAsync();

      var doc = new HtmlDocument();
      doc.LoadHtml(page);

      Log.Trace($"Extract source text for submission {submission.Id}");

      var sourceTextNode = doc.DocumentNode.SelectSingleNode("//pre[@id='program-source-text']");
      if (sourceTextNode != null)
        return HttpUtility.HtmlDecode(sourceTextNode.InnerText);

      Log.Warn($"Source text html element not found! {pageUri}");
      return null;
    }

    public void Dispose()
    {
      this.httpClient.Dispose();
    }

    public SubmissionSourceTextLoader(Throttle throttle, string cookie = null)
    {
      this.throttle = throttle;
      this.httpClient = new HttpClient(new HttpClientHandler
      {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        DefaultProxyCredentials = CredentialCache.DefaultCredentials,
      });

      if (cookie != null)
      {
        this.httpClient.DefaultRequestHeaders.Add("Cookie", cookie);
      }
    }
  }
}
