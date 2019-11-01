using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace CodeforcesDownloader.REST
{
  internal class Client : IDisposable
  {
    private readonly Throttle throttle;
    private readonly string lang;
    private readonly HttpClient httpClient;

    public async Task<Submission[]> UserStatus(string handle, int from, int count)
    {
      if (handle == null)
        throw new ArgumentNullException(nameof(handle));
      if (@from <= 0)
        throw new ArgumentOutOfRangeException(nameof(@from));
      if (count <= 0)
        throw new ArgumentOutOfRangeException(nameof(count));

      var requestUri =
        $"https://codeforces.com/api/user.status?handle={handle}&from={from}&count={count}&lang={this.lang}";
      var responseString = await this.throttle.Do(() => this.httpClient.GetStringAsync(requestUri));
      var response = JsonSerializer.Deserialize<Response<Submission[]>>(responseString);
      return response.Result;
    }

    public async Task<Contest[]> ContestList(bool gym)
    {
      var requestUri = $"https://codeforces.com/api/contest.list?gym={gym}&lang={this.lang}";
      var responseString = await this.throttle.Do(() => this.httpClient.GetStringAsync(requestUri));
      var response = JsonSerializer.Deserialize<Response<Contest[]>>(responseString);
      return response.Result;
    }

    public Client(Throttle throttle, string lang = "ru")
    {
      this.throttle = throttle;
      this.lang = lang;
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
}
