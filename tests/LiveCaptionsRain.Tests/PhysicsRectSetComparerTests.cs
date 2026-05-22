using LiveCaptionsRain.Core.Physics;

namespace LiveCaptionsRain.Tests;

public sealed class PhysicsRectSetComparerTests
{
    [Fact]
    public void AreEquivalent_ignores_order_and_subpixel_jitter()
    {
        var current = new[]
        {
            new PhysicsRect(100.1, 200.1, 300, 18),
            new PhysicsRect(500, 200, 140, 18)
        };
        var next = new[]
        {
            new PhysicsRect(500.2, 199.9, 140, 18),
            new PhysicsRect(99.9, 200.2, 300, 18)
        };

        Assert.True(PhysicsRectSetComparer.AreEquivalent(current, next, tolerance: 0.5));
    }

    [Fact]
    public void AreEquivalent_detects_real_platform_movement()
    {
        var current = new[] { new PhysicsRect(100, 200, 300, 18) };
        var next = new[] { new PhysicsRect(100, 208, 300, 18) };

        Assert.False(PhysicsRectSetComparer.AreEquivalent(current, next, tolerance: 0.5));
    }

    [Fact]
    public void AreEquivalent_detects_added_or_removed_platforms()
    {
        var current = new[] { new PhysicsRect(100, 200, 300, 18) };
        var next = Array.Empty<PhysicsRect>();

        Assert.False(PhysicsRectSetComparer.AreEquivalent(current, next, tolerance: 0.5));
    }
}
