using System;
using CodeforcesDownloader.REST;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeforcesDownloader;

public static class Program
{
  public static void Main(string[] args)
  {
    SubscribeToUnhandledException();
    Parser.Default.ParseArguments<Options>(args)
      .WithParsed(Run)
      .WithNotParsed(_ => Environment.Exit(-1));
  }

  private static void Run(Options commandLineOptions)
  {
    using var serviceProvider = new ServiceCollection()
      .AddLogging(loggingBuilder =>
      {
        loggingBuilder
          .AddSimpleConsole(options =>
          {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
          })
          .SetMinimumLevel(LogLevel.Trace);
      })
      .AddSingleton(commandLineOptions)
      .AddSingleton<IRestClient, RestClient>()
      .AddSingleton<IThrottleFactory, ThrottleFactory>()
      .AddTransient<ISourceTextLoader, SourceTextLoader>()
      .AddTransient<IStatementDownloader, StatementDownloader>()
      .AddTransient<IDownloader, Downloader>()
      .BuildServiceProvider();
    
    // var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    // var logger = loggerFactory.CreateLogger("test");
    // logger.LogTrace("Trace line");
    // logger.LogInformation("Information line");
    // logger.LogWarning("Warning line");
    // logger.LogError("Error line");
    // Console.Error.WriteLine("Error in error stream");

    var downloader = serviceProvider.GetRequiredService<IDownloader>();
    downloader.Run();
  }

  private static void SubscribeToUnhandledException()
  {
    AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    {
      Console.Error.WriteLine(e.ExceptionObject);
      if (e.IsTerminating)
        Environment.Exit(-1);
    };
  }
}
