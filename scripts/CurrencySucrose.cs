using Godot;

namespace ConfectioneryTale.scripts;

public partial class CurrencySucrose : RigidBody2D {
    private double value;
    private float popOutHeight = 0;
    private float fallDuration = 0.1f;
    private float fallDistance = 50f;
    private Tween.EaseType popOutEase = Tween.EaseType.Out;
    private Tween.EaseType fallEase = Tween.EaseType.In;

    public override void _Ready() {
        PopOutTween();
    }

    public void SetValue(double val) { value = val; }
    public double GetValue() { return value; }
    
    private void PopOutTween() {
        popOutHeight = GD.RandRange(0, 50);
        fallDistance = GD.RandRange(50, 200);
        fallDuration = (float)GD.RandRange(.1f, .2f);
        // Disable physics for the tween
        // Mode = ModeEnum.Kinematic;
        LinearVelocity = Vector2.Zero;
        AngularVelocity = 0f;

        //start the pop-out tween
        Tween popTween = CreateTween();
        popTween.TweenProperty(this, "global_position:y", GlobalPosition.Y - popOutHeight, fallDuration / 2)
            .SetEase(popOutEase);
        popTween.Play();

        //start the fall tween after the pop-out
        Tween fallTween = CreateTween();
        fallTween
            .TweenProperty(this, "global_position:y", GlobalPosition.Y + fallDistance, fallDuration)
            // .SetDelay(fallDuration / 2)
            .SetEase(fallEase);
        // .SetLoops(); // Optional: Add a slight bounce or loop

        fallTween.Finished += () => {
            // Re-enable physics if you want it to interact later
            // Mode = ModeEnum.Rigid;
            LinearVelocity = Vector2.Zero;
            GravityScale = 0;
        };

        fallTween.Play();
    }

    public void SetPos(Vector2 pos) {
         GlobalPosition = pos;
     }
}