using Godot;
using Ragna.Core.Characters;
using NVector2 = System.Numerics.Vector2;

namespace Ragna.Gameplay.Characters;

/// <summary>
/// A character drawn as a Ragnarok-style sprite: an <see cref="AnimatedSprite3D"/> that turns around
/// its Y axis to face the camera but stays upright. It plays <c>idle</c> or <c>walk</c> from the
/// speed of <see cref="Body"/>, and <c>attack</c> once when <see cref="PlayAttack"/> is called.
/// Each animation can have eight directional versions (<c>walk_s</c>, <c>walk_sw</c>, ...); the
/// sprite picks one every frame from the angle between the character's facing and the active camera,
/// so it stays right while the camera turns with Q/E. The rules live in <see cref="CharacterAnimator"/>
/// and <see cref="FacingDirections"/>. See docs/characters.md.
/// </summary>
public partial class CharacterSprite : AnimatedSprite3D
{
    private CharacterAnimator _animator = new();
    private CharacterAnimation _shown = CharacterAnimation.Idle;

    // Ground-plane (X, Z) facing. Starts facing +Z, towards the default camera.
    private Vector2 _facing = new(0f, 1f);

    /// <summary>The body whose velocity picks idle or walk. Defaults to the nearest <see cref="CharacterBody3D"/> ancestor.</summary>
    [Export]
    public CharacterBody3D? Body { get; set; }

    /// <summary>Ground speed above which the sprite plays <c>walk</c> and turns to face where it moves.</summary>
    [Export(PropertyHint.Range, "0,2,0.01,suffix:m/s")]
    public float WalkSpeedThreshold { get; set; } = CharacterAnimator.DefaultWalkSpeedThreshold;

    /// <summary>The animation the sprite plays now.</summary>
    public CharacterAnimation CurrentAnimation => _animator.Current;

    /// <summary>The direction the sprite is drawn facing, relative to the camera.</summary>
    public FacingDirection Direction { get; private set; } = FacingDirection.South;

    /// <summary>The way the character faces in the world, on the ground plane (X, Z). Never zero.</summary>
    public Vector2 Facing => _facing;

    public override void _Ready()
    {
        _animator = new CharacterAnimator(WalkSpeedThreshold);
        Body ??= FindBody(GetParent());

        // Upright billboard: the sprite turns to the camera around Y only, so it never leans back
        // under the 45° camera.
        Billboard = BaseMaterial3D.BillboardModeEnum.FixedY;
        AnimationFinished += OnAnimationFinished;
        UpdateDirection();
        Show(_animator.Current, restart: true);
    }

    public override void _Process(double delta)
    {
        var velocity = Body?.Velocity ?? Vector3.Zero;
        var ground = new Vector2(velocity.X, velocity.Z);
        var groundSpeed = ground.Length();

        // Keep the last walking direction when the character stops.
        if (groundSpeed > WalkSpeedThreshold)
        {
            _facing = ground;
        }

        UpdateDirection();
        Show(_animator.Update(groundSpeed), restart: false);
    }

    /// <summary>Turns the character to face <paramref name="worldDirection"/> (Y is ignored), for example towards an attack target.</summary>
    public void Face(Vector3 worldDirection)
    {
        var ground = new Vector2(worldDirection.X, worldDirection.Z);
        if (ground.LengthSquared() > 1e-8f)
        {
            _facing = ground;
        }
    }

    /// <summary>Plays the attack animation once, from its first frame, then goes back to idle or walk.</summary>
    public void PlayAttack()
    {
        _animator.Attack();
        Show(CharacterAnimation.Attack, restart: true);
    }

    private void UpdateDirection()
    {
        var camera = GetViewport()?.GetCamera3D();
        if (camera is null)
        {
            return;
        }

        // The camera's look direction on the ground. Straight down, use its up vector (screen up).
        var basis = camera.GlobalBasis;
        var forward = new Vector2(-basis.Z.X, -basis.Z.Z);
        if (forward.LengthSquared() < 1e-6f)
        {
            forward = new Vector2(basis.Y.X, basis.Y.Z);
        }

        Direction = FacingDirections.FromVectors(ToCore(_facing), ToCore(forward));
    }

    private void Show(CharacterAnimation animation, bool restart)
    {
        var frames = SpriteFrames;
        if (frames is null)
        {
            return;
        }

        var baseName = CharacterAnimator.AnimationName(animation);
        var (name, flip) = FacingDirections.ResolveAnimation(baseName, Direction, n => frames.HasAnimation(n));
        if (!frames.HasAnimation(name))
        {
            return;
        }

        FlipH = flip;
        var sameAnimation = _shown == animation;
        _shown = animation;

        if (restart)
        {
            Stop();
            Play(name);
            return;
        }

        if (Animation != name)
        {
            // Turning mid-animation: switch to the new direction but keep the current frame, so a
            // walk cycle or an attack doesn't jump back to its start.
            var frame = Frame;
            var progress = FrameProgress;
            Play(name);
            if (sameAnimation)
            {
                var last = Math.Max(0, frames.GetFrameCount(name) - 1);
                SetFrameAndProgress(Math.Min(frame, last), progress);
            }
        }
        else if (!IsPlaying())
        {
            Play(name);
        }
    }

    private void OnAnimationFinished()
    {
        if (_shown == CharacterAnimation.Attack)
        {
            _animator.AttackFinished();
        }
    }

    private static NVector2 ToCore(Vector2 v) => new(v.X, v.Y);

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
