using Godot;
using System;

public class Collectable : Node2D
{
    public enum EType
    {
        cherry,
        gem
    }

    [Export] private EType CollectableType;
    private AnimatedSprite AnimatedSprite;
    
    public override void _Ready()
    {
        AnimatedSprite = GetNode<AnimatedSprite>(nameof(AnimatedSprite));
        AnimatedSprite.Play(CollectableType.ToString("F"));
        
        var counterMax = -1;
        var label = GetParent().GetNode<Label>("%LabelCherry");
        var collectables = GetParent().GetNodeOrNull<Node2D>("%Collectables");
        if (collectables != null)
            counterMax = collectables.GetChildCount();
        label.SetMeta("counterMax", counterMax);
        label.Text = counterMax == -1 ? $"x" : $"0 / {counterMax}";
    }

    public void OnBodyEntered(Node node)
    {
        if (!(node is Player p)) return;

        //p.Position = new Vector2(p.Position.x-50, p.Position.y);
        
        
        var label = GetParent().GetNode<Label>("%LabelCherry");
        p.PlaySoundEffect(Player.ESounds.CoinPickup);
        var counter = (int)label.GetMeta("counter", 0);
        var counterMax = (int?)label.GetMeta("counterMax", default(int?));
        counter++;
        label.SetMeta("counter", counter);

        if (counterMax is null)
        {
            var collectables = GetParent().GetNodeOrNull<Node2D>("%Collectables");
            if (collectables != null)
                counterMax = collectables.GetChildCount();
            else
                counterMax = -1;
            label.SetMeta("counterMax", counterMax);
        }
        
        label.Text = counterMax == -1 ? $"{counter}" : $"{counter} / {counterMax}";
        QueueFree();
    }
}
