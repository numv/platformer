using Godot;
using System;
using System.Diagnostics.Eventing.Reader;
using Godot.Collections;

public class Player : KinematicBody2D
{
    public enum ESounds
    {
        CoinPickup,
        DeathTeleport,
        WeaponSwing,
        WeaponSwingAir,
        PunchHeavy
    }

    private Vector2 FloorNormal = Vector2.Up;
    private Vector2 _startPosition = Vector2.Zero;
    private int _rayCastLedgeLength = 19;
    private int _outOfScreenY = int.MaxValue;
    
#region movement
    private float gravity = 1000;
    private Vector2 speedMaxWalk = new Vector2(60, 260);
    private Vector2 speedMaxRun = new Vector2(80, 360);
    private Vector2 speedMaxCrouch = new Vector2(40, 260);
    private Vector2 speedRunningFast = new Vector2(120, 400);
    private const float speedXStart = 10;
    private Vector2 _speed = new Vector2(speedXStart, 360);
    private Vector2 acceleration = new Vector2(80, 1);
    private Vector2 _velocity = Vector2.Zero;
    private Vector2 force = Vector2.Zero;
#endregion
    
#region timers
    private float jumpTimer = 0;
    private const float jumpTimerMax = 0.2f;
    private float startRunningTimer = 0;
    private const float startRunningTimerMax = 1.0f;
    private float chrouchingTimer = 0;
    private const float chrouchinTimerMax = 0.2f;

    private int attackCount = 0;
    private float attackTimer = 0.8f;
    private const float attackTimerMax = 0.8f;
#endregion

    private bool _hasDoubleJumped;
    private bool _isCrouching;
    private bool _isSwordDrawn;
    private bool _isOnLedge;
    private EAnimation? _forcedAnimation;

    private RayCast2D _raycastLedge;
    private RayCast2D _raycastLedgeWall;
    private AnimatedSprite sprite;
    private AnimationTree animation;
    private AnimationNodeStateMachinePlayback playback;
    private readonly AudioStreamPlayer audioJump = new AudioStreamPlayer();
    private readonly AudioStreamPlayer audioEffects = new AudioStreamPlayer();
    private Dictionary<ESounds, AudioStream> soundEffects = new Dictionary<ESounds, AudioStream>();
    

    enum EAnimation
    {
        Idle,
        IdleWeaponDrawn,
        

        // movements
        Jump,
        Walk,
        Run,
        RunFast,
        Crouch,
        CrouchWalk,
        GrabLedge,

        
        SwordSheath,
        SwordSheathWalkin,
        
        AttackAir1,
        AttackRunPunch,
        
        // locked movement
        SwordDraw,
        Attack1,
        Attack2,
        Attack3,
        SmrSlt,
        Fall
    }

    public override void _Ready()
    {
        _startPosition = Position;
        sprite = GetNode<AnimatedSprite>("PlayerSprite");
        animation = GetNode<AnimationTree>("AnimationPlayer/AnimationTree");
        animation.Active = true;
        playback = (AnimationNodeStateMachinePlayback)animation.Get("parameters/playback");

        _raycastLedge = GetNode<RayCast2D>("RayCastLedge");
        _raycastLedgeWall = GetNode<RayCast2D>("RayCastLedgeWall");
        _raycastLedge.Enabled = true;
        _raycastLedgeWall.Enabled = true;
        
        //setup audio
        this.AddChild(audioJump);
        audioJump.Stream = (AudioStream)GD.Load("res://Assets/music/Jump.wav");
        audioJump.VolumeDb = -20;

        this.AddChild(audioEffects);
        audioEffects.Stream = (AudioStream)GD.Load("res://Assets/music/Jump.wav");
        audioEffects.VolumeDb = -10;
        soundEffects.Add(ESounds.CoinPickup, (AudioStream)GD.Load("res://Assets/music/coin.wav"));
        soundEffects.Add(ESounds.DeathTeleport, (AudioStream)GD.Load("res://Assets/music/DeathTeleport.mp3"));
        soundEffects.Add(ESounds.WeaponSwing, (AudioStream)GD.Load("res://AssetsV2/sounds/WHOOSH_ARM_SWING_01.wav"));
        soundEffects.Add(ESounds.WeaponSwingAir, (AudioStream)GD.Load("res://AssetsV2/sounds/WHOOSH_AIRY_FLUTTER_01.wav"));
        soundEffects.Add(ESounds.PunchHeavy, (AudioStream)GD.Load("res://AssetsV2/sounds/PUNCH_DESIGNED_HEAVY_23.wav"));
        
        _outOfScreenY = GetParent().GetNode<Camera2D>("%PlayerCamera").LimitBottom;
    }

