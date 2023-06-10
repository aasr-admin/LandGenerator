using System;

namespace RandomExtensions
{
  public static class RandomExtensions
  {
    public static double NextDoubleRange(this Random random, double min, double max)
    {
      return random.NextDouble() * (max - min) + min;
    }
  }
}
