using System;
using System.Collections.Generic;
using Godot;

namespace ConfectioneryTale.scripts;

//this script solely handles visual drop
public partial class ItemDrop : RigidBody2D {
    [Export] public Texture2D tierOneExtractTexture;
    [Export] public Texture2D tierTwoExtractTexture;
    [Export] public Texture2D tierThreeExtractTexture;
    [Export] public Texture2D tierFourExtractTexture;
    [Export] public Texture2D tierFiveExtractTexture;
    [Export] public Texture2D tierSixExtractTexture;

    private Variables vars;
    private Sprite2D sprite;

    private BaseExtract thisExtractData; //the actual BaseExtract resource assigned from Main.cs
    private BaseMaterial thisMaterial;
    private bool isExtract;
    
    //tween
    private Tween popTween;
    private Tween fallTween;
    private float popOutHeight = 0;
    private float fallDuration = 0.1f;
    private float fallDistance = 50f;
    private Tween.EaseType popOutEase = Tween.EaseType.Out;
    private Tween.EaseType fallEase = Tween.EaseType.In;

    public override void _Ready() {
        vars = GetNode<Variables>("/root/Variables");
        sprite = GetNode<Sprite2D>("Sprite2D");

        if (sprite == null) {
            GD.PushError($"ItemDrop._Ready: Sprite2D child not found on {Name}!");
            //consider QueueFree() here if the item can't function without its sprite
        }

        // CallDeferred(nameof(SetExtractTexture)); //NOW CALLED ELSEWHERE
    }
    
    public void SetPos(Vector2 pos) { GlobalPosition = pos; }
    
    //extract drops
    public void SetThisExtract(BaseExtract itemData) {
        if (itemData == null) {
            GD.PushError($"ItemDrop.SetThisExtract called with null itemData on {Name}!");
            QueueFree(); // Free problematic node
            return;
        }
        thisExtractData = itemData; //assign the BaseExtract data
        isExtract = true;
    }
    
    private void SetExtractTexture() { //deferred call from main
        if (thisExtractData == null || sprite == null) {
            GD.PushError($"ItemDrop.DeferredVisualSetup: Missing _thisItemData ({thisExtractData==null}) or sprite ({sprite==null}) on {Name}. Cannot set texture/tween. Item might be invisible.");
            //consider QueueFree() here if it's in an unrecoverable state.
            return;
        }
        
        if (thisExtractData.DisplayTexture != null) { //uses _thisItemData.DisplayTexture
            sprite.Texture = thisExtractData.DisplayTexture;
            // SetCollisionBox(sprite);
        } else {
            GD.PushWarning($"ItemDrop ID {thisExtractData.Id} has no DisplayTexture. Using default or showing nothing.");
        }
        
        // SetCollisionBox(sprite); //if collision depends on texture size
        PopOutTween();
    }

    public bool GetIsExtract() { return isExtract; } //bad way to do this
    
    public BaseExtract GetExtractData() { return thisExtractData; }
    
    //materials
    private void SetMaterialTexture(Texture2D texture) {
        sprite.Texture = texture;
        PopOutTween();
    }
    
    public void SetThisMaterial(BaseMaterial material) { thisMaterial = material; }
    
    public BaseMaterial GetThisMaterial() { return thisMaterial; }

    //tween    
    private void PopOutTween() {
        popOutHeight = GD.RandRange(0, 50);
        fallDistance = GD.RandRange(100, 250);
        fallDuration = (float)GD.RandRange(.1f, .2f);

        GD.Print(fallDuration);
        
        LinearVelocity = Vector2.Zero;
        AngularVelocity = 0f;

        popTween = CreateTween();
        popTween.TweenProperty(this, "global_position:y", GlobalPosition.Y - popOutHeight, fallDuration / 2)
        .SetEase(popOutEase);
        popTween.Play();

        fallTween = CreateTween();
        fallTween
        .TweenProperty(this, "global_position:y", GlobalPosition.Y + fallDistance, fallDuration)
        .SetEase(fallEase);

        fallTween.Finished += () => {
        LinearVelocity = Vector2.Zero;
        GravityScale = 0;
        };
        fallTween.Play();
    }

    // public int GetModTier() { return thisExtractData?.ModTier ?? 0; } //delete?
    
    public bool GetTweenRunning() { return (popTween != null && popTween.IsRunning()) || (fallTween != null && fallTween.IsRunning()); }
}