    private void TeleportToStart()
    {
        Position = _startPosition;
        PlaySoundEffect(ESounds.DeathTeleport);
    }

    public void AttackAnimationFinished()
    {
        switch (_forcedAnimation)
        {
            case EAnimation.SwordSheathWalkin:
            case EAnimation.SwordSheath:
                GD.Print("Sword is sheath!");
                _isSwordDrawn = false;
                break;
            case EAnimation.SwordDraw:
                GD.Print("Sword is drawn!");
                _isSwordDrawn = true;
                break;
        }
                
        if(_forcedAnimation >= EAnimation.AttackAir1)
            playback.Travel(IsOnFloor() ? "Idle" : "Jump");
        if (_forcedAnimation >= EAnimation.Attack1)
        {
            attackTimer = attackTimerMax;
            playback.Travel("Idle");
        }
        _forcedAnimation = null;
    }

    public override void _PhysicsProcess(float delta)
    {
        if (Position.y > _outOfScreenY) TeleportToStart();
        
        CheckAndUpdateTimers(delta);
        var direction = GetInputDirection();
        var velocity = CalculateMoveVelocity(delta, _velocity, direction);
        
        var forceAnimation = _forcedAnimation;
        var isAttacking = CheckIsAttacking(ref direction, ref velocity, ref forceAnimation);
        if (!isAttacking && !IsOnFloor() && IsOnWall())
        {
            CheckForLedge(direction, ref velocity);
        }
        
        UpdateAnimationAndSound(direction, velocity, forceAnimation);
        var snap = direction.y == 0 ? Vector2.Down * 8 : Vector2.Zero;
        _velocity = MoveAndSlideWithSnap(velocity, snap, FloorNormal, true);
    }

    private void CheckForLedge(Vector2 direction, ref Vector2 velocity)
    {
        if (!_isCrouching && direction.y != -1 && _raycastLedgeWall.IsColliding() && !_raycastLedge.IsColliding())
        {
            
            velocity = Vector2.Zero;
            _hasDoubleJumped = true;
            _isOnLedge = true;
        }
        else
            _isOnLedge = false;

    }

