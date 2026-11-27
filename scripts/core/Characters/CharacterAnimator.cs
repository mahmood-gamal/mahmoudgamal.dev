namespace Ragna.Core.Characters;

/// <summary>The animations every character sprite has.</summary>
public enum CharacterAnimation
{
    Idle,
    Walk,
    Attack,
}

/// <summary>
/// Engine-agnostic choice of which animation a character sprite plays. Idle and walk follow the
/// character's speed. An attack is a one-shot: it plays to the end, over idle or walk, and then the
/// sprite goes back to idle or walk. The engine node calls <see cref="Update"/> every frame and
/// <see cref="AttackFinished"/> when the attack animation ends.
/// </summary>
public sealed class CharacterAnimator
{
    /// <summary>Default for <see cref="WalkSpeedThreshold"/>, in metres per second.</summary>
    public const float DefaultWalkSpeedThreshold = 0.1f;

    public CharacterAnimator(float walkSpeedThreshold = DefaultWalkSpeedThreshold)
    {
        if (walkSpeedThreshold < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(walkSpeedThreshold), "Can't be negative.");
        }

        WalkSpeedThreshold = walkSpeedThreshold;
    }

    /// <summary>Ground speed above which the character counts as walking.</summary>
    public float WalkSpeedThreshold { get; }

    /// <summary>The animation the sprite should play now.</summary>
    public CharacterAnimation Current { get; private set; } = CharacterAnimation.Idle;

    /// <summary>Whether an attack is playing.</summary>
    public bool IsAttacking { get; private set; }

    /// <summary>
    /// Starts an attack. Calling it during an attack starts the attack again from the first frame.
    /// Always returns <c>true</c>: the engine should restart the attack animation.
    /// </summary>
    public bool Attack()
    {
        IsAttacking = true;
        Current = CharacterAnimation.Attack;
        return true;
    }

    /// <summary>The attack animation reached its last frame.</summary>
    public void AttackFinished() => IsAttacking = false;

    /// <summary>
    /// Picks the animation for a character moving over the ground at <paramref name="groundSpeed"/>
    /// metres per second. Returns the attack while one plays, otherwise walk or idle.
    /// </summary>
    public CharacterAnimation Update(float groundSpeed)
    {
        Current = IsAttacking
            ? CharacterAnimation.Attack
            : groundSpeed > WalkSpeedThreshold ? CharacterAnimation.Walk : CharacterAnimation.Idle;
        return Current;
    }

    /// <summary>The animation's name in the sprite's SpriteFrames: <c>idle</c>, <c>walk</c> or <c>attack</c>.</summary>
    public static string AnimationName(CharacterAnimation animation) => animation switch
    {
        CharacterAnimation.Idle => "idle",
        CharacterAnimation.Walk => "walk",
        CharacterAnimation.Attack => "attack",
        _ => throw new ArgumentOutOfRangeException(nameof(animation), animation, null),
    };

    /// <summary>Whether the animation repeats. Idle and walk loop; an attack plays once.</summary>
    public static bool Loops(CharacterAnimation animation) => animation != CharacterAnimation.Attack;
}
