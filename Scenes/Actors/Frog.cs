using Godot;
using System;

public class Frog : KinematicBody2D
{
	private AnimatedSprite _sprite;
    private AnimationTree _animationTree;
    private AnimationNodeStateMachinePlayback _playback;
    private bool isIdle = false;
    private float idleTimer = 2;
    private Vector2 velocity = Vector2.Zero;
    private Vector2 _speed = new Vector2(60, 460);
    private float gravity = 1000;
	private Vector2 FloorNormal = Vector2.Up;

    public override void _Ready()
    {
	    _sprite = GetNode<AnimatedSprite>("AnimatedSprite");
		_animationTree = GetNode<AnimationTree>("AnimationPlayer/AnimationTree");
		_animationTree.Active = true;
		_playback = (AnimationNodeStateMachinePlayback)_animationTree.Get("parameters/playback");
    }

    public override void _PhysicsProcess(float delta)
    {
	    bool isJump = false;
	    idleTimer -= delta;
	    if (idleTimer <= 0)
	    {
			var rand = GD.Randf();
			idleTimer = rand * 4+1.8f;
			if (rand < 0.4f)
			{
				isJump = true;
				idleTimer += 2f;
				velocity.x = _speed.x * (_sprite.FlipH ? -1 : 1);
				velocity.y = -_speed.y;
				_sprite.FlipH = !_sprite.FlipH;
			}
			else
			{
				isIdle = !isIdle;
				_playback.Travel(isIdle ? "idle" : "idle2");
			}
	    }

	    if (!IsOnFloor())
	    {
			if(velocity.y < 0)
				_playback.Travel("jump");
			else
				_playback.Travel("fall");
	    }
	    else
	    {
			_playback.Travel(isIdle ? "idle" : "idle2");
			if(!isJump)
				velocity.x = 0;
	    }

	    velocity.y += gravity * delta;
		var snap = !isJump ? Vector2.Down *8 : Vector2.Zero;
		velocity = MoveAndSlideWithSnap(velocity,snap, FloorNormal, true);
		
    }
}
