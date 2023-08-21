using System.Text.Json.Serialization;

namespace CodeforcesDownloader.REST;

internal class Submission
{
  /*
  https://codeforces.com/apiHelp/objects#Submission
  id 	Целое число.
  contestId 	Целое число. Может отсутствовать.
  creationTimeSeconds 	Целое число. Время создания попытки в формате unix.
  relativeTimeSeconds 	Целое число. Количество секунд, прошедших с начала контеста (или виртуального начала для виртуальных участников), до этой попытки.
  problem 	Объект Problem.
  author 	Объект Party.
  programmingLanguage 	Строка.
  verdict 	Enum: FAILED, OK, PARTIAL, COMPILATION_ERROR, RUNTIME_ERROR, WRONG_ANSWER, PRESENTATION_ERROR, TIME_LIMIT_EXCEEDED, MEMORY_LIMIT_EXCEEDED, IDLENESS_LIMIT_EXCEEDED, SECURITY_VIOLATED, CRASHED, INPUT_PREPARATION_CRASHED, CHALLENGED, SKIPPED, TESTING, REJECTED. Может отсутствовать.
  testset 	Enum: SAMPLES, PRETESTS, TESTS, CHALLENGES, TESTS1, ..., TESTS10. Тестсет, на котором было протестировано решение.
  passedTestCount 	Целое число. Количество пройденных тестов.
  timeConsumedMillis 	Целое число. Максимальное время в миллисекундах, использованное решением для одного теста.
  memoryConsumedBytes 	Целое число. Максимальный объём памяти в байтах, использованный решением для одного теста.
  */

  [JsonPropertyName("id")]
  public int Id { get; set; }

  [JsonPropertyName("contestId")]
  public int? ContestId { get; set; }

  [JsonPropertyName("creationTimeSeconds")]
  public long CreationTimeSeconds { get; set; }

  [JsonPropertyName("relativeTimeSeconds")]
  public long RelativeTimeSeconds { get; set; }

  [JsonPropertyName("problem")]
  public Problem Problem { get; set; }

  [JsonPropertyName("programmingLanguage")]
  public string ProgrammingLanguage { get; set; }

  [JsonPropertyName("verdict")]
  public string Verdict { get; set; }
}