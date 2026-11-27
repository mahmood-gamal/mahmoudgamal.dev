using System.Numerics;
using Ragna.Core.Movement;

namespace Ragna.Core.Tests.Movement;

public sealed class ClickToMoveTests
{
    private static readonly Vector3 A = new(1f, 0f, 1f);
    private static readonly Vector3 B = new(5f, 0f, 1f);

    [Fact]
    public void Starts_standing_still()
    {
        var mover = new ClickToMove();

        Assert.Null(mover.Destination);
        Assert.False(mover.IsHolding);
    }

    [Fact]
    public void Click_sets_destination_and_asks_for_a_path()
    {
        var mover = new ClickToMove();

        Assert.True(mover.Press(A));
        Assert.Equal(A, mover.Destination);
        Assert.True(mover.IsHolding);
    }

    [Fact]
    public void Every_click_asks_for_a_path_even_on_the_same_point()
    {
        var mover = new ClickToMove();
        mover.Press(A);
        mover.Release();

        Assert.True(mover.Press(A));
    }

    [Fact]
    public void Holding_retargets_to_the_cursor_after_the_repath_interval()
    {
        var mover = new ClickToMove(new ClickToMoveSettings { RepathInterval = 0.1f });
        mover.Press(A);

        Assert.False(mover.Hold(B, 0.05f));
        Assert.Equal(A, mover.Destination);

        Assert.True(mover.Hold(B, 0.05f));
        Assert.Equal(B, mover.Destination);
    }

    [Fact]
    public void Holding_still_does_not_ask_for_new_paths()
    {
        var mover = new ClickToMove(new ClickToMoveSettings { RetargetDistance = 0.25f });
        mover.Press(A);

        Assert.False(mover.Hold(A + new Vector3(0.1f, 0f, 0f), 1f));
        Assert.Equal(A, mover.Destination);
    }

    [Fact]
    public void Holding_off_the_ground_keeps_the_last_destination()
    {
        var mover = new ClickToMove();
        mover.Press(A);

        Assert.False(mover.Hold(null, 1f));
        Assert.Equal(A, mover.Destination);
    }

    [Fact]
    public void After_release_the_cursor_is_ignored_but_the_walk_goes_on()
    {
        var mover = new ClickToMove();
        mover.Press(A);
        mover.Release();

        Assert.False(mover.Hold(B, 1f));
        Assert.Equal(A, mover.Destination);
        Assert.False(mover.IsHolding);
    }

    [Fact]
    public void Arriving_clears_the_destination_once_released()
    {
        var mover = new ClickToMove();
        mover.Press(A);

        mover.Arrive();
        Assert.Equal(A, mover.Destination);

        mover.Release();
        mover.Arrive();
        Assert.Null(mover.Destination);
    }

    [Fact]
    public void Velocity_points_at_the_waypoint_on_the_ground_plane()
    {
        var velocity = ClickToMove.HorizontalVelocity(Vector3.Zero, new Vector3(3f, 2f, 4f), 5f, 1f / 60f);

        Assert.Equal(0f, velocity.Y);
        Assert.Equal(5f, velocity.Length(), 4);
        Assert.Equal(0.6f, velocity.X / 5f, 4);
        Assert.Equal(0.8f, velocity.Z / 5f, 4);
    }

    [Fact]
    public void Velocity_slows_down_so_it_never_overshoots()
    {
        var velocity = ClickToMove.HorizontalVelocity(Vector3.Zero, new Vector3(0.05f, 0f, 0f), 5f, 0.1f);

        Assert.Equal(0.5f, velocity.X, 4);
    }

    [Fact]
    public void Velocity_is_zero_on_the_waypoint_or_straight_below_it()
    {
        Assert.Equal(Vector3.Zero, ClickToMove.HorizontalVelocity(A, A, 5f, 0.1f));
        Assert.Equal(Vector3.Zero, ClickToMove.HorizontalVelocity(A, A + Vector3.UnitY, 5f, 0.1f));
    }

    [Fact]
    public void Rejects_bad_settings()
    {
        Assert.Throws<ArgumentException>(() => new ClickToMove(new ClickToMoveSettings { MoveSpeed = 0f }));
        Assert.Throws<ArgumentException>(() => new ClickToMove(new ClickToMoveSettings { RepathInterval = -1f }));
    }
}
