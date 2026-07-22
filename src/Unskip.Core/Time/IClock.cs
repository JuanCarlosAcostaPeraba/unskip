namespace Unskip.Core.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
