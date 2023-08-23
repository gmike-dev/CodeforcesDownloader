using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using CodeforcesDownloader.REST.Models;
using Microsoft.Extensions.Logging;

namespace CodeforcesDownloader.REST;

internal interface IRestClient
{
  Task<Submission[]> UserStatus(string handle, int from, int count);
  Task<Contest[]> ContestList(bool gym);
}

internal sealed class RestClient : IDisposable, IRestClient
{
  private readonly Throttle throttle;
  private readonly string lang;
  private readonly HttpClient httpClient;
  private readonly ILogger<RestClient> logger;

  public async Task<Submission[]> UserStatus(string handle, int from, int count)
  {
    if (handle == null)
      throw new ArgumentNullException(nameof(handle));
    if (@from <= 0)
      throw new ArgumentOutOfRangeException(nameof(@from));
    if (count <= 0)
      throw new ArgumentOutOfRangeException(nameof(count));

    var requestUri = $"https://codeforces.com/api/user.status?handle={handle}&from={from}&count={count}&lang={this.lang}";
    var responseMessage = await this.GetResponse(requestUri, 10);
    var responseString = await responseMessage.Content.ReadAsStringAsync();
    var response = JsonSerializer.Deserialize<Response<Submission[]>>(responseString);
    return response.Result;
  }

  public async Task<Contest[]> ContestList(bool gym)
  {
    var requestUri = $"https://codeforces.com/api/contest.list?gym={gym}&lang={this.lang}";
    var responseMessage = await this.GetResponse(requestUri, 10);
    var responseString = await responseMessage.Content.ReadAsStringAsync();
    var response = JsonSerializer.Deserialize<Response<Contest[]>>(responseString);
    return response.Result;
  }
  
  private async Task<HttpResponseMessage> GetResponse(string requestUri, int maxAttemptsCount)
  {
    var response = await this.throttle.Do(() => this.httpClient.GetAsync(requestUri));
    for (var i = 1; i < maxAttemptsCount && !response.IsSuccessStatusCode; i++)
    {
      this.logger.LogInformation("Failed to get {requestUri}. Response: {statusCode}. Retry #{i}", requestUri, response.StatusCode, i);
      response = await this.throttle.Do(() => this.httpClient.GetAsync(requestUri));
    }
    return response;
  }

  public RestClient(IThrottleFactory throttleFactory, ILogger<RestClient> logger)
  {
    this.logger = logger;
    this.throttle = throttleFactory.Create(TimeSpan.FromMilliseconds(500));
    this.lang = "ru";
    this.httpClient = new HttpClient(new HttpClientHandler
    {
      AutomaticDecompression = DecompressionMethods.All,
      DefaultProxyCredentials = CredentialCache.DefaultCredentials
    });
    this.httpClient.DefaultRequestHeaders.Clear();
    this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
  }

  public void Dispose()
  {
    this.httpClient.Dispose();
  }
}
