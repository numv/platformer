using Godot;
using System;

public class Eagle : KinematicBody2D
{
    private AnimatedSprite AnimatedSprite;
    private Vector2 _spriteFlipHOffset = new Vector2(-8, 0);
    
    private Vector2 velocity = Vector2.Zero;
    private Vector2 speed = new Vector2(20, 5);
    private float directionTimer = 0f;
    private float directionTimerPrev = 0f;
    private bool goingLeft = true;
    
    public override void _Ready()
    {
        AnimatedSprite = GetNode<AnimatedSprite>(nameof(AnimatedSprite));
    }


    public void OnBodyEnteredOntop(Node node)
    {
        if (!(node is Player p)) return;
        p.AddMoveForce(new Vector2(0, -460));
        p.GiveDoubleJumpBack();
        QueueFree();
    }

    public void OnBodyEnteredHurtPlayer(Node node)
    {
        if (!(node is Player p)) return;
        p.GotHurt();
    }

    public override void _PhysicsProcess(float delta)
    {
        directionTimer -= delta;
        if (directionTimer <= 0)
        {
            if(directionTimerPrev != 0)
            {
                goingLeft = !goingLeft;
                directionTimer = directionTimerPrev;
                directionTimerPrev = 0;
            }
            else
            {
                directionTimer = GD.Randf()*3+1.4f;
                directionTimerPrev = directionTimer;
            }
        }
        
        velocity.x = speed.x * (goingLeft ? -1 : 1);
        velocity.y = 0;
        
		var movement = velocity.Normalized();
        if (movement.x != 0 && AnimatedSprite.FlipH != movement.x > 0)
        {
            AnimatedSprite.FlipH = movement.x > 0;
            if (AnimatedSprite.FlipH)
                AnimatedSprite.Position = _spriteFlipHOffset;
            else
                AnimatedSprite.Position = Vector2.Zero;
        }

        velocity = MoveAndSlide(velocity);
    }
}
