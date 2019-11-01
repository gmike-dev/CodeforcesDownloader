using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeforcesDownloader.REST;
using CommandLine;
using NLog;

namespace CodeforcesDownloader
{
  static class Program
  {
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

    static void Main(string[] args)
    {
      Log.Trace($"Start a program");
      SubscribeToUnhandledException();
      Log.Trace($"Parse arguments");
      Parser.Default.ParseArguments<Options>(args)
        .WithParsed(Run)
        .WithNotParsed(_ => Environment.Exit(-1));
    }

    private static void Run(Options options)
    {
      using var downloader = new Downloader(options);
      downloader.Run();
    }

    private static void SubscribeToUnhandledException()
    {
      AppDomain.CurrentDomain.UnhandledException += (s, e) =>
      {
        Log.Error(e.ExceptionObject);
        if (e.IsTerminating)
          Environment.Exit(-1);
      };
    }
  }
}
