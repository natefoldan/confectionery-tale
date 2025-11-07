using Godot;

namespace ConfectioneryTale.scripts;

public partial class CurrencySucrose : RigidBody2D {
    private double value;
    private float popOutHeight = 0;
    private float fallDuration = 0.1f;
    private float fallDistance = 50f;
    private Tween.EaseType popOutEase = Tween.EaseType.Out;
    private Tween.EaseType fallEase = Tween.EaseType.In;
    private Sprite2D sprite;
    private Texture2D _textureToApply = null;
    
    public override void _Ready() {
        sprite = GetNode<Sprite2D>("Sprite2D");
        if (_textureToApply != null) { sprite.Texture = _textureToApply; }
        
        PopOutTween();
    }

    public void SetValue(double val) { value = val; }
    public double GetValue() { return value; }
    
    public void SetTexture(Texture2D texture) {
        // sprite = GetNode<Sprite2D>("Sprite2D");
        // if (sprite != null) { sprite.Texture = texture; }
        _textureToApply = texture;
    }
    
    private void PopOutTween() {
        popOutHeight = GD.RandRange(0, 50);
        fallDistance = GD.RandRange(50, 200);
        fallDuration = (float)GD.RandRange(.1f, .2f);
        
        //Disable physics *before* tweening ---
        // A Rigidbody's physics will fight a tween.
        GravityScale = 0; 
        LinearVelocity = Vector2.Zero;
        AngularVelocity = 0f;

        Tween popTween = CreateTween();
        popTween.TweenProperty(this, "global_position:y", GlobalPosition.Y - popOutHeight, fallDuration / 2)
            .SetEase(popOutEase);
        
        //chain the tweens
        popTween.Finished += OnPopTweenFinished;
        popTween.Play();
    }

    //called *after* the popTween is finished.
    private void OnPopTweenFinished() {
        Tween fallTween = CreateTween();
        fallTween
            .TweenProperty(this, "global_position:y", GlobalPosition.Y + fallDistance, fallDuration)
            .SetEase(fallEase);
        
        // This 'Finished' signal will run after the fall is complete.
        fallTween.Finished += () => {
            // "Freeze" the coin on the ground.
            LinearVelocity = Vector2.Zero;
            GravityScale = 0;
        };

        fallTween.Play();
    }
    
    // private void PopOutTween() { //original
    //     popOutHeight = GD.RandRange(0, 50);
    //     fallDistance = GD.RandRange(50, 200);
    //     fallDuration = (float)GD.RandRange(.1f, .2f);
    //     // Disable physics for the tween
    //     // Mode = ModeEnum.Kinematic;
    //     LinearVelocity = Vector2.Zero;
    //     AngularVelocity = 0f;
    //
    //     //start the pop-out tween
    //     Tween popTween = CreateTween();
    //     popTween.TweenProperty(this, "global_position:y", GlobalPosition.Y - popOutHeight, fallDuration / 2)
    //         .SetEase(popOutEase);
    //     popTween.Play();
    //
    //     //start the fall tween after the pop-out
    //     Tween fallTween = CreateTween();
    //     fallTween
    //         .TweenProperty(this, "global_position:y", GlobalPosition.Y + fallDistance, fallDuration)
    //         // .SetDelay(fallDuration / 2)
    //         .SetEase(fallEase);
    //     // .SetLoops(); // Optional: Add a slight bounce or loop
    //
    //     fallTween.Finished += () => {
    //         // Re-enable physics if you want it to interact later
    //         // Mode = ModeEnum.Rigid;
    //         LinearVelocity = Vector2.Zero;
    //         GravityScale = 0;
    //     };
    //
    //     fallTween.Play();
    // }

    public void SetPos(Vector2 pos) {
         GlobalPosition = pos;
     }
}