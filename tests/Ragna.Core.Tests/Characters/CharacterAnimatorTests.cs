using Ragna.Core.Characters;

namespace Ragna.Core.Tests.Characters;

public sealed class CharacterAnimatorTests
{
    [Fact]
    public void Starts_idle()
    {
        var animator = new CharacterAnimator();

        Assert.Equal(CharacterAnimation.Idle, animator.Current);
        Assert.False(animator.IsAttacking);
    }

    [Theory]
    [InlineData(0f, CharacterAnimation.Idle)]
    [InlineData(0.1f, CharacterAnimation.Idle)]
    [InlineData(0.11f, CharacterAnimation.Walk)]
    [InlineData(5f, CharacterAnimation.Walk)]
    public void Speed_picks_idle_or_walk(float speed, CharacterAnimation expected)
    {
        var animator = new CharacterAnimator();

        Assert.Equal(expected, animator.Update(speed));
        Assert.Equal(expected, animator.Current);
    }

    [Fact]
    public void Stopping_goes_back_to_idle()
    {
        var animator = new CharacterAnimator();
        animator.Update(5f);

        Assert.Equal(CharacterAnimation.Idle, animator.Update(0f));
    }

    [Fact]
    public void Attack_plays_over_walk_until_it_finishes()
    {
        var animator = new CharacterAnimator();
        animator.Update(5f);

        Assert.True(animator.Attack());
        Assert.Equal(CharacterAnimation.Attack, animator.Current);
        Assert.Equal(CharacterAnimation.Attack, animator.Update(5f));
        Assert.Equal(CharacterAnimation.Attack, animator.Update(0f));

        animator.AttackFinished();

        Assert.False(animator.IsAttacking);
        Assert.Equal(CharacterAnimation.Walk, animator.Update(5f));
    }

    [Fact]
    public void Attack_returns_to_idle_when_standing()
    {
        var animator = new CharacterAnimator();
        animator.Attack();
        animator.AttackFinished();

        Assert.Equal(CharacterAnimation.Idle, animator.Update(0f));
    }

    [Fact]
    public void Attacking_again_restarts_the_attack()
    {
        var animator = new CharacterAnimator();
        animator.Attack();

        Assert.True(animator.Attack());
        Assert.True(animator.IsAttacking);
    }

    [Fact]
    public void Custom_walk_threshold()
    {
        var animator = new CharacterAnimator(walkSpeedThreshold: 1f);

        Assert.Equal(CharacterAnimation.Idle, animator.Update(0.5f));
        Assert.Equal(CharacterAnimation.Walk, animator.Update(1.5f));
    }

    [Fact]
    public void Negative_walk_threshold_throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterAnimator(-1f));

    [Theory]
    [InlineData(CharacterAnimation.Idle, "idle", true)]
    [InlineData(CharacterAnimation.Walk, "walk", true)]
    [InlineData(CharacterAnimation.Attack, "attack", false)]
    public void Animation_names_and_looping(CharacterAnimation animation, string name, bool loops)
    {
        Assert.Equal(name, CharacterAnimator.AnimationName(animation));
        Assert.Equal(loops, CharacterAnimator.Loops(animation));
    }
}
