using LiveCaptionsRain.Core.Physics;
using LiveCaptionsRain.Core.Windows;

namespace LiveCaptionsRain.Tests;

public sealed class WordPhysicsWorldTests
{
    [Fact]
    public void Step_moves_word_down_and_floor_prevents_falling_through()
    {
        using var world = new WordPhysicsWorld(new ScreenRect(0, 0, 400, 300));

        var id = world.AddWord("hello", new PhysicsRect(170, 10, 60, 24));
        for (var i = 0; i < 240; i++)
        {
            world.Step(1d / 60d, randomWindPixelsPerSecond: 0);
        }

        var word = Assert.Single(world.Snapshot().Where(item => item.Id == id));
        Assert.True(word.Bounds.Top > 10);
        Assert.True(word.Bounds.Bottom <= 301);
    }

    [Fact]
    public void Overflow_cleanup_marks_oldest_words_for_fade_before_removal()
    {
        using var world = new WordPhysicsWorld(new ScreenRect(0, 0, 400, 300));
        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < 5; i++)
        {
            world.AddWord($"w{i}", new PhysicsRect(20 + i * 20, 10, 20, 20), createdAt: now.AddSeconds(i));
        }

        world.StartOverflowCleanup(maxActiveWords: 3, fadeDuration: TimeSpan.FromSeconds(2), now);

        var snapshots = world.Snapshot().OrderBy(item => item.Text).ToArray();
        Assert.Equal(5, snapshots.Length);
        Assert.True(snapshots.Single(item => item.Text == "w0").IsDeleting);
        Assert.True(snapshots.Single(item => item.Text == "w1").IsDeleting);
        Assert.False(snapshots.Single(item => item.Text == "w2").IsDeleting);

        world.ApplyCleanup(maxActiveWords: 3, lifetime: TimeSpan.FromHours(1), now: now.AddSeconds(1));
        Assert.Equal(5, world.Snapshot().Count);

        world.ApplyCleanup(maxActiveWords: 3, lifetime: TimeSpan.FromHours(1), now: now.AddSeconds(3));
        Assert.Equal(["w2", "w3", "w4"], world.Snapshot().Select(item => item.Text).Order().ToArray());
    }

    [Fact]
    public void Step_applies_wind_per_word_zone()
    {
        using var world = new WordPhysicsWorld(new ScreenRect(0, 0, 500, 500));

        var leftId = world.AddWord("left", new PhysicsRect(80, 20, 40, 20));
        var rightId = world.AddWord("right", new PhysicsRect(380, 20, 40, 20));

        for (var i = 0; i < 90; i++)
        {
            world.Step(
                1d / 60d,
                word => word.Bounds.CenterX < 250
                    ? new WindVector(-220, -44)
                    : new WindVector(220, -44));
        }

        var words = world.Snapshot().ToDictionary(item => item.Id);
        Assert.True(words[leftId].Bounds.CenterX < 100);
        Assert.True(words[rightId].Bounds.CenterX > 400);
    }

    [Fact]
    public void Window_platforms_respect_rect_height_for_vertical_side_walls()
    {
        using var world = new WordPhysicsWorld(new ScreenRect(0, 0, 600, 500));
        world.SetWindowPlatforms([new PhysicsRect(220, 0, 36, 500)]);
        var id = world.AddWord("push", new PhysicsRect(270, 180, 30, 24));

        for (var i = 0; i < 120; i++)
        {
            world.Step(1d / 60d, _ => new WindVector(-900, 0));
        }

        var word = Assert.Single(world.Snapshot().Where(item => item.Id == id));
        Assert.True(word.Bounds.Left >= 250);
    }
}
