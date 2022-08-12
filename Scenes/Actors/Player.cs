using Godot;
using System;
using System.Diagnostics.Eventing.Reader;
using Godot.Collections;

public class Player : KinematicBody2D
{
	public enum ESounds
	{
		CoinPickup,
		DeathTeleport
	}

	private float gravity = 1000;
	private Vector2 _startPosition = Vector2.Zero;
	private Vector2 speedMax = new Vector2(160, 360);
	private const float speedXStart = 20;
	private Vector2 speedRunning= new Vector2(200, 400);
	private Vector2 speed= new Vector2(speedXStart, 360);
	private Vector2 acceleration = new Vector2(200, 1);
	private Vector2 FloorNormal = Vector2.Up;
	private Vector2 velocity = Vector2.Zero;
	private Vector2 force = Vector2.Zero;
	
	private AnimatedSprite sprite;
	private AnimationTree animation;
	private AnimationNodeStateMachinePlayback playback;
	private readonly AudioStreamPlayer audioJump = new AudioStreamPlayer();
	private readonly AudioStreamPlayer audioEffects = new AudioStreamPlayer();
	private Dictionary<ESounds, AudioStream> soundEffects = new Dictionary<ESounds, AudioStream>();
	private float jumpTimer = 0;
	private const float jumpTimerMax = 0.2f;
	private float startRunningTimer = 0;
	private const float startRunningTimerMax = 1.8f;
	private float chrouchingTimer = 0;
	private const float chrouchinTimerMax = 0.2f;
	private bool hasDoubleJumped = true;
	private int _outOfScreenY = 300;
	private bool isCrouching = false;

	public override void _Ready()
	{
		_startPosition = Position;
		sprite = GetNode<AnimatedSprite>("PlayerSprite");
		animation = GetNode<AnimationTree>("AnimationPlayer/AnimationTree");
		animation.Active = true;
		playback = (AnimationNodeStateMachinePlayback)animation.Get("parameters/playback");
		
		//setup audio
		this.AddChild(audioJump);
		audioJump.Stream = (AudioStream)GD.Load("res://Assets/music/Jump.wav");
		audioJump.VolumeDb = -20;
		
		this.AddChild(audioEffects);
		audioEffects.Stream = (AudioStream)GD.Load("res://Assets/music/Jump.wav");
		audioEffects.VolumeDb = -10;
		soundEffects.Add(ESounds.CoinPickup, (AudioStream)GD.Load("res://Assets/music/coin.wav"));
		soundEffects.Add(ESounds.DeathTeleport, (AudioStream)GD.Load("res://Assets/music/DeathTeleport.mp3"));

		_outOfScreenY = GetParent().GetNode<Camera2D>("%PlayerCamera").LimitBottom;
	}

	private void TeleportToStart()
	{
		Position = _startPosition;
		PlaySound(ESounds.DeathTeleport);
	}

	public override void _PhysicsProcess(float delta)
	{
		if (Position.y > _outOfScreenY) TeleportToStart();

		var isJumpInterrupted = (Input.IsActionJustReleased("jump") && velocity.y < 0);
		if (IsOnFloor() && jumpTimer < jumpTimerMax)
		{
			jumpTimer = jumpTimerMax;
			hasDoubleJumped = true;
		}
		else jumpTimer -= delta;

		var direction = GetInputDirection();

		if(chrouchingTimer > 0)
		{
			chrouchingTimer += delta;
			if (chrouchingTimer > chrouchinTimerMax)
				SetCollisionMaskBit(2, true);
		}
		
		
		velocity = CalculateMoveVelocity(delta, velocity, direction, isJumpInterrupted);
		SetAnimation();
		var snap = direction.y == 0 ? Vector2.Down *8 : Vector2.Zero;
		velocity = MoveAndSlideWithSnap(velocity, snap, FloorNormal, true);
	}

	public void PlaySound(ESounds sound)
	{
		if (!soundEffects.TryGetValue(sound, out var soundStream)) return;
		audioEffects.Stream = soundStream;
		audioEffects.Play();
	}

	private void SetAnimation()
	{
		var movement = velocity.Normalized();
		
		// turn direction
		if (movement.x != 0) sprite.FlipH = movement.x < 0;
		
		// check state
		if (!IsOnFloor())
		{
			// player is in the air
			animation.Set("parameters/jump/blend_position", movement.y);
			playback.Travel("jump");
		}
		else if (velocity.x != 0)
		{
			// player is walking
			playback.Travel("walk");
		}
		else if (isCrouching)
		{
			playback.Travel("crouch");
		}
		else
		{
			// player is not doing anything
			playback.Travel("idle");
		}
	}

	private Vector2 CalculateMoveVelocity(float delta, Vector2 linear_velocity, Vector2 direction, bool isJumpInterrupted)
	{
		var new_velocity = linear_velocity;
		if (direction.x == 0 && speed.x != 0)
		{
			speed.x = speedXStart;
		}
		else if(direction.x != 0)
		{
			speed.x += acceleration.x * delta;
		}

		if (speed.y > speedMax.y)
		{
			speed.y = speedMax.y;
		}

		if (speed.x > speedMax.x)
		{
			speed.x = speedMax.x;
			startRunningTimer += delta;
		}
		else
		{
			startRunningTimer = 0;
		}

		if (startRunningTimer >= startRunningTimerMax)
		{
			speed = speedRunning;
			startRunningTimer = startRunningTimerMax;
		}
		
		new_velocity.x = speed.x * direction.x;
		new_velocity.y += gravity * delta;
		if (direction.y < 0)
		{
			new_velocity.y = -speed.y;
		}
		if (isJumpInterrupted)
		{
			new_velocity.y /= 2;
		}

		if (force != Vector2.Zero)
		{
			new_velocity += force;
			force=Vector2.Zero;
		}
		
		return new_velocity;
	}

	private Vector2 GetInputDirection()
	{
		var direction_x = Input.GetActionStrength("move_right") 
		                  - Input.GetActionStrength("move_left");

		isCrouching = Input.IsActionPressed("move_down");
		if (isCrouching)
		{
			SetCollisionMaskBit(2, false);
			chrouchingTimer = 0.0001f;
		}
		
		float direction_y = 0;
		var didJustJump = Input.IsActionJustPressed("jump");
		
		switch (didJustJump)
		{
			case true when jumpTimer > 0:
				audioJump.Play();
				jumpTimer = 0;
				direction_y = -1;
				break;
			case true when hasDoubleJumped:
				audioJump.Play();
				jumpTimer = 0;
				direction_y = -1;
				hasDoubleJumped = false;
				break;
		}
		return new Vector2(direction_x, direction_y);
	}

	private void OnAreaPlatformDropBodyExited(Node body)
	{
		if (body is WoodJumpUp w)
		{
			SetCollisionMaskBit(2, true);
		}
	}

	public void GotHurt()
	{
		TeleportToStart();
	}

	public void AddMoveForce(Vector2 force)
	{
		this.force = force;
	}

	public void GiveDoubleJumpBack()
	{
		hasDoubleJumped = true;
	}
}
