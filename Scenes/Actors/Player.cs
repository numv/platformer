using Godot;
using System;

public class Player : KinematicBody2D
{

	public float gravity = 1000;
	public Vector2 speed = new Vector2(120, 360);

	public Vector2 FloorNormal = Vector2.Up;

	public Vector2 velocity = Vector2.Zero;


	private AnimatedSprite sprite;

	private float jumpTimer = 0;
	private const float jumpTimerMax = 0.2f;
	private bool hasDoubleJumped = true;

	public override void _Ready()
	{
		sprite = GetNode<AnimatedSprite>("PlayerSprite");
		sprite.Play("idle");
	}

	public override void _PhysicsProcess(float delta)
	{
		var isJumpInterrupted = (Input.IsActionJustReleased("jump") && velocity.y < 0);
		if (IsOnFloor() && jumpTimer < jumpTimerMax)
		{
			jumpTimer = jumpTimerMax;
			hasDoubleJumped = true;
		}
		else
			jumpTimer -= delta;

		Vector2 direction = GetInputDirection();
		velocity = CalculateMoveVelocity(velocity, direction, speed, isJumpInterrupted);

		// turn direction
		if (velocity.x != 0)
			sprite.FlipH = velocity.x < 0;

		// set animation
		if (velocity.y < 0)
		{
			sprite.Play("jumping");
		}
		else if (velocity.x != 0)
		{
			sprite.Play("walk");
		}
		else
		{
			sprite.Play("idle");
		}

		Vector2 snap = direction.y == 0 ? Vector2.Down * 20 : Vector2.Zero;
		velocity = MoveAndSlideWithSnap(velocity, snap, FloorNormal, true);
	}

	private Vector2 CalculateMoveVelocity(Vector2 linear_velocity, Vector2 direction, Vector2 speed, bool isJumpInterrupted)
	{
		{
			Vector2 new_velocity = linear_velocity;
			new_velocity.x = speed.x * direction.x;
			new_velocity.y += gravity * GetPhysicsProcessDeltaTime();
			if (direction.y == -1)
			{
				new_velocity.y = -speed.y;
			}
			if (isJumpInterrupted)
			{
				new_velocity.y = new_velocity.y / 2;
			}
			return new_velocity;
		}
	}

	private Vector2 GetInputDirection()
	{
		float direction_x = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
		float direction_y = 0;
		bool didJustJump = Input.IsActionJustPressed("jump");
		if (didJustJump && jumpTimer > 0)
		{
			jumpTimer = 0;
			direction_y = -1;
		}
		else if (didJustJump && hasDoubleJumped)
		{
			jumpTimer = 0;
			direction_y = -1;
			hasDoubleJumped = false;
		}
		return new Vector2(direction_x, direction_y);
	}


}