    private bool CheckIsAttacking(
        ref Vector2 direction,
        ref Vector2 velocity,
        ref EAnimation? forceAnimation)
    {
        if (forceAnimation >= EAnimation.SwordDraw)
        {
            // stuck in attack animation
            startRunningTimer = 0;
            direction = Vector2.Zero;
            velocity = Vector2.Zero;
            return true;
        }
        if (forceAnimation >= EAnimation.AttackAir1)
        {
            return false;
        }
        
        if (_isSwordDrawn && (direction.x != 0 || direction.y != 0))
        {
            if(direction.x != 0)
                forceAnimation = EAnimation.SwordSheathWalkin;
            else
            {
                forceAnimation = EAnimation.SwordSheath;
                direction = Vector2.Zero;
                velocity = _velocity;
                velocity = Vector2.Zero;
            }
            
            return true;
        }
        
        
        if (IsOnFloor() && Input.IsActionPressed("attack"))
        {
            GD.Print(Math.Abs(velocity.x), " runspeed:", speedRunningFast.x);
            if (Math.Abs(velocity.x) >= speedRunningFast.x)
            {
                PlaySoundEffect(ESounds.PunchHeavy);
                forceAnimation = EAnimation.AttackRunPunch;
                _speed.x = speedXStart*2;
            }
            else
            {
                _isSwordDrawn = true;
                attackCount++;
                attackTimer = attackTimerMax;
                attackTimer -= GetProcessDeltaTime();
                PlaySoundEffect(ESounds.WeaponSwing);
                if(attackCount == 1)
                    forceAnimation = EAnimation.Attack1;
                else if(attackCount == 2)
                    forceAnimation = EAnimation.Attack2;
                else if (attackCount == 3)
                {
                    forceAnimation = EAnimation.Attack3;
                    attackCount = 0;
                }
                direction = Vector2.Zero;
                velocity = _velocity;
            }
            return true;
        }
        if (Input.IsActionJustPressed("attack") && forceAnimation != EAnimation.AttackAir1)
        {
            PlaySoundEffect(ESounds.WeaponSwingAir);
            forceAnimation = EAnimation.AttackAir1;
            _hasDoubleJumped = true;
            //_isSwordDrawn = true;
            return true;
        }
        
        
        if (IsOnFloor() && Input.IsActionJustPressed("drawWeapon") && velocity.x == 0)
        {
            if (!_isSwordDrawn)
            {
                forceAnimation = EAnimation.SwordDraw;
                GD.Print("force draw");
            }
            else
            {
                forceAnimation = EAnimation.SwordSheath;
            }
            return true;
        }
        return false;
    }

    private void CheckAndUpdateTimers(float delta)
    {
        // JumpTimer
        if (IsOnFloor() && jumpTimer < jumpTimerMax)
        {
            jumpTimer = jumpTimerMax;
            _hasDoubleJumped = true;
        }
        else jumpTimer -= delta;

        // ChrouchTimer
        if (chrouchingTimer > 0)
        {
            chrouchingTimer += delta;
            if (chrouchingTimer > chrouchinTimerMax)
                SetCollisionMaskBit(2, true);
        }

        if (attackTimer < attackTimerMax)
        {
            attackTimer -= delta;
            if (attackTimer <= 0)
            {
                attackCount = 0;
                attackTimer = attackTimerMax;
            }
        }
    }

    private void UpdateAnimationAndSound(Vector2 direction, Vector2 velocity, EAnimation? targetAnimation = null)
    {
        if (targetAnimation is EAnimation a)
        {
            _forcedAnimation = a;
            playback.Travel(a.ToString("F"));
            return;
        }
        if (_forcedAnimation.HasValue)
            return;
        
        var movement = velocity.Normalized();
        if (movement.x != 0 && sprite.FlipH != movement.x < 0)
        {
            sprite.FlipH = movement.x < 0;
            if (sprite.FlipH)
            {
                var castTo = Vector2.Left * _rayCastLedgeLength;
                _raycastLedge.CastTo = castTo;
                _raycastLedgeWall.CastTo = castTo;
            }
            else
            {
                var castTo = Vector2.Right * _rayCastLedgeLength;
                _raycastLedge.CastTo = castTo;
                _raycastLedgeWall.CastTo = castTo;
            }
        }

        // check state
        if (_isOnLedge)
        {
            playback.Travel(EAnimation.GrabLedge.ToString("F"));
            return;
        }
        if (direction.y == -1)
        {
           // player is in the air
           //animation.Set("parameters/Jump/blend_position", movement.y);
           if (!_hasDoubleJumped)
           {
               playback.Travel(EAnimation.SmrSlt.ToString("F"));
               audioJump.Play();
           }
           else if (_hasDoubleJumped)
           {
               playback.Travel(EAnimation.Jump.ToString("F"));
               audioJump.Play();
           }
           return;
        }
        
        if (!IsOnFloor())
        {
            if (_hasDoubleJumped)
            {
               playback.Travel(EAnimation.Fall.ToString("F"));
            }
            else
            {
               playback.Travel(EAnimation.SmrSlt.ToString("F"));
            }
                
           return;
        }
        
        if (velocity.x != 0)
        {
            var speed = Math.Abs(velocity.x);
            if (_isCrouching)
                playback.Travel(EAnimation.CrouchWalk.ToString("F"));
            else if (speed <= speedMaxWalk.x)
                playback.Travel(EAnimation.Walk.ToString("F"));
            else if (speed <= speedMaxRun.x)
                playback.Travel(EAnimation.Run.ToString("F"));
            else if (speed > speedMaxRun.x)
                playback.Travel(EAnimation.RunFast.ToString("F"));
            else
                playback.Travel(EAnimation.Run.ToString("F"));
            return;
        }
        
        if (_isCrouching)
        {
            playback.Travel(EAnimation.Crouch.ToString("F"));
            return;
        }
        
        // player is not doing anything
        if (_isSwordDrawn)
        {
            playback.Travel(EAnimation.IdleWeaponDrawn.ToString("F"));
            return;
        }
        playback.Travel(EAnimation.Idle.ToString("F"));
    }

