namespace Unskip.Infrastructure.Windows.Tests;

public sealed class WindowsMsgSenderOptionsTests
{
    [Fact]
    public void DefaultTimeoutIsTenSeconds()
    {
        var options = new WindowsMsgSenderOptions();

        Assert.Equal(TimeSpan.FromSeconds(10), options.Timeout);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(121)]
    public void InvalidTimeoutIsRejected(int seconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new WindowsMsgSenderOptions(TimeSpan.FromSeconds(seconds)));
    }
}
