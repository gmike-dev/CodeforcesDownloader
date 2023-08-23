using System.Text.Json.Serialization;

namespace CodeforcesDownloader.REST.Models;

internal class Problem
{
  /*
  https://codeforces.com/apiHelp/objects#Problem
  contestId 	Целое число. Может отсутствовать. Id соревнования, содержащего задачу.
  problemsetName 	Строка. Может отсутствовать. Короткое имя дополнительного архива, которому принадлежит задача.
  index 	Строка. Обычно буква или буква с цифрой, обозначающие индекс задачи в соревновании.
  name 	Строка. Локализовано.
  type 	Enum: PROGRAMMING, QUESTION.
  points 	Число с плавающей запятой. Может отсутствовать. Максимальное количество баллов за задачу.
  rating 	Целое число. Может отсутствовать. Рейтинг задачи (сложность).
  tags 	Список строк. Теги задачи.
  */

  [JsonPropertyName("contestId")]
  public int? ContestId { get; set; }

  [JsonPropertyName("problemsetName")]
  public string ProblemsetName { get; set; }

  [JsonPropertyName("index")]
  public string Index { get; set; }

  [JsonPropertyName("name")]
  public string Name { get; set; }

  [JsonPropertyName("type")]
  public string Type { get; set; }

  [JsonPropertyName("rating")]
  public int? Rating { get; set; }

  [JsonPropertyName("tags")]
  public string[] Tags { get; set; }
}