    private Vector2 CalculateMoveVelocity(
        float delta,
        Vector2 linear_velocity, 
        Vector2 direction)
    {
        
        var isJumpInterrupted = (Input.IsActionJustReleased("jump") && _velocity.y < 0);
        var new_velocity = linear_velocity;
        if (direction.x == 0 && _speed.x != 0)
        {
            _speed.x = speedXStart;
        }
        else if (direction.x != 0)
        {
            _speed.x += acceleration.x * delta;
        }

        if (_speed.y > speedMaxRun.y)
        {
            _speed.y = speedMaxRun.y;
        }

        if (_speed.x > speedMaxRun.x)
        {
            _speed.x = speedMaxRun.x;
            //startRunningTimer += delta;
        }
        else
        {
            startRunningTimer = 0;
        }


        if (_isCrouching)
        {
            if (_speed.x > speedMaxCrouch.x)
                _speed.x = speedMaxCrouch.x;
        }
        else if (startRunningTimer >= startRunningTimerMax)
        {
            _speed = speedRunningFast;
            startRunningTimer = startRunningTimerMax;
        }


        // walkin
        new_velocity.x = _speed.x * direction.x; 
        
        // gravity
        new_velocity.y += gravity * delta; 
        
        // check jumping
        if (direction.y < 0)
        {
            // add upward speed
            new_velocity.y = -_speed.y;
        }

        if (isJumpInterrupted)
        {
            // half the upwardspeed to jump a little
            new_velocity.y /= 2;
        }

        // apply other external forces
        if (force == Vector2.Zero) 
            return new_velocity;
        
        new_velocity += force;
        force = Vector2.Zero;
        return new_velocity;
    }

    private Vector2 GetInputDirection()
    {
        var direction_x = Input.GetActionStrength("move_right")
                          - Input.GetActionStrength("move_left");

        _isCrouching = Input.IsActionPressed("move_down");
        if (_isCrouching)
        {
            SetCollisionMaskBit(2, false);
            chrouchingTimer = 0.0001f;
        }

        if (Input.IsActionPressed("run"))
        {
            startRunningTimer = startRunningTimerMax;
        }

        float direction_y = 0;
        var didJustJump = Input.IsActionJustPressed("jump");
        switch (didJustJump)
        {
            case true when jumpTimer > 0:
                jumpTimer = 0;
                direction_y = -1;
                break;
            case true when _hasDoubleJumped:
                jumpTimer = 0;
                direction_y = -1;
                _hasDoubleJumped = false;
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
        _hasDoubleJumped = true;
    }
    
    public void PlaySoundEffect(ESounds sound)
    {
        if (!soundEffects.TryGetValue(sound, out var soundStream)) return;
        audioEffects.Stream = soundStream;
        audioEffects.Play();
    }
}