using Godot;
using System;

public class WoodJumpUp : StaticBody2D
{
	public enum EDirection
	{
		left,
		right
	}

	[Export] private EDirection Direction = EDirection.right;

	public override void _Ready()
	{
		var sprite = GetNode<AnimatedSprite>("Sprite");
		sprite.Play(Direction.ToString("F"));
	}

}
