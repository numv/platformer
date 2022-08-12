using Godot;
using System;

public class EagleFriendly : KinematicBody2D
{
    
    
private AnimatedSprite AnimatedSprite;
    private Vector2 spriteFlipHOffset = new Vector2(-8, 0);
    
    private Vector2 velocity = Vector2.Zero;
    private Vector2 speed = new Vector2(60, 5);
    private bool goingLeft = false;
    private bool startFly = false;
    
    public override void _Ready()
    {
        AnimatedSprite = GetNode<AnimatedSprite>(nameof(AnimatedSprite));
        AnimatedSprite.FlipH = !goingLeft;
    }


    public void OnBodyEnteredOntop(Node node)
    {
        if (!(node is Player p)) return;
        startFly = true;
    }

    public override void _PhysicsProcess(float delta)
    {
        if (startFly)
        {
            velocity.x = speed.x * (goingLeft ? -1 : 1);
        }

        if (velocity.y > 0)
            velocity.y = -velocity.y;
        
		var movement = velocity.Normalized();
        if (movement.x != 0 && AnimatedSprite.FlipH != movement.x > 0)
        {
            AnimatedSprite.FlipH = movement.x > 0;
            if (AnimatedSprite.FlipH)
                AnimatedSprite.Position = spriteFlipHOffset;
            else
                AnimatedSprite.Position = Vector2.Zero;
                
        }

        velocity = MoveAndSlide(velocity);
    }
}
