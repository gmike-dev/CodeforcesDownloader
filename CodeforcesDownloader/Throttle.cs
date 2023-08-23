using System;
using System.Threading;

namespace CodeforcesDownloader;

internal interface IThrottleFactory
{
  Throttle Create(TimeSpan interval, bool variableTimeout = false);
}

internal sealed class ThrottleFactory : IThrottleFactory
{
  public Throttle Create(TimeSpan interval, bool variableTimeout = false) => new(interval, variableTimeout);
}

public sealed class Throttle
{
  private readonly TimeSpan interval;
  private readonly bool variableTimeout;
  private DateTime lastCall;
  private uint counter;
  private readonly Random random = new();

  public T Do<T>(Func<T> func)
  {
    var i = this.interval;

    if (this.variableTimeout && this.lastCall != default)
    {
      if (this.counter % 10 == 0)
        i *= 10;
      else if (this.counter % 5 == 0)
        i *= 5;
    }

    var now = DateTime.Now;
    if (this.lastCall != default && this.lastCall + i > now)
    {
      var timeout = this.lastCall + i - now;
      timeout += timeout * (2 * this.random.NextDouble() - 1) / 2;
      Thread.Sleep(timeout);
    }
    try
    {
      return func();
    }
    finally
    {
      this.lastCall = DateTime.Now;
      this.counter++;
    }
  }

  public Throttle(TimeSpan interval, bool variableTimeout = false)
  {
    this.interval = interval;
    this.variableTimeout = variableTimeout;
  }
}
