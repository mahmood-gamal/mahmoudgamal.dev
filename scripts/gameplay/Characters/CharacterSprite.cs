using Godot;
using Ragna.Core.Characters;

namespace Ragna.Gameplay.Characters;

/// <summary>
/// A character drawn as a Ragnarok-style sprite: an <see cref="AnimatedSprite3D"/> that turns around
/// its Y axis to face the camera but stays upright. It plays <c>idle</c> or <c>walk</c> from the
/// speed of <see cref="Body"/>, and <c>attack</c> once when <see cref="PlayAttack"/> is called.
/// The rules live in <see cref="CharacterAnimator"/>. See docs/characters.md.
/// </summary>
public partial class CharacterSprite : AnimatedSprite3D
{
    private CharacterAnimator _animator = new();

    /// <summary>The body whose velocity picks idle or walk. Defaults to the nearest <see cref="CharacterBody3D"/> ancestor.</summary>
    [Export]
    public CharacterBody3D? Body { get; set; }

    /// <summary>Ground speed above which the sprite plays <c>walk</c>.</summary>
    [Export(PropertyHint.Range, "0,2,0.01,suffix:m/s")]
    public float WalkSpeedThreshold { get; set; } = CharacterAnimator.DefaultWalkSpeedThreshold;

    /// <summary>The animation the sprite plays now.</summary>
    public CharacterAnimation CurrentAnimation => _animator.Current;

    public override void _Ready()
    {
        _animator = new CharacterAnimator(WalkSpeedThreshold);
        Body ??= FindBody(GetParent());

        // Upright billboard: the sprite turns to the camera around Y only, so it never leans back
        // under the 45° camera.
        Billboard = BaseMaterial3D.BillboardModeEnum.FixedY;
        AnimationFinished += OnAnimationFinished;
        Show(_animator.Current, restart: true);
    }

    public override void _Process(double delta)
    {
        var velocity = Body?.Velocity ?? Vector3.Zero;
        var groundSpeed = new Vector2(velocity.X, velocity.Z).Length();
        Show(_animator.Update(groundSpeed), restart: false);
    }

    /// <summary>Plays the attack animation once, from its first frame, then goes back to idle or walk.</summary>
    public void PlayAttack()
    {
        _animator.Attack();
        Show(CharacterAnimation.Attack, restart: true);
    }

    private void Show(CharacterAnimation animation, bool restart)
    {
        var name = CharacterAnimator.AnimationName(animation);
        if (SpriteFrames is null || !SpriteFrames.HasAnimation(name))
        {
            return;
        }

        if (restart || Animation != name || !IsPlaying())
        {
            if (restart)
            {
                Stop();
            }

            Play(name);
        }
    }

    private void OnAnimationFinished()
    {
        if (Animation == CharacterAnimator.AnimationName(CharacterAnimation.Attack))
        {
            _animator.AttackFinished();
        }
    }

    private static CharacterBody3D? FindBody(Node? node)
    {
        for (; node is not null; node = node.GetParent())
        {
            if (node is CharacterBody3D body)
            {
                return body;
            }
        }

        return null;
    }
}
