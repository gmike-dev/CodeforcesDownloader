using System.Text.Json.Serialization;

namespace CodeforcesDownloader.REST;

public class Response<T>
{
  [JsonPropertyName("status")]
  public string Status { get; set; }

  [JsonPropertyName("result")]
  public T Result { get; set; }
}