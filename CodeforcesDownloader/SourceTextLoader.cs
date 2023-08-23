using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using CodeforcesDownloader.REST.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace CodeforcesDownloader;

internal interface ISourceTextLoader
{
  Task<string> GetSourceTextAsync(Submission submission, bool gym);
}

internal sealed class SourceTextLoader : ISourceTextLoader, IDisposable
{
  private readonly ILogger<SourceTextLoader> logger;
  private readonly Throttle throttle;
  private readonly HttpClient httpClient;

  public async Task<string> GetSourceTextAsync(Submission submission, bool gym)
  {
    if (submission == null)
      throw new ArgumentNullException(nameof(submission));
    if (submission.ContestId == null)
      throw new ArgumentNullException(nameof(submission.ContestId));

    var contestType = gym ? "gym" : "contest";

    var pageUri = $"https://codeforces.com/{contestType}/{submission.ContestId}/submission/{submission.Id}";
    this.logger.LogTrace("Download {pageUri}", pageUri);
    var response = await this.GetResponse(pageUri, 5);
    response.EnsureSuccessStatusCode();

    var page = await response.Content.ReadAsStringAsync();

    var doc = new HtmlDocument();
    doc.LoadHtml(page);

    this.logger.LogTrace("Extract source text for {submissionId}", submission.Id);

    var sourceTextNode = doc.DocumentNode.SelectSingleNode("//pre[@id='program-source-text']");
    if (sourceTextNode != null)
      return HttpUtility.HtmlDecode(sourceTextNode.InnerText);

    this.logger.LogWarning("Source text html element not found in {pageUri}", pageUri);
    return null;
  }

  private async Task<HttpResponseMessage> GetResponse(string pageUri, int maxAttemptsCount)
  {
    var response = await this.throttle.Do(() => this.httpClient.GetAsync(pageUri));
    for (var i = 1; i < maxAttemptsCount && !response.IsSuccessStatusCode; i++)
    {
      this.logger.LogInformation("Failed to get {pageUri}. Response: {statusCode}. Retry #{i}", pageUri, response.StatusCode, i);
      response = await this.throttle.Do(() => this.httpClient.GetAsync(pageUri));
    }
    return response;
  }

  public void Dispose()
  {
    this.httpClient.Dispose();
  }

  public SourceTextLoader(IThrottleFactory throttleFactory, ILogger<SourceTextLoader> logger, Options options)
  {
    this.throttle = throttleFactory.Create(TimeSpan.FromMilliseconds(1000), true);
    this.logger = logger;
    this.httpClient = new HttpClient(new HttpClientHandler
    {
      AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
      DefaultProxyCredentials = CredentialCache.DefaultCredentials,
    });

    if (options.Cookie != null)
      this.httpClient.DefaultRequestHeaders.Add("Cookie", options.Cookie);
  }
}
