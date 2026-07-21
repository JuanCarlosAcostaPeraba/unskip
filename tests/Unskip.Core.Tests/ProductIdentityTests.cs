using Unskip.Core;

namespace Unskip.Core.Tests;

public sealed class ProductIdentityTests
{
    [Fact]
    public void PublicIdentityStatesUnofficialStatus()
    {
        Assert.Equal("Unskip", ProductIdentity.Name);
        Assert.Contains("unofficial", ProductIdentity.AffiliationNotice, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Microsoft", ProductIdentity.AffiliationNotice, StringComparison.Ordinal);
    }
}
