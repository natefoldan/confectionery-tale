using Godot;

namespace ConfectioneryTale.scripts;

public partial class PopupText : Label {

    public void SetTextAndSize(string value, int size) {
        // var theNode = GetNode<Label>("Label"); //-with node2d as root
        // theNode.Set("theme_override_font_sizes/font_size", size); //-with node2d as root
        // theNode.Text = value; //-with node2d as root
        Set("theme_override_font_sizes/font_size", size);
        Text = value;
        // GD.Print(value);
    }

    public void SetColor(Color color) {
        // var theNode = GetNode<Label>("Label"); //don't keep getting the label -with node2d as root
        // theNode.Set("theme_override_colors/font_color", color); -with node2d as root
        Set("theme_override_colors/font_color", color);
    }

    public void RemoveStroke() {
        Set("theme_override_constants/outline_size", 0);
    }
    
    public void SetPosition(Vector2 pos) {
        // PivotOffset = new Vector2(GetRect().Size.X / 2f, GetRect().Size.Y / 2f);
        Position = Vector2.Zero; // Center the label within its parent
        var newX = GetRect().Size.X / 2;
        Position = new Vector2(-50, -200);
        // var textNode = GetNode<Label>("Label");
        // textNode.PivotOffset = new Vector2(textNode.GetRect().Size.X / 2f, 0); // Center horizontally, keep top vertically (adjust Y if needed)
        // Position = pos;
        // GD.Print(textNode.Position.X);
    }

    public void SetWorldPosition(Vector2 pos) {
        Position = pos;
    }
    
    public void DamageTween() {
        var tween = GetTree().CreateTween();
        var randStopY = GD.RandRange(-100, -200);
        tween.SetParallel(true);
        // tween.TweenProperty(this, "position:y", -30, 1.0f).AsRelative(); //original -slow
        tween.TweenProperty(this, "position:y", randStopY, .5f).AsRelative();
        tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 1.5f);
        tween.TweenCallback(Callable.From(OnTweenFinished)).SetDelay(.2f);
    }

    public void FadeTween() {
        var tween = GetTree().CreateTween();
        var randStopY = GD.RandRange(-100, -200);
        tween.SetParallel();
        // tween.TweenProperty(this, "position:y", -30, 1.0f).AsRelative(); //original -slow
        tween.TweenProperty(this, "position:y", randStopY, .5f).AsRelative();
        tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 1.3f);
        // tween.TweenCallback(Callable.From(OnTweenFinished)).SetDelay(.2f);
        tween.TweenCallback(Callable.From(OnTweenFinished)).SetDelay(2f);
    }
    
    private void OnTweenFinished() {
        QueueFree();
    }
}