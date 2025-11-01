using System.Collections.Generic;
using Godot;

namespace ConfectioneryTale.scripts;

public partial class Bullet : Area2D {
    [Signal] public delegate void BulletHitEventHandler(Area2D body, Vector2 pos);
    // private Variables vars;
    private Main main;
    private Sprite2D sprite;
    private string bulletType;
    private float speed;
    private Vector2 direction;
    // private Vector2 targetLocation; // Not used for direct movement
    private bool bulletReady;
    private int totalEnemiesHit;

    private int travelDistance;
    private int range;
    private int pierce;
    private SceneTreeTimer smasherTimer;
    
    private List<string> _enemiesHit = new List<string>();

    public override void _Ready() {
        // PhysicsProcessMode = ProcessMode.Physics; // Ensure _PhysicsProcess is called
    }

    public override void _PhysicsProcess(double delta) {
        if (bulletReady) {
            Translate(direction * speed * (float)delta);
            travelDistance += 1;
            if (travelDistance > range) { QueueFree(); }
        }
    }

    public void SetBulletTexture(Texture2D texture, Vector2 scale) {
        sprite = GetNode<Sprite2D>("Sprite2D");
        sprite.Texture = texture;
        // sprite.Scale = new Vector2(4, 4);
        sprite.Scale = scale;
    }
    
    public void SetupMouseBullet(Vector2 spawnLoc, Vector2 dir) {
        main = GetNode<Main>("/root/Main");
        GlobalPosition = spawnLoc; //use GlobalPosition for initial placement
        direction = dir;
        // speed = 1000;
        speed = main.GetFinalWeaponSpeed();
        range = main.GetFinalWeaponRange();
        pierce = main.GetPlayerFinalPierce();
        SetCollisionCircle();
        bulletReady = true;
    }

    public void SetupStaticBullet(Vector2 position) {
        main = GetNode<Main>("/root/Main");
        pierce = main.GetPlayerFinalPierce();
        SetCollisionCircle();
        bulletReady = true;
        smasherTimer = GetTree().CreateTimer(.2f);
        smasherTimer.Timeout += OnSmasherTimerTimeout;
        GlobalPosition = position;
    
        // If the bullet is a RigidBody2D, you might want to freeze its physics movement.
        // LinearVelocity = Vector2.Zero;
        // Freeze = true;
    
        // This bullet is now ready to play an animation or do its effect.
    }

    private void OnSmasherTimerTimeout() {
        QueueFree();
    }
    
    private void SetCollisionCircle() {
        if (sprite == null) { return; }

        var collisionShape = new CollisionShape2D();
        var collisionCircle = new CircleShape2D();

        //determine the radius based on the sprite's size
        //for a round bullet, the width and height should be similar. can use either dimension or their average
        float radius = Mathf.Max(sprite.Texture.GetWidth(), sprite.Texture.GetHeight()) / 2f;
        
        //multiply the radius by the sprite's scale
        collisionCircle.Radius = radius * sprite.Scale.X; //assuming uniform X/Y scale
        
        // collisionCircle.Radius = radius; //doesn't work with scaling
        collisionShape.Shape = collisionCircle;
        collisionShape.Position = Vector2.Zero; //center the circle on the Area2D's origin
        AddChild(collisionShape);
    }
    
    //this method is called by the enemy when it is hit
    public bool HasAlreadyHit(string enemyId) {
        return _enemiesHit.Contains(enemyId);
    }

    public void AddEnemyHit(string enemyId) {
        _enemiesHit.Add(enemyId);
        if (_enemiesHit.Count >= pierce) { QueueFree(); }
    }
}