using System.Diagnostics;
using Xunit;

namespace XoGame.Tests;

internal static class TestHelpers
{
    public static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 3000)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.ElapsedMilliseconds > timeoutMs)
                Assert.Fail("Timed out waiting for a condition.");
            await Task.Delay(10);
        }
    }
}